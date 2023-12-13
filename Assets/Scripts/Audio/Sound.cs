using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = .75f;

    [Range(0f, 1f)]
    public float pitch = 1f;

    public bool loop = false;

    // public AudioMixerGroup mixerGroup;

    [HideInInspector]
    public AudioSource source;

}
