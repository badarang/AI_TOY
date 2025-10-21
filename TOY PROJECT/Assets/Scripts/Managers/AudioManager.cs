using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour, IManager
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Data")]
    [SerializeField] private AudioClipData audioClipData;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("Settings")]
    [SerializeField] private float sfxVolume = 1.0f;
    [SerializeField] private float bgmVolume = 0.5f;

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();

    public void BeforeInit()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeDictionaries();
        SetupAudioSources();
    }

    public void AfterInit()
    {
    }

    public void Dispose() { }

    private void InitializeDictionaries()
    {
        sfxDictionary.Clear();
        bgmDictionary.Clear();

        if (audioClipData == null)
        {
            Debug.LogError("AudioClipData is not assigned in AudioManager!");
            return;
        }

        foreach (var entry in audioClipData.sfxClips)
        {
            if (entry.clip != null && !string.IsNullOrEmpty(entry.key))
            {
                if (sfxDictionary.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"Duplicate SFX key: {entry.key}");
                }
                else
                {
                    sfxDictionary.Add(entry.key, entry.clip);
                }
            }
        }

        foreach (var entry in audioClipData.bgmClips)
        {
            if (entry.clip != null && !string.IsNullOrEmpty(entry.key))
            {
                if (bgmDictionary.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"Duplicate BGM key: {entry.key}");
                }
                else
                {
                    bgmDictionary.Add(entry.key, entry.clip);
                }
            }
        }

        Debug.Log($"AudioManager initialized: {sfxDictionary.Count} SFX, {bgmDictionary.Count} BGM");
    }

    private void SetupAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        sfxSource.volume = sfxVolume;
        bgmSource.volume = bgmVolume;
    }

    public void PlaySFX(string clipName)
    {
        if (sfxDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
        else
        {
            Debug.LogWarning($"SFX not found: {clipName}");
        }
    }

    public void PlayBGM(string clipName)
    {
        if (bgmDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM not found: {clipName}");
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }
}
