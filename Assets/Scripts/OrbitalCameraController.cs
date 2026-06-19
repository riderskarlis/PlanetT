using UnityEngine;

public class OrbitalCameraController : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 50f;
    public float minDistance = 10f;
    public float atmosphereRadius = 100f;
    public MonoBehaviour simpleCameraController;

    [Header("Audio Settings")]
    public AudioClip scrollTickSound;

    private float lastScrollSoundTime = 0f;

    float distance;
    Vector3 orbitDirection;
    Quaternion lastPlanetRotation;

    void Start()
    {
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            rotationSpeed = 100f * (PlayerPrefs.GetFloat("MouseSensitivity") / 2f);
        }
    }

    void OnEnable()
    {
        if (target == null)
            return;

        // Show cursor when orbiting a planet
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        lastPlanetRotation = target.rotation;

        UpdateMinDistance();

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

    void LateUpdate()
    {
        if (target == null)
            return;

        // Parent to target's parent (the planet) for perfect sync
        if (transform.parent != target.parent)
        {
            transform.SetParent(target.parent);
        }

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
        if (right.sqrMagnitude < 0.0001f)
        {
            // At the poles, fall back to the camera's local right vector to prevent gimbal lock
            right = transform.right;
        }
        else
        {
            right = right.normalized;
        }

        if (Mathf.Abs(v) > 0.0001f)
        {
            orbitDirection = Quaternion.AngleAxis(
                -v * rotationSpeed * Time.deltaTime,
                right
            ) * orbitDirection;
        }

        orbitDirection.Normalize();

        // Exponential relative zoom: zoom speed scales with distance for smooth control near surface
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f)
        {
            float sensitivity = (zoomSpeed / 50f) * 0.1f;
            float zoomFactor = 1f - Input.mouseScrollDelta.y * sensitivity;
            zoomFactor = Mathf.Clamp(zoomFactor, 0.5f, 2.0f);
            distance *= zoomFactor;

            PlayScrollTickSound();
        }
        distance = Mathf.Max(minDistance, distance);

        // Calculate position relative to parent
        transform.position = target.position + orbitDirection * distance;
        transform.LookAt(target.position);

        if (distance > atmosphereRadius)
        {
            transform.SetParent(null); // Detach when leaving orbit
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

                // Lock cursor back when returning to free-look
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void UpdateMinDistance()
    {
        if (target != null)
        {
            LowPolyPlanet planet = target.GetComponent<LowPolyPlanet>();
            if (planet != null)
            {
                // dynamically prevent clipping through peaks: terrain max height is radius + heightMultiplier
                minDistance = planet.radius + planet.heightMultiplier + 5f;
            }
            else
            {
                minDistance = 10f;
            }
        }
    }

    public void BeginOrbit(Transform newTarget, float newAtmosphereRadius)
    {
        target = newTarget;
        atmosphereRadius = newAtmosphereRadius;

        if (target == null)
            return;

        // Show cursor when orbiting
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateMinDistance();

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

    private void PlayScrollTickSound()
    {
        if (scrollTickSound != null && Time.unscaledTime - lastScrollSoundTime > 0.12f)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(scrollTickSound);
                lastScrollSoundTime = Time.unscaledTime;
            }
        }
    }
}
