using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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

        var shade = Rect("Background", root, Vector2.zero, Vector2.one);
        var shadeImg = shade.gameObject.AddComponent<Image>();
        shadeImg.color = new Color(0.035f, 0.075f, 0.085f, 1f);
        shadeImg.raycastTarget = false;

        var title = Label("Title", root, GameTitle.ToUpper(), titleFont, 210, Color.white, TextAnchor.UpperLeft);
        Place(title.rectTransform, new Vector2(132, 560), new Vector2(1000, 240));
        var sh = title.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.75f);
        sh.effectDistance = new Vector2(6, -8);
        var underline = Rect("Underline", root, Vector2.zero, Vector2.zero);
        Place(underline, new Vector2(144, 548), new Vector2(160, 8));
        Img(underline, Accent);

        var play = MakeButton(root, "Play Button", "PLAY", GroceryStoreBuilderGlyph("play"), new Vector2(140, 410), primary: true);
        var settings = MakeButton(root, "Settings Button", "SETTINGS", GroceryStoreBuilderGlyph("settings"), new Vector2(140, 305), primary: false);
        var quit = MakeButton(root, "Quit Button", "QUIT", GroceryStoreBuilderGlyph("close"), new Vector2(140, 200), primary: false);

        var footer = Label("Footer", root, "v0.1   •   WASD MOVE   •   SPACE JUMP   •   E PICK UP", bodyFont, 22, new Color(1, 1, 1, 0.45f), TextAnchor.LowerLeft);
        Place(footer.rectTransform, new Vector2(140, 60), new Vector2(1100, 40));

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
        var stage = BuildHenryStage(out var portrait);
        var artImg = art.gameObject.AddComponent<RawImage>();
        artImg.texture = portrait;
        artImg.raycastTarget = false;
        var caption = Label("Caption", frame, "HENRY, 11 CM TALL", titleFont, 40, Ink, TextAnchor.MiddleCenter);
        caption.rectTransform.anchorMin = new Vector2(0, 0);
        caption.rectTransform.anchorMax = new Vector2(1, 0);
        caption.rectTransform.anchoredPosition = new Vector2(0, 42);
        caption.rectTransform.sizeDelta = new Vector2(0, 60);

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
        settingsPanel.SetActive(false);

        var menu = canvasGo.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("spawnPoint").objectReferenceValue = spawn;
        so.FindProperty("stage").objectReferenceValue = stage;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(settings.onClick, menu.OpenSettings);
        UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);
        UnityEventTools.AddPersistentListener(back.onClick, menu.CloseSettings);
        events.GetComponent<EventSystem>().firstSelectedGameObject = play.gameObject;
    }

    static string GroceryStoreBuilderGlyph(string name) => GroceryStoreBuilder.Glyph(name);

    [MenuItem("JameGam/Debug/Rebuild Menu Henry %#&2")]
    static void RebuildStage()
    {
        var stage = BuildHenryStage(out _);
        Debug.Log("Menu Henry stage rebuilt");
        var menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null) return;
        var so = new SerializedObject(menu);
        so.FindProperty("stage").objectReferenceValue = stage;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject BuildHenryStage(out RenderTexture portrait)
    {
        var old = GameObject.Find("Menu Henry Stage");
        if (old != null) Object.DestroyImmediate(old);
        const string rtPath = "Assets/Settings/MenuHenry.renderTexture";
        portrait = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (portrait == null)
        {
            portrait = new RenderTexture(720, 822, 24) { name = "MenuHenry", antiAliasing = 4 };
            AssetDatabase.CreateAsset(portrait, rtPath);
        }

        var stage = new GameObject("Menu Henry Stage");
        Undo.RegisterCreatedObjectUndo(stage, "Build Menu");
        stage.transform.position = new Vector3(0, -600, 0);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSetup.ModelPath);
        if (prefab != null)
        {
            var henry = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
            henry.name = "Henry";
            henry.transform.localPosition = Vector3.zero;
            henry.transform.localRotation = Quaternion.identity;
            var mat = CharacterSetup.HenryMaterial();
            foreach (var r in henry.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = System.Linq.Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            var renderers = henry.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            henry.transform.localScale *= 2f / b.size.y;

            var anim = henry.GetComponent<Animator>();
            if (anim != null && anim.avatar != null && anim.avatar.isHuman)
            {
                CrossArms(anim);
                Object.DestroyImmediate(anim);
            }
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            henry.transform.position += Vector3.up * (stage.transform.position.y - b.min.y);
        }

        var camGo = new GameObject("Portrait Camera");
        camGo.transform.SetParent(stage.transform, false);
        camGo.transform.localPosition = new Vector3(-0.35f, 1.25f, 4.6f);
        camGo.transform.LookAt(stage.transform.position + new Vector3(0, 1.05f, 0));
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = portrait;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.22f, 0.24f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;
        cam.depth = -5;

        var key = new GameObject("Key Light").AddComponent<Light>();
        key.transform.SetParent(stage.transform, false);
        key.transform.localPosition = new Vector3(1.6f, 3f, 3f);
        key.type = LightType.Point;
        key.range = 10f;
        key.intensity = 22f;
        key.color = new Color(1f, 0.95f, 0.88f);
        var rim = new GameObject("Rim Light").AddComponent<Light>();
        rim.transform.SetParent(stage.transform, false);
        rim.transform.localPosition = new Vector3(-1.8f, 2.6f, -2f);
        rim.type = LightType.Point;
        rim.range = 8f;
        rim.intensity = 14f;
        rim.color = new Color(0.4f, 0.95f, 0.85f);
        return stage;
    }

    static void CrossArms(Animator anim)
    {
        var handler = new HumanPoseHandler(anim.avatar, anim.transform);
        var pose = new HumanPose();
        handler.GetHumanPose(ref pose);
        for (int i = 0; i < pose.muscles.Length; i++) pose.muscles[i] = 0f;
        void Set(string muscle, float v)
        {
            int i = System.Array.IndexOf(HumanTrait.MuscleName, muscle);
            if (i >= 0) pose.muscles[i] = v;
        }
        Set("Head Nod Down-Up", 0.05f);
        handler.SetHumanPose(ref pose);
        handler.Dispose();

        var body = anim.transform;
        Aim(anim, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -body.right, body, 0.08f);
        Aim(anim, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, body.right, body, -0.04f);
    }

    static void Aim(Animator anim, HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones handBone, Vector3 outward, Transform body, float lift)
    {
        var upper = anim.GetBoneTransform(upperBone);
        var lower = anim.GetBoneTransform(lowerBone);
        var hand = anim.GetBoneTransform(handBone);
        if (upper == null || lower == null || hand == null) return;
        var f = body.forward;
        var elbowDir = (Vector3.down * 0.85f + f * 0.5f + outward * 0.2f).normalized;
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbowDir) * upper.rotation;
        var foreDir = (-outward + f * 0.35f + Vector3.up * (0.25f + lift)).normalized;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, foreDir) * lower.rotation;
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
