using UnityEngine;

public class PlanetOrbit : MonoBehaviour
{
    public Transform sun;
    public float speed;
    public float spinSpeed;
    public float minSpeed = 5f;
    public float maxSpeed = 25f;
    public float minSpinSpeed = 10f;
    public float maxSpinSpeed = 60f;

    void Start()
    {
        speed = Random.Range(minSpeed, maxSpeed);
        spinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
    }

    void Update()
    {
        transform.RotateAround(sun.position, Vector3.up, speed * Time.deltaTime);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }
}