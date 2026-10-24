using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] Transform leftPanel, rightPanel;
    [SerializeField] float openDistance = 60f;
    [SerializeField] float slideAmount = 25f;
    [SerializeField] float speed = 2f;

    Transform player;
    Vector3 leftClosed, rightClosed;
    float open;

    void Start()
    {
        leftClosed = leftPanel.localPosition;
        rightClosed = rightPanel.localPosition;
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;
    }

    void Update()
    {
        bool near = player != null && Vector3.Distance(player.position, transform.position) < openDistance;
        open = Mathf.MoveTowards(open, near ? 1f : 0f, speed * Time.deltaTime);
        float t = Mathf.SmoothStep(0f, 1f, open);
        leftPanel.localPosition = leftClosed + Vector3.back * slideAmount * t;
        rightPanel.localPosition = rightClosed + Vector3.forward * slideAmount * t;
    }

    public void Setup(Transform left, Transform right, float slide, float distance)
    {
        leftPanel = left;
        rightPanel = right;
        slideAmount = slide;
        openDistance = distance;
    }
}
