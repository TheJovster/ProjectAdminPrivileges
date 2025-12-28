using System;
using System.Collections.Generic;

namespace ProjectAdminPrivileges.Persistence
{
    /// <summary>
    /// Root save data structure. Contains all persistent game data.
    /// Serialized to JSON file.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Metadata
        public string saveVersion = "1.0";
        public string lastSaveDate; // ISO 8601 format
        public float totalPlayTime; // Seconds

        // Component-specific data (keyed by component name)
        // Dictionary serializes as JSON object: { "ExperienceManager": {...}, "UnlockManager": {...} }
        public Dictionary<string, string> componentData = new Dictionary<string, string>();

        public SaveData()
        {
            lastSaveDate = DateTime.UtcNow.ToString("o"); // ISO 8601
        }
    }
}