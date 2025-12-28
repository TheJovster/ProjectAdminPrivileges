using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Combat.Weapons
{
    public class PlayerWeaponHandler : MonoBehaviour
    {

        [SerializeField] private GameObject weaponContainer;
        [SerializeField] private PlayerCharacter.PlayerIKController ikController;
        [SerializeField] private PlayerCharacter.PlayerAnimatorController animatorController;
        [SerializeField] private Transform playerAimPoint;

        private Weapon currentWeapon;
        public Weapon CurrentWeapon => currentWeapon;

        private List<Weapon> weapons = new List<Weapon>();
        private int currentWeaponIndex = 0;

        public event Action<Weapon> OnWeaponChanged;

        private void Awake()
        {
            if (weaponContainer == null)
                weaponContainer = transform.Find("WeaponContainer").gameObject;

            if (ikController == null)
                ikController = GetComponent<PlayerCharacter.PlayerIKController>();

            if (animatorController == null)
                animatorController = GetComponent<PlayerCharacter.PlayerAnimatorController>();

            foreach (Weapon weapon in weaponContainer.GetComponentsInChildren<Weapon>(true))
            {
                weapons.Add(weapon);
                weapon.gameObject.SetActive(false);
            }

            if (weapons.Count > 0)
                EquipWeapon(0);
        }

        private void LateUpdate()
        {
            if (ikController != null && currentWeapon != null)
            {
                ikController.UpdateProxyTargets(
                    currentWeapon.GetLeftHandGrip(),
                    currentWeapon.GetRightHandGrip(),
                    currentWeapon.GetLeftHandHint(),
                    currentWeapon.GetRightHandHint()
                );
            }
        }

        public void EquipWeapon(int index)
        {
            if (weapons.Count == 0) return;
            if (index < 0 || index >= weapons.Count) return;

            if (currentWeapon != null)
                currentWeapon.gameObject.SetActive(false);

            currentWeaponIndex = index;
            currentWeapon = weapons[index];
            currentWeapon.gameObject.SetActive(true);

            if (playerAimPoint != null)
                currentWeapon.SetAimPoint(playerAimPoint);

            if (ikController != null)
            {
                ikController.SetHandTargets(
                    currentWeapon.GetLeftHandGrip(),
                    currentWeapon.GetRightHandGrip(),
                    currentWeapon.GetLeftHandHint(),
                    currentWeapon.GetRightHandHint()
                );
                ConfigureIKForWeapon(currentWeapon);
            }

            if (animatorController != null)
            {
                animatorController.SetWeaponAnimations(
                    currentWeapon.GetWeaponData().animatorOverrideController
                );
            }

            OnWeaponChanged?.Invoke(currentWeapon);
        }

        private void ConfigureIKForWeapon(Weapon weapon)
        {
            WeaponData data = weapon.GetWeaponData();

            switch(data.weaponType)
            {
                case Weapon.WeaponType.Pistol:
                    ikController.SetHandWeights(1f, 0.25f); // Right hand only (trigger)
                    break;

                case Weapon.WeaponType.Rifle:
                    ikController.SetHandWeights(1.0f, 0.25f); //right hand needs to be 0.25 max
                    break;
                case Weapon.WeaponType.Shotgun:
                    ikController.SetHandWeights(1.0f, 0.25f);
                    break;
                case Weapon.WeaponType.SMG:
                    ikController.SetHandWeights(1f, 1f); // Both hands on weapon
                    break;

                default:
                    ikController.SetHandWeights(1f, 1f);
                    break;
            }
        }

        public bool TryFireCurrentWeapon(bool buttonHeld, bool buttonTriggered)
        {
            return currentWeapon != null && currentWeapon.TryFireWeapon(buttonHeld, buttonTriggered);
        }

        public void TryReloadCurrentWeapon()
        {
            currentWeapon?.StartReload();
        }

        public void SwitchToNextWeapon()
        {
            if (weapons.Count <= 1) return;
            EquipWeapon((currentWeaponIndex + 1) % weapons.Count);
        }

        public void SwitchToPreviousWeapon()
        {
            if (weapons.Count <= 1) return;
            EquipWeapon((currentWeaponIndex - 1 + weapons.Count) % weapons.Count);
        }

        public void AddTemporaryWeapon(GameObject weaponPrefab)
        {
            if (weaponPrefab == null)
            {
                Debug.LogError("[PlayerWeaponHandler] Null weapon prefab!");
                return;
            }

            GameObject weaponObj = Instantiate(weaponPrefab, weaponContainer.transform);
            Weapon weapon = weaponObj.GetComponent<Weapon>();

            if (weapon == null)
            {
                Debug.LogError($"[PlayerWeaponHandler] Prefab {weaponPrefab.name} has no Weapon component!");
                Destroy(weaponObj);
                return;
            }

            weapons.Add(weapon);
            weaponObj.SetActive(false);

            Debug.Log($"[PlayerWeaponHandler] Temporary weapon added: {weapon.GetWeaponData().weaponName}. Total weapons: {weapons.Count}");

            // Equip it immediately if it's the only weapon
            if (weapons.Count == 1)
            {
                EquipWeapon(0);
            }
        }

        /// <summary>
        /// Load all permanently unlocked weapons at run start (called by GameManager)
        /// </summary>
        public void LoadUnlockedWeapons()
        {
            if (ArsenalManager.Instance == null)
            {
                Debug.LogWarning("[PlayerWeaponHandler] ArsenalManager.Instance is null - can't load unlocks");
                return;
            }

            List<GameObject> unlockedWeapons = ArsenalManager.Instance.GetUnlockedWeapons();

            Debug.Log($"[PlayerWeaponHandler] Loading {unlockedWeapons.Count} unlocked weapons");

            foreach (GameObject weaponPrefab in unlockedWeapons)
            {
                GameObject weaponObj = Instantiate(weaponPrefab, weaponContainer.transform);
                Weapon weapon = weaponObj.GetComponent<Weapon>();
                weapons.Add(weapon);
                weaponObj.SetActive(false);
            }

            if (weapons.Count > 0)
            {
                EquipWeapon(0);
            }
        }
    }
}