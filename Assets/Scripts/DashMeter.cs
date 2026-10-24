using UnityEngine;
using UnityEngine.UI;

public class DashMeter : MonoBehaviour
{
    [SerializeField] Font font;

    PlayerController player;
    CanvasGroup group;
    Image fill;
    Text label;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        var canvas = new GameObject("Dash Meter", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvas.transform.SetParent(transform, false);
        var c = canvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 25;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvas.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;

        var back = Box(canvas.transform, new Vector2(0, 70), new Vector2(240, 18), new Color(0, 0, 0, 0.55f));
        fill = Box(back.transform, Vector2.zero, new Vector2(232, 10), new Color(0.2f, 0.85f, 0.75f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);

        label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(canvas.transform, false);
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 100);
        rt.sizeDelta = new Vector2(300, 40);
        label.font = font;
        label.fontSize = 30;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
    }

    static Image Box(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var img = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, parent is RectTransform p && p.GetComponent<Canvas>() == null ? 0.5f : 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    void Update()
    {
        if (player == null || group == null) return;
        bool show = player.enabled && !player.InCutscene;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.deltaTime * 4f);
        float k = player.DashReady;
        fill.fillAmount = k;
        bool ready = k >= 1f;
        fill.color = ready ? new Color(0.2f, 0.85f, 0.75f) : new Color(0.55f, 0.6f, 0.6f);
        label.text = ready ? "DASH  [SHIFT]" : "DASH";
        label.color = new Color(1, 1, 1, ready ? 0.9f : 0.5f);
    }

    public void Setup(Font labelFont) => font = labelFont;
}
