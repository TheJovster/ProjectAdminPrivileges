using UnityEngine;
using System;

namespace ProjectAdminPrivileges.Dialogue
{
    /// <summary>
    /// Tracks Queen's affection across runs (persistent).
    /// Singleton - place on empty GameObject in scene.
    /// </summary>
    public class RelationshipManager : MonoBehaviour
    {
        public static RelationshipManager Instance { get; private set; }

        [Header("Affection Thresholds")]
        [SerializeField] private int stage2Threshold = 100;  // Familiar
        [SerializeField] private int stage3Threshold = 300;  // Trusted
        [SerializeField] private int stage4Threshold = 600;  // Loving
        [SerializeField] private int stage5Threshold = 1000; // Soulbound

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        [SerializeField]private int currentAffection = 0;

        public int CurrentAffection => currentAffection;
        public event Action<int> OnAffectionChanged;
        public event Action<int> OnStageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAffection();
        }

        public void ModifyAffection(int amount)
        {
            int previousStage = GetCurrentStage();

            currentAffection += amount;
            currentAffection = Mathf.Max(0, currentAffection); // Can't go below 0

            if (showDebugLogs)
            {
                Debug.Log($"[RelationshipManager] Affection changed by {amount}. Total: {currentAffection}");
            }

            OnAffectionChanged?.Invoke(currentAffection);

            int newStage = GetCurrentStage();
            if (newStage != previousStage)
            {
                OnStageChanged?.Invoke(newStage);
                if (showDebugLogs)
                {
                    Debug.Log($"[RelationshipManager] Stage changed to {newStage}!");
                }
            }

            SaveAffection();
        }

        public int GetCurrentStage()
        {
            if (currentAffection >= stage5Threshold) return 5;
            if (currentAffection >= stage4Threshold) return 4;
            if (currentAffection >= stage3Threshold) return 3;
            if (currentAffection >= stage2Threshold) return 2;
            return 1;
        }

        private void SaveAffection()
        {
            PlayerPrefs.SetInt("QueenAffection", currentAffection);
            PlayerPrefs.Save();
        }

        private void LoadAffection()
        {
            currentAffection = PlayerPrefs.GetInt("QueenAffection", 0);
            if (showDebugLogs)
            {
                Debug.Log($"[RelationshipManager] Loaded affection: {currentAffection} (Stage {GetCurrentStage()})");
            }
        }

        // Debug helper
        [ContextMenu("Reset Affection")]
        private void ResetAffection()
        {
            currentAffection = 0;
            SaveAffection();
            Debug.Log("[RelationshipManager] Affection reset to 0");
        }
    }
}