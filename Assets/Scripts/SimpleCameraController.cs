using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float scrollSpeed = 20f;
    public float scrollIntensity = 1f;
    public float panSpeed = 0.4f;
    public float lookSmooth = 15f;

    public static bool BlockLook { get; set; }
    public static bool IsCtrlHeld =>
        Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

    Vector2 rotation;
    Plane yZeroPlane = new Plane(Vector3.up, Vector3.zero);

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        }
    }

    void Update()
    {
        bool ctrl = IsCtrlHeld;

        if (ctrl)
        {
            // Show cursor when holding Ctrl (for selection/lasso)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetMouseButton(1))
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                Vector3 right = transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                transform.position += (-right * mx - forward * my) * panSpeed;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (yZeroPlane.Raycast(ray, out float dist))
            {
                Vector3 hit = ray.GetPoint(dist);
                Debug.DrawLine(transform.position, hit, Color.yellow);
                RenderSelection.SetHoverPoint(hit, true);
            }
            else
            {
                RenderSelection.SetHoverPoint(Vector3.zero, false);
            }
        }
        else
        {
            RenderSelection.SetHoverPoint(Vector3.zero, false);

            if (!BlockLook)
            {
                // Lock cursor for camera look
                if (Time.timeScale > 0) // Only lock if not paused
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? 1 : -1);

                rotation.x += mouseY;
                rotation.y += mouseX;
                rotation.x = Mathf.Clamp(rotation.x, -89f, 89f);

                Quaternion targetRot = Quaternion.Euler(rotation.x, rotation.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lookSmooth * Time.deltaTime);
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            transform.position += transform.forward * scroll * scrollSpeed * scrollIntensity;
        }

        if (transform.position.y < 0f)
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);

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
        if (hasFocus && enabled && !IsCtrlHeld && !BlockLook && Time.timeScale > 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
