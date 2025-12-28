using System.Collections.Generic;
using UnityEngine;
using ProjectAdminPrivileges.ShopSystem;

namespace ProjectAdminPrivileges.Combat.Weapons
{
    public class ArsenalManager : MonoBehaviour
    {
        public static ArsenalManager Instance;

        [Header("All Unlockables")]
        [SerializeField] private ArsenalUnlock[] allUnlocks;

        [Header("Persistent State")]
        private HashSet<string> unlockedIDs = new HashSet<string>();

        private void Awake()
        {
            // CRITICAL FIX: Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadUnlocks();
        }

        public void PurchaseUnlock(ArsenalUnlock unlock)
        {
            if (unlock == null)
            {
                Debug.LogError("[Arsenal] Cannot purchase NULL unlock!");
                return;
            }

            if (IsUnlocked(unlock.UnlockID))
            {
                Debug.Log($"[Arsenal] {unlock.unlockName} already unlocked!");
                return;
            }

            if (!IAExperienceManager.Instance.TrySpendIAXP(unlock.iaCost))
            {
                Debug.Log($"[Arsenal] Not enough IA XP! Need {unlock.iaCost}, have {IAExperienceManager.Instance.TotalIAXP}");
                return;
            }

            unlockedIDs.Add(unlock.UnlockID);
            SaveUnlocks();

            Debug.Log($"[Arsenal] Unlocked: {unlock.unlockName}");
        }

        public bool IsUnlocked(string unlockID)
        {
            return unlockedIDs.Contains(unlockID);
        }

        /// <summary>
        /// Get all unlocks for Arsenal UI display
        /// </summary>
        public ArsenalUnlock[] GetAllUnlocks()
        {
            return allUnlocks;
        }

        public List<GameObject> GetUnlockedWeapons()
        {
            // NOTE: Weapons are instantiated via Shop purchases, not Arsenal unlocks
            // ArsenalUnlock just gates what appears in Shop
            return new List<GameObject>();
        }

        public List<GameObject> GetUnlockedAbilities()
        {
            // NOTE: Abilities are instantiated via Shop purchases, not Arsenal unlocks
            // ArsenalUnlock just gates what appears in Shop
            return new List<GameObject>();
        }

        private void SaveUnlocks()
        {
            string json = JsonUtility.ToJson(new UnlockData(unlockedIDs));
            PlayerPrefs.SetString("ArsenalUnlocks", json);
            PlayerPrefs.Save();
        }

        private void LoadUnlocks()
        {
            string json = PlayerPrefs.GetString("ArsenalUnlocks", "");
            if (!string.IsNullOrEmpty(json))
            {
                UnlockData data = JsonUtility.FromJson<UnlockData>(json);
                unlockedIDs = new HashSet<string>(data.unlockedIDs);
            }

            // Apply default unlocks (items marked as starting unlocks)
            ApplyDefaultUnlocks();
        }

        private void ApplyDefaultUnlocks()
        {
            if (allUnlocks == null) return;

            bool addedAny = false;
            foreach (var unlock in allUnlocks)
            {
                if (unlock != null && unlock.IsDefaultUnlocked && !unlockedIDs.Contains(unlock.UnlockID))
                {
                    unlockedIDs.Add(unlock.UnlockID);
                    addedAny = true;
                    Debug.Log($"[Arsenal] Applied default unlock: {unlock.unlockName}");
                }
            }

            if (addedAny)
            {
                SaveUnlocks(); // Persist default unlocks
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate all unlocks for duplicate IDs and missing data.
        /// Right-click ArsenalManager component → Validate All Unlocks
        /// </summary>
        [ContextMenu("Validate All Unlocks")]
        private void ValidateAllUnlocks()
        {
            if (allUnlocks == null || allUnlocks.Length == 0)
            {
                Debug.LogWarning("[Arsenal] No unlocks assigned!");
                return;
            }

            HashSet<string> seenIDs = new HashSet<string>();
            int duplicates = 0;
            int missing = 0;

            foreach (var unlock in allUnlocks)
            {
                if (unlock == null)
                {
                    Debug.LogError("[Arsenal] NULL unlock in array!");
                    continue;
                }

                if (string.IsNullOrEmpty(unlock.UnlockID))
                {
                    Debug.LogError($"[Arsenal] {unlock.unlockName} has EMPTY unlockID!");
                    missing++;
                    continue;
                }

                if (seenIDs.Contains(unlock.UnlockID))
                {
                    Debug.LogError($"[Arsenal] DUPLICATE ID: {unlock.UnlockID} ({unlock.unlockName})");
                    duplicates++;
                }
                else
                {
                    seenIDs.Add(unlock.UnlockID);
                }
            }

            if (duplicates == 0 && missing == 0)
            {
                Debug.Log($"[Arsenal] ✅ All {allUnlocks.Length} unlocks valid!");
            }
            else
            {
                Debug.LogError($"[Arsenal] ❌ Found {duplicates} duplicates, {missing} missing IDs");
            }
        }

        [ContextMenu("Clear All Unlocks (Reset Save)")]
        private void ClearAllUnlocks()
        {
            PlayerPrefs.DeleteKey("ArsenalUnlocks");
            PlayerPrefs.Save();
            unlockedIDs.Clear();
            Debug.LogWarning("[Arsenal] All unlocks CLEARED! (Save data deleted)");
        }
#endif

        [System.Serializable]
        private class UnlockData
        {
            public List<string> unlockedIDs;
            public UnlockData(HashSet<string> ids)
            {
                unlockedIDs = new List<string>(ids);
            }
        }
    }
}