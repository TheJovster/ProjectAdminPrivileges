using UnityEngine;
using System;
using ProjectAdminPrivileges.Combat.Weapons;
using ProjectAdminPrivileges.Combat.Effects;
using ProjectAdminPrivileges.ShopSystem;

[RequireComponent(typeof(EnemyController))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject bloodEffectPrefab;

    private int currentHealth;
    private EnemyController controller;
    private EnemyDeathHandler deathHandler; // Optional death handler
    private Collider capsuleColider;

    // Track last hit info for death handler
    private int lastDamageAmount;
    private Vector3 lastHitPoint;
    private Vector3 lastHitDirection;

    public bool IsAlive => currentHealth > 0;
    public int MaxHealth => maxHealth; // Expose for death handler
    public event Action OnDeath;

    public int CurrentHealth => currentHealth; // Expose for UI or other systems

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
        deathHandler = GetComponent<EnemyDeathHandler>(); // Optional component
        capsuleColider = GetComponent<Collider>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void SetMaxHealth(int health)
    {
        maxHealth = health;
        currentHealth = health;
    }

    // Old interface method (for compatibility)
    public void TakeDamage(int damageAmount)
    {
        TakeDamageAtPoint(damageAmount, transform.position);
    }

    // New method with hit point
    public void TakeDamageAtPoint(int damageAmount, Vector3 hitPoint)
    {
        if (!IsAlive) return;

        // Store damage info for death handler
        lastDamageAmount = damageAmount;
        lastHitPoint = hitPoint;
        lastHitDirection = (hitPoint - transform.position).normalized;

        // Notify death handler of damage (for overkill tracking)
        if (deathHandler != null)
        {
            deathHandler.RegisterDamage(damageAmount, hitPoint);
        }

        currentHealth -= damageAmount;

        // Spawn damage number
        if (DamageNumberSpawner.Instance != null)
        {
            DamageNumberSpawner.Instance.SpawnDamageNumber(damageAmount, hitPoint);
        }

        // Spawn blood effect at exact hit location (only if not dead yet)
        if (bloodEffectPrefab != null && currentHealth > 0)
        {
            GameObject blood = Instantiate(bloodEffectPrefab, hitPoint, Quaternion.identity);
            ParticleSystem ps = blood.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(blood, ps.main.duration + 0.1f);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // Stop movement
        controller.StopMovement();
        controller.enabled = false;
        capsuleColider.enabled = false;

        // Award credits
        if (ProjectAdminPrivileges.ShopSystem.CreditManager.Instance != null)
        {
            CreditManager.Instance.AddCredits(
                CreditManager.Instance.CreditsPerEnemy
            );
        }

        // Invoke death event (for WaveManager kill tracking)
        OnDeath?.Invoke();

        // Use death handler if present
        if (deathHandler != null)
        {
            deathHandler.ExecuteDeath();
            // Death handler will destroy gameObject when ready
        }
        else
        {
            // Fallback: immediate destruction
            Debug.LogWarning($"{gameObject.name}: No EnemyDeathHandler attached, using instant death");
            Destroy(gameObject, 0.1f);
        }
    }
}