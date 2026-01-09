using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace ProjectAdminPrivileges.Enemy.Boss
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Boss1AI : MonoBehaviour
    {
        private NavMeshAgent navMeshAgent;

        private void Awake()
        {
            if(navMeshAgent == null) 
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }
        }

        public void MoveToTarget(Vector3 targetPosition)
        {
            if(navMeshAgent != null) 
            {
                navMeshAgent.SetDestination(targetPosition);
            }
        }

        public void StopMovement()
        {
            if(navMeshAgent != null) 
            {
                navMeshAgent.ResetPath();
            }
        }

        public void StopImmediate()
        {
            if(navMeshAgent != null) 
            {
                navMeshAgent.isStopped = true;
            }
        }
    }
}

