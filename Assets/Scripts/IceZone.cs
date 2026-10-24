using UnityEngine;

// While Henry is inside this volume the ground is ice: he keeps sliding and turns slowly.
[RequireComponent(typeof(Collider))]
public class IceZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponent<PlayerController>();
        if (p != null) p.EnterIce();
    }

    void OnTriggerExit(Collider other)
    {
        var p = other.GetComponent<PlayerController>();
        if (p != null) p.ExitIce();
    }
}
