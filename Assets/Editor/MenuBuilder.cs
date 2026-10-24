using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds the title screen canvas: title + Play/Settings on the left, character art on the right.
// Called from GroceryStoreBuilder.
static class MenuBuilder
{
    public const string GameTitle = "Tiny Henry";
    static Font font;

    public static void Build(PlayerController player, Transform spawn)
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BebasNeue-Regular.ttf")
               ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        PlayerSettings.productName = GameTitle;

        var events = new GameObject("EventSystem", typeof(EventSystem));
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        Undo.RegisterCreatedObjectUndo(events, "Build Menu");

        var canvasGo = new GameObject("Main Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Menu");
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        Panel("Background", root, Vector2.zero, Vector2.one, new Color(0.08f, 0.09f, 0.12f, 1f));

        // Left side
        Label("Title", root, GameTitle.ToUpper(), 150, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.62f), new Vector2(0.55f, 0.88f));
        var play = MakeButton("Play Button", root, "Play", new Vector2(0.06f, 0.42f), new Vector2(0.3f, 0.52f));
        var settings = MakeButton("Settings Button", root, "Settings", new Vector2(0.06f, 0.29f), new Vector2(0.3f, 0.39f));

        // Right side: drop the character drawing into this Image's Source Image.
        var art = Panel("Character Art", root, new Vector2(0.55f, 0.1f), new Vector2(0.94f, 0.9f), new Color(1, 1, 1, 0.08f));
        art.GetComponent<Image>().preserveAspect = true;
        Label("Placeholder Text", art.transform, "Character drawing goes here\n(set Source Image on Character Art)",
            32, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

        // Settings placeholder panel
        var settingsPanel = Panel("Settings Panel", root, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f), new Color(0.15f, 0.16f, 0.2f, 0.98f));
        Label("Settings Title", settingsPanel.transform, "Settings\n(coming soon)", 60, TextAnchor.MiddleCenter, new Vector2(0, 0.4f), Vector2.one);
        var back = MakeButton("Back Button", settingsPanel.transform, "Back", new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.3f));

        var menu = canvasGo.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("spawnPoint").objectReferenceValue = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(settings.onClick, menu.OpenSettings);
        UnityEventTools.AddPersistentListener(back.onClick, menu.CloseSettings);
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var rt = Rect(name, parent, min, max);
        rt.gameObject.AddComponent<Image>().color = color;
        return rt.gameObject;
    }

    static Text Label(string name, Transform parent, string text, int size, TextAnchor align, Vector2 min, Vector2 max)
    {
        var t = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max)
    {
        var go = Panel(name, parent, min, max, new Color(0.25f, 0.45f, 0.75f));
        Label("Text", go.transform, text, 48, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        return go.AddComponent<Button>();
    }
}
