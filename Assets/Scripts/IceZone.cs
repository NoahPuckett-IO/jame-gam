using System.Collections.Generic;
using UnityEngine;

// a volume where the floor is slippery. The player asks each frame whether its feet are inside one, instead of
// counting trigger enters and exits: riding a slide or zipline switches the player's collider off, so an exit
// can be missed and the ice would follow Henry onto normal floor
[RequireComponent(typeof(Collider))]
public class IceZone : MonoBehaviour
{
    static readonly List<IceZone> zones = new List<IceZone>();
    Collider area;

    void OnEnable()
    {
        area = GetComponent<Collider>();
        zones.Add(this);
    }

    void OnDisable() => zones.Remove(this);

    public static bool Contains(Vector3 point)
    {
        foreach (var z in zones)
            if (z.area != null && z.area.enabled && z.area.bounds.Contains(point)) return true;
        return false;
    }
}
