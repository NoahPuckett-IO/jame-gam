using UnityEngine;

// Trigger volume at a turning point. Walking into it makes its `spawn` the player's respawn point.
[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] Transform spawn;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null && spawn != null) player.SetCheckpoint(spawn.position, spawn.rotation);
    }

    public void Setup(Transform spawnPoint) => spawn = spawnPoint;
}
