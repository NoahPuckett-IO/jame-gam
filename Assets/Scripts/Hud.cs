using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The key hint shown when something can be used, built in code on first use like the pause menu, so no scene
// object refers to it.
public class Hud : MonoBehaviour
{
    static Hud instance;

    CanvasGroup hintGroup;
    Text hintKey, hintText;
    float hintSeen = -1f;
    Object hintOwner;
    InputAction interact;

    static Hud Get()
    {
        if (instance == null) instance = new GameObject("Hud").AddComponent<Hud>().Build();
        return instance;
    }

    // call every frame while the hint should show; the key shown is whatever Interact is bound to
    public static void Hint(Object owner, string text)
    {
        var h = Get();
        if (h.hintSeen == Time.unscaledTime && h.hintOwner != owner) return;
        h.hintOwner = owner;
        h.hintSeen = Time.unscaledTime;
        h.hintText.text = text;
        h.hintKey.text = h.interact != null ? h.interact.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"), InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpper() : "E";
    }

    Hud Build()
    {
        interact = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Interact") : null;
        var canvas = UiKit.Canvas("Hud Canvas", 20);
        canvas.transform.SetParent(transform, false);

        var hint = UiKit.Rect(canvas.transform, "Hint", new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(420, 70));
        hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
        UiKit.Image(hint, new Color(0.06f, 0.09f, 0.1f, 0.85f));
        var chip = UiKit.Rect(hint, "Key", new Vector2(0f, 0.5f), new Vector2(50, 0), new Vector2(64, 50));
        UiKit.Image(chip, UiKit.Accent);
        hintKey = UiKit.Label(chip, "E", UiKit.TitleFont, 34, UiKit.Ink, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 50));
        hintText = UiKit.Label(hint, "", UiKit.TitleFont, 34, Color.white, TextAnchor.MiddleLeft, new Vector2(45, 0), new Vector2(300, 60));
        hintGroup.alpha = 0f;
        return this;
    }

    void Update()
    {
        bool show = !PauseMenu.Paused && !SettingsPanel.IsOpen && Time.unscaledTime - hintSeen < 0.1f;
        hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 6f);
    }
}
