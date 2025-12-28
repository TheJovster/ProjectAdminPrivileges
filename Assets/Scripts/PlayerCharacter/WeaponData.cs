using UnityEngine;
using static ProjectAdminPrivileges.Combat.Weapons.Weapon;
using ProjectAdminPrivileges.Audio;

namespace ProjectAdminPrivileges.Combat.Weapons
{
    public enum ReloadType 
    {
        Individual,
        Magazine
    }

    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Combat/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public WeaponType weaponType;
        public ReloadType reloadType;
        public AnimatorOverrideController animatorOverrideController;
        public string weaponName;
        public int damage;
        public bool isAutomatic = false;
        public int pelletsPerShot;
        public float spreadAngle;
        public int magazineSize;
        public float reloadTime = 0.3f;
        public float fireRate = .1f; //time between shots in seconds
        public float range;
        public float reloadTimePerRound;
        public GameObject projectilePrefab;
        public WeaponAudioData audioData;

        [Header("Visual Effects")]
        public Gradient tracerGradient;
        public float tracerStartWidth = 0.25f;
        public float tracerEndWidth = 0.08f;
    }
}