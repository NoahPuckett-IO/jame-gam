using System.Collections;
using UnityEngine;

public class RoombaBoss : MonoBehaviour
{
    enum Phase { Waiting, Scan, Rampage, Hunt, Dead }

    [SerializeField] Transform eye, fan;
    [SerializeField] Renderer fanRenderer, buttonRenderer, ledRenderer;
    [SerializeField] Light statusLight;
    [SerializeField] Transform[] brushes;
    [SerializeField] float radius = 3.1f, top = 1.55f;
    [SerializeField] Vector2 xRange, zRange;
    [SerializeField] float startZ, westShelfX, eastShelfX;
    [SerializeField] GameObject gate;
    [SerializeField] Collider gateSeal;
    [SerializeField] string[] glyphs;

    [SerializeField] float viewRange = 46f, viewHalfAngle = 26f;
    [SerializeField] float scanSeconds = 22f, rampageSeconds = 20f;

    static readonly Color ScanColor = new Color(1f, 0.85f, 0.2f, 0.45f);
    static readonly Color AlertColor = new Color(1f, 0.15f, 0.1f, 0.55f);
    static readonly Color CalmColor = new Color(0.3f, 0.75f, 1f, 0.42f);

    Phase phase = Phase.Waiting;
    PlayerController player;
    FallingItemsDirector items;
    TutorialPopup popup;
    Mesh fanMesh;
    Vector3[] fanVerts;
    float phaseTime, seenTime, chaseLeft, pauseLeft, spin = 35f, spinDir = 1f, nextFlip;
    Vector3 target;
    bool rampWest, hasTarget;
    Vector3 homePos;
    Bounds gateArea;
    Quaternion homeRot;
    float brushSpeed;
    const int Rays = 40;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        items = FindAnyObjectByType<FallingItemsDirector>();
        popup = FindAnyObjectByType<TutorialPopup>();
        if (player != null) player.Respawned += OnPlayerRespawned;

        fanMesh = new Mesh { name = "Vision" };
        fanMesh.MarkDynamic();
        fanVerts = new Vector3[Rays + 1];
        var tris = new int[(Rays - 1) * 3];
        for (int i = 0; i < Rays - 1; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        fanMesh.vertices = fanVerts;
        fanMesh.triangles = tris;
        fan.GetComponent<MeshFilter>().sharedMesh = fanMesh;
        fan.gameObject.SetActive(false);
        SetButton(Color.black, 0f);
        if (statusLight != null) statusLight.enabled = false;
        if (ledRenderer != null) ledRenderer.material.SetColor("_EmissionColor", Color.black);
        homePos = transform.position;
        homeRot = transform.rotation;
        if (gateSeal != null) gateArea = gateSeal.bounds;
    }

    void OnDestroy()
    {
        if (player != null) player.Respawned -= OnPlayerRespawned;
    }

    void OnPlayerRespawned()
    {
        if (phase != Phase.Scan && phase != Phase.Rampage && phase != Phase.Hunt) return;
        transform.SetPositionAndRotation(homePos, homeRot);
        phase = Phase.Scan;
        phaseTime = 0f;
        seenTime = 0f;
        chaseLeft = 0f;
        pauseLeft = 0f;
        hasTarget = false;
        SetButton(new Color(1f, 0.2f, 0.1f), 2f);
    }

    void Update()
    {
        if (player == null) return;
        foreach (var b in brushes) b.Rotate(0f, brushSpeed * Time.deltaTime, 0f, Space.Self);

        switch (phase)
        {
            case Phase.Waiting:
                var p = player.transform.position;
                if (p.z > startZ && p.x > xRange.x - 6f && p.x < xRange.y + 6f && p.y < 20f) StartCoroutine(Wake());
                return;
            case Phase.Dead:
                return;
        }

        phaseTime += Time.deltaTime;
        if (Debug.isDebugBuild && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
            phaseTime = 999f;
        if (phase == Phase.Scan) Scan();
        else if (phase == Phase.Rampage) Rampage();
        else if (phase == Phase.Hunt) Hunt();

        UpdateFan();
        CheckTouch();
    }

    IEnumerator Wake()
    {
        phase = Phase.Dead;
        player.BeginCutscene();
        player.CameraLocked = true;
        var cam = Camera.main.transform;
        var fromPos = cam.position;
        var fromRot = cam.rotation;
        var focus = transform.position + Vector3.up * 0.9f;
        var shot = transform.position + transform.forward * 9f + transform.right * 3f + Vector3.up * 2.4f;
        var shotRot = Quaternion.LookRotation(focus - shot);
        for (float t = 0; t < 1.3f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.3f);
            cam.SetPositionAndRotation(Vector3.Lerp(fromPos, shot, k), Quaternion.Slerp(fromRot, shotRot, k));
            yield return null;
        }
        yield return new WaitForSeconds(0.4f);

        var red = new Color(1f, 0.2f, 0.1f);
        if (statusLight != null) statusLight.enabled = true;
        for (float t = 0; t < 1.8f; t += Time.deltaTime)
        {
            float k = t / 1.8f;
            bool blink = k < 0.5f && Mathf.Repeat(t, 0.24f) < 0.12f;
            float on = blink ? 0f : k;
            SetButton(red, 2f * on);
            if (ledRenderer != null) ledRenderer.material.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.2f) * 4f * on);
            if (statusLight != null) statusLight.intensity = 18f * on;
            brushSpeed = 720f * k * k;
            transform.Rotate(0f, 300f * k * k * Time.deltaTime, 0f);
            transform.position = homePos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 30f)) * 0.04f * (1f - k);
            yield return null;
        }
        transform.position = homePos;
        PlayerController.Shake(0.4f);
        fan.gameObject.SetActive(true);
        SetFan(ScanColor);
        UpdateFan();
        yield return new WaitForSeconds(0.9f);

        for (float t = 0; t < 0.9f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.9f);
            cam.SetPositionAndRotation(Vector3.Lerp(shot, fromPos, k), Quaternion.Slerp(shotRot, fromRot, k));
            yield return null;
        }
        player.CameraLocked = false;
        player.EndCutscene();
        homeRot = transform.rotation;
        Show(0, "VAC-3000", "Someone left the vacuum on! Dodge its attacks and turn it off to escape.");
        StartCoroutine(HideCard(6f));
        phase = Phase.Scan;
        phaseTime = 0f;
    }

    void Scan()
    {
        spin = Mathf.Lerp(32f, 58f, phaseTime / scanSeconds);
        if (Time.time > nextFlip)
        {
            spinDir = Random.value < 0.6f ? -spinDir : spinDir;
            nextFlip = Time.time + Random.Range(1.5f, 4.5f);
        }
        transform.Rotate(0f, spin * spinDir * Time.deltaTime, 0f);
        bool seen = CanSee();
        SetFan(seen ? AlertColor : ScanColor);
        seenTime = seen ? seenTime + Time.deltaTime : 0f;
        if (seenTime > 0.35f)
        {
            seenTime = 0f;
            Zap();
        }
        if (phaseTime > scanSeconds)
        {
            phase = Phase.Rampage;
            phaseTime = 0f;
            hasTarget = false;
        }
    }

    void Rampage()
    {
        SetFan(AlertColor);
        if (pauseLeft > 0f)
        {
            pauseLeft -= Time.deltaTime;
            return;
        }
        if (!hasTarget)
        {
            rampWest = !rampWest;
            float wallX = rampWest ? xRange.x : xRange.y;
            target = new Vector3(wallX, transform.position.y, Mathf.Clamp(player.transform.position.z + Random.Range(-14f, 14f), zRange.x, zRange.y));
            for (int i = 0; i < 8 && Blocked(target); i++)
                target.z = Random.Range(zRange.x, zRange.y);
            hasTarget = true;
        }
        if (Drive(target, 30f, 400f))
        {
            hasTarget = false;
            pauseLeft = 0.6f;
            Slam(rampWest);
        }
        if (phaseTime > rampageSeconds)
        {
            phase = Phase.Hunt;
            phaseTime = 0f;
            hasTarget = false;
            SetButton(new Color(0.2f, 1f, 0.35f), 4f);
        }
    }

    void Hunt()
    {
        if (phaseTime > 900f)
        {
            StartCoroutine(PowerDown());
            return;
        }
        bool seen = CanSee();
        if (seen && chaseLeft <= 0f) chaseLeft = 5f;
        if (chaseLeft > 0f)
        {
            chaseLeft -= Time.deltaTime;
            SetFan(AlertColor);
            var to = player.transform.position;
            to.y = transform.position.y;
            Drive(Steer(to), 16f, 220f);
            hasTarget = false;
            return;
        }
        SetFan(CalmColor);
        if (!hasTarget || Drive(target, 8f, 150f))
        {
            for (int i = 0; i < 10; i++)
            {
                target = new Vector3(Random.Range(xRange.x, xRange.y), transform.position.y, Random.Range(zRange.x, zRange.y));
                if (!Blocked(target)) break;
            }
            hasTarget = true;
        }
        else if (Blocked(transform.position + transform.forward * 4f)) hasTarget = false;
    }

    bool Drive(Vector3 to, float speed, float turn)
    {
        var d = to - transform.position;
        d.y = 0f;
        if (d.magnitude < 0.6f) return true;
        var want = Quaternion.LookRotation(d);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turn * Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, want) < 50f)
        {
            var next = transform.position + transform.forward * Mathf.Min(speed * Time.deltaTime, d.magnitude);
            next.x = Mathf.Clamp(next.x, xRange.x, xRange.y);
            next.z = Mathf.Clamp(next.z, zRange.x, zRange.y);
            transform.position = next;
        }
        return false;
    }

    Vector3 Steer(Vector3 goal)
    {
        var d = goal - transform.position;
        d.y = 0f;
        if (!Blocked(transform.position + d.normalized * Mathf.Min(6f, d.magnitude))) return goal;
        foreach (float a in new[] { 45f, -45f, 90f, -90f, 135f, -135f })
        {
            var dir = Quaternion.Euler(0f, a, 0f) * d.normalized;
            if (!Blocked(transform.position + dir * 6f)) return transform.position + dir * 6f;
        }
        return goal;
    }

    bool Blocked(Vector3 to)
    {
        float lift = radius * 0.9f + 0.15f;
        var from = transform.position + Vector3.up * lift;
        var d = to + Vector3.up * lift - from;
        foreach (var h in Physics.SphereCastAll(from, radius * 0.9f, d.normalized, d.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!h.transform.IsChildOf(transform) && h.transform != player.transform && h.point.y > 0.1f)
                return true;
        return false;
    }

    void Slam(bool west)
    {
        PlayerController.Shake(0.7f);
        if (items == null) return;
        float shelfX = west ? westShelfX : eastShelfX;
        float wallX = west ? xRange.x - radius : xRange.y + radius;
        for (int i = 0; i < 5; i++)
        {
            var at = new Vector3(Mathf.Lerp(wallX, (xRange.x + xRange.y) / 2, Random.Range(0.1f, 0.9f)), 0f,
                transform.position.z + Random.Range(-16f, 16f));
            items.Drop(at, shelfX, 1.3f);
        }
        var p = player.transform.position;
        var v = player.GetComponent<CharacterController>().velocity;
        items.Drop(new Vector3(p.x, 0f, p.z), shelfX, 1.3f);
        items.Drop(new Vector3(p.x + v.x * 1.2f, 0f, p.z + v.z * 1.2f), shelfX, 1.3f);
    }

    void CheckTouch()
    {
        var p = player.transform.position;
        var flat = new Vector2(p.x - transform.position.x, p.z - transform.position.z);
        float feet = p.y - 1f;
        float roof = transform.position.y + top;
        if (flat.magnitude > radius + 0.7f || player.InCutscene) return;

        if (phase != Phase.Hunt)
        {
            if (feet < roof + 0.1f) Zap();
            return;
        }
        if (feet > roof - 0.35f)
        {
            if (flat.magnitude < 1.5f && player.IsGrounded) StartCoroutine(PowerDown());
            return;
        }
        if (feet < roof - 0.6f) Zap();
    }

    void Zap()
    {
        var away = player.transform.position - transform.position;
        away.y = 0f;
        player.GetComponent<FallingHitResponder>()?.Kill(away.normalized);
        chaseLeft = 0f;
    }

    bool CanSee()
    {
        var from = eye.position;
        var to = player.transform.position + Vector3.up * 0.3f - from;
        float dist = to.magnitude;
        if (dist > viewRange) return false;
        var flat = new Vector3(to.x, 0f, to.z);
        if (Vector3.Angle(eye.forward, flat) > viewHalfAngle) return false;
        foreach (var h in Physics.RaycastAll(from, to / dist, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (h.transform != player.transform && !h.transform.IsChildOf(transform)) return false;
        return true;
    }

    void UpdateFan()
    {
        var from = eye.position;
        float floorY = 0.08f;
        fanVerts[0] = fan.InverseTransformPoint(new Vector3(from.x, floorY, from.z));
        for (int i = 0; i < Rays; i++)
        {
            float a = Mathf.Lerp(-viewHalfAngle, viewHalfAngle, i / (Rays - 1f));
            var dir = Quaternion.Euler(0f, a, 0f) * new Vector3(eye.forward.x, 0f, eye.forward.z).normalized;
            float reach = viewRange;
            foreach (var h in Physics.RaycastAll(from, dir, viewRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (h.distance < reach && h.transform != player.transform && !h.transform.IsChildOf(transform))
                    reach = h.distance;
            var end = from + dir * reach;
            fanVerts[i + 1] = fan.InverseTransformPoint(new Vector3(end.x, floorY, end.z));
        }
        fanMesh.vertices = fanVerts;
        fanMesh.RecalculateBounds();
    }

    void SetFan(Color c)
    {
        fanRenderer.material.SetColor("_BaseColor", c);
        if (ledRenderer != null) ledRenderer.material.SetColor("_EmissionColor", new Color(c.r, c.g, c.b) * 4f);
        if (statusLight != null) statusLight.color = new Color(c.r, c.g, c.b);
    }

    void SetButton(Color c, float glow)
    {
        if (buttonRenderer == null) return;
        buttonRenderer.material.SetColor("_BaseColor", Color.Lerp(new Color(0.15f, 0.15f, 0.17f), c, glow > 0f ? 0.6f : 0f));
        buttonRenderer.material.SetColor("_EmissionColor", c * glow);
    }

    IEnumerator PowerDown()
    {
        brushSpeed = 0f;
        phase = Phase.Dead;
        player.Launch(10f);
        PlayerController.Shake(0.5f);
        SetButton(Color.black, 0f);
        fan.gameObject.SetActive(false);
        if (statusLight != null) statusLight.enabled = false;
        if (ledRenderer != null) ledRenderer.material.SetColor("_EmissionColor", Color.black);
        Show(3, "LIGHTS OUT", "The VAC-3000 is switched off and the boxes at the far end have come down. Head for the door!");
        OpenGate();
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            transform.Rotate(0f, 200f * (1f - t / 1.2f) * Time.deltaTime, 0f);
            yield return null;
        }
        yield return new WaitForSeconds(6f);
        if (popup != null) popup.Hide(this);
    }

    void OpenGate()
    {
        if (gateSeal != null) gateSeal.enabled = false;
        if (gate == null) return;
        var cc = player.GetComponent<CharacterController>();
        foreach (var c in gate.GetComponentsInChildren<Collider>())
        {
            if (c == gateSeal) continue;
            if (cc != null) Physics.IgnoreCollision(cc, c, true);
            var body = c.attachedRigidbody != null ? c.attachedRigidbody : c.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.mass = 2f;
            body.AddForce(new Vector3(Random.Range(-18f, -9f), Random.Range(3f, 7f), Random.Range(-4f, 4f)), ForceMode.VelocityChange);
            body.AddTorque(Random.insideUnitSphere * 4f, ForceMode.VelocityChange);
        }
        StartCoroutine(ClearGate());
    }

    IEnumerator ClearGate()
    {
        yield return new WaitForSeconds(3f);
        foreach (var body in gate.GetComponentsInChildren<Rigidbody>())
        {
            var p = body.position;
            if (p.x < gateArea.min.x - 1f || p.x > gateArea.max.x + 1f || p.z < gateArea.min.z - 1f || p.z > gateArea.max.z + 1f) continue;
            StartCoroutine(Shrink(body));
        }
    }

    static IEnumerator Shrink(Rigidbody body)
    {
        body.isKinematic = true;
        foreach (var c in body.GetComponentsInChildren<Collider>()) c.enabled = false;
        var t = body.transform;
        var from = t.localScale;
        for (float k = 0; k < 0.6f; k += Time.deltaTime)
        {
            t.localScale = from * (1f - k / 0.6f);
            yield return null;
        }
        t.gameObject.SetActive(false);
    }

    IEnumerator HideCard(float after)
    {
        yield return new WaitForSeconds(after);
        if (popup != null) popup.Hide(this);
    }

    void Show(int glyph, string heading, string text)
    {
        if (popup != null) popup.Show(this, glyphs != null && glyph < glyphs.Length ? glyphs[glyph] : "", heading, text, null);
    }

    public void Setup(Transform eyeT, Transform fanT, Renderer button, Renderer led, Light light, Transform[] brushSet,
        float bodyRadius, float bodyTop, Vector2 xs, Vector2 zs, float startAt, float westX, float eastX,
        GameObject gateRoot, Collider seal, string[] icons)
    {
        eye = eyeT;
        fan = fanT;
        fanRenderer = fanT.GetComponent<Renderer>();
        buttonRenderer = button;
        ledRenderer = led;
        statusLight = light;
        brushes = brushSet;
        radius = bodyRadius;
        top = bodyTop;
        xRange = xs;
        zRange = zs;
        startZ = startAt;
        westShelfX = westX;
        eastShelfX = eastX;
        gate = gateRoot;
        gateSeal = seal;
        glyphs = icons;
    }
}
