using UnityEngine;

public class AtmosphereTrigger : MonoBehaviour
{
    public float atmosphereRadius = 100f;

    void OnTriggerEnter(Collider other)
    {
        OrbitalCameraController orbital = other.GetComponent<OrbitalCameraController>();

        if (orbital == null)
            orbital = other.GetComponentInParent<OrbitalCameraController>();

        if (orbital == null)
            return;

        orbital.BeginOrbit(transform.parent, atmosphereRadius);
    }
}