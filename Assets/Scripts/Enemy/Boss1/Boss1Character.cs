using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectAdminPrivileges.Enemy.Boss
{
    [RequireComponent(typeof(EnemyHealth), typeof(Boss1AI))]
    public class Boss1Character : MonoBehaviour
    {
        private EnemyHealth enemyHealth;
        private Boss1AI boss1AI;

        [SerializeField] private int maxHealth = 500;

        private void Awake()
        {
            if(enemyHealth == null) 
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }
            if(boss1AI == null) 
            {
                boss1AI = GetComponent<Boss1AI>();
            }

        }

        private void DisableAI() 
        {
            if(boss1AI != null) 
            {
                boss1AI.StopImmediate();
                boss1AI.enabled = false;
            }
        }
    }
}
