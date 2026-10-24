using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    [SerializeField] PlayerController player;
    [SerializeField] Transform spawnPoint;
    [SerializeField] GameObject stage;

    void Start()
    {
        player.enabled = false;
        settingsPanel.SetActive(false);
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

    public void OpenSettings() => settingsPanel.SetActive(true);

    public void CloseSettings() => settingsPanel.SetActive(false);
}
