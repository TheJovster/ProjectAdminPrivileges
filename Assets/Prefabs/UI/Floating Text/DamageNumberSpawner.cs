using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [SerializeField] private DamageNumberPool pool;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (pool == null)
        {
            pool = GetComponent<DamageNumberPool>();
        }
    }

    public void SpawnDamageNumber(int damage, Vector3 worldPosition)
    {
        DamageNumber number = pool.Get();
        if (number != null)
        {
            number.Initialize(damage, worldPosition, mainCamera, pool.Return);
        }
    }
}