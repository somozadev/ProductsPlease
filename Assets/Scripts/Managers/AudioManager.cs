using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Settings")]
    public AudioSource sfxSource;
    public Sound[] sfxSounds;

    [Header("Music Settings")]
    public AudioSource musicSourceA;
    public AudioSource musicSourceB;
    public AudioClip[] musicPlaylist;
    public float fadeDuration = 2f;
    private int currentTrackIndex = 0;
    private bool isPlayingA = true;

    private void Awake()
    {
        // Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Start background music loop
        if (musicPlaylist.Length > 0)
            StartCoroutine(LoopMusic());
    }

    public void PlaySFX(string name)
    {
        Sound s = System.Array.Find(sfxSounds, sound => sound.name == name);
        if (s != null)
        {
            sfxSource.PlayOneShot(s.clip);
        }
        else
        {
            Debug.LogWarning($"SFX '{name}' not found!");
        }
    }

    private IEnumerator LoopMusic()
    {
        while (true)
        {
            AudioClip nextTrack = musicPlaylist[currentTrackIndex];
            AudioSource activeSource = isPlayingA ? musicSourceA : musicSourceB;
            AudioSource inactiveSource = isPlayingA ? musicSourceB : musicSourceA;

            inactiveSource.clip = nextTrack;
            inactiveSource.volume = 0f;
            inactiveSource.Play();

            // Fade out active, fade in inactive
            float timer = 0f;
            while (timer < fadeDuration)
            {
                float t = timer / fadeDuration;
                activeSource.volume = Mathf.Lerp(0.075f, 0f, t);
                inactiveSource.volume = Mathf.Lerp(0f, 0.075f, t);
                timer += Time.deltaTime;
                yield return null;
            }

            activeSource.Stop();
            inactiveSource.volume = 0.075f;

            // Prepare for next track
            isPlayingA = !isPlayingA;
            currentTrackIndex = (currentTrackIndex + 1) % musicPlaylist.Length;

            yield return new WaitForSeconds(nextTrack.length - fadeDuration);
        }
    }
}

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}
