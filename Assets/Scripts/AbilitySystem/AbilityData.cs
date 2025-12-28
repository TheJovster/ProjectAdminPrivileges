using UnityEngine;
using ProjectAdminPrivileges.Audio;

namespace ProjectAdminPrivileges.Abilities
{
    [CreateAssetMenu(fileName = "NewAbilityData", menuName = "Abilities/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("Ability Info")]
        [SerializeField] private string abilityName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Cooldown")]
        [SerializeField] private float cooldown = 30f;

        [Header("Damage")]
        [SerializeField] private int damage = 200;
        [SerializeField] private float radius = 10f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject explosionVFXPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClipData strikeSound;

        public AudioClipData StrikeSound => strikeSound;

        // Properties
        public string AbilityName => abilityName;
        public string Description => description;
        public float Cooldown => cooldown;
        public int Damage => damage;
        public float Radius => radius;
        public GameObject ExplosionVFXPrefab => explosionVFXPrefab;
    }
}