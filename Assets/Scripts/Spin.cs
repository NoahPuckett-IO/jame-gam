using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] Vector3 degreesPerSecond = new Vector3(0, 60, 0);

    void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);

    public void Setup(Vector3 speed) => degreesPerSecond = speed;
}
