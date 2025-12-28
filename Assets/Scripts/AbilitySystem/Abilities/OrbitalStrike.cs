using System.Collections;
using UnityEngine;
using ProjectAdminPrivileges.Combat.Weapons;

namespace ProjectAdminPrivileges.Abilities
{
    public class OrbitalStrike : AbilityBase
    {
        [Header("Orbital Strike Settings")]
        [SerializeField] private float strikeDelay = 2.0f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private GameObject warningIndicatorPrefab;
        [SerializeField] private float cameraShakeIntensity = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugSphere = true;

        protected override void Activate(Vector3 targetPosition)
        {
            StartCoroutine(OrbitalStrikeSequence(targetPosition));
        }

        private IEnumerator OrbitalStrikeSequence(Vector3 targetPosition)
        {
            // Spawn warning indicator
            GameObject warningIndicator = null;
            if (warningIndicatorPrefab != null)
            {
                warningIndicator = Instantiate(warningIndicatorPrefab, targetPosition, Quaternion.identity);
                Debug.Log($"[OrbitalStrike] Warning indicator spawned at {targetPosition}");
            }

            // Wait for strike delay
            yield return new WaitForSeconds(strikeDelay);

            // Destroy warning indicator
            if (warningIndicator != null)
            {
                Destroy(warningIndicator);
            }

            // Execute strike
            ExecuteStrike(targetPosition);
        }

        private void ExecuteStrike(Vector3 targetPosition)
        {
            Debug.Log($"[OrbitalStrike] Strike executed at {targetPosition}");

            // Spawn explosion VFX
            if (abilityData.ExplosionVFXPrefab != null)
            {
                GameObject explosion = Instantiate(abilityData.ExplosionVFXPrefab, targetPosition, Quaternion.identity);

                ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(explosion, ps.main.duration + 0.5f);
                }
                else
                {
                    Destroy(explosion, 3f);
                }
            }

            // Play sound effect
            if (abilityData.StrikeSound != null && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayAudio(abilityData.StrikeSound, targetPosition, 1.5f);
            }

            // Camera shake
            if (Camera.main != null)
            {
                var cameraController = Camera.main.GetComponentInParent<PlayerCharacter.CameraController>();
                if (cameraController != null)
                {
                    cameraController.Shake(cameraShakeIntensity);
                }
            }

            // Damage enemies in radius - FORCE GIB DEATHS
            Collider[] hitColliders = Physics.OverlapSphere(targetPosition, abilityData.Radius, enemyLayer);
            int enemiesHit = 0;

            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Force gib on orbital strike kills
                    var deathHandler = hitCollider.GetComponent<Combat.Effects.EnemyDeathHandler>();
                    if (deathHandler != null)
                    {
                        deathHandler.ForceGibDeath();
                    }

                    damageable.TakeDamageAtPoint(abilityData.Damage, targetPosition);
                    enemiesHit++;
                }
            }

            Debug.Log($"[OrbitalStrike] Hit {enemiesHit} enemies for {abilityData.Damage} damage each");
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugSphere || abilityData == null) return;

            // Draw radius sphere at mouse position when selected in editor
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, abilityData.Radius);
        }
    }
}