using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    /// <summary>
    /// ScriptableObject that holds all audio data for a weapon.
    /// Reference this in WeaponData to keep audio data organized.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponAudioData", menuName = "Audio/Weapon Audio Data")]
    public class WeaponAudioData : ScriptableObject
    {
        [Header("Weapon Sounds")]
        [Tooltip("Sound played when weapon fires")]
        [SerializeField] private AudioClipData fireSound;

        [Tooltip("Sound played during reload")]
        [SerializeField] private AudioClipData reloadSound;

        [Tooltip("Sound played when trigger pulled with empty magazine")]
        [SerializeField] private AudioClipData emptyClickSound;

        [Header("Optional Sounds")]
        [Tooltip("Sound played when aiming down sights (optional)")]
        [SerializeField] private AudioClipData aimSound;

        [Tooltip("Sound played when switching to this weapon (optional)")]
        [SerializeField] private AudioClipData equipSound;

        public AudioClipData FireSound => fireSound;
        public AudioClipData ReloadSound => reloadSound;
        public AudioClipData EmptyClickSound => emptyClickSound;
        public AudioClipData AimSound => aimSound;
        public AudioClipData EquipSound => equipSound;
    }
}
