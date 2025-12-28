using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.ShopSystem
{
    public class CreditManager : MonoBehaviour
    {
        //Instance for Singleton Pattern
        public static CreditManager Instance { get; private set; }

        [Header("Current Run")]
        [SerializeField]private int currentCredits = 0; // Credits available in the current run

        [Header("Earning Rates")]
        [SerializeField] private int creditsPerEnemy = 10; // Credits earned per enemy defeated -> start at 10, modify in editor
        [SerializeField] private int creditsPerTank = 10; // Credits earned per tank defeated -> start at 10, modify in editor
        [SerializeField] private int creditsPerDrone = 5; // Credits earned per drone defeated -> start at 5, modify in editor
        [SerializeField] private int creditsPerBoss = 50; // Credits earned per boss defeated -> start at 50, modify in editor -> maybe have this in the Boss SO?
        [SerializeField] private int creditsPerDay = 25; // Credits earned per day completed
        [SerializeField] private int creditsPerFlawlessDay = 25; // Bonus for completing a day without taking damage

        //public properties to access private field amounts
        public int CreditsPerEnemy => creditsPerEnemy;
        public int CurrentCredits => currentCredits;

        //public events
        public event Action<int> OnCreditsChanged;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        public void AddCredits(int amount)
        {
            currentCredits += amount;
            OnCreditsChanged?.Invoke(currentCredits);
        }

        public bool TrySpendCredits(int amount)
        {
            if (currentCredits >= amount)
            {
                currentCredits -= amount;
                OnCreditsChanged?.Invoke(currentCredits);
                return true;
            }
            return false;
        }

        public void ResetCredits()
        {
            currentCredits = 0;
            OnCreditsChanged?.Invoke(currentCredits);
        }

    }

}