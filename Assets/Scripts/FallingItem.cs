using System.Collections;
using UnityEngine;

public class FallingItem : MonoBehaviour
{
    public static Material DashMaterial;

    public IEnumerator Run(Vector3 from, Vector3 landAt, float warnTime, PlayerController player)
    {
        float yaw = Random.Range(0f, 360f);
        var baseRot = transform.rotation;
        var restRot = Quaternion.Euler(0, yaw, 0) * (Random.value < 0.45f ? Quaternion.Euler(0, 0, 90) : Quaternion.identity) * baseRot;
        transform.SetPositionAndRotation(from, restRot);
        var b = Bounds();
        var restPos = from + new Vector3(landAt.x - b.center.x, landAt.y - b.min.y, landAt.z - b.center.z);
        transform.rotation = Quaternion.Euler(0, yaw, 0) * baseRot;
        var size = new Vector2(b.size.x, b.size.z) * 0.55f;
        bool round = Mathf.Abs(b.size.x - b.size.z) < Mathf.Max(b.size.x, b.size.z) * 0.25f;
        var ring = MakeOutline(landAt, size, round);

        var startRot = transform.rotation;
        float teeter = warnTime * 0.3f;
        for (float t = 0; t < teeter; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.AngleAxis(Mathf.Sin(t * 40f) * 6f, transform.right) * startRot;
            Pulse(ring, t);
            yield return null;
        }

        float fall = warnTime - teeter;
        var spinAxis = Random.onUnitSphere;
        float spinSpeed = Random.Range(200f, 420f);
        Quaternion spun = transform.rotation;
        for (float t = 0; t < fall; t += Time.deltaTime)
        {
            float k = t / fall;
            var p = Vector3.Lerp(from, restPos, k);
            p.y = Mathf.Lerp(from.y, restPos.y, k * k) + Mathf.Sin(k * Mathf.PI) * 2f;
            transform.position = p;
            spun = Quaternion.AngleAxis(spinSpeed * Time.deltaTime, spinAxis) * spun;
            float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, k));
            transform.rotation = Quaternion.Slerp(spun, restRot, settle);
            Pulse(ring, teeter + t);
            yield return null;
        }
        transform.SetPositionAndRotation(restPos, restRot);
        var lb = Bounds();
        Destroy(ring.gameObject);

        if (player != null && !player.InCutscene)
        {
            var d = player.transform.position - landAt;
            float nx = d.x / Mathf.Max(size.x, 0.8f), nz = d.z / Mathf.Max(size.y, 0.8f);
            bool inside = round ? nx * nx + nz * nz <= 1.2f : Mathf.Abs(nx) <= 1.1f && Mathf.Abs(nz) <= 1.1f;
            if (inside && player.transform.position.y - 1f < landAt.y + lb.size.y)
                player.GetComponent<FallingHitResponder>()?.Hit(new Vector3(d.x, 0, d.z).normalized);
        }

        var tiltAxis = Vector3.Cross(Vector3.up, Random.insideUnitSphere).normalized;
        if (tiltAxis.sqrMagnitude < 0.1f) tiltAxis = Vector3.right;
        var baseScale = transform.localScale;
        var pivotOffset = transform.position - lb.center;
        for (float t = 0; t < 0.9f; t += Time.deltaTime)
        {
            float decay = Mathf.Exp(-5f * t);
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI / 0.32f)) * 0.9f * decay;
            float rock = Mathf.Sin(t * 22f) * 9f * decay;
            var rot = Quaternion.AngleAxis(rock, tiltAxis);
            transform.rotation = rot * restRot;
            transform.position = lb.center + rot * pivotOffset + Vector3.up * hop;
            float squash = Mathf.Max(0f, 1f - hop * 3f) * 0.12f * decay;
            transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + squash, 1f - squash * 1.6f, 1f + squash));
            yield return null;
        }
        transform.SetPositionAndRotation(restPos, restRot);
        transform.localScale = baseScale;

        yield return new WaitForSeconds(2.2f);
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            transform.position += Vector3.down * Time.deltaTime * 2f;
            yield return null;
        }
        Destroy(gameObject);
    }

    Bounds Bounds()
    {
        var rs = GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    LineRenderer MakeOutline(Vector3 at, Vector2 half, bool round)
    {
        var go = new GameObject("Landing Warning");
        go.transform.position = at + Vector3.up * 0.06f;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
        lr.widthMultiplier = 0.28f;
        lr.textureMode = LineTextureMode.Tile;
        lr.material = DashMaterial;
        lr.startColor = lr.endColor = new Color(1f, 0.1f, 0.1f, 0.95f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        const int n = 48;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Vector2 p;
            if (round) p = new Vector2(Mathf.Cos(a) * half.x, Mathf.Sin(a) * half.y);
            else
            {
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                p = new Vector2(Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.3f) * half.x, Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.3f) * half.y);
            }
            lr.SetPosition(i, new Vector3(p.x, p.y, 0));
        }
        return lr;
    }

    static void Pulse(LineRenderer lr, float t)
    {
        if (lr == null) return;
        lr.widthMultiplier = 0.25f + Mathf.Abs(Mathf.Sin(t * 10f)) * 0.12f;
        lr.material.mainTextureOffset = new Vector2(-t * 2f, 0);
    }
}
