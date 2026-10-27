using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// the white practice room off the main menu. It teaches one thing at a time on the tutorial card, and the glass gate
// at the end of each station drops away once its lesson is done. Finishing the last one goes back to the menu
public class TutorialRoom : MonoBehaviour
{
    public enum Goal { Reach, PickUp, StopOn }

    [System.Serializable]
    public class Lesson
    {
        public string icon, title, body;
        public string[] keys;
        public Goal goal;
        public Bounds area;         // Reach and StopOn: where Henry has to be
        public Transform gate;      // dropped when the lesson is done; may be empty
        public Transform respawn;   // the checkpoint for this lesson; may be empty
    }

    [SerializeField] Lesson[] lessons;
    [SerializeField] Transform start;
    [SerializeField] Carryable box;
    [SerializeField] Color ambient = new Color(0.86f, 0.87f, 0.9f);
    [SerializeField] string doneIcon, doneTitle = "YOU'RE READY", doneBody = "That's everything Henry needs. Heading back to the menu...";

    PlayerController player;
    CharacterController body;
    TutorialPopup popup;
    Vector3 boxHome;
    Quaternion boxHomeRot;
    int current = -1;
    float stillFor;

    public void Begin(PlayerController who)
    {
        player = who;
        body = who.GetComponent<CharacterController>();
        popup = FindAnyObjectByType<TutorialPopup>();
        if (box != null)
        {
            boxHome = box.transform.position;
            boxHomeRot = box.transform.rotation;
        }
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        player.Teleport(start.position, start.rotation);
        player.SetCheckpoint(start.position, start.rotation);
        player.SetLook(start.eulerAngles.y, 12f);
        player.Respawned += OnRespawned;
        player.enabled = true;
        StartCoroutine(Show(0, 0.8f));
    }

    void OnDestroy()
    {
        if (player != null) player.Respawned -= OnRespawned;
    }

    // a box left somewhere useless goes back to where it started when Henry respawns (R)
    void OnRespawned()
    {
        if (box == null || box.IsHeld || current < 0 || current >= lessons.Length || lessons[current].goal == Goal.StopOn) return;
        box.transform.SetPositionAndRotation(boxHome, boxHomeRot);
    }

    IEnumerator Show(int index, float delay)
    {
        current = -1;
        if (popup != null) popup.Hide(this);
        yield return new WaitForSeconds(delay);
        if (index >= lessons.Length)
        {
            yield return Finish();
            yield break;
        }
        var l = lessons[index];
        if (l.respawn != null) player.SetCheckpoint(l.respawn.position, l.respawn.rotation);
        if (popup != null) popup.Show(this, l.icon, l.title, l.body, l.keys);
        Debug.Log($"[Tutorial] lesson {index + 1}: {l.title}");
        stillFor = 0f;
        current = index;
    }

    void Update()
    {
        if (player == null || current < 0) return;
        var l = lessons[current];
        if (!Done(l)) return;
        if (l.gate != null) StartCoroutine(Open(l.gate));
        StartCoroutine(Show(current + 1, 0.9f));
    }

    bool Done(Lesson l)
    {
        var p = player.transform.position;
        switch (l.goal)
        {
            case Goal.PickUp:
                return box != null && box.IsHeld;
            case Goal.StopOn:
                var v = body != null ? body.velocity : Vector3.zero;
                v.y = 0f;
                stillFor = l.area.Contains(p) && player.IsGrounded && v.magnitude < 1.5f ? stillFor + Time.deltaTime : 0f;
                return stillFor > 0.35f;
            default:
                return l.area.Contains(p);
        }
    }

    IEnumerator Open(Transform gate)
    {
        foreach (var c in gate.GetComponentsInChildren<Collider>()) c.enabled = false;
        var from = gate.position;
        float drop = 0f;
        foreach (var r in gate.GetComponentsInChildren<Renderer>()) drop = Mathf.Max(drop, r.bounds.size.y);
        for (float t = 0; t < 0.8f; t += Time.deltaTime)
        {
            gate.position = from + Vector3.down * drop * Mathf.SmoothStep(0f, 1f, t / 0.8f);
            yield return null;
        }
        gate.gameObject.SetActive(false);
    }

    IEnumerator Finish()
    {
        if (popup != null) popup.Show(this, doneIcon, doneTitle, doneBody, null);
        yield return new WaitForSeconds(4f);
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.Fade(1f, 0.6f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Setup(Transform startPoint, Carryable carry, Lesson[] list, string finishIcon)
    {
        start = startPoint;
        box = carry;
        lessons = list;
        doneIcon = finishIcon;
    }
}
