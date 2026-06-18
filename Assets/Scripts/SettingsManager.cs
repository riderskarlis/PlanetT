using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Settings Panel UI")]
    public GameObject settingsPanel;
    public Slider musicVolumeSlider;
    public Slider soundVolumeSlider;
    public Slider mouseSensitivitySlider;
    public Toggle vsyncToggle;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;

    [Header("Optional Value Labels")]
    public Text settingsLabel;
    public Text musicVolumeLabel;
    public Text soundVolumeLabel;
    public Text mouseSensitivityLabel;
    public Text vsyncLabel;
    public Text fullscreenLabel;
    public Text resolutionLabel;

    [Header("Default Values")]
    public float defaultMusicVolume = 0.8f;
    public float defaultSoundVolume = 0.8f;
    public float defaultMouseSensitivity = 2f;

    private List<Resolution> uniqueResolutions = new List<Resolution>();

    // Saved settings (the committed state)
    public float MusicVolume { get; private set; }
    public float SoundVolume { get; private set; }
    public float MouseSensitivity { get; private set; }
    public bool VSync { get; private set; }
    public bool Fullscreen { get; private set; }
    public int ResolutionIndex { get; private set; }

    // Temporary/working settings (modified by UI)
    private float tempMusicVolume;
    private float tempSoundVolume;
    private float tempMouseSensitivity;
    private bool tempVSync;
    private bool tempFullscreen;
    private int tempResolutionIndex;

    private Button settingsButton;
    private GameObject pausePanel;
    private bool wasPausePanelActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ClearReferences();
        FindUIElements();
        InitializeUI();
        ApplySettings();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (settingsPanel == null)
        {
            FindUIElements();
            InitializeUI();
            ApplySettings();
            
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
    }

    private void ClearReferences()
    {
        settingsPanel = null;
        musicVolumeSlider = null;
        soundVolumeSlider = null;
        mouseSensitivitySlider = null;
        vsyncToggle = null;
        fullscreenToggle = null;
        resolutionDropdown = null;

        settingsLabel = null;
        musicVolumeLabel = null;
        soundVolumeLabel = null;
        mouseSensitivityLabel = null;
        vsyncLabel = null;
        fullscreenLabel = null;
        resolutionLabel = null;
        settingsButton = null;
        pausePanel = null;
        wasPausePanelActive = false;
    }

    private void FindUIElements()
    {
        // 1. Search for SettingsPanel in the newly loaded scene
        if (settingsPanel == null)
        {
            GameObject foundPanel = GameObject.Find("SettingsPanel");
            if (foundPanel != null)
            {
                settingsPanel = foundPanel;
            }
            else
            {
                // Fallback: search inactive objects
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in allObjects)
                {
                    if (go.name == "SettingsPanel" && go.scene.isLoaded)
                    {
                        settingsPanel = go;
                        break;
                    }
                }
            }
        }

        if (settingsPanel == null) return;

        // 2. Find UI Controls inside SettingsPanel children
        musicVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "MusicVolumeSlider");
        if (musicVolumeSlider == null) musicVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "MusicVolume");
        if (musicVolumeSlider == null) musicVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "Music");

        soundVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "SoundVolumeSlider");
        if (soundVolumeSlider == null) soundVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "SoundVolume");
        if (soundVolumeSlider == null) soundVolumeSlider = FindComponentInChild<Slider>(settingsPanel, "Sound");
        
        mouseSensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "MouseSensitivitySlider");
        if (mouseSensitivitySlider == null) mouseSensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "SensitivitySlider");
        if (mouseSensitivitySlider == null) mouseSensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "MouseSensitivity");
        if (mouseSensitivitySlider == null) mouseSensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "Sensitivity");
        if (mouseSensitivitySlider == null) mouseSensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "MouseSensitivityLabel");
        if (mouseSensitivitySlider == null) mouseSensitivitySlider = FindComponentInChild<Slider>(settingsPanel, "SensitivityLabel");

        vsyncToggle = FindComponentInChild<Toggle>(settingsPanel, "VSyncToggle");
        if (vsyncToggle == null) vsyncToggle = FindComponentInChild<Toggle>(settingsPanel, "VSync Toggle");
        if (vsyncToggle == null) vsyncToggle = FindComponentInChild<Toggle>(settingsPanel, "VSync");

        fullscreenToggle = FindComponentInChild<Toggle>(settingsPanel, "FullscreenToggle");
        if (fullscreenToggle == null) fullscreenToggle = FindComponentInChild<Toggle>(settingsPanel, "Fullscreen Toggle");
        if (fullscreenToggle == null) fullscreenToggle = FindComponentInChild<Toggle>(settingsPanel, "Fullscreen");

        resolutionDropdown = FindComponentInChild<Dropdown>(settingsPanel, "ResolutionDropdown");
        if (resolutionDropdown == null) resolutionDropdown = FindComponentInChild<Dropdown>(settingsPanel, "Resolution Dropdown");
        if (resolutionDropdown == null) resolutionDropdown = FindComponentInChild<Dropdown>(settingsPanel, "Resolution");

        // 3. Find Text Labels in children
        settingsLabel = FindComponentInChild<Text>(settingsPanel, "SettingsLabel");
        musicVolumeLabel = FindComponentInChild<Text>(settingsPanel, "MusicVolumeLabel");
        soundVolumeLabel = FindComponentInChild<Text>(settingsPanel, "SoundVolumeLabel");
        mouseSensitivityLabel = FindComponentInChild<Text>(settingsPanel, "MouseSensitivityLabel");
        vsyncLabel = FindComponentInChild<Text>(settingsPanel, "VSyncLabel");
        fullscreenLabel = FindComponentInChild<Text>(settingsPanel, "FullscreenLabel");
        resolutionLabel = FindComponentInChild<Text>(settingsPanel, "ResolutionLabel");

        // 4. Bind Button onClick listeners dynamically
        Button saveBtn = FindComponentInChild<Button>(settingsPanel, "SaveButton");
        if (saveBtn != null)
        {
            saveBtn.onClick.RemoveListener(SaveSettingsButton);
            saveBtn.onClick.AddListener(SaveSettingsButton);
        }

        Button cancelBtn = FindComponentInChild<Button>(settingsPanel, "CancelButton");
        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveListener(CancelSettingsButton);
            cancelBtn.onClick.AddListener(CancelSettingsButton);
        }

        // 5. Find and bind the Settings Button (typically outside the panel on menu/pause canvas)
        if (settingsButton == null)
        {
            GameObject foundBtnObj = GameObject.Find("SettingsButton");
            if (foundBtnObj != null)
            {
                settingsButton = foundBtnObj.GetComponent<Button>();
            }
            else
            {
                foundBtnObj = GameObject.Find("Settings Button");
                if (foundBtnObj != null)
                {
                    settingsButton = foundBtnObj.GetComponent<Button>();
                }
            }

            if (settingsButton == null)
            {
                var allButtons = Resources.FindObjectsOfTypeAll<Button>();
                foreach (var btn in allButtons)
                {
                    if (btn.name.Replace(" ", "").Equals("SettingsButton", System.StringComparison.OrdinalIgnoreCase) && btn.gameObject.scene.isLoaded)
                    {
                        settingsButton = btn;
                        break;
                    }
                }
            }
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        // 6. Find the PausePanel in the scene
        if (pausePanel == null)
        {
            GameObject foundPause = GameObject.Find("PausePanel");
            if (foundPause != null)
            {
                pausePanel = foundPause;
            }
            else
            {
                foundPause = GameObject.Find("PauseMenuUI");
                if (foundPause != null)
                {
                    pausePanel = foundPause;
                }
                else
                {
                    foundPause = GameObject.Find("PauseMenu");
                    if (foundPause != null)
                    {
                        pausePanel = foundPause;
                    }
                }
            }

            if (pausePanel == null)
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in allObjects)
                {
                    if ((go.name == "PausePanel" || go.name == "PauseMenuUI" || go.name == "PauseMenu") && go.scene.isLoaded)
                    {
                        pausePanel = go;
                        break;
                    }
                }
            }
        }
    }

    private T FindComponentInChild<T>(GameObject parent, string name) where T : Component
    {
        T[] components = parent.GetComponentsInChildren<T>(true);
        foreach (T comp in components)
        {
            string cleanCompName = comp.name.Replace(" ", "").ToLower();
            string cleanSearchName = name.Replace(" ", "").ToLower();
            if (cleanCompName == cleanSearchName)
            {
                return comp;
            }
        }
        return null;
    }

    private void InitializeUI()
    {
        PopulateResolutions();

        // Assign UI values to match current settings
        if (musicVolumeSlider != null) musicVolumeSlider.value = MusicVolume;
        if (soundVolumeSlider != null) soundVolumeSlider.value = SoundVolume;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = MouseSensitivity;
        if (vsyncToggle != null) vsyncToggle.isOn = VSync;
        if (fullscreenToggle != null) fullscreenToggle.isOn = Fullscreen;

        // Populate working temp variables
        tempMusicVolume = MusicVolume;
        tempSoundVolume = SoundVolume;
        tempMouseSensitivity = MouseSensitivity;
        tempVSync = VSync;
        tempFullscreen = Fullscreen;
        tempResolutionIndex = ResolutionIndex;

        UpdateLabelTexts();
        AddListeners();
    }

    private void PopulateResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        Resolution[] allResolutions = Screen.resolutions;
        uniqueResolutions.Clear();

        List<string> options = new List<string>();
        int currentResIndex = 0;
        HashSet<string> seenResolutions = new HashSet<string>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            string optionText = $"{allResolutions[i].width} x {allResolutions[i].height}";
            if (seenResolutions.Add(optionText))
            {
                uniqueResolutions.Add(allResolutions[i]);
                options.Add(optionText);

                if (allResolutions[i].width == Screen.currentResolution.width &&
                    allResolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = uniqueResolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);

        ResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex);
        ResolutionIndex = Mathf.Clamp(ResolutionIndex, 0, uniqueResolutions.Count - 1);
        
        RemoveListeners();
        resolutionDropdown.value = ResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        AddListeners();
    }

    // ==========================================
    // UI Event Handlers
    // ==========================================
    private void AddListeners()
    {
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (soundVolumeSlider != null) soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void RemoveListeners()
    {
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (soundVolumeSlider != null) soundVolumeSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
        if (vsyncToggle != null) vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
    }

    private void OnMusicVolumeChanged(float val)
    {
        tempMusicVolume = val;
        ApplyMusicVolume(tempMusicVolume);
        UpdateLabelTexts();
    }

    private void OnSoundVolumeChanged(float val)
    {
        tempSoundVolume = val;
        ApplySoundVolume(tempSoundVolume);
        UpdateLabelTexts();
    }

    private void OnMouseSensitivityChanged(float val)
    {
        tempMouseSensitivity = val;
        ApplyMouseSensitivity(tempMouseSensitivity);
        UpdateLabelTexts();
    }

    private void OnVSyncChanged(bool val)
    {
        tempVSync = val;
        ApplyVSync(tempVSync);
    }

    private void OnFullscreenChanged(bool val)
    {
        tempFullscreen = val;
        ApplyFullscreenAndResolution(tempFullscreen, tempResolutionIndex);
    }

    private void OnResolutionChanged(int val)
    {
        tempResolutionIndex = val;
        ApplyFullscreenAndResolution(tempFullscreen, tempResolutionIndex);
    }

    private void UpdateLabelTexts()
    {
        if (musicVolumeLabel != null) musicVolumeLabel.text = $"Music Volume: {Mathf.RoundToInt(tempMusicVolume * 100)}%";
        if (soundVolumeLabel != null) soundVolumeLabel.text = $"Sound Volume: {Mathf.RoundToInt(tempSoundVolume * 100)}%";
        if (mouseSensitivityLabel != null) mouseSensitivityLabel.text = $"Sensitivity: {tempMouseSensitivity:F1}";
    }

    // ==========================================
    // Settings Loading & Application Logic
    // ==========================================
    public void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        SoundVolume = PlayerPrefs.GetFloat("SoundVolume", defaultSoundVolume);
        MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultMouseSensitivity);
        VSync = PlayerPrefs.GetInt("VSync", 0) == 1;
        Fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
    }

    public void ApplySettings()
    {
        ApplyMusicVolume(MusicVolume);
        ApplySoundVolume(SoundVolume);
        ApplyMouseSensitivity(MouseSensitivity);
        ApplyVSync(VSync);
        ApplyFullscreenAndResolution(Fullscreen, ResolutionIndex);
    }

    private void ApplyMusicVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume(AudioChannel.Music, volume);
        }
    }

    private void ApplySoundVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume(AudioChannel.SFX, volume);
            SoundManager.Instance.SetVolume(AudioChannel.UI, volume);
        }
    }

    private void ApplyMouseSensitivity(float sensitivity)
    {
        // Find SimpleCameraController (even if inactive/disabled during orbit mode)
        SimpleCameraController cam = FindObjectOfType<SimpleCameraController>();
        if (cam == null)
        {
            SimpleCameraController[] cams = Resources.FindObjectsOfTypeAll<SimpleCameraController>();
            foreach (var c in cams)
            {
                if (c.gameObject.scene.isLoaded)
                {
                    cam = c;
                    break;
                }
            }
        }

        if (cam != null)
        {
            cam.mouseSensitivity = sensitivity;
        }

        // Find OrbitalCameraController (even if inactive/disabled during free-look mode)
        OrbitalCameraController orbitalCam = FindObjectOfType<OrbitalCameraController>();
        if (orbitalCam == null)
        {
            OrbitalCameraController[] cams = Resources.FindObjectsOfTypeAll<OrbitalCameraController>();
            foreach (var c in cams)
            {
                if (c.gameObject.scene.isLoaded)
                {
                    orbitalCam = c;
                    break;
                }
            }
        }

        if (orbitalCam != null)
        {
            orbitalCam.rotationSpeed = 100f * (sensitivity / defaultMouseSensitivity);
        }
    }

    private void ApplyVSync(bool vsync)
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0;
    }

    private void ApplyFullscreenAndResolution(bool fullscreen, int resolutionIndex)
    {
        if (uniqueResolutions.Count > 0 && resolutionIndex >= 0 && resolutionIndex < uniqueResolutions.Count)
        {
            Resolution targetRes = uniqueResolutions[resolutionIndex];
            Screen.SetResolution(targetRes.width, targetRes.height, fullscreen);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }
    }

    // ==========================================
    // Buttons API: Save, Cancel, Open
    // ==========================================
    public void OpenSettings()
    {
        // 1. Hide PausePanel if it is currently active in the scene
        if (pausePanel != null && pausePanel.activeSelf)
        {
            wasPausePanelActive = true;
            pausePanel.SetActive(false);
        }
        else
        {
            wasPausePanelActive = false;
        }

        // 2. Load active settings into working copy
        tempMusicVolume = MusicVolume;
        tempSoundVolume = SoundVolume;
        tempMouseSensitivity = MouseSensitivity;
        tempVSync = VSync;
        tempFullscreen = Fullscreen;
        tempResolutionIndex = ResolutionIndex;

        // 2. Temporarily detach listeners, update UI positions, and re-attach listeners
        RemoveListeners();
        
        if (musicVolumeSlider != null) musicVolumeSlider.value = tempMusicVolume;
        if (soundVolumeSlider != null) soundVolumeSlider.value = tempSoundVolume;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = tempMouseSensitivity;
        if (vsyncToggle != null) vsyncToggle.isOn = tempVSync;
        if (fullscreenToggle != null) fullscreenToggle.isOn = tempFullscreen;
        if (resolutionDropdown != null)
        {
            resolutionDropdown.value = tempResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        UpdateLabelTexts();
        AddListeners();

        // 3. Show UI Panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void SaveSettingsButton()
    {
        // 1. Commit temp values to permanent variables
        MusicVolume = tempMusicVolume;
        SoundVolume = tempSoundVolume;
        MouseSensitivity = tempMouseSensitivity;
        VSync = tempVSync;
        Fullscreen = tempFullscreen;
        ResolutionIndex = tempResolutionIndex;

        // 2. Save permanent variables to PlayerPrefs
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SoundVolume", SoundVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", MouseSensitivity);
        PlayerPrefs.SetInt("VSync", VSync ? 1 : 0);
        PlayerPrefs.SetInt("Fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionIndex", ResolutionIndex);
        PlayerPrefs.Save();

        // 3. Apply settings cleanly
        ApplySettings();

        // 4. Reactivate PausePanel if we hid it when opening settings
        if (wasPausePanelActive && pausePanel != null)
        {
            pausePanel.SetActive(true);
            wasPausePanelActive = false;
        }

        // 5. Hide UI Panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void CancelSettingsButton()
    {
        // 1. Discard temp changes and revert game configuration back to permanent values
        ApplySettings();

        // 2. Reactivate PausePanel if we hid it when opening settings
        if (wasPausePanelActive && pausePanel != null)
        {
            pausePanel.SetActive(true);
            wasPausePanelActive = false;
        }

        // 3. Hide UI Panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            if (settingsPanel.activeSelf)
            {
                CancelSettingsButton();
            }
            else
            {
                OpenSettings();
            }
        }
    }
}
