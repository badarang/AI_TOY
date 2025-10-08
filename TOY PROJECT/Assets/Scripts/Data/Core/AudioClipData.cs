using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioClipData", menuName = "Audio/AudioClipData")]
public class AudioClipData : ScriptableObject
{
    [System.Serializable]
    public class AudioClipEntry
    {
        public string key;
        public AudioClip clip;
    }

    public AudioClipEntry[] sfxClips;
    public AudioClipEntry[] bgmClips;
}
