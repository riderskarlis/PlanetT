using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    public float skyBoxRotationSpeed = 0.25f;
    public string mainGameScene = "Main";

    void Update()
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetFloat("_Rotation", Time.time * skyBoxRotationSpeed);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(mainGameScene);
    }

    public void OpenSettings()
    {
        
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
