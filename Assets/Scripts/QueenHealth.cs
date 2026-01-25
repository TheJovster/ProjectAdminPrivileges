using UnityEngine;
using System;
using ProjectAdminPrivileges.Combat.Weapons;

public class QueenHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int currentHealth;

    public static QueenHealth Instance { get; private set; }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

            currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamageAtPoint(int damageAmount, Vector3 hitPoint)
    {
        TakeDamage(damageAmount);
    }
    public void Heal(int amount)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Die()
    {
        Debug.Log("Queen has died! Game Over!");
        OnDeath?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.GameOver);
        }
    }
}