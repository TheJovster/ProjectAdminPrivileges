using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAdminPrivileges.Abilities;
using ProjectAdminPrivileges.Combat.Weapons;
using ProjectAdminPrivileges.PlayerCharacter;

public class GameUIManager : MonoBehaviour
{
    [Header("Player Health UI")]
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private Slider playerHealthBar;
    [SerializeField] private PlayerHealth playerHealth; //TODO: Add player health script

    [Header("Boss Health UI")]
    [SerializeField] private Slider bossHealthBar;
    [SerializeField] private EnemyHealth bossHealth;

    [Header("References")]
    [SerializeField] private QueenHealth queenHealth;
    [SerializeField] private PlayerWeaponHandler weaponHandler;
    [SerializeField] private AbilityManager abilityManager;
    private EnemyHealth currentBossHealth;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI queenHealthText;
    [SerializeField] private Slider queenHealthBar;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI weaponName;
    [SerializeField] private Image abilityCooldownFill;
    [SerializeField] private TextMeshProUGUI abilityCooldownText;

    private void Start()
    {
        if (queenHealth != null)
        {
            queenHealth.OnHealthChanged += UpdateQueenHealth;
            UpdateQueenHealth(queenHealth.CurrentHealth, queenHealth.MaxHealth);
        }

        // NEW: Subscribe to weapon changes
        if (weaponHandler != null)
        {
            weaponHandler.OnWeaponChanged += OnWeaponChanged;

            if (weaponHandler.CurrentWeapon != null)
            {
                OnWeaponChanged(weaponHandler.CurrentWeapon);
            }
        }
        if (abilityManager == null) 
        {
            abilityManager = FindFirstObjectByType<AbilityManager>();
        }

        // NEW: Player health
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdatePlayerHealth;
            UpdatePlayerHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if(bossHealthBar != null && bossHealthBar.gameObject.activeInHierarchy) 
        {
            //add the boss health subscription
            //update BossHealth
        }

    }

    private void Update()
    {
        UpdateAmmo();
        UpdateAbilityCooldown();
    }

    private void UpdateQueenHealth(int current, int max)
    {
        if (queenHealthText != null)
        {
            queenHealthText.text = $"Queen: {current}/{max}";
        }

        if (queenHealthBar != null)
        {
            queenHealthBar.maxValue = max;
            queenHealthBar.value = current;
        }
    }
    private void UpdatePlayerHealth(int current, int max)
    {
        if (playerHealthText != null)
        {
            playerHealthText.text = $"{current}/{max}";
        }

        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = max;
            playerHealthBar.value = current;
        }
    }

    private void UpdateBossHealth(int current, int max)
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.maxValue = max;
            bossHealthBar.value = current;
        }
    }

    private void UpdateAmmo()
    {
        if (weaponHandler == null || weaponHandler.CurrentWeapon == null || ammoText == null) return;

        var weapon = weaponHandler.CurrentWeapon;
        ammoText.text = $"Ammo: {weapon.CurrentAmmoInMag}";
    }

    public void UpdateWave(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {waveNumber}";
        }
    }

    private void UpdateAbilityCooldown()
    {
        if (abilityManager == null) return;

        var currentAbility = abilityManager.GetCurrentAbility();
        if (currentAbility == null) return;

        if (abilityCooldownFill != null)
        {
            float fillAmount = currentAbility.IsOnCooldown
                ? (1f - (currentAbility.CooldownRemaining / currentAbility.Data.Cooldown))
                : 1f;
            abilityCooldownFill.fillAmount = fillAmount;
        }

        if (abilityCooldownText != null)
        {
            abilityCooldownText.text = currentAbility.IsOnCooldown
                ? $"{Mathf.CeilToInt(currentAbility.CooldownRemaining)}s"
                : "READY";
        }
    }

    public void SubscribeToBossHealth(EnemyHealth bossHealth)
    {
        // Unsubscribe from previous boss (if any)
        if (currentBossHealth != null)
        {
            currentBossHealth.OnHealthChanged -= UpdateBossHealthBar;
        }

        // Subscribe to new boss
        currentBossHealth = bossHealth;
        currentBossHealth.OnHealthChanged += UpdateBossHealthBar;

        // Initial update
        UpdateBossHealthBar();
    }

    public void UnsubscribeFromBossHealth(EnemyHealth bossHealth)
    {
        if (currentBossHealth != null)
        {
            currentBossHealth.OnHealthChanged -= UpdateBossHealthBar;
            currentBossHealth = null;
        }
    }

    public void SetBossHealthBar(bool value)
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(value);
        }
    }

    private void UpdateBossHealthBar()
    {
        if (currentBossHealth == null || bossHealthBar == null) { return; }

        bossHealthBar.maxValue = currentBossHealth.MaxHealth;
        bossHealthBar.value = currentBossHealth.CurrentHealth;
    }

    private void OnWeaponChanged(ProjectAdminPrivileges.Combat.Weapons.Weapon weapon)
    {
        if (weaponName != null && weapon != null)
        {
            weaponName.text = weapon.GetWeaponData().weaponName;
        }
    }


}