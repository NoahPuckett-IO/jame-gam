using UnityEngine;

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
