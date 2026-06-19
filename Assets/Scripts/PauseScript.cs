using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseScript : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public string mainMenuScene = "Menu";
    
    [Header("FPS Counter")]
    public Text fpsText;
    public float fpsUpdateInterval = 0.5f;
    
    private bool isPaused = false;
    private float fpsAccumulator = 0f;
    private int fpsFramesCount = 0;
    private float fpsTimeLeft = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        fpsTimeLeft = fpsUpdateInterval;

        // Disable FPS counter UI by default; user toggles it with F3
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Toggle FPS Counter with F3
        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (fpsText != null)
            {
                fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
            }
        }

        // Take screenshot with F11
        if (Input.GetKeyDown(KeyCode.F11))
        {
            TakeScreenshot();
        }

        UpdateFPSCounter();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void UpdateFPSCounter()
    {
        if (fpsText == null || !fpsText.gameObject.activeSelf) return;

        fpsTimeLeft -= Time.unscaledDeltaTime;
        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFramesCount++;

        if (fpsTimeLeft <= 0f)
        {
            float fps = fpsAccumulator > 0f ? (fpsFramesCount / fpsAccumulator) : 0f;
            fpsText.text = string.Format("FPS: {0:F0}", fps);

            fpsTimeLeft = fpsUpdateInterval;
            fpsAccumulator = 0f;
            fpsFramesCount = 0;
        }
    }

    private void TakeScreenshot()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Screenshot_{timestamp}.png";
        ScreenCapture.CaptureScreenshot(filename);
        Debug.Log($"[Screenshot] Saved screenshot to: {filename}");
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        
        // Restore cursor state based on whether we are currently orbiting a planet
        OrbitalCameraController orbital = FindObjectOfType<OrbitalCameraController>();
        if (orbital != null && orbital.enabled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
