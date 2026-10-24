using UnityEngine;

// A springy thing (sponge, whipped cream, marshmallow...) that launches Henry upward when he lands
// on it, squashing and wobbling back as he leaves.
public class BouncePad : MonoBehaviour
{
    [SerializeField] float launchSpeed = 22f;
    [SerializeField] Transform squashTarget;

    Vector3 baseScale;
    float squash;

    void Awake()
    {
        if (squashTarget == null) squashTarget = transform;
        baseScale = squashTarget.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        player.Launch(launchSpeed);
        squash = 1f;
    }

    void Update()
    {
        if (squash <= 0f) return;
        squash = Mathf.Max(0f, squash - Time.deltaTime * 2.5f);
        float wobble = Mathf.Sin((1f - squash) * 18f) * squash;
        squashTarget.localScale = Vector3.Scale(baseScale, new Vector3(1f + wobble * 0.25f, 1f - wobble * 0.35f, 1f + wobble * 0.25f));
    }

    public void Setup(float speed, Transform visual)
    {
        launchSpeed = speed;
        squashTarget = visual;
    }
}
