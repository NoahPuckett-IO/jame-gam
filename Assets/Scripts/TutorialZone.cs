using UnityEngine;

// Trigger volume that shows a tutorial card while the player is inside it.
[RequireComponent(typeof(BoxCollider))]
public class TutorialZone : MonoBehaviour
{
    [SerializeField] string iconGlyph;
    [SerializeField] string title;
    [TextArea] [SerializeField] string body;
    [SerializeField] string[] keys;

    TutorialPopup popup;
    bool showing;

    void Start() => popup = FindAnyObjectByType<TutorialPopup>();

    // Stay rather than Enter: the player starts inside the first zone while the title menu is up,
    // so the card waits until they actually have control (and steps aside during cutscenes).
    void OnTriggerStay(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (popup == null || player == null) return;
        bool active = player.enabled && !player.InCutscene;
        if (active && !showing) popup.Show(this, iconGlyph, title, body, keys);
        else if (!active && showing) popup.Hide(this);
        showing = active;
    }

    void OnTriggerExit(Collider other)
    {
        if (popup == null || other.GetComponent<PlayerController>() == null) return;
        if (showing) popup.Hide(this);
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
