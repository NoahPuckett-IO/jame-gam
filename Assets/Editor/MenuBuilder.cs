using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds the title screen on a solid deep-teal background: title, tagline and buttons on the left,
// a framed card on the right for the character drawing. Called from GroceryStoreBuilder.
static class MenuBuilder
{
    public const string GameTitle = "Tiny Henry";
    static readonly Color Accent = new Color(0.2f, 0.85f, 0.75f);
    static readonly Color Ink = new Color(0.04f, 0.07f, 0.08f);
    static Font titleFont, bodyFont, iconFont;
    static Sprite rounded;

    public static void Build(PlayerController player, Transform spawn)
    {
        titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BebasNeue-Regular.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Oswald.ttf") ?? titleFont;
        iconFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/MaterialIcons-Regular.ttf");
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        PlayerSettings.productName = GameTitle;

        var events = new GameObject("EventSystem", typeof(EventSystem));
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        Undo.RegisterCreatedObjectUndo(events, "Build Menu");

        var canvasGo = new GameObject("Main Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Menu");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // Backdrop: one solid deep-teal color.
        var shade = Rect("Background", root, Vector2.zero, Vector2.one);
        var shadeImg = shade.gameObject.AddComponent<Image>();
        shadeImg.color = new Color(0.035f, 0.075f, 0.085f, 1f);
        shadeImg.raycastTarget = false;

        // Title block.
        var kicker = Label("Kicker", root, "A TINY ADVENTURE IN A VERY BIG STORE", bodyFont, 30, Accent, TextAnchor.LowerLeft);
        Place(kicker.rectTransform, new Vector2(144, 805), new Vector2(900, 50));
        var title = Label("Title", root, GameTitle.ToUpper(), titleFont, 210, Color.white, TextAnchor.UpperLeft);
        Place(title.rectTransform, new Vector2(132, 560), new Vector2(1000, 240));
        var sh = title.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.75f);
        sh.effectDistance = new Vector2(6, -8);
        var underline = Rect("Underline", root, Vector2.zero, Vector2.zero);
        Place(underline, new Vector2(144, 548), new Vector2(160, 8));
        Img(underline, Accent);

        // Buttons.
        var play = MakeButton(root, "Play Button", "PLAY", GroceryStoreBuilderGlyph("play"), new Vector2(140, 410), primary: true);
        var settings = MakeButton(root, "Settings Button", "SETTINGS", GroceryStoreBuilderGlyph("settings"), new Vector2(140, 305), primary: false);
        var quit = MakeButton(root, "Quit Button", "QUIT", GroceryStoreBuilderGlyph("close"), new Vector2(140, 200), primary: false);

        var footer = Label("Footer", root, "v0.1   •   WASD MOVE   •   SPACE JUMP   •   E PICK UP", bodyFont, 22, new Color(1, 1, 1, 0.45f), TextAnchor.LowerLeft);
        Place(footer.rectTransform, new Vector2(140, 60), new Vector2(1100, 40));

        // Right: framed card for the character drawing (set Source Image on Character Art).
        var frame = Rect("Character Frame", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f));
        frame.pivot = new Vector2(1, 0.5f);
        frame.anchoredPosition = new Vector2(-140, 0);
        frame.sizeDelta = new Vector2(620, 760);
        Img(frame, new Color(1, 1, 1, 0.92f));
        var frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0, 0, 0, 0.6f);
        frameShadow.effectDistance = new Vector2(10, -14);
        frame.localRotation = Quaternion.Euler(0, 0, -2.5f);
        var art = Rect("Character Art", frame, Vector2.zero, Vector2.one);
        art.offsetMin = new Vector2(22, 80);
        art.offsetMax = new Vector2(-22, -22);
        var artImg = art.gameObject.AddComponent<Image>();
        artImg.color = new Color(0.12f, 0.16f, 0.17f, 1f);
        artImg.preserveAspect = true;
        var placeholder = Label("Placeholder Text", art, "YOUR HENRY DRAWING\nGOES HERE", titleFont, 46, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter);
        Stretch(placeholder.rectTransform);
        var caption = Label("Caption", frame, "HENRY, 11 CM TALL", titleFont, 40, Ink, TextAnchor.MiddleCenter);
        caption.rectTransform.anchorMin = new Vector2(0, 0);
        caption.rectTransform.anchorMax = new Vector2(1, 0);
        caption.rectTransform.anchoredPosition = new Vector2(0, 42);
        caption.rectTransform.sizeDelta = new Vector2(0, 60);

        // Settings: a dimmed overlay with a centered card.
        var settingsPanel = Rect("Settings Panel", root, Vector2.zero, Vector2.one).gameObject;
        Img(settingsPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0.7f)).sprite = null;
        var card = Rect("Card", settingsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(760, 520);
        Img(card, new Color(0.06f, 0.09f, 0.1f, 0.97f));
        card.gameObject.AddComponent<Outline>().effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.4f);
        var sTitle = Label("Settings Title", card, "SETTINGS", titleFont, 90, Color.white, TextAnchor.UpperCenter);
        sTitle.rectTransform.anchorMin = new Vector2(0, 1);
        sTitle.rectTransform.anchorMax = new Vector2(1, 1);
        sTitle.rectTransform.anchoredPosition = new Vector2(0, -40);
        sTitle.rectTransform.sizeDelta = new Vector2(0, 110);
        var sBody = Label("Settings Body", card, "Volume, sensitivity and graphics options are coming soon.", bodyFont, 30, new Color(0.8f, 0.85f, 0.85f), TextAnchor.MiddleCenter);
        Stretch(sBody.rectTransform);
        sBody.rectTransform.offsetMin = new Vector2(60, 140);
        sBody.rectTransform.offsetMax = new Vector2(-60, -160);
        var back = MakeButton(card, "Back Button", "BACK", GroceryStoreBuilderGlyph("close"), Vector2.zero, primary: true);
        var backRt = (RectTransform)back.transform;
        backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0);
        backRt.pivot = new Vector2(0.5f, 0);
        backRt.anchoredPosition = new Vector2(0, 50);
        settingsPanel.SetActive(false); // hidden until Settings is clicked (also keeps it out of the Game view in edit mode)

        var menu = canvasGo.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("spawnPoint").objectReferenceValue = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(settings.onClick, menu.OpenSettings);
        UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);
        UnityEventTools.AddPersistentListener(back.onClick, menu.CloseSettings);
        events.GetComponent<EventSystem>().firstSelectedGameObject = play.gameObject;
    }

    static string GroceryStoreBuilderGlyph(string name) => GroceryStoreBuilder.Glyph(name);


    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Bottom-left anchored box at a position on the 1920×1080 reference canvas.
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image Img(RectTransform rt, Color c)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = rounded;
        img.type = Image.Type.Sliced;
        img.color = c;
        return img;
    }

    static Text Label(string name, Transform parent, string text, Font f, int size, Color color, TextAnchor align)
    {
        var t = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = f;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    // Rounded pill button with an icon. Primary = solid accent; secondary = outlined ghost.
    static Button MakeButton(Transform parent, string name, string text, string glyph, Vector2 pos, bool primary)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
        Place(rt, pos, new Vector2(460, 84));
        var img = Img(rt, primary ? Accent : new Color(0.03f, 0.07f, 0.08f, 0.85f));
        if (!primary) rt.gameObject.AddComponent<Outline>().effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.55f);
        var shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(0, -6);

        var fg = primary ? Ink : Color.white;
        if (iconFont != null)
        {
            var icon = Label("Icon", rt, glyph, iconFont, 50, fg, TextAnchor.MiddleCenter);
            icon.rectTransform.anchorMin = new Vector2(0, 0);
            icon.rectTransform.anchorMax = new Vector2(0, 1);
            icon.rectTransform.pivot = new Vector2(0, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(26, 0);
            icon.rectTransform.sizeDelta = new Vector2(60, 0);
        }
        var label = Label("Text", rt, text, titleFont, 54, fg, TextAnchor.MiddleLeft);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(100, 0);

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.selectedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.colorMultiplier = 1.5f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        rt.gameObject.AddComponent<MenuButtonFx>();
        return button;
    }
}
