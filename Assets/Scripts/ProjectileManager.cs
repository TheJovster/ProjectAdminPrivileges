using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Combat.Weapons 
{
    public class ProjectileManager : MonoBehaviour
    {
        private static ProjectileManager instance;
        public static ProjectileManager Instance => instance;

        private List<Projectile> activeProjectiles = new List<Projectile>(200);

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            // Single loop for all projectiles
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = activeProjectiles[i];

                if (projectile == null || !projectile.IsFlying)
                {
                    activeProjectiles.RemoveAt(i);
                    continue;
                }

                projectile.ManualFixedUpdate(deltaTime);
            }
        }

        public void RegisterProjectile(Projectile projectile)
        {
            activeProjectiles.Add(projectile);
        }

        public void UnregisterProjectile(Projectile projectile)
        {
            activeProjectiles.Remove(projectile);
        }
    }
}
