using System.Collections;
using UnityEngine;
using ProjectAdminPrivileges.PlayerCharacter;

namespace ProjectAdminPrivileges.Enemy.Boss
{
    public enum BossState
    {
        Idle,
        Chase,
        MeleeAttack,
        RangedAttack
    }

    [RequireComponent(typeof(EnemyHealth), typeof(Boss1AI))]
    public class Boss1Character : MonoBehaviour
    {
        // State
        private BossState currentState = BossState.Idle;
        private float stateTimer = 0f;

        // Cooldowns
        private float meleeCooldown = 0f;
        private float rangedCooldown = 0f;

        // Enrage
        private bool isEnraged = false;

        // References
        private EnemyHealth health;
        private Boss1AI ai;
        private Transform playerTransform;
        private PlayerController playerController;

        [Header("Combat Settings")]
        [SerializeField] private float normalSpeed = 3.5f;
        [SerializeField] private float chargeSpeedMultiplier = 1.75f;
        [SerializeField] private int meleeDamage = 15;
        [SerializeField] private float meleeKnockbackDistance = 10f;
        [SerializeField] private float meleeRange = 2f;
        [SerializeField] private float meleePauseDuration = 1f;

        [Header("Ranged Attack")]
        [SerializeField] private float rangedRange = 5f;
        [SerializeField] private int projectileDamage = 5;
        [SerializeField] private int projectileHealth = 10;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileSpawnDelay = 0.8f;
        [SerializeField] private int projectileCount = 5;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint; // Boss chest height

        [Header("Cooldowns")]
        [SerializeField] private float meleeCooldownDuration = 1f;
        [SerializeField] private float rangedCooldownDuration = 1.5f;

        [Header("Enrage")]
        [SerializeField] private float enrageThreshold = 0.4f;
        [SerializeField] private bool useEnragedChargeSpeed = true;
        [SerializeField] private float enragedProjectileSpeedMultiplier = 1.2f;

        [Header("Initial Delay")]
        [SerializeField] private float idleDuration = 2f;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            ai = GetComponent<Boss1AI>();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
            }

            if (projectileSpawnPoint == null)
            {
                // Create spawn point at chest height if not assigned
                GameObject spawnObj = new GameObject("ProjectileSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.up * 1.5f;
                projectileSpawnPoint = spawnObj.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // Update timers
            stateTimer += Time.deltaTime;
            if (meleeCooldown > 0f) meleeCooldown -= Time.deltaTime;
            if (rangedCooldown > 0f) rangedCooldown -= Time.deltaTime;

            // Check enrage
            float healthPercent = (float)health.CurrentHealth / health.MaxHealth;
            isEnraged = healthPercent < enrageThreshold;

            // Run state machine
            UpdateStateMachine();
        }

        private void UpdateStateMachine()
        {
            float distanceToPlayer = ai.DistanceToTarget(playerTransform.position);

            switch (currentState)
            {
                case BossState.Idle:
                    if (stateTimer >= idleDuration)
                    {
                        TransitionTo(BossState.Chase);
                    }
                    break;

                case BossState.Chase:
                    // Move towards player
                    float speed = isEnraged && useEnragedChargeSpeed ? chargeSpeedMultiplier : 1f;
                    ai.MoveToTarget(playerTransform.position, speed);

                    // Decision: Melee or Ranged?
                    if (meleeCooldown <= 0f && distanceToPlayer < meleeRange)
                    {
                        TransitionTo(BossState.MeleeAttack);
                    }
                    else if (rangedCooldown <= 0f && distanceToPlayer > rangedRange && !isEnraged)
                    {
                        // Only use ranged when not enraged (enraged prefers melee)
                        TransitionTo(BossState.RangedAttack);
                    }
                    else if (rangedCooldown <= 0f && distanceToPlayer > rangedRange && isEnraged)
                    {
                        // Enraged can still use ranged, but less preferred
                        if (Random.value > 0.3f) // 70% chance to ignore ranged when enraged
                        {
                            TransitionTo(BossState.RangedAttack);
                        }
                    }
                    break;

                case BossState.MeleeAttack:
                    // Check if player is still in range
                    if (distanceToPlayer < meleeRange)
                    {
                        ExecuteMeleeAttack();
                        meleeCooldown = meleeCooldownDuration;
                        StartCoroutine(MeleePauseRoutine());
                    }
                    else
                    {
                        // Player escaped
                        TransitionTo(BossState.Chase);
                    }
                    break;

                case BossState.RangedAttack:
                    // Already handled by coroutine
                    break;
            }
        }

        private void TransitionTo(BossState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            switch (newState)
            {
                case BossState.Idle:
                    ai.StopMovement();
                    break;

                case BossState.Chase:
                    ai.ResumeMovement();
                    ai.ResetSpeed();
                    break;

                case BossState.MeleeAttack:
                    ai.StopMovement();
                    break;

                case BossState.RangedAttack:
                    ai.StopMovement();
                    StartCoroutine(RangedAttackRoutine());
                    break;
            }
        }

        private void ExecuteMeleeAttack()
        {
            // Deal damage
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(meleeDamage);
            }

            // Apply knockback
            if (playerController != null)
            {
                playerController.ApplyKnockback(transform.position, meleeKnockbackDistance);
            }
        }

        private IEnumerator MeleePauseRoutine()
        {
            yield return new WaitForSeconds(meleePauseDuration);
            TransitionTo(BossState.Chase);
        }

        private IEnumerator RangedAttackRoutine()
        {
            yield return new WaitForSeconds(1.5f); // Longer cast time - boss stands still

            // Spawn projectiles
            for (int i = 0; i < projectileCount; i++)
            {
                SpawnProjectile();
                yield return new WaitForSeconds(projectileSpawnDelay);
            }

            // Total time locked: 1.5s + (5 * 0.8s) = 5.5 seconds standing still
            rangedCooldown = rangedCooldownDuration;
            TransitionTo(BossState.Chase);
        }

        private void SpawnProjectile()
        {
            if (projectilePrefab == null || playerTransform == null) return;

            GameObject projGO = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            Boss1Projectile proj = projGO.GetComponent<Boss1Projectile>();

            if (proj != null)
            {
                float speed = isEnraged ? projectileSpeed * enragedProjectileSpeedMultiplier : projectileSpeed;
                proj.Initialize(playerTransform, projectileDamage, projectileHealth, speed);
            }
        }

        private void OnDestroy()
        {
            // Stop all coroutines when boss dies
            StopAllCoroutines();
        }
    }
}