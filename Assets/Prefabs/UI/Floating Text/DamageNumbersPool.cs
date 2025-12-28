using UnityEngine;
using System.Collections.Generic;

public class DamageNumberPool : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private Transform poolParent;

    private Queue<DamageNumber> availableNumbers = new Queue<DamageNumber>();
    private HashSet<DamageNumber> activeNumbers = new HashSet<DamageNumber>();

    private void Awake()
    {
        // Create pool parent if not assigned
        if (poolParent == null)
        {
            poolParent = new GameObject("DamageNumberPool").transform;
            poolParent.SetParent(transform);
        }

        // Prewarm pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewNumber();
        }
    }

    private DamageNumber CreateNewNumber()
    {
        GameObject go = Instantiate(damageNumberPrefab, poolParent);
        DamageNumber number = go.GetComponent<DamageNumber>();
        go.SetActive(false);
        availableNumbers.Enqueue(number);
        return number;
    }

    public DamageNumber Get()
    {
        DamageNumber number;

        if (availableNumbers.Count > 0)
        {
            number = availableNumbers.Dequeue();
        }
        else if (activeNumbers.Count < maxPoolSize)
        {
            number = CreateNewNumber();
        }
        else
        {
            // Pool exhausted, reuse oldest
            Debug.LogWarning("[DamageNumberPool] Pool exhausted, reusing active number");
            return null;
        }

        number.gameObject.SetActive(true);
        activeNumbers.Add(number);
        return number;
    }

    public void Return(DamageNumber number)
    {
        if (activeNumbers.Remove(number))
        {
            number.gameObject.SetActive(false);
            availableNumbers.Enqueue(number);
        }
    }
}