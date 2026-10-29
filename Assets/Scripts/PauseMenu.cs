using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(SettingsPanel))]
public class PauseMenu : MonoBehaviour
{
    PlayerController player;
    SettingsPanel settings;
    Canvas canvas;
    CanvasGroup group;
    RectTransform card;
    bool leaving;

    public static bool Paused { get; private set; }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        settings = GetComponent<SettingsPanel>();
        Paused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Paused && group != null && group.alpha < 1f)
        {
            // eases in on the unscaled clock, since time is stopped
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime * 8f);
            card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, group.alpha);
        }
        var kb = Keyboard.current;
        if (leaving || kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (SettingsPanel.IsOpen || SettingsPanel.Rebinding || SettingsPanel.ClosedFrame == Time.frameCount) return;
        if (Paused) Resume();
        else if (player != null && player.enabled && !player.InCutscene) Pause();
    }

    void Pause()
    {
        if (canvas == null) Build();
        Paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        player.enabled = false;
        canvas.gameObject.SetActive(true);
        group.alpha = 0f;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        UiSounds.Open();
    }

    public void Resume()
    {
        if (leaving) return;
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        canvas.gameObject.SetActive(false);
        if (player != null) player.enabled = true;
        UiSounds.Back();
    }

    // for when Henry is stuck somewhere: straight back to the last checkpoint
    void Restart()
    {
        if (leaving) return;
        Resume();
        if (player != null && !player.InCutscene) StartCoroutine(BackToCheckpoint());
    }

    IEnumerator BackToCheckpoint()
    {
        leaving = true;
        player.LockMovement(0.6f);
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.Fade(1f, 0.25f);
        player.Respawn();
        if (fader != null) yield return fader.Fade(0f, 0.35f);
        leaving = false;
    }

    void OpenSettings()
    {
        canvas.gameObject.SetActive(false);
        settings.Open(() => canvas.gameObject.SetActive(true));
    }

    void ToMenu()
    {
        if (leaving) return;
        StartCoroutine(FadeToMenu());
    }

    IEnumerator FadeToMenu()
    {
        leaving = true;
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.Fade(1f, 0.35f);
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Build()
    {
        canvas = UiKit.Canvas("Pause Menu", 50);
        canvas.transform.SetParent(transform, false);
        var root = canvas.transform;
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        UiKit.Image(UiKit.Fill(root, "Dim"), new Color(0, 0, 0, 0.6f), false);
        // the browser build has no quit button: closing the tab does that
        bool canQuit = Application.platform != RuntimePlatform.WebGLPlayer;
        card = UiKit.Rect(root, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, canQuit ? 740 : 640));
        UiKit.Image(card, UiKit.Card);
        card.gameObject.AddComponent<Outline>().effectColor = new Color(UiKit.Accent.r, UiKit.Accent.g, UiKit.Accent.b, 0.35f);
        float y = (canQuit ? 740 : 640) / 2f - 85f;
        UiKit.Label(card, "PAUSED", UiKit.TitleFont, 100, Color.white, TextAnchor.MiddleCenter, new Vector2(0, y), new Vector2(560, 120));
        y -= 140;
        UiKit.Button(card, "RESUME", new Vector2(0, y), new Vector2(440, 80), true, Resume, MenuButtonFx.Sound.Confirm);
        UiKit.Button(card, "UNSTUCK", new Vector2(0, y -= 100), new Vector2(440, 80), false, Restart, MenuButtonFx.Sound.Confirm);
        UiKit.Button(card, "SETTINGS", new Vector2(0, y -= 100), new Vector2(440, 80), false, OpenSettings);
        UiKit.Button(card, "MAIN MENU", new Vector2(0, y -= 100), new Vector2(440, 80), false, ToMenu);
        if (canQuit) UiKit.Button(card, "QUIT GAME", new Vector2(0, y -= 100), new Vector2(440, 80), false, Quit, MenuButtonFx.Sound.Back);
    }
}
