using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAdminPrivileges.ShopSystem;

using System.Collections.Generic;
using ProjectAdminPrivileges.Combat.Weapons;
using UnityEngine.Rendering;

namespace ProjectAdminPrivileges.UI
{
    /// <summary>
    /// Main UI controller for the shop system.
    /// Handles displaying items, tabs, and purchasing.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI creditsDisplay;
        [SerializeField] private Transform itemGridParent;
        [SerializeField] private Button readyButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button weaponsTabButton;
        [SerializeField] private Button abilitiesTabButton;
        [SerializeField] private Button consumablesTabButton;
        [SerializeField] private Button buffsTabButton;

        [Header("Prefab")]
        [SerializeField] private GameObject shopItemButtonPrefab;

        private List<GameObject> spawnedButtons = new List<GameObject>();
        private ShopCategory currentCategory = ShopCategory.Weapons;

        private enum ShopCategory
        {
            Weapons,
            Abilities,
            Consumables,
            Buffs
        }

        private void Awake()
        {
            // Hook up tab buttons
            if (weaponsTabButton != null)
                weaponsTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Weapons));
            if (abilitiesTabButton != null)
                abilitiesTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Abilities));
            if (consumablesTabButton != null)
                consumablesTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Consumables));
            if (buffsTabButton != null)
                buffsTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Buffs));

            // Hook up ready button
            if (readyButton != null)
                readyButton.onClick.AddListener(OnReadyClicked);

            // Subscribe to credit changes
            if (CreditManager.Instance != null)
            {
                CreditManager.Instance.OnCreditsChanged += UpdateCreditsDisplay;
            }
        }

        private void OnEnable()
        {
            // Refresh when shop opens
            ShowCategory(ShopCategory.Weapons);
            UpdateCreditsDisplay(CreditManager.Instance != null ? CreditManager.Instance.CurrentCredits : 0);
        }

        private void OnDestroy()
        {
            if (CreditManager.Instance != null)
            {
                CreditManager.Instance.OnCreditsChanged -= UpdateCreditsDisplay;
            }
        }

        private void ShowCategory(ShopCategory category)
        {
            currentCategory = category;
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            // Clear existing buttons
            foreach (GameObject button in spawnedButtons)
            {
                Destroy(button);
            }
            spawnedButtons.Clear();

            // Get items for current category from ShopManager
            ShopItem[] items = GetItemsForCategory(currentCategory);

            if (items == null || items.Length == 0)
            {
                Debug.Log($"[ShopUI] No items in category {currentCategory}");
                return;
            }

            // Spawn button for each item
            foreach (ShopItem item in items)
            {
                if (item != null)
                {
                    CreateItemButton(item);
                }
            }
        }

        private ShopItem[] GetItemsForCategory(ShopCategory category)
        {
            if (ShopManager.Instance == null) return new ShopItem[0];

            ShopItem[] allItems = null;

            switch (category)
            {
                case ShopCategory.Weapons:
                    allItems = ShopManager.Instance.WeaponItems;
                    break;
                case ShopCategory.Abilities:
                    allItems = ShopManager.Instance.AbilityItems;
                    break;
                case ShopCategory.Consumables:
                    allItems = ShopManager.Instance.ConsumableItems;
                    break;
                case ShopCategory.Buffs:
                    allItems = ShopManager.Instance.BuffItems;
                    break;
            }

            if (allItems == null) return new ShopItem[0];

            // NEW: Filter by unlocks
            return FilterByUnlocks(allItems, category);
        }

        private ShopItem[] FilterByUnlocks(ShopItem[] items, ShopCategory category)
        {
            if (ArsenalManager.Instance == null)
            {
                Debug.LogWarning("[ShopUI] ArsenalManager not found - showing all items");
                return items;
            }

            List<ShopItem> unlockedItems = new List<ShopItem>();

            foreach (ShopItem item in items)
            {
                bool isUnlocked = false;

                switch (category)
                {
                    case ShopCategory.Weapons:
                        // Check if weapon is unlocked
                        if (item.itemType == ShopItemType.WeaponUnlock && item.weaponPrefab != null)
                        {
                            // Need to match prefab to unlock ID - we'll need to add this to ShopItem
                            isUnlocked = IsWeaponUnlocked(item);
                        }
                        else
                        {
                            isUnlocked = true; // Non-weapon items always shown
                        }
                        break;

                    case ShopCategory.Abilities:
                        if (item.itemType == ShopItemType.AbilityUnlock && item.abilityPrefab != null)
                        {
                            isUnlocked = IsAbilityUnlocked(item);
                        }
                        else
                        {
                            isUnlocked = true;
                        }
                        break;

                    case ShopCategory.Consumables:
                    case ShopCategory.Buffs:
                        isUnlocked = true; // Consumables/buffs always available
                        break;
                }

                if (isUnlocked)
                {
                    unlockedItems.Add(item);
                }
            }

            return unlockedItems.ToArray();
        }

        private void CreateItemButton(ShopItem item)
        {
            if (shopItemButtonPrefab == null)
            {
                Debug.LogError("[ShopUI] shopItemButtonPrefab is not assigned!");
                return;
            }

            GameObject buttonObj = Instantiate(shopItemButtonPrefab, itemGridParent);
            ShopItemButton buttonScript = buttonObj.GetComponent<ShopItemButton>();

            if (buttonScript != null)
            {
                buttonScript.Setup(item, this);
            }
            else
            {
                Debug.LogError("[ShopUI] ShopItemButton component missing on prefab!");
            }

            spawnedButtons.Add(buttonObj);
        }

        /// <summary>
        /// Called by ShopItemButton when player clicks Buy
        /// </summary>
        public void OnItemPurchased(ShopItem item)
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.PurchaseItem(item);
                RefreshUI();
            }
        }

        /// <summary>
        /// Refresh all button states (after purchase or credit change)
        /// </summary>
        public void RefreshUI()
        {
            foreach (GameObject buttonObj in spawnedButtons)
            {
                ShopItemButton buttonScript = buttonObj.GetComponent<ShopItemButton>();
                if (buttonScript != null)
                {
                    buttonScript.RefreshButton(this);
                }
            }
        }

        private void UpdateCreditsDisplay(int credits)
        {
            if (creditsDisplay != null)
            {
                creditsDisplay.text = $"Credits: {credits}";
            }
        }

        private void OnReadyClicked()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.CloseShop();
            }
        }

        private bool IsWeaponUnlocked(ShopItem item)
        {
            if (ArsenalManager.Instance == null) // <-- ADD THIS
            {
                Debug.LogError("[ShopUI] ArsenalManager.Instance is NULL!");
                return false;
            }

            if (string.IsNullOrEmpty(item.requiredUnlockID))
            {
                Debug.LogWarning($"[ShopUI] Weapon item '{item.itemName}' has no requiredUnlockID set!");
                return false;
            }

            return ArsenalManager.Instance.IsUnlocked(item.requiredUnlockID);
        }

        private bool IsAbilityUnlocked(ShopItem item)
        {
            if (ArsenalManager.Instance == null) // <-- ADD THIS
            {
                Debug.LogError("[ShopUI] ArsenalManager.Instance is NULL!");
                return false;
            }

            if (string.IsNullOrEmpty(item.requiredUnlockID))
            {
                Debug.LogWarning($"[ShopUI] Ability item '{item.itemName}' has no requiredUnlockID set!");
                return false;
            }

            return ArsenalManager.Instance.IsUnlocked(item.requiredUnlockID);
        }
    }
}