using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    /// <summary>
    /// Manages voice audio separately from AudioManager.
    /// Handles two systems: Barks (immediate, interruptible) and Dialogue (queued, sequential).
    /// </summary>
    public class VoiceManager : MonoBehaviour
    {
        public static VoiceManager Instance { get; private set; }

        [Header("Voice Sources")]
        [SerializeField] private AudioSource barkSource;
        [SerializeField] private AudioSource dialogueSource;

        [Header("Dialogue Settings")]
        [SerializeField] private float dialoguePauseBetweenLines = 0.5f;

        private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
        private bool isPlayingDialogue = false;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSources();
        }

        private void InitializeSources()
        {
            // Create bark source if not assigned
            if (barkSource == null)
            {
                GameObject barkObj = new GameObject("BarkSource");
                barkObj.transform.SetParent(transform);
                barkSource = barkObj.AddComponent<AudioSource>();
            }
            barkSource.playOnAwake = false;
            barkSource.loop = false;
            barkSource.spatialBlend = 0f; // 2D

            // Create dialogue source if not assigned
            if (dialogueSource == null)
            {
                GameObject dialogueObj = new GameObject("DialogueSource");
                dialogueObj.transform.SetParent(transform);
                dialogueSource = dialogueObj.AddComponent<AudioSource>();
            }
            dialogueSource.playOnAwake = false;
            dialogueSource.loop = false;
            dialogueSource.spatialBlend = 0f; // 2D
        }

        /// <summary>
        /// Play a bark immediately. Will interrupt any currently playing bark.
        /// Use for: "Reloading!", "Grenade out!", enemy death screams, one-liners.
        /// </summary>
        public void PlayBark(AudioClipData barkData, float volumeMultiplier = 1f)
        {
            if (barkData == null)
            {
                Debug.LogWarning("VoiceManager: Attempted to play null bark");
                return;
            }

            AudioClip clip = barkData.GetRandomClip();
            if (clip == null) return;

            barkSource.Stop(); // Interrupt previous bark
            barkSource.clip = clip;
            barkSource.volume = barkData.GetRandomVolume() * volumeMultiplier;
            barkSource.pitch = barkData.GetRandomPitch();
            barkSource.Play();
        }

        /// <summary>
        /// Queue a single dialogue line. Will play after all previous dialogue finishes.
        /// Use for: Queen/MC conversations, story beats.
        /// </summary>
        public void QueueDialogue(AudioClipData dialogueData, float volumeMultiplier = 1f)
        {
            if (dialogueData == null)
            {
                Debug.LogWarning("VoiceManager: Attempted to queue null dialogue");
                return;
            }

            AudioClip clip = dialogueData.GetRandomClip();
            if (clip == null) return;

            dialogueQueue.Enqueue(new DialogueLine(clip, dialogueData.GetRandomVolume() * volumeMultiplier, dialogueData.GetRandomPitch()));

            if (!isPlayingDialogue)
            {
                StartCoroutine(PlayDialogueQueue());
            }
        }

        /// <summary>
        /// Queue multiple dialogue lines in sequence.
        /// </summary>
        public void QueueDialogueSequence(AudioClipData[] dialogueSequence, float volumeMultiplier = 1f)
        {
            foreach (var dialogueData in dialogueSequence)
            {
                if (dialogueData != null)
                {
                    AudioClip clip = dialogueData.GetRandomClip();
                    if (clip != null)
                    {
                        dialogueQueue.Enqueue(new DialogueLine(clip, dialogueData.GetRandomVolume() * volumeMultiplier, dialogueData.GetRandomPitch()));
                    }
                }
            }

            if (!isPlayingDialogue)
            {
                StartCoroutine(PlayDialogueQueue());
            }
        }

        /// <summary>
        /// Clear all queued dialogue. Use when transitioning scenes or canceling conversations.
        /// </summary>
        public void ClearDialogueQueue()
        {
            dialogueQueue.Clear();
            dialogueSource.Stop();
            isPlayingDialogue = false;
        }

        /// <summary>
        /// Check if dialogue is currently playing or queued.
        /// </summary>
        public bool IsDialogueActive()
        {
            return isPlayingDialogue || dialogueQueue.Count > 0;
        }

        private IEnumerator PlayDialogueQueue()
        {
            isPlayingDialogue = true;

            while (dialogueQueue.Count > 0)
            {
                DialogueLine line = dialogueQueue.Dequeue();

                dialogueSource.clip = line.Clip;
                dialogueSource.volume = line.Volume;
                dialogueSource.pitch = line.Pitch;
                dialogueSource.Play();

                // Wait for clip to finish
                yield return new WaitForSeconds(line.Clip.length);

                // Pause between lines
                if (dialogueQueue.Count > 0)
                {
                    yield return new WaitForSeconds(dialoguePauseBetweenLines);
                }
            }

            isPlayingDialogue = false;
        }

        /// <summary>
        /// Helper struct to store dialogue line data.
        /// </summary>
        private struct DialogueLine
        {
            public AudioClip Clip;
            public float Volume;
            public float Pitch;

            public DialogueLine(AudioClip clip, float volume, float pitch)
            {
                Clip = clip;
                Volume = volume;
                Pitch = pitch;
            }
        }
    }
}
