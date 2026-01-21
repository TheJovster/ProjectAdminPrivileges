using UnityEngine;
using ProjectAdminPrivileges.Combat.Weapons;
using ProjectAdminPrivileges.PlayerCharacter;

namespace ProjectAdminPrivileges.Enemy.Boss
{
    public class Boss1Projectile : MonoBehaviour, IDamageable
    {
        private int health;
        private int damage;
        private float speed;
        private Transform target;
        private float startHeight;
        private float targetHeight;

        private float spawnDistanceToPlayer; 

        public void Initialize(Transform playerTarget, int damage, int healthPoints, float speed)
        {
            target = playerTarget;
            this.damage = damage;
            health = healthPoints;
            this.speed = speed;

            startHeight = transform.position.y;
            targetHeight = playerTarget.position.y + 1f;

            Vector3 spawnPositionFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPositionFlat = new Vector3(playerTarget.position.x, 0, playerTarget.position.z);
            spawnDistanceToPlayer = Vector3.Distance(spawnPositionFlat, targetPositionFlat);
        }

        private void Update() //-> parabolic descent of the projectile
        {
            if (target == null) { Destroy(gameObject); return; }

            Vector3 directionToPlayer = target.position - transform.position;
            directionToPlayer.y = 0;
            transform.position += directionToPlayer.normalized * speed * Time.deltaTime;

            float currentDistanceToPlayer = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(target.position.x, 0, target.position.z)
            );

            float distanceTraveledFromSpawn = spawnDistanceToPlayer - currentDistanceToPlayer;
            float descentDistance = 8f;

            float descentProgress = Mathf.Clamp01(distanceTraveledFromSpawn / descentDistance);

            Vector3 newPosition = transform.position;
            newPosition.y = Mathf.Lerp(startHeight, targetHeight, descentProgress);
            transform.position = newPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
        }

        public void TakeDamage(int damage)
        {
            health -= damage;
            if (health <= 0) Destroy(gameObject);
        }

        public void TakeDamageAtPoint(int damage, Vector3 point)
        {
            TakeDamage(damage);
        }
    }
}