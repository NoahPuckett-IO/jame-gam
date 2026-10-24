using UnityEngine;

// Title screen. The player starts disabled; Play hides the menu, moves the player to the
// spawn point and hands over control.
public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    [SerializeField] PlayerController player;
    [SerializeField] Transform spawnPoint;

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
