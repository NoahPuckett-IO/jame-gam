using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TutorialZone : MonoBehaviour
{
    [SerializeField] string iconGlyph;
    [SerializeField] string title;
    [TextArea] [SerializeField] string body;
    [SerializeField] string[] keys;

    TutorialPopup popup;
    PlayerController shownTo;
    Collider zone;
    bool showing, used;

    void Start()
    {
        popup = FindAnyObjectByType<TutorialPopup>();
        zone = GetComponent<Collider>();
    }

    void OnTriggerStay(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (popup == null || player == null) return;
        bool active = !used && player.enabled && !player.InCutscene && !player.Attached;
        if (active && !showing) popup.Show(this, iconGlyph, title, body, keys);
        else if (!active && showing) popup.Hide(this);
        showing = active;
        shownTo = player;
    }

    void OnTriggerExit(Collider other)
    {
        if (popup == null || other.GetComponent<PlayerController>() == null) return;
        if (showing) popup.Hide(this);
        showing = false;
    }

    // grabbing a ladder or zipline switches the player's collider off, so no trigger exit ever comes;
    // once they've used it (or got carried or warped away) the hint has done its job, so drop it for good
    void Update()
    {
        if (!showing || shownTo == null) return;
        bool away = !zone.bounds.Contains(shownTo.transform.position) &&
                    zone.bounds.SqrDistance(shownTo.transform.position) > 4f;
        if (shownTo.Attached) used = true;
        if (!used && !away) return;
        popup.Hide(this);
        showing = false;
    }

    public void Setup(string glyph, string heading, string text, string[] keyNames)
    {
        iconGlyph = glyph;
        title = heading;
        body = text;
        keys = keyNames;
    }
}
