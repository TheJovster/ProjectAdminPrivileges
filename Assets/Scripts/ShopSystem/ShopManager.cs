using ProjectAdminPrivileges.Abilities;
using ProjectAdminPrivileges.Combat.Weapons;
using ProjectAdminPrivileges.UI;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.ShopSystem
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Available Items")]
        [SerializeField] private ShopItem[] weaponItems;
        [SerializeField] private ShopItem[] abilityItems;
        [SerializeField] private ShopItem[] consumableItems;
        [SerializeField] private ShopItem[] buffItems;

        [Header("UI")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private ShopUI shopUI;

        [Header("Current Run Purchases")]
        private List<ShopItem> purchasedThisRun = new List<ShopItem>();

        // Public properties to expose items to ShopUI
        public ShopItem[] WeaponItems => weaponItems;
        public ShopItem[] AbilityItems => abilityItems;
        public ShopItem[] ConsumableItems => consumableItems;
        public ShopItem[] BuffItems => buffItems;

        [Header("References")]
        [SerializeField] private PlayerWeaponHandler playerWeaponHandler;
        [SerializeField] private AbilityManager abilityManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

            if (playerWeaponHandler == null)
            {
                playerWeaponHandler = FindAnyObjectByType<PlayerWeaponHandler>();
            }

            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }
        }

        public void OpenShop()
        {
            shopPanel.SetActive(true);
            //shopUI.PopulateShop(weaponItems, abilityItems, consumableItems, buffItems);
            GameManager.Instance.SetGameState(GameManager.GameState.Shopping);
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
            GameManager.Instance.SetGameState(GameManager.GameState.Playing);
        }

        public void PurchaseItem(ShopItem item)
        {
            if (!CreditManager.Instance.TrySpendCredits(item.kredCost))
            {
                Debug.Log("[Shop] Not enough Kredit!");
                return;
            }

            purchasedThisRun.Add(item);
            ApplyItem(item);

            //shopUI.RefreshUI(); // Update button states
        }

        private void ApplyItem(ShopItem item)
        {
            switch (item.itemType)
            {
                case ShopItemType.WeaponUnlock:
                    playerWeaponHandler.AddTemporaryWeapon(item.weaponPrefab);
                    break;

                case ShopItemType.AbilityUnlock:
                    abilityManager.AddTemporaryAbility(item.abilityPrefab);
                    break;

                case ShopItemType.HealQueen:
                    QueenHealth.Instance.Heal(item.healAmount);
                    break;

                case ShopItemType.DamageBuff:
                    //PlayerStats.Instance.ApplyDamageBuff(item.damageMultiplier);
                    break;

            }
        }

        public void ResetShop()
        {
            purchasedThisRun.Clear();
        }

        public bool IsItemPurchased(ShopItem item)
        {
            return purchasedThisRun.Contains(item);
        }
    }
}