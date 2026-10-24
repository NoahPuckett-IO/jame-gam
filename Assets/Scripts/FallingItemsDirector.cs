using System.Collections.Generic;
using UnityEngine;

public class FallingItemsDirector : MonoBehaviour
{
    [SerializeField] GameObject[] templates;
    [SerializeField] float hallX, halfWidth, zStart, zEnd;
    [SerializeField] float[] shelfHeights = { 8.6f, 17.3f, 25.9f };
    [SerializeField] Material dashMaterial;
    [SerializeField] float warnTime = 1.15f;

    PlayerController player;
    float next;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        FallingItem.DashMaterial = dashMaterial;
    }

    bool PlayerInHall()
    {
        if (player == null || !player.enabled || player.InCutscene) return false;
        var p = player.transform.position;
        float zMin = Mathf.Min(zStart, zEnd), zMax = Mathf.Max(zStart, zEnd);
        return Mathf.Abs(p.x - hallX) < halfWidth + 1f && p.z > zMin && p.z < zMax;
    }

    void Update()
    {
        if (!PlayerInHall() || templates == null || templates.Length == 0) return;
        if (Time.time < next) return;

        var p = player.transform.position;
        float dir = Mathf.Sign(zEnd - zStart);
        float progress = Mathf.InverseLerp(zStart, zEnd, p.z);
        next = Time.time + Mathf.Lerp(0.34f, 0.17f, progress) * Random.Range(0.8f, 1.2f);

        var vel = player.GetComponent<CharacterController>().velocity;
        var flat = new Vector3(vel.x, 0, vel.z);
        Vector3 target;
        float roll = Random.value;
        if (roll < 0.3f)
            target = p + flat * warnTime;
        else if (roll < 0.45f)
            target = p + flat * (warnTime * 0.5f);
        else if (roll < 0.75f)
            target = p + flat * warnTime + new Vector3(Random.Range(-4f, 4f), 0, dir * Random.Range(0f, 8f));
        else
            target = new Vector3(hallX + Random.Range(-halfWidth, halfWidth), 0, p.z + dir * Random.Range(4f, 45f));
        target.x = Mathf.Clamp(target.x, hallX - halfWidth + 2f, hallX + halfWidth - 2f);
        target.y = 0;

        float side = target.x > hallX ? 1f : -1f;
        Drop(target, hallX + side * (halfWidth + 3f));
    }

    public void Drop(Vector3 target, float shelfX, float warn = -1f)
    {
        if (templates == null || templates.Length == 0) return;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        FallingItem.DashMaterial = dashMaterial;
        var from = new Vector3(shelfX, shelfHeights[Random.Range(0, shelfHeights.Length)] + 1f, target.z + Random.Range(-3f, 3f));
        var item = Instantiate(templates[Random.Range(0, templates.Length)]);
        item.SetActive(true);
        foreach (var c in item.GetComponentsInChildren<Collider>()) c.enabled = false;
        var fi = item.AddComponent<FallingItem>();
        StartCoroutine(fi.Run(from, target, warn > 0f ? warn : warnTime, player));
    }

    public void Setup(GameObject[] items, float x, float half, float zFrom, float zTo, Material dash)
    {
        templates = items;
        hallX = x;
        halfWidth = half;
        zStart = zFrom;
        zEnd = zTo;
        dashMaterial = dash;
    }
}
