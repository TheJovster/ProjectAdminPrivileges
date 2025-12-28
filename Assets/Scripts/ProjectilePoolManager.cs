using ProjectAdminPrivileges.Combat.Weapons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolManager : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int defaultCapacity = 50;
    [SerializeField] private int maxCapacity = 200;

    [Header("Pool Setup")]
    [Tooltip("Parent transform for pooled projectiles (set to ProjectilePoolHolder under Player)")]
    [SerializeField] private Transform poolParent;

    private IObjectPool<Projectile> pool;

    void Awake()
    {
        pool = new ObjectPool<Projectile>(
            CreateProjectile,
            OnGetProjectile,
            OnReleaseProjectile,
            OnDestroyProjectile,
            true, // collection checks
            defaultCapacity,
            maxCapacity
        );
    }

    private Projectile CreateProjectile()
    {
        Projectile projectile = Instantiate(projectilePrefab);
        projectile.SetPool(pool);

        // Parent to pool holder if specified
        if (poolParent != null)
        {
            projectile.transform.SetParent(poolParent);
        }

        return projectile;
    }

    private void OnGetProjectile(Projectile projectile)
    {
        // Set position to pool parent BEFORE activating
        // This prevents trail from starting at (0,0,0)
        if (poolParent != null)
        {
            projectile.transform.position = poolParent.position;
        }

        projectile.gameObject.SetActive(true);
    }

    private void OnReleaseProjectile(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }

    private void OnDestroyProjectile(Projectile projectile)
    {
        Destroy(projectile.gameObject);
    }

    public Projectile Get()
    {
        return pool.Get();
    }

    // Prewarm the pool
    public void Prewarm(int count)
    {
        List<Projectile> prewarmed = new List<Projectile>(count);
        for (int i = 0; i < count; i++)
        {
            prewarmed.Add(pool.Get());
        }
        foreach (var projectile in prewarmed)
        {
            pool.Release(projectile);
        }
    }
}