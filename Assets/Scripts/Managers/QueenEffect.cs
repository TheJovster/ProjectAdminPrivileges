using ProjectAdminPrivileges.PlayerCharacter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueenEffect : MonoBehaviour
{
    [SerializeField] private Collider effectCollider;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // Apply the effect to the player here
            Debug.Log("Player entered the Queen's effect area!");
            other.GetComponent<PlayerHealth>().Heal(100); // Example: Heal the player to full health
        }
    }
}
