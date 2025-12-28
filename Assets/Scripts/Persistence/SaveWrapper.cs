using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectAdminPrivileges.Persistence
{
    /// <summary>
    /// Central save system manager. Orchestrates save/load operations across all ISaveable components.
    /// Uses JSON serialization to persist data to disk.
    /// </summary>
    public class SaveWrapper : MonoBehaviour
    {
        public static SaveWrapper Instance { get; private set; }

        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "savegame.json";
        [Tooltip("Enable to save to PlayerPrefs instead of file (useful for WebGL)")]
        [SerializeField] private bool usePlayerPrefs = false;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

        // Registry of all saveable components
        private Dictionary<string, ISaveable> saveables = new Dictionary<string, ISaveable>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Register a component to participate in save/load system.
        /// Call this in component's Awake/OnEnable.
        /// </summary>
        public void RegisterSaveable(string uniqueID, ISaveable saveable)
        {
            if (saveables.ContainsKey(uniqueID))
            {
                Debug.LogWarning($"[SaveWrapper] Saveable with ID '{uniqueID}' already registered. Overwriting.");
            }
            saveables[uniqueID] = saveable;
            Debug.Log($"[SaveWrapper] Registered saveable: {uniqueID}");
        }

        /// <summary>
        /// Unregister a component (call in OnDestroy if component can be destroyed).
        /// </summary>
        public void UnregisterSaveable(string uniqueID)
        {
            if (saveables.Remove(uniqueID))
            {
                Debug.Log($"[SaveWrapper] Unregistered saveable: {uniqueID}");
            }
        }

        /// <summary>
        /// Save all registered saveables to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                SaveData saveData = new SaveData();

                // Capture state from all registered saveables
                foreach (var kvp in saveables)
                {
                    string componentID = kvp.Key;
                    ISaveable saveable = kvp.Value;

                    object state = saveable.CaptureState();
                    if (state == null)
                    {
                        Debug.LogWarning($"[SaveWrapper] {componentID}.CaptureState() returned null. Skipping.");
                        continue;
                    }

                    // Serialize component state to JSON string
                    string stateJson = JsonUtility.ToJson(state);
                    saveData.componentData[componentID] = stateJson;
                }

                // Serialize root SaveData to JSON
                string json = JsonUtility.ToJson(saveData, prettyPrint: true);

                // Write to disk or PlayerPrefs
                if (usePlayerPrefs)
                {
                    PlayerPrefs.SetString("SaveData", json);
                    PlayerPrefs.Save();
                    Debug.Log("[SaveWrapper] Saved to PlayerPrefs");
                }
                else
                {
                    File.WriteAllText(SaveFilePath, json);
                    Debug.Log($"[SaveWrapper] Saved to {SaveFilePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveWrapper] Save failed: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Load all saved data and restore state to registered saveables.
        /// </summary>
        public void Load()
        {
            try
            {
                string json = null;

                // Read from disk or PlayerPrefs
                if (usePlayerPrefs)
                {
                    if (PlayerPrefs.HasKey("SaveData"))
                    {
                        json = PlayerPrefs.GetString("SaveData");
                        Debug.Log("[SaveWrapper] Loaded from PlayerPrefs");
                    }
                    else
                    {
                        Debug.Log("[SaveWrapper] No save data found in PlayerPrefs");
                        return;
                    }
                }
                else
                {
                    if (File.Exists(SaveFilePath))
                    {
                        json = File.ReadAllText(SaveFilePath);
                        Debug.Log($"[SaveWrapper] Loaded from {SaveFilePath}");
                    }
                    else
                    {
                        Debug.Log($"[SaveWrapper] No save file found at {SaveFilePath}");
                        return;
                    }
                }

                // Deserialize root SaveData
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData == null)
                {
                    Debug.LogError("[SaveWrapper] Failed to deserialize SaveData");
                    return;
                }

                // Restore state to all registered saveables
                foreach (var kvp in saveables)
                {
                    string componentID = kvp.Key;
                    ISaveable saveable = kvp.Value;

                    if (saveData.componentData.TryGetValue(componentID, out string stateJson))
                    {
                        // Each component deserializes its own state structure
                        // Component must know its own data type (handled in RestoreState implementation)
                        saveable.RestoreState(stateJson);
                        Debug.Log($"[SaveWrapper] Restored state for {componentID}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SaveWrapper] No saved data found for {componentID}");
                    }
                }

                Debug.Log($"[SaveWrapper] Load complete. Save version: {saveData.saveVersion}, Last save: {saveData.lastSaveDate}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveWrapper] Load failed: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        public bool SaveExists()
        {
            if (usePlayerPrefs)
            {
                return PlayerPrefs.HasKey("SaveData");
            }
            else
            {
                return File.Exists(SaveFilePath);
            }
        }

        /// <summary>
        /// Delete the save file (use for "New Game" or testing).
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (usePlayerPrefs)
                {
                    PlayerPrefs.DeleteKey("SaveData");
                    PlayerPrefs.Save();
                    Debug.Log("[SaveWrapper] Deleted save from PlayerPrefs");
                }
                else
                {
                    if (File.Exists(SaveFilePath))
                    {
                        File.Delete(SaveFilePath);
                        Debug.Log($"[SaveWrapper] Deleted save file at {SaveFilePath}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveWrapper] Delete save failed: {e.Message}");
            }
        }

        // Auto-save on application quit
        private void OnApplicationQuit()
        {
            Debug.Log("[SaveWrapper] Application quitting, auto-saving...");
            Save();
        }
    }
}