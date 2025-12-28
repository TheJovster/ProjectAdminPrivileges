using System;
using System.Collections.Generic;

namespace ProjectAdminPrivileges.Persistence
{
    /// <summary>
    /// Serializable data structure for ExperienceManager state.
    /// </summary>
    [Serializable]
    public class ExperienceData
    {
        // Core XP tracking
        public int totalExperience;
        public int currentRunExperience; // Reset on new run, but saved for "Continue Run" feature later

        // Run statistics
        public int totalRuns; // Number of times player has started a run
        public int completedRuns; // Number of runs that reached victory condition
        public int highestWaveReached;
        public int totalKills;

        // Unlocks (weapon IDs)
        public List<string> unlockedWeaponIDs = new List<string>();

        // Future: Unlocked upgrades, abilities, etc.
        // public List<string> unlockedUpgradeIDs = new List<string>();
        // public List<string> unlockedAbilityIDs = new List<string>();
    }
}