using UnityEngine;
using System.Collections;

namespace ProjectAdminPrivileges.Combat.Effects
{
    /// <summary>
    /// Handles enemy death animations, effects, and gib spawning.
    /// Attach to enemy prefab alongside EnemyHealth.
    /// </summary>
    public class EnemyDeathHandler : MonoBehaviour
    {
        [Header("Death Type Settings")]
        [SerializeField] private DeathType deathType = DeathType.Random;
        [SerializeField, Range(0f, 1f)] private float gibChance = 0.3f;
        [SerializeField, Range(0f, 1f)] private float overkillThreshold = 0.5f; // Damage > 50% max HP = increased gib chance

        [Header("Animation Settings")]
        [SerializeField] private string deathTrigger = "Death";
        [SerializeField] private float deathAnimationDuration = 1.5f;
        [SerializeField] private bool ragdollOnDeath = false; // Future: ragdoll physics

        [Header("Gib Settings")]
        [SerializeField] private GameObject gibPrefab;
        [SerializeField] private int minGibs = 4;
        [SerializeField] private int maxGibs = 8;
        [SerializeField] private float explosionForce = 300f;
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private float upwardModifier = 1.5f;
        [SerializeField] private float gibSpreadAngle = 45f;

        [Header("VFX")]
        [SerializeField] private GameObject bloodExplosionPrefab;
        [SerializeField] private GameObject deathParticlesPrefab;
        [SerializeField] private GameObject characterModel;

        [Header("Audio")]
        [SerializeField] private Audio.AudioClipData deathSound;
        [SerializeField] private Audio.AudioClipData gibSound;

        private Animator animator;
        private EnemyHealth enemyHealth;
        private MeshRenderer[] meshRenderers;

        // Track last damage for overkill detection
        private int lastDamage;
        private Vector3 lastHitPoint;
        private Vector3 lastHitDirection;
        

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            enemyHealth = GetComponent<EnemyHealth>();
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }

        /// <summary>
        /// Called by EnemyHealth when damage is taken. Store data for death decision.
        /// </summary>
        public void OnDamageTaken(int damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            lastDamage = damage;
            lastHitPoint = hitPoint;
            lastHitDirection = hitDirection;
        }

        /// <summary>
        /// Main death execution. Called by EnemyHealth.Die()
        /// </summary>
        public void ExecuteDeath()
        {
            DeathType actualType = DetermineDeathType();

            switch (actualType)
            {
                case DeathType.Normal:
                    StartCoroutine(NormalDeath());
                    break;
                case DeathType.Gib:
                    StartCoroutine(GibDeath());
                    break;
            }
        }

        /// <summary>
        /// Determines which death type to use based on conditions
        /// </summary>
        private DeathType DetermineDeathType()
        {
            // If explicitly set, use that
            if (deathType != DeathType.Random)
                return deathType;

            // Calculate gib score from multiple factors
            float gibScore = gibChance;

            // Overkill bonus
            if (enemyHealth != null)
            {
                float damageRatio = (float)lastDamage / enemyHealth.MaxHealth;
                if (damageRatio > overkillThreshold)
                {
                    gibScore += 0.3f;
                }
            }

            // Close range bonus
            float distance = Vector3.Distance(lastHitPoint, transform.position);
            if (distance < 3f)
            {
                gibScore += 0.15f;
            }

            // TODO: Weapon type bonus (shotgun, explosive)
            // if (lastWeaponType == WeaponType.Shotgun) gibScore += 0.2f;

            return Random.value < gibScore ? DeathType.Gib : DeathType.Normal;
        }

        /// <summary>
        /// Standard death with animation
        /// </summary>
        private IEnumerator NormalDeath()
        {
            // Play death particles
            if (deathParticlesPrefab != null)
            {
                GameObject particles = Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
                Destroy(particles, 3f);
            }

            // Play death sound
            if (deathSound != null && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayAudio(deathSound, transform.position);
            }

            // Trigger death animation if animator exists
            if (animator != null)
            {
                animator.SetTrigger(deathTrigger);
                yield return new WaitForSeconds(deathAnimationDuration);
            }
            else
            {
                // Fallback: just wait a moment before destroying
                yield return new WaitForSeconds(0.5f);
            }

            // Destroy the enemy
            Destroy(gameObject);
        }

        /// <summary>
        /// Explosive gib death
        /// </summary>
        private IEnumerator GibDeath()
        {
            // Hide original mesh immediately
            //meshrenderers are too slow
            /*            if (meshRenderers != null)
                        {
                            foreach (var renderer in meshRenderers)
                            {
                                renderer.enabled = false;
                            }
                        }*/




            // Spawn blood explosion at center
            if (bloodExplosionPrefab != null)
            {
                GameObject explosion = Instantiate(bloodExplosionPrefab, transform.position + Vector3.up, Quaternion.identity);
                Destroy(explosion, 3f);
            }

            //spawn death effect
            if(deathParticlesPrefab!= null)
            {
                GameObject deathParticle = Instantiate(deathParticlesPrefab, transform.position + Vector3.up, Quaternion.identity);
                Destroy(deathParticle, 3f);
            }

            // Play gib sound
            if (gibSound != null && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayAudio(gibSound, transform.position);
            }

            yield return new WaitForSeconds(0.05f);
            characterModel.SetActive(false);


            // Spawn gibs
            if (gibPrefab != null)
            {
                int gibCount = Random.Range(minGibs, maxGibs + 1);

                for (int i = 0; i < gibCount; i++)
                {
                    SpawnGib();
                }
            }

            // Wait a moment before destroying the base object
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        /// <summary>
        /// Spawns a single gib with physics
        /// </summary>
        private void SpawnGib()
        {
            // Spawn position: slight offset from center
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.3f;
            Quaternion spawnRot = Random.rotation;

            GameObject gib = Instantiate(gibPrefab, spawnPos, spawnRot);

            // Apply explosion force
            Rigidbody rb = gib.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Direction: away from hit point + random spread
                Vector3 baseDirection = (spawnPos - lastHitPoint).normalized;
                Vector3 randomSpread = Random.insideUnitSphere * gibSpreadAngle;
                Vector3 finalDirection = (baseDirection + randomSpread).normalized;

                // Apply force
                rb.AddForce(finalDirection * explosionForce, ForceMode.Impulse);

                // Add upward component
                rb.AddForce(Vector3.up * explosionForce * upwardModifier, ForceMode.Impulse);

                // Add random torque for spinning
                Vector3 randomTorque = new Vector3(
                    Random.Range(-10f, 10f),
                    Random.Range(-10f, 10f),
                    Random.Range(-10f, 10f)
                );
                rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Call this from EnemyHealth to track damage info
        /// </summary>
        public void RegisterDamage(int damage, Vector3 hitPoint)
        {
            lastDamage = damage;
            lastHitPoint = hitPoint;
            lastHitDirection = (hitPoint - transform.position).normalized;
        }

        /// <summary>
        /// Force this enemy to gib on death (e.g., from orbital strike)
        /// </summary>
        public void ForceGibDeath()
        {
            deathType = DeathType.Gib;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize explosion radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
#endif
    }

    public enum DeathType
    {
        Normal,     // Standard death animation
        Gib,        // Explosive gibs
        Random      // Choose based on conditions
    }
}