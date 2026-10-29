using UnityEngine;
using UnityEngine.SceneManagement;

// Small touches that make moving around feel physical: a dust puff and thump on hard landings, a whoosh on the
// dash, a shimmer on respawning. It sets itself up in any scene with a player, so nothing in the scene refers to it.
public class GameFeel : MonoBehaviour
{
    static GameFeel instance;
    static ParticleSystem dust;

    PlayerController player;
    CharacterController body;
    bool wasGrounded = true;
    float airTime, fallSpeed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    static void Attach()
    {
        if (instance != null) return;
        var player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player == null) return;
        instance = new GameObject("Game Feel").AddComponent<GameFeel>();
        instance.player = player;
        instance.body = player.GetComponent<CharacterController>();
        player.Dashed += instance.OnDashed;
        player.Respawned += instance.OnRespawned;
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.Dashed -= OnDashed;
            player.Respawned -= OnRespawned;
        }
        if (instance == this) instance = null;
    }

    void OnDashed()
    {
        Sfx.Play("whoosh", 0.8f);
        Puff(player.transform.position + Vector3.down * 0.9f, 6);
    }

    void OnRespawned()
    {
        Sfx.Play("respawn", 0.6f);
        Puff(player.transform.position + Vector3.down * 0.5f, 10, new Color(0.75f, 0.95f, 1f, 0.8f));
        wasGrounded = true;
        airTime = fallSpeed = 0f;
    }

    void Update()
    {
        if (player == null || body == null) return;
        if (player.Attached || player.InCutscene || !body.enabled)
        {
            wasGrounded = true;
            airTime = fallSpeed = 0f;
            return;
        }
        bool grounded = player.IsGrounded;
        if (!grounded)
        {
            airTime += Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, -body.velocity.y);
        }
        else if (!wasGrounded)
        {
            var feet = player.transform.position + Vector3.down * 0.95f;
            if (fallSpeed > 13f)
            {
                float hard = Mathf.InverseLerp(13f, 30f, fallSpeed);
                Sfx.Play("land", Mathf.Lerp(0.5f, 1f, hard), Mathf.Lerp(1.1f, 0.85f, hard));
                Puff(feet, Mathf.RoundToInt(Mathf.Lerp(8, 16, hard)));
                PlayerController.Shake(Mathf.Lerp(0.06f, 0.2f, hard));
            }
            else if (airTime > 0.3f) Puff(feet, 4);
            airTime = fallSpeed = 0f;
        }
        wasGrounded = grounded;
    }

    // a little ring of dust kicked up at a point
    public static void Puff(Vector3 at, int count) => Puff(at, count, new Color(0.86f, 0.83f, 0.76f, 0.7f));

    public static void Puff(Vector3 at, int count, Color color)
    {
        if (dust == null && !MakeDust()) return;
        var p = new ParticleSystem.EmitParams { startColor = color };
        for (int i = 0; i < count; i++)
        {
            float a = Random.value * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            p.position = at + dir * 0.3f + Vector3.up * 0.1f;
            p.velocity = dir * Random.Range(1.5f, 3.5f) + Vector3.up * Random.Range(0.4f, 1.4f);
            p.startSize = Random.Range(0.35f, 0.75f);
            p.startLifetime = Random.Range(0.45f, 0.8f);
            dust.Emit(p, 1);
        }
    }

    // sparks for zaps and the Roomba going down: small, bright and fast
    public static void Sparks(Vector3 at, int count, Color color)
    {
        if (dust == null && !MakeDust()) return;
        var p = new ParticleSystem.EmitParams { startColor = color };
        for (int i = 0; i < count; i++)
        {
            p.position = at;
            p.velocity = Random.onUnitSphere * Random.Range(3f, 8f) + Vector3.up * 3f;
            p.startSize = Random.Range(0.2f, 0.45f);
            p.startLifetime = Random.Range(0.3f, 0.6f);
            dust.Emit(p, 1);
        }
    }

    static bool MakeDust()
    {
        var mat = Resources.Load<Material>("Fx/Dust");
        if (mat == null) return false;
        var go = new GameObject("Dust");
        DontDestroyOnLoad(go);
        dust = go.AddComponent<ParticleSystem>();
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = dust.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.25f;
        main.maxParticles = 300;
        var emission = dust.emission;
        emission.enabled = false;
        var shape = dust.shape;
        shape.enabled = false;
        var drag = dust.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 3f;
        var size = dust.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));
        var fade = dust.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        dust.Play();
        return true;
    }
}
