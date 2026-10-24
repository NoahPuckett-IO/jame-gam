using UnityEngine;

// Something resting on the long end of a Seesaw. Each frame the seesaw reports where the top of
// its tip is, and the load moves so it keeps sitting on it.
public abstract class LeverLoad : MonoBehaviour
{
    public abstract void OnTip(Vector3 tipTop);
}
