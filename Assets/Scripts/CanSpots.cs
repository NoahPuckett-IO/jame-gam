using UnityEngine;

// three marked spots on a shelf board: once a can stands on every one, the ladder they hold up drops into place.
// A can put down somewhere it can't come back from (off the shelf) is set back where it started.
public class CanSpots : MonoBehaviour
{
    [SerializeField] Transform[] spots;
    [SerializeField] Renderer[] marks;
    [SerializeField] Material empty, filled;
    [SerializeField] Carryable[] cans;
    [SerializeField] Transform ladder;
    [SerializeField] float dropHeight = 9f;
    [SerializeField] float resetBelow;
    [SerializeField] float radius = 1.4f;

    const float DropTime = 0.9f;
    Vector3[] cansHome;
    Vector3 ladderHome;
    float dropT = -1f;
    bool solved;

    void Start()
    {
        cansHome = new Vector3[cans.Length];
        for (int i = 0; i < cans.Length; i++) cansHome[i] = cans[i].transform.position;
        if (ladder == null) return;
        ladderHome = ladder.position;
        ladder.gameObject.SetActive(false);
    }

    void Update()
    {
        for (int i = 0; i < cans.Length; i++)
            if (cans[i] != null && !cans[i].IsHeld && cans[i].transform.position.y < resetBelow)
                cans[i].transform.position = cansHome[i];

        if (dropT >= 0f)
        {
            Drop();
            return;
        }
        if (solved) return;

        int count = 0;
        for (int i = 0; i < spots.Length; i++)
        {
            bool on = Occupied(spots[i].position);
            if (on) count++;
            var mat = on ? filled : empty;
            if (marks[i].sharedMaterial != mat) marks[i].sharedMaterial = mat;
        }
        if (count < spots.Length) return;

        solved = true;
        if (ladder == null) return;
        ladder.position = ladderHome + Vector3.up * dropHeight;
        var climb = ladder.GetComponent<Ladder>();
        if (climb != null) climb.enabled = false;
        ladder.gameObject.SetActive(true);
        dropT = 0f;
    }

    void Drop()
    {
        dropT += Time.deltaTime / DropTime;
        float k = Mathf.Clamp01(dropT);
        // falls in with gravity, then a small bounce as it catches on the shelf
        float fall = k < 0.8f ? 1f - (k / 0.8f) * (k / 0.8f) : Mathf.Sin((k - 0.8f) / 0.2f * Mathf.PI) * 0.04f;
        ladder.position = ladderHome + Vector3.up * dropHeight * fall;
        if (k < 1f) return;
        ladder.position = ladderHome;
        dropT = -1f;
        var climb = ladder.GetComponent<Ladder>();
        if (climb != null) climb.enabled = true;
        PlayerController.Shake(0.25f);
    }

    bool Occupied(Vector3 spot)
    {
        foreach (var c in cans)
        {
            if (c == null || c.IsHeld) continue;
            var d = c.transform.position - spot;
            if (Mathf.Abs(d.y) < 1.5f && new Vector2(d.x, d.z).magnitude < radius) return true;
        }
        return false;
    }

    public void Setup(Transform[] spotPoints, Renderer[] spotMarks, Material emptyMat, Material filledMat, Carryable[] items, Transform drops, float lost)
    {
        spots = spotPoints;
        marks = spotMarks;
        empty = emptyMat;
        filled = filledMat;
        cans = items;
        ladder = drops;
        resetBelow = lost;
    }
}
