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
