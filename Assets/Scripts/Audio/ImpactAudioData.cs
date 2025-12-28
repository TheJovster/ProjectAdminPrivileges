using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    /// <summary>
    /// ScriptableObject that maps a surface type to its impact sounds.
    /// Create one for each surface type (Concrete, Metal, Flesh, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "NewImpactAudioData", menuName = "Audio/Impact Audio Data")]
    public class ImpactAudioData : ScriptableObject
    {
        [Header("Surface Type")]
        [SerializeField] private SurfaceType surfaceType;

        [Header("Impact Sound")]
        [Tooltip("Sound played when projectile hits this surface")]
        [SerializeField] private AudioClipData impactSound;

        public SurfaceType SurfaceType => surfaceType;
        public AudioClipData ImpactSound => impactSound;
    }
}
