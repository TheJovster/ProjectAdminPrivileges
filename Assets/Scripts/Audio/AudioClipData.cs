using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    /// <summary>
    /// ScriptableObject that holds a collection of audio clip variations with randomization settings.
    /// Allows for varied audio playback to avoid repetitive sounds.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioClipData", menuName = "Audio/Audio Clip Data")]
    public class AudioClipData : ScriptableObject
    {
        [Header("Audio Clips")]
        [Tooltip("Array of clip variations - a random one will be chosen each time")]
        [SerializeField] private AudioClip[] clipVariations;

        [Header("Randomization")]
        [Tooltip("Random volume range")]
        [SerializeField] private float volumeMin = 0.8f;
        [SerializeField] private float volumeMax = 1.0f;

        [Tooltip("Random pitch range")]
        [SerializeField] private float pitchMin = 0.95f;
        [SerializeField] private float pitchMax = 1.05f;

        [Header("Category")]
        [SerializeField] private AudioCategory category = AudioCategory.SFX;

        [Header("Spatial Settings")]
        [Tooltip("Max distance for 3D spatial audio (0 = 2D audio)")]
        [SerializeField] private float maxDistance = 50f;

        public AudioCategory Category => category;
        public float MaxDistance => maxDistance;

        /// <summary>
        /// Gets a random clip from the variations array.
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (clipVariations == null || clipVariations.Length == 0)
            {
                Debug.LogWarning($"AudioClipData '{name}' has no clip variations!");
                return null;
            }

            return clipVariations[Random.Range(0, clipVariations.Length)];
        }

        /// <summary>
        /// Gets a random volume within the specified range.
        /// </summary>
        public float GetRandomVolume()
        {
            return Random.Range(volumeMin, volumeMax);
        }

        /// <summary>
        /// Gets a random pitch within the specified range.
        /// </summary>
        public float GetRandomPitch()
        {
            return Random.Range(pitchMin, pitchMax);
        }
    }

    /// <summary>
    /// Categories for audio routing to appropriate pools.
    /// </summary>
    public enum AudioCategory
    {
        Gunfire,    // Weapon fire sounds - high priority, rapid fire
        Impact,     // Projectile impacts - high priority, frequent
        SFX,        // Misc sound effects - medium priority
        Voice,      // Character barks/dialogue - handled by VoiceManager
        Footstep,   // Footstep sounds - single source loop
        Music       // Background music - separate system
    }
}
