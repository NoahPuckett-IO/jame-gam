using UnityEngine;

// A wall of stacked cans that blocks the aisle. Every can is a kinematic rigidbody until Topple(),
// when they all become physical, get shoved away and stop blocking the player.
public class CanWall : MonoBehaviour
{
    const float Push = 2.5f; // gentle: the cans should tumble down, not fly up the aisle
    bool toppled;

    public void Topple(Vector3 direction)
    {
        if (toppled) return;
        toppled = true;
        foreach (var seal in GetComponents<Collider>()) seal.enabled = false; // the invisible strip across the hall

        var player = FindAnyObjectByType<PlayerController>();
        var playerCol = player != null ? player.GetComponent<Collider>() : null;

        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            float height = rb.position.y - transform.position.y;
            var dir = (direction.normalized + Vector3.up * 0.4f + Random.insideUnitSphere * 0.4f).normalized;
            rb.AddForce(dir * Push * (1f + height * 0.08f), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
            if (playerCol != null)
                foreach (var c in rb.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(playerCol, c);
        }
    }
}
