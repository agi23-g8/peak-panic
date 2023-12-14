using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAudio : MonoBehaviour
{

    [SerializeField]
    private float musicVolume = 0.5f;

    [SerializeField]
    private Sound windSound;

    [SerializeField]
    private List<Sound> musicSounds;

    private AudioSource windAudioSource;
    private AudioSource musicAudioSource;

    private int previousMusicIndex = -1;

    private bool muteMusic = false;

    void Start()
    {
        windAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource = gameObject.AddComponent<AudioSource>();

    }

    // Update is called once per frame
    void Update()
    {
        //if m is pressed
        if (Input.GetKeyDown(KeyCode.M))
        {
            muteMusic = !muteMusic;
        }

        if (!windAudioSource.isPlaying)
        {
            windAudioSource.clip = windSound.clip;
            windAudioSource.volume = windSound.volume;
            windAudioSource.loop = windSound.loop;
            windAudioSource.Play();
        }

        if (!musicAudioSource.isPlaying)
        {
            int musicIndex = Random.Range(0, this.musicSounds.Count);
            while (musicIndex == previousMusicIndex)
            {
                musicIndex = Random.Range(0, this.musicSounds.Count);
            }
            previousMusicIndex = musicIndex;
            Sound musicSound = this.musicSounds[musicIndex];
            musicAudioSource.clip = musicSound.clip;
            musicAudioSource.volume = musicSound.volume * musicVolume;
            musicAudioSource.loop = musicSound.loop;
            musicAudioSource.Play();
        }
        if (muteMusic)
        {
            musicAudioSource.volume = 0;
        }
        else
        {
            musicAudioSource.volume = musicVolume;
        }
    }
}
