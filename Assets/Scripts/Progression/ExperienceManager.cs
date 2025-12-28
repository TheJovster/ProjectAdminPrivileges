using UnityEngine;
using System.Collections.Generic;
using ProjectAdminPrivileges.Persistence;

namespace ProjectAdminPrivileges.Progression
{
    /// <summary>
    /// Manages player XP, run statistics, and weapon unlocks.
    /// Implements ISaveable to persist progression between sessions.
    /// </summary>
    public class ExperienceManager : MonoBehaviour, ISaveable
    {
        public static ExperienceManager Instance { get; private set; }

        [Header("Experience")]
        [SerializeField] private int totalExperience = 0;
        [SerializeField] private int currentRunExperience = 0;

        [Header("Run Statistics")]
        [SerializeField] private int totalRuns = 0;
        [SerializeField] private int completedRuns = 0;
        [SerializeField] private int highestWaveReached = 0;
        [SerializeField] private int totalKills = 0;

        [Header("Unlocks")]
        [SerializeField] private List<string> unlockedWeaponIDs = new List<string> { "weapon_rifle", "weapon_shotgun" }; // Starting weapons

        // Events for UI updates
        public System.Action<int> OnExperienceChanged;
        public System.Action<string> OnWeaponUnlocked;

        // Public accessors
        public int TotalExperience => totalExperience;
        public int CurrentRunExperience => currentRunExperience;
        public int TotalRuns => totalRuns;
        public int CompletedRuns => completedRuns;
        public int HighestWaveReached => highestWaveReached;
        public int TotalKills => totalKills;
        public List<string> UnlockedWeaponIDs => new List<string>(unlockedWeaponIDs); // Return copy to prevent external modification

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register with save system
            if (SaveWrapper.Instance != null)
            {
                SaveWrapper.Instance.RegisterSaveable("ExperienceManager", this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from save system
            if (SaveWrapper.Instance != null)
            {
                SaveWrapper.Instance.UnregisterSaveable("ExperienceManager");
            }
        }

        /// <summary>
        /// Add XP to both current run and total XP.
        /// </summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;

            currentRunExperience += amount;
            totalExperience += amount;

            OnExperienceChanged?.Invoke(totalExperience);

            Debug.Log($"[ExperienceManager] Added {amount} XP. Run: {currentRunExperience}, Total: {totalExperience}");
        }

        /// <summary>
        /// Called when a new run starts. Resets current run stats.
        /// </summary>
        public void StartNewRun()
        {
            currentRunExperience = 0;
            totalRuns++;

            Debug.Log($"[ExperienceManager] Started run #{totalRuns}");
        }

        /// <summary>
        /// Called when run ends (player/queen death). Updates statistics.
        /// </summary>
        public void EndRun(int wavesReached, int killsThisRun)
        {
            if (wavesReached > highestWaveReached)
            {
                highestWaveReached = wavesReached;
                Debug.Log($"[ExperienceManager] New highest wave: {highestWaveReached}");
            }

            totalKills += killsThisRun;

            Debug.Log($"[ExperienceManager] Run ended. Waves: {wavesReached}, Kills: {killsThisRun}");
        }

        /// <summary>
        /// Called when player reaches victory condition (Wave 10, etc.)
        /// </summary>
        public void CompleteRun()
        {
            completedRuns++;
            Debug.Log($"[ExperienceManager] Run completed! Total completions: {completedRuns}");
        }

        /// <summary>
        /// Unlock a weapon by ID. Returns true if newly unlocked, false if already unlocked.
        /// </summary>
        public bool UnlockWeapon(string weaponID)
        {
            if (string.IsNullOrEmpty(weaponID))
            {
                Debug.LogWarning("[ExperienceManager] Attempted to unlock weapon with null/empty ID");
                return false;
            }

            if (unlockedWeaponIDs.Contains(weaponID))
            {
                Debug.Log($"[ExperienceManager] Weapon '{weaponID}' already unlocked");
                return false;
            }

            unlockedWeaponIDs.Add(weaponID);
            OnWeaponUnlocked?.Invoke(weaponID);

            Debug.Log($"[ExperienceManager] Unlocked weapon: {weaponID}");
            return true;
        }

        /// <summary>
        /// Check if a weapon is unlocked.
        /// </summary>
        public bool IsWeaponUnlocked(string weaponID)
        {
            return unlockedWeaponIDs.Contains(weaponID);
        }

        /// <summary>
        /// Spend XP on an unlock. Returns true if successful (had enough XP), false otherwise.
        /// </summary>
        public bool SpendExperience(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("[ExperienceManager] Attempted to spend non-positive XP amount");
                return false;
            }

            if (totalExperience < amount)
            {
                Debug.LogWarning($"[ExperienceManager] Not enough XP. Have: {totalExperience}, Need: {amount}");
                return false;
            }

            totalExperience -= amount;
            OnExperienceChanged?.Invoke(totalExperience);

            Debug.Log($"[ExperienceManager] Spent {amount} XP. Remaining: {totalExperience}");
            return true;
        }

        // ==================== ISaveable Implementation ====================

        public object CaptureState()
        {
            ExperienceData data = new ExperienceData
            {
                totalExperience = this.totalExperience,
                currentRunExperience = this.currentRunExperience,
                totalRuns = this.totalRuns,
                completedRuns = this.completedRuns,
                highestWaveReached = this.highestWaveReached,
                totalKills = this.totalKills,
                unlockedWeaponIDs = new List<string>(this.unlockedWeaponIDs) // Copy list
            };

            return data;
        }

        public void RestoreState(object state)
        {
            // State comes in as JSON string from SaveWrapper
            string json = state as string;
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[ExperienceManager] RestoreState received null/empty JSON");
                return;
            }

            ExperienceData data = JsonUtility.FromJson<ExperienceData>(json);
            if (data == null)
            {
                Debug.LogError("[ExperienceManager] Failed to deserialize ExperienceData");
                return;
            }

            // Restore all fields
            this.totalExperience = data.totalExperience;
            this.currentRunExperience = data.currentRunExperience;
            this.totalRuns = data.totalRuns;
            this.completedRuns = data.completedRuns;
            this.highestWaveReached = data.highestWaveReached;
            this.totalKills = data.totalKills;
            this.unlockedWeaponIDs = data.unlockedWeaponIDs ?? new List<string> { "weapon_rifle", "weapon_shotgun" };

            // Notify listeners
            OnExperienceChanged?.Invoke(totalExperience);

            Debug.Log($"[ExperienceManager] State restored. Total XP: {totalExperience}, Runs: {totalRuns}, Weapons: {unlockedWeaponIDs.Count}");
        }
    }
}