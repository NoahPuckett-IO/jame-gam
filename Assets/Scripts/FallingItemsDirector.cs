using System.Collections.Generic;
using UnityEngine;

// The collapsing aisle: while Henry is in this hall, groceries keep getting knocked off both
// shelves and crash onto the floor. Most are aimed just ahead of where he's running (so standing
// still or dawdling gets you flattened); the rest land randomly. It gets busier the further he gets.
public class FallingItemsDirector : MonoBehaviour
{
    [SerializeField] GameObject[] templates;  // inactive, correctly scaled store items
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

        // Half ahead of Henry, where he'll be in about warnTime; half anywhere in the next stretch.
        var vel = player.GetComponent<CharacterController>().velocity;
        Vector3 target;
        if (Random.value < 0.5f)
            target = p + new Vector3(vel.x, 0, vel.z) * warnTime + new Vector3(Random.Range(-4f, 4f), 0, dir * Random.Range(0f, 8f));
        else
            target = new Vector3(hallX + Random.Range(-halfWidth, halfWidth), 0, p.z + dir * Random.Range(4f, 45f));
        target.x = Mathf.Clamp(target.x, hallX - halfWidth + 2f, hallX + halfWidth - 2f);
        target.y = 0;

        // Knocked off whichever shelf is nearer, from a random board.
        float side = target.x > hallX ? 1f : -1f;
        var from = new Vector3(hallX + side * (halfWidth + 3f), shelfHeights[Random.Range(0, shelfHeights.Length)] + 1f, target.z + Random.Range(-3f, 3f));

        var item = Instantiate(templates[Random.Range(0, templates.Length)]);
        item.SetActive(true);
        foreach (var c in item.GetComponentsInChildren<Collider>()) c.enabled = false;
        var fi = item.AddComponent<FallingItem>();
        StartCoroutine(fi.Run(from, target, warnTime, player));
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
