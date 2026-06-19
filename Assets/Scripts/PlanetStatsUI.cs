using UnityEngine;
using UnityEngine.UI;

public class PlanetStatsUI : MonoBehaviour
{
    [Header("UI Panel Reference")]
    public GameObject planetStatsPanel;

    [Header("Text Labels")]
    public Text clayLabel;
    public Text coalLabel;
    public Text ironLabel;

    private OrbitalCameraController orbitalCamera;

    void Start()
    {
        orbitalCamera = FindObjectOfType<OrbitalCameraController>();
        
        // Hide panel initially
        if (planetStatsPanel != null)
        {
            planetStatsPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Only show panel if we are active in orbital camera mode focusing on a target planet
        if (orbitalCamera != null && orbitalCamera.enabled && orbitalCamera.target != null)
        {
            if (planetStatsPanel != null && !planetStatsPanel.activeSelf)
            {
                planetStatsPanel.SetActive(true);
            }
            UpdateStats(orbitalCamera.target);
        }
        else
        {
            if (planetStatsPanel != null && planetStatsPanel.activeSelf)
            {
                planetStatsPanel.SetActive(false);
            }
        }
    }

    private void UpdateStats(Transform targetPlanet)
    {
        int clayCount = 0;
        int coalCount = 0;
        int ironCount = 0;

        // Count resources attached as child objects on the planet
        foreach (Transform child in targetPlanet)
        {
            if (child.name.StartsWith("Clay")) clayCount++;
            else if (child.name.StartsWith("Coal")) coalCount++;
            else if (child.name.StartsWith("Iron")) ironCount++;
        }

        // Update labels
        if (clayLabel != null) clayLabel.text = $"Clay: {clayCount}";
        if (coalLabel != null) coalLabel.text = $"Coal: {coalCount}";
        if (ironLabel != null) ironLabel.text = $"Iron: {ironCount}";


    }
}
