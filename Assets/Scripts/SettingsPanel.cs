using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] Font titleFont, bodyFont;
    [SerializeField] Sprite rounded, knob;

    static readonly (string label, string action, string part)[] Keys =
    {
        ("MOVE FORWARD", "Player/Move", "up"),
        ("MOVE BACK", "Player/Move", "down"),
        ("MOVE LEFT", "Player/Move", "left"),
        ("MOVE RIGHT", "Player/Move", "right"),
        ("JUMP", "Player/Jump", null),
        ("DASH", "Player/Sprint", null),
        ("PICK UP / DROP", "Player/Interact", null),
    };

    public static bool IsOpen { get; private set; }
    public static bool Rebinding { get; private set; }
    public static int ClosedFrame { get; private set; } = -1;

    Canvas canvas;
    readonly List<(Text text, InputAction action, int index)> keyLabels = new List<(Text, InputAction, int)>();
    System.Action onClose;
    InputActionRebindingExtensions.RebindingOperation op;

    void Awake()
    {
        UiKit.TitleFont = titleFont;
        UiKit.BodyFont = bodyFont;
        UiKit.Rounded = rounded;
        UiKit.Knob = knob;
    }

    public void Open(System.Action closed = null)
    {
        onClose = closed;
        if (canvas == null) Build();
        canvas.gameObject.SetActive(true);
        IsOpen = true;
        Refresh();
        UiSounds.Open();
    }

    public void Close()
    {
        if (Rebinding) return;
        canvas.gameObject.SetActive(false);
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        onClose?.Invoke();
    }

    void Update()
    {
        if (!IsOpen || Rebinding || ClosedFrame == Time.frameCount) return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            UiSounds.Back();
            Close();
        }
    }

    void Build()
    {
        canvas = UiKit.Canvas("Settings", 60);
        canvas.transform.SetParent(transform, false);
        var root = canvas.transform;
        var dim = UiKit.Fill(root, "Dim");
        UiKit.Image(dim, new Color(0, 0, 0, 0.75f), false);

        var card = UiKit.Rect(root, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 800));
        UiKit.Image(card, UiKit.Card);
        card.gameObject.AddComponent<Outline>().effectColor = new Color(UiKit.Accent.r, UiKit.Accent.g, UiKit.Accent.b, 0.35f);
        UiKit.Label(card, "SETTINGS", titleFont, 90, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 330), new Vector2(800, 110));

        UiKit.Label(card, "AUDIO", titleFont, 44, UiKit.Accent, TextAnchor.MiddleLeft, new Vector2(-300, 230), new Vector2(520, 60));
        Row(card, "MASTER VOLUME", 160, GameSettings.Master, v => GameSettings.Master = v);
        Row(card, "MUSIC", 85, GameSettings.Music, v => GameSettings.Music = v);
        Row(card, "SOUND EFFECTS", 10, GameSettings.Sfx, v => GameSettings.Sfx = v);
        UiKit.Label(card, "MOUSE & SCREEN", titleFont, 44, UiKit.Accent, TextAnchor.MiddleLeft, new Vector2(-300, -80), new Vector2(520, 60));
        Row(card, "LOOK SENSITIVITY", -150, Mathf.InverseLerp(0.25f, 2.5f, GameSettings.Sensitivity),
            v => GameSettings.Sensitivity = Mathf.Lerp(0.25f, 2.5f, v));
        UiKit.Label(card, "FULLSCREEN", bodyFont, 28, new Color(0.85f, 0.9f, 0.9f), TextAnchor.MiddleLeft, new Vector2(-405, -225), new Vector2(310, 50));
        var full = UiKit.Button(card, "", new Vector2(-135, -225), new Vector2(200, 50), false, null, MenuButtonFx.Sound.Click, 30);
        fullLabel = full.transform.Find("Label").GetComponent<Text>();
        full.onClick.AddListener(() =>
        {
            Screen.fullScreen = !Screen.fullScreen;
            // the switch lands next frame, so show where it's going rather than where it is
            fullLabel.text = Screen.fullScreen ? "OFF" : "ON";
        });

        UiKit.Label(card, "CONTROLS", titleFont, 44, UiKit.Accent, TextAnchor.MiddleLeft, new Vector2(300, 230), new Vector2(520, 60));
        float y = 168f;
        foreach (var (label, actionName, part) in Keys)
        {
            var action = InputSystem.actions.FindAction(actionName);
            int index = action != null ? FindBinding(action, part) : -1;
            if (index < 0) continue;
            UiKit.Label(card, label, bodyFont, 28, new Color(0.85f, 0.9f, 0.9f), TextAnchor.MiddleLeft, new Vector2(195, y), new Vector2(310, 50));
            var b = UiKit.Button(card, "", new Vector2(450, y), new Vector2(200, 50), false, null, MenuButtonFx.Sound.Click, 30);
            var text = b.transform.Find("Label").GetComponent<Text>();
            b.onClick.AddListener(() => StartRebind(action, index, text));
            keyLabels.Add((text, action, index));
            y -= 62f;
        }

        UiKit.Button(card, "RESET CONTROLS", new Vector2(-220, -320), new Vector2(360, 72), false, () =>
        {
            GameSettings.ResetBindings();
            Refresh();
        });
        UiKit.Button(card, "BACK", new Vector2(220, -320), new Vector2(360, 72), true, Close, MenuButtonFx.Sound.Back);
    }

    void Row(RectTransform card, string label, float y, float value, UnityEngine.Events.UnityAction<float> set)
    {
        UiKit.Label(card, label, bodyFont, 28, new Color(0.85f, 0.9f, 0.9f), TextAnchor.MiddleLeft, new Vector2(-405, y), new Vector2(310, 50));
        UiKit.Slider(card, new Vector2(-135, y), 300, value, set);
    }

    static int FindBinding(InputAction action, string part)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite) continue;
            if (part != null && (!b.isPartOfComposite || b.name != part)) continue;
            if (part == null && b.isPartOfComposite) continue;
            if (b.path.StartsWith("<Keyboard>")) return i;
        }
        return -1;
    }

    Text fullLabel;

    void Refresh()
    {
        if (fullLabel != null) fullLabel.text = Screen.fullScreen ? "ON" : "OFF";
        foreach (var (text, action, index) in keyLabels)
            text.text = action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpper();
    }

    void StartRebind(InputAction action, int index, Text text)
    {
        if (Rebinding) return;
        Rebinding = true;
        text.text = "PRESS A KEY";
        bool wasEnabled = action.enabled;
        action.Disable();
        op = action.PerformInteractiveRebinding(index)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.05f)
            .OnComplete(o => Finish(action, wasEnabled, true))
            .OnCancel(o => Finish(action, wasEnabled, false))
            .Start();
    }

    void Finish(InputAction action, bool enable, bool saved)
    {
        op?.Dispose();
        op = null;
        if (enable) action.Enable();
        if (saved) GameSettings.SaveBindings();
        Refresh();
        Rebinding = false;
        ClosedFrame = Time.frameCount;
    }

    public void Setup(Font title, Font body, Sprite roundedSprite, Sprite knobSprite)
    {
        titleFont = title;
        bodyFont = body;
        rounded = roundedSprite;
        knob = knobSprite;
    }
}
