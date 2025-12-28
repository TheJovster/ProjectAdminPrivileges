using System;
using UnityEngine;

namespace ProjectAdminPrivileges.ShopSystem
{
    public class IAExperienceManager : MonoBehaviour
    {
        public static IAExperienceManager Instance { get; private set; }

        [Header("Current Run Tracking")]
        [SerializeField] private int xpThisRun = 0;
        [SerializeField] private int killsThisRun = 0;
        [SerializeField] private int daysThisRun = 0;

        [Header("Persistent Tracking")]
        [SerializeField] private int totalIAXP = 0;

        [Header("Earning Rates")]
        [SerializeField] private int xpPerKill = 2;
        [SerializeField] private int xpPerDay = 10;
        [SerializeField] private float xpPerQueenHP = 0.1f;
        [SerializeField] private int xpPerRelationshipStage = 50;

        public event Action<int> OnIAXPChanged;

        public int XPThisRun => xpThisRun;
        public int KillsThisRun => killsThisRun;
        public int DaysThisRun => daysThisRun;
        public int TotalIAXP => totalIAXP;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // CRITICAL: Load saved IA XP on startup
            LoadIAXP();
            Debug.Log($"[IAExperienceManager] Loaded {totalIAXP} IA XP from save");
        }

        public void OnKill()
        {
            killsThisRun++;
            xpThisRun += xpPerKill;
        }

        public void OnDayComplete()
        {
            daysThisRun++;
            xpThisRun += xpPerDay;
        }

        public void OnRunEnd(int queenHPRemaining, int relationshipStage)
        {
            xpThisRun += Mathf.RoundToInt(queenHPRemaining * xpPerQueenHP);
            xpThisRun += relationshipStage * xpPerRelationshipStage;

            totalIAXP += xpThisRun;
            SaveIAXP();

            OnIAXPChanged?.Invoke(totalIAXP); // Pass total, not this run

            Debug.Log($"[IA XP] Run complete: +{xpThisRun} XP (Total: {totalIAXP})");

            // Reset run tracking for next run
            xpThisRun = 0;
            killsThisRun = 0;
            daysThisRun = 0;
        }

        public bool TrySpendIAXP(int amount)
        {
            if (totalIAXP >= amount)
            {
                totalIAXP -= amount;
                SaveIAXP();
                OnIAXPChanged?.Invoke(totalIAXP);
                Debug.Log($"[IAExperienceManager] Spent {amount} IA XP. Remaining: {totalIAXP}");
                return true;
            }

            Debug.LogWarning($"[IAExperienceManager] Not enough IA XP! Need {amount}, have {totalIAXP}");
            return false;
        }

        private void SaveIAXP()
        {
            PlayerPrefs.SetInt("TotalIAXP", totalIAXP);
            PlayerPrefs.Save();
            Debug.Log($"[IAExperienceManager] Saved {totalIAXP} IA XP");
        }

        private void LoadIAXP()
        {
            totalIAXP = PlayerPrefs.GetInt("TotalIAXP", 0);
        }

        /// <summary>
        /// Debug method to add IA XP for testing
        /// </summary>
        [ContextMenu("Add 1000 Test IA XP")]
        private void AddTestIAXP()
        {
            totalIAXP += 1000;
            SaveIAXP();
            OnIAXPChanged?.Invoke(totalIAXP);
            Debug.Log($"[IAExperienceManager] Added 1000 test IA XP. Total: {totalIAXP}");
        }

        /// <summary>
        /// Debug method to reset IA XP
        /// </summary>
        [ContextMenu("Reset IA XP")]
        private void ResetIAXP()
        {
            totalIAXP = 0;
            SaveIAXP();
            OnIAXPChanged?.Invoke(totalIAXP);
            Debug.Log("[IAExperienceManager] Reset IA XP to 0");
        }

        [ContextMenu("DELETE ALL PLAYERPREFS")]
        private void ClearAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.LogWarning("[DEBUG] ALL PLAYERPREFS DELETED!");
        }
    }
}