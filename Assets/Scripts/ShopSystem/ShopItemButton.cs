using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAdminPrivileges.ShopSystem;
using ProjectAdminPrivileges.Combat.Weapons;

namespace ProjectAdminPrivileges.UI
{
    /// <summary>
    /// Individual shop item button component.
    /// Displays item info and handles purchase interaction.
    /// </summary>
    public class ShopItemButton : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;

        [Header("Optional - Owned Indicator")]
        [SerializeField] private GameObject ownedOverlay;

        private ShopItem shopItem;
        private ShopUI shopUI;

        /// <summary>
        /// Called by ShopUI to initialize this button
        /// </summary>
        public void Setup(ShopItem shopItem, ShopUI ui)
        {
            this.shopItem = shopItem;
            shopUI = ui;

            // Set display info
            if (iconImage != null && this.shopItem.icon != null)
            {
                iconImage.sprite = this.shopItem.icon;
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            if (nameText != null)
                nameText.text = this.shopItem.itemName;

            if (descriptionText != null)
                descriptionText.text = this.shopItem.description;

            if (costText != null)
                costText.text = $"{this.shopItem.kredCost} Credits";

            // INITIALIZE DEFAULT COLORS HERE
            ResetColors(); // <-- NEW METHOD

            // Hook up button
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }

            RefreshButton(shopUI);
        }


        /// <summary>
        /// Update button interactable state based on credits and ownership
        /// </summary>
        public void RefreshButton(ShopUI shopUI)
        {
            this.shopUI = shopUI;

            ResetColors(); // <-- Reset to default before applying states

            bool isPurchased = ShopManager.Instance != null && ShopManager.Instance.IsItemPurchased(shopItem);
            bool canAfford = CreditManager.Instance != null && CreditManager.Instance.CurrentCredits >= shopItem.kredCost;
            bool isUnlocked = IsItemUnlocked();

            if (isPurchased)
            {
                buyButton.interactable = false;
                buyButtonText.text = "OWNED";
                if (ownedOverlay != null) ownedOverlay.SetActive(true);
            }
            else if (!isUnlocked)
            {
                buyButton.interactable = false;
                buyButtonText.text = "LOCKED";

                // Override with gray
                nameText.color = Color.gray;
                descriptionText.color = Color.gray;
                costText.color = Color.gray;
                iconImage.color = Color.gray;
            }
            else if (!canAfford)
            {
                buyButton.interactable = false;
                buyButtonText.text = "BUY";
                // Colors already reset to default
            }
            else
            {
                buyButton.interactable = true;
                buyButtonText.text = "BUY";
                // Colors already reset to default
            }
        }
        private bool IsItemUnlocked()
        {
            if (ArsenalManager.Instance == null) return true; // Default to unlocked if no manager

            // Consumables and buffs are always unlocked
            if (shopItem.itemType == ShopItemType.HealQueen ||
                shopItem.itemType == ShopItemType.AmmoRefill ||
                shopItem.itemType == ShopItemType.ExtraLife ||
                shopItem.itemType == ShopItemType.DamageBuff ||
                shopItem.itemType == ShopItemType.FireRateBuff ||
                shopItem.itemType == ShopItemType.MoveSpeedBuff ||
                shopItem.itemType == ShopItemType.AbilityCooldownBuff)
            {
                return true;
            }

            // Weapons and abilities require Arsenal unlock
            if (string.IsNullOrEmpty(shopItem.requiredUnlockID))
            {
                Debug.LogWarning($"[ShopItemButton] {shopItem.itemName} has no requiredUnlockID!");
                return false; // Default to locked
            }

            return ArsenalManager.Instance.IsUnlocked(shopItem.requiredUnlockID);
        }


        private void OnBuyClicked()
        {
            if (shopUI != null && shopItem != null)
            {
                shopUI.OnItemPurchased(shopItem);
            }
        }

        private void ResetColors()
        {
            if (nameText != null) nameText.color = Color.white;
            if (descriptionText != null) descriptionText.color = Color.white;
            if (costText != null) costText.color = Color.yellow;
            if (iconImage != null) iconImage.color = Color.white;
        }
    }
}