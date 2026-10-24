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
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
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
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        UiSounds.Open();
    }

    public void Resume()
    {
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        canvas.gameObject.SetActive(false);
        if (player != null) player.enabled = true;
        UiSounds.Back();
    }

    void OpenSettings()
    {
        canvas.gameObject.SetActive(false);
        settings.Open(() => canvas.gameObject.SetActive(true));
    }

    void ToMenu()
    {
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
        UiKit.Image(UiKit.Fill(root, "Dim"), new Color(0, 0, 0, 0.6f), false);
        var card = UiKit.Rect(root, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 700));
        UiKit.Image(card, UiKit.Card);
        card.gameObject.AddComponent<Outline>().effectColor = new Color(UiKit.Accent.r, UiKit.Accent.g, UiKit.Accent.b, 0.35f);
        UiKit.Label(card, "PAUSED", UiKit.TitleFont, 100, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(560, 120));
        UiKit.Button(card, "RESUME", new Vector2(0, 110), new Vector2(440, 80), true, Resume, MenuButtonFx.Sound.Confirm);
        UiKit.Button(card, "SETTINGS", new Vector2(0, 10), new Vector2(440, 80), false, OpenSettings);
        UiKit.Button(card, "MAIN MENU", new Vector2(0, -90), new Vector2(440, 80), false, ToMenu);
        UiKit.Button(card, "QUIT GAME", new Vector2(0, -190), new Vector2(440, 80), false, Quit, MenuButtonFx.Sound.Back);
    }
}
