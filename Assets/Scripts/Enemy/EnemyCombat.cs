using UnityEngine;
using ProjectAdminPrivileges.Combat.Weapons;

/// <summary>
/// Enemy combat logic - attacks whatever EnemyController is targeting.
/// Targets dynamically switch between Player and Queen based on proximity.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyCombat : MonoBehaviour
{
    private EnemyController controller;
    private EnemyHealth health;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1f;

    private float lastAttackTime = 0f;

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
        health = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        if (!health.IsAlive) return;

        // Get current target from controller (Player or Queen, whichever is closer)
        Transform currentTarget = controller.CurrentTarget;
        if (currentTarget == null) return;

        float distance = controller.DistanceToTarget();

        if (distance <= attackRange)
        {
            TryAttack(currentTarget);
        }
    }

    private void TryAttack(Transform target)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        IDamageable targetHealth = target.GetComponent<IDamageable>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(attackDamage);
            lastAttackTime = Time.time;

            // Debug log shows what we attacked
            string targetName = target.CompareTag("Player") ? "Player" : "Queen";
            Debug.Log($"[EnemyCombat] Enemy attacked {targetName} for {attackDamage} damage!");
        }
    }
}