using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public float skyBoxRotationSpeed = 0.25f;
    public string mainGameScene = "Main";

    [Header("Setup Panels")]
    public GameObject MainMenuPanel;
    public GameObject SetupPanel;

    [Header("Game Setup Controls")]
    public InputField NameInputField;
    public InputField SeedInputField;
    public Slider NumberOfPlanetsSlider;
    public Dropdown ColorDropdown;
    public Text NumberOfPlanetsValue;
    public RawImage ColorDisplayImage;

    [Header("Color Preview Settings")]
    public List<Color> DropdownColors = new List<Color>
    {
        new Color(1f, 0f, 0f),       // Red
        new Color(1f, 0.5f, 0f),     // Orange
        new Color(1f, 1f, 0f),       // Yellow
        new Color(0f, 1f, 0f),       // Green
        new Color(0f, 0f, 1f),       // Blue
        new Color(0.29f, 0f, 0.51f), // Indigo
        new Color(0.5f, 0f, 0.5f)    // Violet
    };

    [Header("Game Setup Buttons")]
    public Button PlayButton;
    public Button CancelButton;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Initialize setup elements with default values
        if (NumberOfPlanetsSlider != null)
        {
            NumberOfPlanetsSlider.onValueChanged.AddListener(OnPlanetSliderChanged);
            NumberOfPlanetsSlider.value = GameSetupData.planetCount;
            if (NumberOfPlanetsValue != null)
            {
                NumberOfPlanetsValue.text = Mathf.RoundToInt(NumberOfPlanetsSlider.value).ToString();
            }
        }

        if (NameInputField != null)
        {
            NameInputField.text = GameSetupData.profileName;
        }

        if (SeedInputField != null)
        {
            SeedInputField.text = GameSetupData.customSeed;
        }

        if (ColorDropdown != null)
        {
            // Force reset DropdownColors list to prevent Unity Inspector serialization from holding onto old colors
            DropdownColors = new List<Color>
            {
                new Color(1f, 0f, 0f),       // Red
                new Color(1f, 0.5f, 0f),     // Orange
                new Color(1f, 1f, 0f),       // Yellow
                new Color(0f, 1f, 0f),       // Green
                new Color(0f, 0f, 1f),       // Blue
                new Color(0.29f, 0f, 0.51f), // Indigo
                new Color(0.5f, 0f, 0.5f)    // Violet
            };

            ColorDropdown.ClearOptions();
            List<string> defaultColors = new List<string> {
                "Red",
                "Orange",
                "Yellow",
                "Green",
                "Blue",
                "Indigo",
                "Violet"
            };
            ColorDropdown.AddOptions(defaultColors);
            
            ColorDropdown.onValueChanged.AddListener(OnColorDropdownChanged);
            ColorDropdown.value = GameSetupData.colorIndex;
            OnColorDropdownChanged(ColorDropdown.value);
        }

        // Bind button actions
        if (PlayButton != null)
        {
            PlayButton.onClick.AddListener(PlayGame);
        }

        if (CancelButton != null)
        {
            CancelButton.onClick.AddListener(CancelSetup);
        }
    }

    void OnDestroy()
    {
        if (NumberOfPlanetsSlider != null)
        {
            NumberOfPlanetsSlider.onValueChanged.RemoveListener(OnPlanetSliderChanged);
        }
        if (ColorDropdown != null)
        {
            ColorDropdown.onValueChanged.RemoveListener(OnColorDropdownChanged);
        }
        if (PlayButton != null)
        {
            PlayButton.onClick.RemoveListener(PlayGame);
        }
        if (CancelButton != null)
        {
            CancelButton.onClick.RemoveListener(CancelSetup);
        }
    }

    private void OnPlanetSliderChanged(float value)
    {
        if (NumberOfPlanetsValue != null)
        {
            NumberOfPlanetsValue.text = Mathf.RoundToInt(value).ToString();
        }
    }

    private void OnColorDropdownChanged(int index)
    {
        if (ColorDisplayImage != null && DropdownColors != null && index >= 0 && index < DropdownColors.Count)
        {
            ColorDisplayImage.color = DropdownColors[index];
        }
    }

    void Update()
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetFloat("_Rotation", Time.time * skyBoxRotationSpeed);
        }
    }

    public void OpenSetup()
    {
        if (SetupPanel != null) SetupPanel.SetActive(true);
        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
    }

    public void CancelSetup()
    {
        if (SetupPanel != null) SetupPanel.SetActive(false);
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
    }

    public void PlayGame()
    {
        // Store user selections in static GameSetupData
        if (NameInputField != null)
        {
            GameSetupData.profileName = NameInputField.text;
        }
        if (SeedInputField != null)
        {
            GameSetupData.customSeed = SeedInputField.text;
        }
        if (NumberOfPlanetsSlider != null)
        {
            GameSetupData.planetCount = Mathf.RoundToInt(NumberOfPlanetsSlider.value);
        }
        if (ColorDropdown != null)
        {
            GameSetupData.colorIndex = ColorDropdown.value;
        }

        SceneManager.LoadScene(mainGameScene);
    }

    public void OpenSettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OpenSettings();
        }
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
