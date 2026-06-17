using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float scrollSpeed = 20f;
    public float scrollIntensity = 1f;

    public float lookSmooth = 15f;

    private Vector2 rotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? 1 : -1);

        rotation.x += mouseY;
        rotation.y += mouseX;
        rotation.x = Mathf.Clamp(rotation.x, -89f, 89f);

        Quaternion targetRot = Quaternion.Euler(rotation.x, rotation.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lookSmooth * Time.deltaTime);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            transform.position += transform.forward * scroll * scrollSpeed * scrollIntensity;
        }

        RenderSettings.skybox.SetFloat("_Rotation", Time.time * 0.15f);
    }

    public void SyncRotationFromTransform()
    {
        Vector3 euler = transform.rotation.eulerAngles;

        rotation.y = euler.y;
        rotation.x = euler.x;
        if (rotation.x > 180f)
            rotation.x -= 360f;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && this.enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}