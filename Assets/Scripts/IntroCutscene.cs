using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroCutscene : MonoBehaviour
{
    [Header("Woods")]
    [SerializeField] Animator woodsHenry;
    [SerializeField] Transform pathStart, pathStop, ufo, ufoHover, beam, camWalk, camLookUp, camBeam;
    [Header("Ship")]
    [SerializeField] Transform shipHenrySpot, alien, gun, muzzle, camShip;
    [SerializeField] LineRenderer zap;
    [SerializeField] Light zapLight;
    [Header("Store")]
    [SerializeField] PlayerController player;
    [SerializeField] Transform boxInside, boxOutside, storeSpawn;
    [SerializeField] Text skipHint;

    Camera cam;
    bool skipping, running;
    Light[] lights;
    float[] lightLevels;
    PlayerAnimator playerAnim;

    public bool Running => running;

    public void Play()
    {
        cam = Camera.main;
        playerAnim = player.GetComponent<PlayerAnimator>();
        lights = player.GetComponentsInChildren<Light>();
        lightLevels = System.Array.ConvertAll(lights, l => l.intensity);
        running = true;
        StartCoroutine(Sequence());
    }

    void Update()
    {
        if (!running || skipping) return;
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
        {
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(Finish(true));
        }
    }

    void CamTo(Transform at)
    {
        cam.transform.SetPositionAndRotation(at.position, at.rotation);
    }

    IEnumerator CamMove(Transform from, Transform to, float seconds, Transform lookAt = null)
    {
        for (float t = 0; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / seconds);
            cam.transform.position = Vector3.Lerp(from.position, to.position, k);
            cam.transform.rotation = lookAt != null
                ? Quaternion.LookRotation(lookAt.position - cam.transform.position)
                : Quaternion.Slerp(from.rotation, to.rotation, k);
            yield return null;
        }
    }

    IEnumerator Fade(float to, float seconds)
    {
        var f = ScreenFader.Instance;
        if (f != null) yield return f.Fade(to, seconds);
    }

    void HenryLook(Animator a, Transform target, float weight)
    {
        var hl = a.GetComponent<HeadLook>();
        if (hl == null) return;
        hl.target = target;
        hl.weight = weight;
    }

    IEnumerator Sequence()
    {
        if (skipHint != null) skipHint.gameObject.SetActive(true);
        ScreenFader.Instance?.Set(1f);
        player.enabled = true;
        player.BeginCutscene();
        player.CameraLocked = true;
        player.Teleport(boxInside.position, boxInside.rotation);
        playerAnim?.Play("Sit", 0f);

        woodsHenry.gameObject.SetActive(true);
        woodsHenry.SetBool("Grounded", true);
        woodsHenry.transform.SetPositionAndRotation(pathStart.position, pathStart.rotation);
        woodsHenry.SetFloat("Speed", 4f);
        ufo.position = ufoHover.position + Vector3.up * 60f + ufoHover.right * 40f;
        beam.gameObject.SetActive(false);
        CamTo(camWalk);
        StartCoroutine(Fade(0f, 1.5f));

        var camOffset = camWalk.position - pathStart.position;
        var dir = (pathStop.position - pathStart.position).normalized;
        float pathLen = Vector3.Distance(pathStart.position, pathStop.position);
        var root = woodsHenry.transform;
        woodsHenry.applyRootMotion = true;
        for (float t = 0; Vector3.Dot(root.position - pathStart.position, dir) < pathLen && t < 15f; t += Time.deltaTime)
        {
            yield return null;
            var flat = root.position;
            flat.y = pathStart.position.y;
            root.position = flat;
            cam.transform.position = root.position + camOffset;
            cam.transform.rotation = Quaternion.LookRotation(root.position + Vector3.up * 1.4f - cam.transform.position);
            ufo.position = Vector3.Lerp(ufo.position, ufoHover.position, Time.deltaTime * 0.6f);
        }
        woodsHenry.SetFloat("Speed", 0f);
        woodsHenry.applyRootMotion = false;

        HenryLook(woodsHenry, ufo, 0f);
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            HenryLook(woodsHenry, ufo, t / 1.2f);
            ufo.position = Vector3.Lerp(ufo.position, ufoHover.position, Time.deltaTime * 2f);
            yield return null;
        }
        yield return CamMove(cam.transform, camLookUp, 1.6f, ufo);
        yield return new WaitForSeconds(0.6f);

        beam.gameObject.SetActive(true);
        beam.localScale = new Vector3(0.05f, 1f, 0.05f);
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            float s = Mathf.Lerp(0.05f, 1f, t / 0.4f);
            beam.localScale = new Vector3(s, 1f, s);
            yield return null;
        }
        woodsHenry.CrossFadeInFixedTime("Flail", 0.2f);
        var steps = woodsHenry.GetComponent<PlayerSounds>();
        if (steps != null) steps.enabled = false;
        HenryLook(woodsHenry, ufo, 0f);
        CamTo(camBeam);
        var start = woodsHenry.transform.position;
        var hatch = ufo.position + Vector3.down * 1.5f;
        float rise = 3.2f;
        for (float t = 0; t < rise; t += Time.deltaTime)
        {
            float k = t / rise;
            woodsHenry.transform.position = Vector3.Lerp(start, hatch, k * k * (3 - 2 * k));
            woodsHenry.transform.Rotate(0, 140f * Time.deltaTime, 0, Space.World);
            woodsHenry.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((k - 0.8f) * 5f));
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation,
                Quaternion.LookRotation(woodsHenry.transform.position - cam.transform.position), Time.deltaTime * 4f);
            yield return null;
        }
        woodsHenry.gameObject.SetActive(false);
        beam.gameObject.SetActive(false);
        var away = ufo.position + ufoHover.right * -150f + Vector3.up * 60f;
        var ufoStart = ufo.position;
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            ufo.position = Vector3.Lerp(ufoStart, away, t * t);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(ufo.position - cam.transform.position), Time.deltaTime * 5f);
            yield return null;
        }
        yield return Fade(1f, 0.5f);

        woodsHenry.gameObject.SetActive(true);
        woodsHenry.SetBool("Grounded", true);
        woodsHenry.transform.localScale = Vector3.one;
        woodsHenry.transform.SetPositionAndRotation(shipHenrySpot.position, shipHenrySpot.rotation);
        woodsHenry.CrossFadeInFixedTime("LookAround", 0f);
        HenryLook(woodsHenry, alien, 1f);
        CamTo(camShip);
        var gunRest = gun.localRotation;
        gun.localRotation = gunRest * Quaternion.Euler(50, 0, 0);
        zap.enabled = false;
        if (zapLight != null) zapLight.enabled = false;
        yield return Fade(0f, 0.6f);
        yield return new WaitForSeconds(0.7f);
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            gun.localRotation = Quaternion.Slerp(gunRest * Quaternion.Euler(50, 0, 0), gunRest, Mathf.SmoothStep(0, 1, t / 1.2f));
            yield return null;
        }
        gun.localRotation = gunRest;
        yield return new WaitForSeconds(0.9f);
        zap.enabled = true;
        if (zapLight != null) zapLight.enabled = true;
        woodsHenry.CrossFadeInFixedTime("Hit", 0.05f);
        var target = woodsHenry.transform.position + Vector3.up * 1.1f;
        for (float t = 0; t < 1.3f; t += Time.deltaTime)
        {
            zap.SetPosition(0, muzzle.position);
            zap.SetPosition(1, target + Random.insideUnitSphere * 0.15f);
            zap.widthMultiplier = 0.2f + Random.value * 0.15f;
            float k = Mathf.Clamp01(t / 1.1f);
            float wobble = 1f + Mathf.Sin(t * 40f) * 0.08f * (1f - k);
            woodsHenry.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.08f, k * k) * wobble;
            if (zapLight != null) zapLight.intensity = 30f + Random.value * 40f;
            yield return null;
        }
        zap.enabled = false;
        if (zapLight != null) zapLight.enabled = false;
        yield return new WaitForSeconds(0.6f);
        yield return Fade(1f, 0.8f);
        woodsHenry.gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);

        yield return Finish(false);
    }

    IEnumerator Finish(bool skipped)
    {
        if (skipped)
        {
            yield return Fade(1f, 0.25f);
            woodsHenry.gameObject.SetActive(false);
            beam.gameObject.SetActive(false);
            zap.enabled = false;
        }
        if (skipHint != null) skipHint.gameObject.SetActive(false);

        player.Teleport(boxInside.position, boxInside.rotation);
        player.BeginCutscene();
        player.CameraLocked = false;
        foreach (var l in lights) l.intensity = 0f;
        float yaw = boxInside.eulerAngles.y;
        player.SetLook(yaw, 12f);
        playerAnim?.Play("Sit", 0f);
        yield return Fade(0f, 1.2f);

        if (!skipped)
        {
            yield return new WaitForSeconds(0.6f);
            playerAnim?.Play("WakeUp", 0.3f);
            yield return new WaitForSeconds(1.2f);
            playerAnim?.Play("Crawl", 0.3f);
            var to = boxOutside.position - player.transform.position;
            to.y = 0;
            float dist = to.magnitude;
            for (float t = 0; t < dist / 3.6f; t += Time.deltaTime)
            {
                player.ScriptedMove(to.normalized * 3.6f);
                yield return null;
            }
            player.ScriptedMove(Vector3.zero);
            playerAnim?.Play("LookAround", 0.4f);
            var look = player.GetComponentInChildren<HeadLook>();
            if (look != null) look.target = null;
            for (float t = 0; t < 4f; t += Time.deltaTime)
            {
                float k = t / 4f;
                player.SetLook(yaw + Mathf.Sin(k * Mathf.PI * 2f) * 55f, Mathf.Lerp(12f, -25f, Mathf.Sin(k * Mathf.PI)));
                if (look != null)
                {
                    look.point = player.transform.position + Quaternion.Euler(0, yaw + Mathf.Sin(k * Mathf.PI * 2f) * 70f, 0) * Vector3.forward * 10f + Vector3.up * 6f;
                    look.weight = Mathf.Sin(k * Mathf.PI);
                }
                yield return null;
            }
            if (look != null) look.weight = 0f;
        }
        else
        {
            player.Teleport(boxOutside.position, boxOutside.rotation);
            player.SetLook(yaw, 15f);
        }

        for (int i = 0; i < lights.Length; i++) StartCoroutine(FadeLight(lights[i], lightLevels[i], 1.2f));
        playerAnim?.Resume();
        player.SetCheckpoint(boxOutside.position, boxOutside.rotation);
        player.EndCutscene();
        running = false;
    }

    static IEnumerator FadeLight(Light l, float to, float seconds)
    {
        for (float t = 0; t < seconds; t += Time.deltaTime)
        {
            l.intensity = Mathf.Lerp(0f, to, t / seconds);
            yield return null;
        }
        l.intensity = to;
    }

    public void Setup(Animator henry, Transform[] woods, Transform[] ship, LineRenderer zapLine, Light flash,
        PlayerController storePlayer, Transform inside, Transform outside, Text hint)
    {
        woodsHenry = henry;
        pathStart = woods[0]; pathStop = woods[1]; ufo = woods[2]; ufoHover = woods[3]; beam = woods[4];
        camWalk = woods[5]; camLookUp = woods[6]; camBeam = woods[7];
        shipHenrySpot = ship[0]; alien = ship[1]; gun = ship[2]; muzzle = ship[3]; camShip = ship[4];
        zap = zapLine;
        zapLight = flash;
        player = storePlayer;
        boxInside = inside;
        boxOutside = outside;
        skipHint = hint;
    }
}
