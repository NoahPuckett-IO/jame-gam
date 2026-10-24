using System.Collections;
using UnityEngine;

// A stale cracker (or thin ice) that holds Henry for a moment, then cracks and drops away. It
// shakes as a warning, falls, and is back in place a few seconds later.
public class CrumblePlatform : MonoBehaviour
{
    [SerializeField] float holdSeconds = 0.9f;
    [SerializeField] float respawnSeconds = 3f;

    Vector3 home;
    Quaternion homeRot;
    Collider[] solids;
    Renderer[] renderers;
    bool busy;

    void Awake()
    {
        home = transform.position;
        homeRot = transform.rotation;
        var all = GetComponentsInChildren<Collider>();
        solids = System.Array.FindAll(all, c => !c.isTrigger);
        renderers = GetComponentsInChildren<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!busy && other.GetComponent<PlayerController>() != null) StartCoroutine(Crumble());
    }

    IEnumerator Crumble()
    {
        busy = true;
        for (float t = 0; t < holdSeconds; t += Time.deltaTime)
        {
            float shake = 0.06f + 0.12f * t / holdSeconds;
            transform.SetPositionAndRotation(home + Random.insideUnitSphere * shake,
                homeRot * Quaternion.Euler(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)));
            yield return null;
        }
        foreach (var c in solids) c.enabled = false;
        var spin = Random.onUnitSphere;
        float v = 0f;
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            v += 30f * Time.deltaTime;
            transform.position += Vector3.down * v * Time.deltaTime;
            transform.Rotate(spin, 90f * Time.deltaTime, Space.World);
            yield return null;
        }
        foreach (var r in renderers) r.enabled = false;
        yield return new WaitForSeconds(respawnSeconds);
        transform.SetPositionAndRotation(home, homeRot);
        foreach (var r in renderers) r.enabled = true;
        foreach (var c in solids) c.enabled = true;
        busy = false;
    }
}
