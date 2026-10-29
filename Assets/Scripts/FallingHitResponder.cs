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

    public void Kill(Vector3 away)
    {
        if (!busy) StartCoroutine(Die(away));
    }

    IEnumerator Die(Vector3 away)
    {
        busy = true;
        var player = GetComponent<PlayerController>();
        var anim = GetComponent<PlayerAnimator>();
        player.Knock(away * 14f + Vector3.up * 6f);
        player.LockMovement(2.2f);
        anim?.Play("Death", 0.1f);
        Sfx.Play("hurt", 0.45f);
        PlayerController.Shake(0.5f);
        yield return new WaitForSeconds(1.4f);
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.Fade(1f, 0.3f);
        player.Respawn();
        anim?.Resume();
        if (fader != null) yield return fader.Fade(0f, 0.4f);
        busy = false;
    }

    IEnumerator Run(Vector3 away)
    {
        busy = true;
        var player = GetComponent<PlayerController>();
        GetComponent<PlayerAnimator>()?.PlayHit();
        Sfx.Play("hurt", 0.45f);
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
