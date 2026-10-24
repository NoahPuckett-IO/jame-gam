using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hazard : MonoBehaviour
{
    bool busy;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null || busy || player.InCutscene) return;
        StartCoroutine(Reset(player));
    }

    IEnumerator Reset(PlayerController player)
    {
        busy = true;
        var fader = ScreenFader.Instance;
        player.LockMovement(0.6f);
        if (fader != null) yield return fader.Fade(1f, 0.25f);
        player.Respawn();
        if (fader != null) yield return fader.Fade(0f, 0.35f);
        busy = false;
    }
}
