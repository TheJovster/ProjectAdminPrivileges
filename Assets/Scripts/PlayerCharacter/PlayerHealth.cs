using UnityEngine;
using System;
using System.Collections;

namespace ProjectAdminPrivileges.PlayerCharacter
{
    /// <summary>
    /// Manages player health, damage, death, and damage feedback systems.
    /// Attach to Player GameObject.
    /// Uses observer pattern to notify GameManager of death.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, Combat.Weapons.IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;

        [Header("Damage Feedback - Camera")]
        [SerializeField] private float damageScreenShakeIntensity = 0.3f;
        [SerializeField] private float damageVignetteDuration = 0.3f;
        [SerializeField] private Color damageVignetteColor = new Color(1f, 0f, 0f, 0.5f); // Red, 50% alpha

        [Header("Damage Feedback - Visual")]
        [SerializeField] private GameObject bloodEffectPrefab;
        [SerializeField] private Transform bloodSpawnPoint; // Above player head

        [Header("Damage Feedback - Audio")]
        [SerializeField] private Audio.AudioClipData damageSound;

        [Header("Invulnerability")]
        [SerializeField] private float invulnerabilityDuration = 0.5f;
        private float invulnerabilityTimer = 0f;

        [Header("References")]
        [SerializeField] private UnityEngine.UI.Image vignetteImage; // Assign red vignette UI element

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Public properties
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;

        // Observer pattern events
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnDeath; // Subscribed by GameManager

        private CameraController cameraController;
        private Coroutine vignetteCoroutine;


        private void Awake()
        {
            cameraController = FindAnyObjectByType<CameraController>();

            if (bloodSpawnPoint == null)
            {
                // Create spawn point above player if not assigned
                GameObject spawnObj = new GameObject("BloodSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.up * 1.5f; // Above head
                bloodSpawnPoint = spawnObj.transform;
            }
        }

        private void Start()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Ensure vignette starts invisible
            if (vignetteImage != null)
            {
                Color c = vignetteImage.color;
                c.a = 0f;
                vignetteImage.color = c;
            }

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerHealth] Initialized with {currentHealth}/{maxHealth} HP");
            }
        }

        private void Update()
        {
            if (invulnerabilityTimer > 0f)
            {
                invulnerabilityTimer -= Time.deltaTime;
            }
        }

        public void TakeDamage(int damageAmount)
        {
            TakeDamageAtPoint(damageAmount, transform.position + Vector3.up * 1.5f);
        }

        public void TakeDamageAtPoint(int damageAmount, Vector3 hitPoint)
        {
            if (!IsAlive) return;

            // Invulnerability frames
            if (invulnerabilityTimer > 0f)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[PlayerHealth] Invulnerable - damage ignored");
                }
                return;
            }

            currentHealth -= damageAmount;
            currentHealth = Mathf.Max(currentHealth, 0);

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerHealth] Took {damageAmount} damage. HP: {currentHealth}/{maxHealth}");
            }

            // Notify observers (GameUIManager updates health bar)
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // === DAMAGE FEEDBACK ===

            // 1. Screen shake
            if (cameraController != null)
            {
                cameraController.Shake(damageScreenShakeIntensity);
            }

            // 2. Red vignette flash
            if (vignetteImage != null)
            {
                if (vignetteCoroutine != null)
                {
                    StopCoroutine(vignetteCoroutine);
                }
                vignetteCoroutine = StartCoroutine(FlashVignette());
            }

            // 3. Damage number above player head
            if (DamageNumberSpawner.Instance != null)
            {
                DamageNumberSpawner.Instance.SpawnDamageNumber(damageAmount, hitPoint);
            }

            // 4. Blood particle effect
            if (bloodEffectPrefab != null && bloodSpawnPoint != null)
            {
                GameObject blood = Instantiate(bloodEffectPrefab, bloodSpawnPoint.position, Quaternion.identity);
                ParticleSystem ps = blood.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    Destroy(blood, ps.main.duration + 0.1f);
                }
                else
                {
                    Destroy(blood, 2f);
                }
            }

            // 5. Damage sound
            if (damageSound != null && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayAudio(damageSound, transform.position, 1f);
            }

            // Start invulnerability
            invulnerabilityTimer = invulnerabilityDuration;

            // Check death
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashVignette()
        {
            // Fade in
            float elapsed = 0f;
            float halfDuration = damageVignetteDuration / 2f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled time (works during pause)
                float t = elapsed / halfDuration;

                Color c = vignetteImage.color;
                c = Color.Lerp(new Color(c.r, c.g, c.b, 0f), damageVignetteColor, t);
                vignetteImage.color = c;

                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;

                Color c = vignetteImage.color;
                c = Color.Lerp(damageVignetteColor, new Color(c.r, c.g, c.b, 0f), t);
                vignetteImage.color = c;

                yield return null;
            }

            // Ensure fully transparent
            Color finalColor = vignetteImage.color;
            finalColor.a = 0f;
            vignetteImage.color = finalColor;
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerHealth] Healed {amount}. HP: {currentHealth}/{maxHealth}");
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(int newMax)
        {
            int difference = newMax - maxHealth;
            maxHealth = newMax;

            // Increase current health proportionally
            if (difference > 0)
            {
                currentHealth += difference;
            }

            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerHealth] Max health set to {maxHealth}. Current: {currentHealth}");
            }
        }

        private void Die()
        {
            if (showDebugLogs)
            {
                Debug.Log("[PlayerHealth] Player died!");
            }

            // Notify observers (GameManager listens for this)
            OnDeath?.Invoke();

            // GameManager will handle game over state transition
            // We don't call GameManager directly to maintain loose coupling
        }

        /// <summary>
        /// Called by Arsenal/Shop system to apply max health upgrades
        /// </summary>
        public void ApplyMaxHealthUpgrade(int amount)
        {
            SetMaxHealth(maxHealth + amount);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (bloodSpawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(bloodSpawnPoint.position, 0.2f);
            }
        }
#endif
    }
}