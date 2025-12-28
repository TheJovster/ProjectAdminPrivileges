using ProjectAdminPrivileges.Combat.Weapons;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Abilities
{
    public class AbilityManager : MonoBehaviour
    {

        [Header("Abilities")]
        [SerializeField] private List<AbilityBase> abilities = new List<AbilityBase>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        [SerializeField]private int currentAbilityIndex = 0;


        private void Awake()
        {
            // Get all AbilityBase components on this GameObject or children
            AbilityBase[] foundAbilities = GetComponentsInChildren<AbilityBase>();

            if (foundAbilities.Length > 0)
            {
                abilities.Clear();
                abilities.AddRange(foundAbilities);

                if (showDebugLogs)
                {
                    Debug.Log($"[AbilityManager] Found {abilities.Count} abilities");
                    foreach (var ability in abilities)
                    {
                        Debug.Log($"  - {ability.Data.AbilityName}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AbilityManager] No abilities found! Add AbilityBase components as children.");
            }
        }

        /// <summary>
        /// Try to activate the current ability at target position
        /// </summary>
        public void TryActivateCurrentAbility(Vector3 targetPosition)
        {
            if (abilities.Count == 0)
            {
                Debug.LogWarning("[AbilityManager] No abilities available!");
                return;
            }

            AbilityBase currentAbility = abilities[currentAbilityIndex];

            if (showDebugLogs)
            {
                Debug.Log($"[AbilityManager] Attempting to activate {currentAbility.Data.AbilityName}");
            }

            currentAbility.TryActivate(targetPosition);
        }

        /// <summary>
        /// Get the currently selected ability
        /// </summary>
        public AbilityBase GetCurrentAbility()
        {
            if (abilities.Count == 0) return null;
            return abilities[currentAbilityIndex];
        }

        /// <summary>
        /// Switch to next ability (for future when you have multiple)
        /// </summary>
        public void SwitchToNextAbility()
        {
            if (abilities.Count <= 1) return;

            currentAbilityIndex = (currentAbilityIndex + 1) % abilities.Count;

            if (showDebugLogs)
            {
                Debug.Log($"[AbilityManager] Switched to {abilities[currentAbilityIndex].Data.AbilityName}");
            }
        }

        /// <summary>
        /// Switch to previous ability (for future when you have multiple)
        /// </summary>
        public void SwitchToPreviousAbility()
        {
            if (abilities.Count <= 1) return;

            currentAbilityIndex = (currentAbilityIndex - 1 + abilities.Count) % abilities.Count;

            if (showDebugLogs)
            {
                Debug.Log($"[AbilityManager] Switched to {abilities[currentAbilityIndex].Data.AbilityName}");
            }
        }
        /// <summary>
        /// Add temporary ability for current run only
        /// </summary>
        public void AddTemporaryAbility(GameObject abilityPrefab)
        {
            GameObject abilityObj = Instantiate(abilityPrefab, transform);
            AbilityBase ability = abilityObj.GetComponent<AbilityBase>();
            abilities.Add(ability);

            Debug.Log($"[AbilityManager] Temporary ability added: {ability.Data.AbilityName}");
        }

        /// <summary>
        /// Load permanent unlocks from ArsenalManager
        /// </summary>
        public void LoadUnlockedAbilities()
        {
            List<GameObject> unlockedAbilities = ArsenalManager.Instance.GetUnlockedAbilities();

            foreach (GameObject abilityPrefab in unlockedAbilities)
            {
                GameObject abilityObj = Instantiate(abilityPrefab, transform);
                AbilityBase ability = abilityObj.GetComponent<AbilityBase>();
                abilities.Add(ability);
            }
        }


        public void ResetAllCooldowns()
        {
            foreach (var ability in abilities)
            {
                ability.ResetCooldown();
            }
        }
    }
}