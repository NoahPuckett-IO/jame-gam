using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] RectTransform card;
    [SerializeField] Text icon, title, body;
    [SerializeField] GameObject[] chips;
    [SerializeField] Text[] chipLabels;

    Vector2 shownPos;
    Coroutine anim;
    Object owner;

    void Awake()
    {
        shownPos = card.anchoredPosition;
        group.alpha = 0;
        card.anchoredPosition = shownPos + new Vector2(0, 40);
    }

    public void Show(Object who, string iconGlyph, string heading, string text, string[] keys)
    {
        owner = who;
        icon.text = iconGlyph;
        title.text = heading;
        body.text = text;
        for (int i = 0; i < chips.Length; i++)
        {
            bool on = keys != null && i < keys.Length;
            chips[i].SetActive(on);
            if (on) chipLabels[i].text = keys[i];
        }
        Play(true);
    }

    public void Hide(Object who)
    {
        if (who != owner) return;
        Play(false);
    }

    void Play(bool show)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Fade(show));
    }

    IEnumerator Fade(bool show)
    {
        float from = group.alpha, to = show ? 1 : 0;
        var hidden = shownPos + new Vector2(0, 40);
        for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / 0.3f);
            group.alpha = Mathf.Lerp(from, to, k);
            card.anchoredPosition = Vector2.Lerp(show ? hidden : shownPos, show ? shownPos : hidden, k);
            yield return null;
        }
        group.alpha = to;
        card.anchoredPosition = show ? shownPos : hidden;
    }

    public void Setup(CanvasGroup g, RectTransform c, Text i, Text t, Text b, GameObject[] keyChips, Text[] keyLabels)
    {
        group = g; card = c; icon = i; title = t; body = b; chips = keyChips; chipLabels = keyLabels;
    }
}
