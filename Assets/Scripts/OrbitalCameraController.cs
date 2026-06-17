using UnityEngine;

public class OrbitalCameraController : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 50f;
    public float minDistance = 10f;
    public float atmosphereRadius = 100f;
    public MonoBehaviour simpleCameraController;

    float distance;
    Vector3 orbitDirection;
    Quaternion lastPlanetRotation;

    void OnEnable()
    {
        if (target == null)
            return;

        lastPlanetRotation = target.rotation;

        Vector3 dir = transform.position - target.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            orbitDirection = dir.normalized;
            distance = dir.magnitude;
        }
        else
        {
            orbitDirection = Vector3.forward;
            distance = minDistance;
        }
    }

    void Update()
    {
        if (target == null)
            return;

        Quaternion rotationDelta = target.rotation * Quaternion.Inverse(lastPlanetRotation);
        orbitDirection = rotationDelta * orbitDirection;
        lastPlanetRotation = target.rotation;

        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A)) h = 1f;
        if (Input.GetKey(KeyCode.D)) h = -1f;
        if (Input.GetKey(KeyCode.W)) v = -1f;
        if (Input.GetKey(KeyCode.S)) v = 1f;

        if (Mathf.Abs(h) > 0.0001f)
        {
            orbitDirection = Quaternion.AngleAxis(
                h * rotationSpeed * Time.deltaTime,
                Vector3.up
            ) * orbitDirection;
        }

        Vector3 right = Vector3.Cross(orbitDirection, Vector3.up);
        if (right.sqrMagnitude > 0.0001f)
        {
            right = right.normalized;

            if (Mathf.Abs(v) > 0.0001f)
            {
                orbitDirection = Quaternion.AngleAxis(
                    -v * rotationSpeed * Time.deltaTime,
                    right
                ) * orbitDirection;
            }
        }

        orbitDirection.Normalize();

        distance -= Input.mouseScrollDelta.y * zoomSpeed;
        distance = Mathf.Max(minDistance, distance);

        transform.position = target.position + orbitDirection * distance;
        transform.LookAt(target.position);

        if (distance > atmosphereRadius)
        {
            enabled = false;

            if (simpleCameraController != null)
            {
                SimpleCameraController cam =
                    simpleCameraController as SimpleCameraController;

                if (cam != null)
                {
                    cam.SyncRotationFromTransform();
                }

                simpleCameraController.enabled = true;

                //Mouse turn off
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

            }
        }
    }

    public void BeginOrbit(Transform newTarget, float newAtmosphereRadius)
    {
        target = newTarget;
        atmosphereRadius = newAtmosphereRadius;

        if (target == null)
            return;

        //Mouse turn on
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Vector3 dir = transform.position - target.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            orbitDirection = dir.normalized;
            distance = dir.magnitude;
        }
        else
        {
            orbitDirection = Vector3.forward;
            distance = minDistance;
        }

        if (simpleCameraController != null)
            simpleCameraController.enabled = false;

        enabled = true;
    }
}