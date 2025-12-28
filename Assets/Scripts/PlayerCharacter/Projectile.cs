using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Pool;
using ProjectAdminPrivileges.Audio;


namespace ProjectAdminPrivileges.Combat.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private bool ricochetEnabled = false;

        //gravity and drag
        [Header("Projectile Physics")]
        [SerializeField] private float InitialProjectileVelocity = 150.0f;
        [SerializeField] private float gravityValue = 0f;
        [SerializeField, Range(0.0f, 1.0f)] private float dragCoeficient = 0f;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private float maxDistance = 200f;

        private int damageAmount = 0;

        //velocity and movement variables
        private Vector3 currentVelocity;
        private Vector3 startPosition;
        private Vector3 initialPosition;
        private bool isFlying = false;
        private float timeInFlight = 0.0f;

        //ricochet variables
        [Header("Projectile Ricochet Settings")]
        [SerializeField, Range(0, 10)] private int maxRicochetCount;
        [SerializeField, Range(0.0f, 1.0f)] private float ricochetVelocityLoss = 0.5f;
        [SerializeField] private float minRicochetAngle = 10.0f;
        [SerializeField] private float maxRicochetAngle = 60.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float ricochetChance = 1.0f;
        [SerializeField] private GameObject impactParticle;
        [SerializeField] private GameObject decalGameObject;
        [SerializeField] private float decalLifetime;
        private int ricochetCount = 0;

        private IObjectPool<Projectile> pool;
        [SerializeField] private LayerMask hitMask;

        [Header("Debug")]
        [SerializeField] private bool useRaycast = true;

        private ProjectileTrail trailCache;

        public bool IsFlying => isFlying;

        private void Awake()
        {
            trailCache = GetComponent<ProjectileTrail>();
            
        }

        private void OnEnable()
        {
            isFlying = false;
            timeInFlight = 0f;
            ricochetCount = 0;
            currentVelocity = Vector3.zero;
        }

        public void ManualFixedUpdate(float deltaTime)
        {
            if (!isFlying) return;

            timeInFlight += deltaTime;

            if (timeInFlight >= maxLifetime)
            {
                ReturnToPool();
                return;
            }

            float distanceTraveled = Vector3.Distance(initialPosition, transform.position);
            if (distanceTraveled >= maxDistance)
            {
                ReturnToPool();
                return;
            }

            // DEBUG: Log velocity before modifications
            Vector3 velocityBefore = currentVelocity;

            currentVelocity.y += gravityValue * deltaTime;
            float dragForce = -dragCoeficient * currentVelocity.magnitude;
            currentVelocity += currentVelocity.normalized * (dragForce * deltaTime);

            // DEBUG: Log if velocity changed
            if (Vector3.Distance(velocityBefore, currentVelocity) > 0.01f)
            {
                Debug.LogWarning($"Velocity changed! Before: {velocityBefore}, After: {currentVelocity}, Gravity: {gravityValue}, Drag: {dragCoeficient}");
            }

            transform.position += currentVelocity * deltaTime;

            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(currentVelocity);
            }

            CheckCollision();
        }

        private void CheckCollision()
        {
            Vector3 direction = transform.position - startPosition;
            float distance = direction.magnitude;

            if (distance < 0.001f)
            {
                startPosition = transform.position;
                return;
            }

            RaycastHit hit;
            bool didHit = false;

            if (useRaycast)
            {
                didHit = Physics.Raycast(startPosition, direction.normalized, out hit, distance, hitMask);
            }
            else
            {
                didHit = Physics.SphereCast(startPosition, collisionRadius, direction.normalized, out hit, distance, hitMask);
            }

            if (didHit)
            {
                HandleImpact(hit, direction.normalized);
                return;
            }

            startPosition = transform.position;
        }

        private void HandleImpact(RaycastHit hit, Vector3 incomingDirection)
        {
            if (HandleRicochet(hit, incomingDirection)) return;

            isFlying = false;

            SurfaceType surfaceType = Surface.GetSurfaceTypeFromHit(hit);

            AudioManager.Instance.PlayImpactSound(surfaceType, hit.point);

            bool hasDamageable = hit.transform.TryGetComponent<IDamageable>(out IDamageable damageable);

            if (hit.transform.gameObject.CompareTag("Queen"))
            {
                ReturnToPool();
                return;
            }
            if (hasDamageable)
            {
                damageable.TakeDamageAtPoint(damageAmount, hit.point);
            }

            PlayImpactEffect(hit);
            ProjectDecal(hit);

            ReturnToPool();
        }

        private void PlayImpactEffect(RaycastHit hit)
        {
            if (impactParticle != null)
            {
                GameObject particleInstance = Instantiate(impactParticle, hit.point, Quaternion.identity);
                ParticleSystem ps = particleInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    Destroy(particleInstance, ps.main.duration + 0.1f);
                }
                else
                {
                    Destroy(particleInstance, 2f);
                }
            }
        }

        private void ProjectDecal(RaycastHit hit)
        {
            if (decalGameObject != null)
            {
                Quaternion rotation = Quaternion.LookRotation(hit.normal);
                GameObject decalInstance = Instantiate(decalGameObject, hit.point, rotation);
                Destroy(decalInstance, decalLifetime);
            }
        }

        private bool HandleRicochet(RaycastHit hit, Vector3 incomingDirection)
        {
            if (!ricochetEnabled) return false;

            float currentRicochetChance = Random.Range(0.0f, 1.0f);

            if (currentRicochetChance <= ricochetChance)
            {
                if (ricochetCount < maxRicochetCount)
                {
                    float ricochetIncidentAngle = Vector3.Angle(-incomingDirection, hit.normal);

                    if (ricochetIncidentAngle >= minRicochetAngle && ricochetIncidentAngle <= maxRicochetAngle)
                    {
                        Vector3 reflectedVelocity = Vector3.Reflect(currentVelocity, hit.normal);
                        currentVelocity = reflectedVelocity * (1.0f - ricochetVelocityLoss);
                        ricochetCount++;
                        transform.position = hit.point + hit.normal * 0.1f;
                        startPosition = transform.position;
                        PlayImpactEffect(hit);
                        return true;
                    }
                }
            }
            return false;
        }

        public void Fire(Vector3 direction, int damage)
        {
            direction.Normalize();
            damageAmount = damage;
            currentVelocity = direction * InitialProjectileVelocity;
            isFlying = true;
            timeInFlight = 0.0f;
            startPosition = transform.position;
            initialPosition = transform.position;

            if (trailCache != null)
            {
                trailCache.ActivateTrail();  // ← CHANGE TO ActivateTrail()
            }

            if (ProjectileManager.Instance != null)
            {
                ProjectileManager.Instance.RegisterProjectile(this);
            }
        }

        public void SetPool(IObjectPool<Projectile> objectPool)
        {
            pool = objectPool;
        }

        private void ReturnToPool()
        {
            if (!isFlying && timeInFlight == 0f) return;

            if (ProjectileManager.Instance != null)
            {
                ProjectileManager.Instance.UnregisterProjectile(this);
            }

            isFlying = false;
            ricochetCount = 0;
            timeInFlight = 0f;

            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Clears trail after repositioning. Called by Weapon.cs after setting position.
        /// </summary>
        public void ClearTrail()
        {
            if (trailCache != null)
            {
                trailCache.ClearTrail();
            }
        }

        private void OnBecameInvisible()
        {
            if (isFlying)
            {
                ReturnToPool();
            }
        }

        private void OnDisable()
        {
            if (ProjectileManager.Instance != null && isFlying)
            {
                ProjectileManager.Instance.UnregisterProjectile(this);
            }
        }

        private void OnDrawGizmos()
        {
            if (isFlying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.2f);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2f);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);

                float angle = Vector3.Angle(currentVelocity.normalized, transform.forward);
                if (angle > 10f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, 0.5f); // Big warning sphere
                }
            }
        }
    }
}