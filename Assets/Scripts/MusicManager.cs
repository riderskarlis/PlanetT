using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Playlist Configuration")]
    [Tooltip("The list of audio tracks that will play randomly in the background")]
    public AudioClip[] playlist;

    [Header("Silence Range (Seconds)")]
    [Tooltip("Minimum duration of silence between tracks (default: 30s)")]
    public float minSilenceDuration = 30f;
    [Tooltip("Maximum duration of silence between tracks (default: 120s / 2m)")]
    public float maxSilenceDuration = 120f;

    private int lastTrackIndex = -1;
    private bool isRunning = false;

    void Start()
    {
        // Start the music loop coroutine on startup
        StartCoroutine(MusicLoopCoroutine());
    }

    private IEnumerator MusicLoopCoroutine()
    {
        isRunning = true;

        while (isRunning)
        {
            if (playlist == null || playlist.Length == 0)
            {
                // Wait slightly and retry if playlist is empty or not yet assigned
                yield return new WaitForSeconds(5f);
                continue;
            }

            // 1. Get a random track from the playlist (avoid repeating the last track if possible)
            int trackIndex = GetRandomTrackIndex();
            AudioClip clip = playlist[trackIndex];

            // 2. Play the track via the SoundManager framework, setting loop to false
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayMusic(clip, 2.0f, false);
            }
            else
            {
                Debug.LogWarning("[MusicManager] SoundManager.Instance is null! Make sure SoundManager is present in the scene.");
            }

            // Wait briefly for the clip to start playing and register inside SoundManager
            yield return new WaitForSeconds(2.5f);

            // 3. Wait until the music track has completed playing
            while (SoundManager.Instance != null && SoundManager.Instance.IsMusicPlaying)
            {
                yield return new WaitForSeconds(1.0f);
            }

            // 4. Generate random silence duration between 30 seconds and 2 minutes (120 seconds)
            float silenceDuration = Random.Range(minSilenceDuration, maxSilenceDuration);

            yield return new WaitForSeconds(silenceDuration);
        }
    }

    private int GetRandomTrackIndex()
    {
        if (playlist.Length <= 1) return 0;

        int index;
        do
        {
            index = Random.Range(0, playlist.Length);
        } while (index == lastTrackIndex);

        lastTrackIndex = index;
        return index;
    }
}
