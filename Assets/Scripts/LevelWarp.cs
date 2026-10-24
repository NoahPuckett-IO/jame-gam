using UnityEngine;
using UnityEngine.InputSystem;

public class LevelWarp : MonoBehaviour
{
    [SerializeField] Transform[] starts;

    PlayerController player;

    void Start() => player = FindAnyObjectByType<PlayerController>();

    void Update()
    {
        if (!Debug.isDebugBuild || player == null || !player.enabled || player.InCutscene) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals, Key.LeftBracket, Key.RightBracket, Key.Backslash };
        for (int i = 0; i < keys.Length && i < starts.Length; i++)
        {
            if (!kb[keys[i]].wasPressedThisFrame || starts[i] == null) continue;
            player.Teleport(starts[i].position, starts[i].rotation);
            player.SetCheckpoint(starts[i].position, starts[i].rotation);
            player.SetLook(starts[i].eulerAngles.y, 15f);
        }
    }

    public void Setup(Transform[] levelStarts) => starts = levelStarts;
}
