using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AudioChannel { Master, Music, SFX, UI }

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Volume Settings (0.0 to 1.0)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 0.9f;

    [Header("Pool Sizes")]
    [SerializeField] private int sfxPoolSize = 10;
    [SerializeField] private int uiPoolSize = 5;

    [Header("UI Audio Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonHoverClip;

    // Dedicated channels
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool isMusicSourceAActive = true;
    private Coroutine crossfadeCoroutine;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private List<AudioSource> uiPool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeChannels();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeChannels()
    {
        // 1. Setup Music Sources (A/B for crossfading)
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceA.loop = true;
        musicSourceB.loop = true;

        // Create parents for clean hierarchy
        Transform sfxRoot = new GameObject("SFX_Channel").transform;
        sfxRoot.SetParent(transform);
        Transform uiRoot = new GameObject("UI_Channel").transform;
        uiRoot.SetParent(transform);

        // 2. Setup SFX Pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = new GameObject($"PooledSFX_{i}").AddComponent<AudioSource>();
            source.transform.SetParent(sfxRoot);
            sfxPool.Add(source);
        }

        // 3. Setup UI Pool
        for (int i = 0; i < uiPoolSize; i++)
        {
            AudioSource source = new GameObject($"PooledUI_{i}").AddComponent<AudioSource>();
            source.transform.SetParent(uiRoot);
            uiPool.Add(source);
        }

        UpdateAllVolumes();
    }

    // ==========================================
    // Volume Control API
    // ==========================================
    public void SetVolume(AudioChannel channel, float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        switch (channel)
        {
            case AudioChannel.Master: masterVolume = volume; break;
            case AudioChannel.Music: musicVolume = volume; break;
            case AudioChannel.SFX: sfxVolume = volume; break;
            case AudioChannel.UI: uiVolume = volume; break;
        }

        UpdateAllVolumes();
    }

    public float GetVolume(AudioChannel channel)
    {
        switch (channel)
        {
            case AudioChannel.Master: return masterVolume;
            case AudioChannel.Music: return musicVolume;
            case AudioChannel.SFX: return sfxVolume;
            case AudioChannel.UI: return uiVolume;
            default: return 1f;
        }
    }

    private void UpdateAllVolumes()
    {
        // Update Music Volume
        float finalMusicVol = musicVolume * masterVolume;
        if (isMusicSourceAActive)
        {
            musicSourceA.volume = finalMusicVol;
            musicSourceB.volume = 0f;
        }
        else
        {
            musicSourceB.volume = finalMusicVol;
            musicSourceA.volume = 0f;
        }

        // Update SFX Pool Volume
        float finalSFXVol = sfxVolume * masterVolume;
        foreach (var source in sfxPool)
        {
            source.volume = finalSFXVol;
        }

        // Update UI Pool Volume
        float finalUIVol = uiVolume * masterVolume;
        foreach (var source in uiPool)
        {
            source.volume = finalUIVol;
        }
    }

    // ==========================================
    // Audio Playback API
    // ==========================================
    public void PlaySFX(AudioClip clip, float pitchRandomness = 0f)
    {
        AudioSource source = GetAvailableSource(sfxPool, sfxVolume * masterVolume);
        if (source != null)
        {
            source.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
            source.PlayOneShot(clip);
        }
    }

    public void PlayUI(AudioClip clip)
    {
        AudioSource source = GetAvailableSource(uiPool, uiVolume * masterVolume);
        if (source != null)
        {
            source.pitch = 1f;
            source.PlayOneShot(clip);
        }
    }

    public bool IsMusicPlaying
    {
        get
        {
            if (isMusicSourceAActive)
                return musicSourceA != null && musicSourceA.isPlaying;
            else
                return musicSourceB != null && musicSourceB.isPlaying;
        }
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1.0f, bool loop = true)
    {
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeDuration, loop));
    }

    public void StopMusic(float fadeDuration = 1.0f)
    {
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine = StartCoroutine(CrossfadeMusic(null, fadeDuration, false));
    }

    private AudioSource GetAvailableSource(List<AudioSource> pool, float currentVolume)
    {
        foreach (var source in pool)
        {
            if (!source.isPlaying)
            {
                source.volume = currentVolume;
                return source;
            }
        }
        
        // Return null if all channels are filled, or expand pool dynamically
        return null;
    }

    private IEnumerator CrossfadeMusic(AudioClip nextClip, float duration, bool loop)
    {
        float targetMusicVolume = musicVolume * masterVolume;
        AudioSource activeSource = isMusicSourceAActive ? musicSourceA : musicSourceB;
        AudioSource transitionSource = isMusicSourceAActive ? musicSourceB : musicSourceA;

        isMusicSourceAActive = !isMusicSourceAActive;
        
        if (nextClip != null)
        {
            transitionSource.clip = nextClip;
            transitionSource.loop = loop;
            transitionSource.Play();
        }
        transitionSource.volume = 0f;

        float elapsed = 0f;
        float startActiveVol = activeSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            activeSource.volume = Mathf.Lerp(startActiveVol, 0f, percent);
            if (nextClip != null)
            {
                transitionSource.volume = Mathf.Lerp(0f, targetMusicVolume, percent);
            }

            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();
        if (nextClip != null)
        {
            transitionSource.volume = targetMusicVolume;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        AutoBindUIButtons();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AutoBindUIButtons();
    }

    public void AutoBindUIButtons()
    {
        UnityEngine.UI.Button[] buttons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
        foreach (var btn in buttons)
        {
            UIEventSoundBinder binder = btn.gameObject.GetComponent<UIEventSoundBinder>();
            if (binder == null)
            {
                binder = btn.gameObject.AddComponent<UIEventSoundBinder>();
                binder.Bind(btn, this);
            }
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickClip != null)
        {
            PlayUI(buttonClickClip);
        }
    }

    public void PlayButtonHover()
    {
        if (buttonHoverClip != null)
        {
            PlayUI(buttonHoverClip);
        }
    }
}

public class UIEventSoundBinder : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    private SoundManager manager;

    public void Bind(UnityEngine.UI.Button button, SoundManager soundManager)
    {
        manager = soundManager;
        button.onClick.AddListener(OnButtonClick);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.PlayButtonHover();
        }
    }

    private void OnButtonClick()
    {
        if (manager != null)
        {
            manager.PlayButtonClick();
        }
    }
}
