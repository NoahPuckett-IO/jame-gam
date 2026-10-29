using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Transform spawnPoint;
    [SerializeField] GameObject stage;

    void Start()
    {
        player.enabled = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // quitting does nothing in a browser tab, so the web build leaves the button out
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            foreach (var b in GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (b.name == "Quit Button") b.gameObject.SetActive(false);
        // coming back from the game the screen is black; ease the menu in
        var fader = ScreenFader.Instance;
        if (fader != null)
        {
            fader.Set(1f);
            StartCoroutine(fader.Fade(0f, 0.5f));
        }
    }

    public void Play()
    {
        gameObject.SetActive(false);
        if (stage != null) stage.SetActive(false);
        var intro = FindAnyObjectByType<IntroCutscene>();
        if (intro != null)
        {
            intro.Play();
            return;
        }
        player.Teleport(spawnPoint.position, spawnPoint.rotation);
        player.SetCheckpoint(spawnPoint.position, spawnPoint.rotation);
        player.enabled = true;
    }

    // the white practice room: no intro, straight in
    public void Tutorial()
    {
        var room = FindAnyObjectByType<TutorialRoom>();
        if (room == null)
        {
            Play();
            return;
        }
        gameObject.SetActive(false);
        if (stage != null) stage.SetActive(false);
        room.Begin(player);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        FindAnyObjectByType<SettingsPanel>()?.Open(() =>
        {
            if (events != null) events.SetSelectedGameObject(events.firstSelectedGameObject);
        });
    }
}
