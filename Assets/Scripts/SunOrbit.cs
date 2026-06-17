using UnityEngine;

public class SunOrbit : MonoBehaviour
{
    public float spinSpeed;
    public float minSpinSpeed = 10f;
    public float maxSpinSpeed = 60f;

    void Start()
    {
        spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }
}