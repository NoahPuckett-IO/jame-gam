using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UiKit
{
    public static readonly Color Accent = new Color(0.2f, 0.85f, 0.75f);
    public static readonly Color Ink = new Color(0.04f, 0.07f, 0.08f);
    public static readonly Color Card = new Color(0.06f, 0.09f, 0.1f, 0.97f);

    public static Font TitleFont, BodyFont;
    public static Sprite Rounded, Knob;

    public static Canvas Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Fill(Transform parent, string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static Image Image(RectTransform rt, Color c, bool sliced = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        if (sliced && Rounded != null)
        {
            img.sprite = Rounded;
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }
        img.color = c;
        return img;
    }

    public static Text Label(Transform parent, string text, Font font, int size, Color c, TextAnchor anchor, Vector2 pos, Vector2 box)
    {
        var rt = Rect(parent, text, new Vector2(0.5f, 0.5f), pos, box);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = c;
        t.alignment = anchor;
        t.text = text;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Button Button(Transform parent, string text, Vector2 pos, Vector2 size, bool primary, UnityAction onClick,
        MenuButtonFx.Sound sound = MenuButtonFx.Sound.Click, int fontSize = 40)
    {
        var rt = Rect(parent, text + " Button", new Vector2(0.5f, 0.5f), pos, size);
        var img = Image(rt, primary ? Accent : new Color(1f, 1f, 1f, 0.06f));
        if (!primary) rt.gameObject.AddComponent<Outline>().effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.selectedColor = new Color(1.1f, 1.1f, 1.1f);
        b.colors = colors;
        if (onClick != null) b.onClick.AddListener(onClick);
        var label = Label(rt, text, TitleFont, fontSize, primary ? Ink : Color.white, TextAnchor.MiddleCenter, Vector2.zero, size);
        label.name = "Label";
        rt.gameObject.AddComponent<MenuButtonFx>().Setup(sound, 0f);
        return b;
    }

    public static Slider Slider(Transform parent, Vector2 pos, float width, float value, UnityAction<float> onChange)
    {
        var rt = Rect(parent, "Slider", new Vector2(0.5f, 0.5f), pos, new Vector2(width, 28));
        Image(Fill(rt, "Track"), new Color(1f, 1f, 1f, 0.12f));
        var fillArea = Fill(rt, "Fill Area");
        fillArea.offsetMin = new Vector2(6, 6);
        fillArea.offsetMax = new Vector2(-6, -6);
        var fill = Fill(fillArea, "Fill");
        Image(fill, Accent);
        var handleArea = Fill(rt, "Handle Area");
        handleArea.offsetMin = new Vector2(14, 0);
        handleArea.offsetMax = new Vector2(-14, 0);
        // the slider stretches the handle to the full track height, so only add what's left to make it 34 tall
        var handle = Rect(handleArea, "Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34 - 28));
        var knob = handle.gameObject.AddComponent<Image>();
        knob.sprite = Knob;
        knob.type = UnityEngine.UI.Image.Type.Simple;
        knob.preserveAspect = true;
        knob.color = Color.white;
        var s = rt.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = knob;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = value;
        s.onValueChanged.AddListener(onChange);
        return s;
    }
}
