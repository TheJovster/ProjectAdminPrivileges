using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

namespace ProjectAdminPrivileges.Audio
{
    public class SoundtrackManager : MonoBehaviour
    {
        [SerializeField] private AudioSource soundtrackSource;
        [SerializeField] private AudioClip mainMenuSoundtrack;
        [SerializeField] private AudioClip combatSoundtrack;
        [SerializeField] private AudioClip dialogueSoundtrack;

        public static SoundtrackManager Instance;

        private void Awake()
        {
            if(Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else 
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            
        }

        private void Start()
        {
            PlayMainMenuSoundtrack();
        }


        public void PlayDialogueSoundtrack()
        {
            soundtrackSource.Stop();    
            soundtrackSource.clip = dialogueSoundtrack;
            soundtrackSource.Play();
        }

        public void PlayCombatSoundtrack()
        {
            if(soundtrackSource.clip == combatSoundtrack && soundtrackSource.isPlaying)
                return;
            soundtrackSource.Stop();
            soundtrackSource.clip = combatSoundtrack;
            soundtrackSource.Play();
        }

        public void ActivateSoundtrackSource()
        {
            if (!soundtrackSource.isPlaying)
            {
                soundtrackSource.loop = true;
                soundtrackSource.Play();
            }
        }

        public void DeactivateSoundtrackSource()
        {
            if (soundtrackSource.isPlaying)
            {
                soundtrackSource.Stop();
            }
        }

        public void SetSoundtrackVolume(float volume)
        {
            soundtrackSource.volume = volume;
        }

        public void PauseSoundtrack()
        {
            if (soundtrackSource.isPlaying)
            {
                soundtrackSource.Pause();
            }
        }

        public void PlayMainMenuSoundtrack() 
        {
            if(soundtrackSource.clip != null) 
            {
                if (soundtrackSource.isPlaying) 
                {
                    soundtrackSource.Stop();
                }
                soundtrackSource.clip = mainMenuSoundtrack;
                soundtrackSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
