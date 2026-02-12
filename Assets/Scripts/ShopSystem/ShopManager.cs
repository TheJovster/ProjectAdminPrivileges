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
        [SerializeField] private ProjectAdminPrivileges.PlayerCharacter.PlayerMotor playerMotor;

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

            if (playerMotor == null)
            {
                playerMotor = FindAnyObjectByType<ProjectAdminPrivileges.PlayerCharacter.PlayerMotor>();
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
                    if (playerWeaponHandler != null)
                        playerWeaponHandler.AddTemporaryWeapon(item.weaponPrefab);
                    else
                        Debug.LogError("[ShopManager] PlayerWeaponHandler is null — can't add weapon!");
                    break;

                case ShopItemType.AbilityUnlock:
                    if (abilityManager != null)
                        abilityManager.AddTemporaryAbility(item.abilityPrefab);
                    else
                        Debug.LogError("[ShopManager] AbilityManager is null — can't add ability!");
                    break;

                case ShopItemType.HealQueen:
                    if (QueenHealth.Instance != null)
                        QueenHealth.Instance.Heal(item.healAmount);
                    else
                        Debug.LogError("[ShopManager] QueenHealth.Instance is null!");
                    break;

                case ShopItemType.DamageBuff:
                    Weapon.ApplyDamageMultiplier(item.damageMultiplier);
                    Debug.Log($"[ShopManager] Applied damage buff: x{item.damageMultiplier}");
                    break;

                case ShopItemType.FireRateBuff:
                    Weapon.ApplyFireRateMultiplier(item.fireRateMultiplier);
                    Debug.Log($"[ShopManager] Applied fire rate buff: x{item.fireRateMultiplier}");
                    break;

                case ShopItemType.MoveSpeedBuff:
                    if (playerMotor != null)
                        playerMotor.ApplySpeedMultiplier(item.moveSpeedMultiplier);
                    else
                        Debug.LogError("[ShopManager] PlayerMotor is null — can't apply speed buff!");
                    break;

                case ShopItemType.AbilityCooldownBuff:
                    // Reduce all current cooldowns by multiplier
                    if (abilityManager != null)
                        abilityManager.ResetAllCooldowns();
                    Debug.Log($"[ShopManager] Applied ability cooldown buff: x{item.abilityCooldownMultiplier}");
                    break;

                case ShopItemType.AmmoRefill:
                    // Refill current weapon ammo via reload
                    if (playerWeaponHandler != null && playerWeaponHandler.CurrentWeapon != null)
                        playerWeaponHandler.TryReloadCurrentWeapon();
                    Debug.Log("[ShopManager] Applied ammo refill");
                    break;

                case ShopItemType.ExtraLife:
                    // TODO: Implement extra life system — needs PlayerHealth.AddExtraLife()
                    Debug.LogWarning("[ShopManager] ExtraLife not yet implemented — need extra life system on PlayerHealth");
                    break;

                default:
                    Debug.LogWarning($"[ShopManager] Unhandled item type: {item.itemType}");
                    break;
            }
        }

        public void ResetShop()
        {
            purchasedThisRun.Clear();

            // Reset all per-run buff multipliers
            Weapon.ResetBuffMultipliers();

            if (playerMotor != null)
                playerMotor.ResetSpeedMultiplier();

            Debug.Log("[ShopManager] Shop reset — all purchases and buffs cleared");
        }

        public bool IsItemPurchased(ShopItem item)
        {
            return purchasedThisRun.Contains(item);
        }
    }
}