using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class FallingHitResponder : MonoBehaviour
{
    bool busy;

    public void Hit(Vector3 away)
    {
        if (!busy) StartCoroutine(Run(away));
    }

    IEnumerator Run(Vector3 away)
    {
        busy = true;
        var player = GetComponent<PlayerController>();
        GetComponent<PlayerAnimator>()?.PlayHit();
        player.Knock(away * 18f + Vector3.up * 8f);
        player.LockMovement(1f);
        yield return new WaitForSeconds(0.5f);
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.Fade(1f, 0.25f);
        player.Respawn();
        if (fader != null) yield return fader.Fade(0f, 0.35f);
        busy = false;
    }
}
