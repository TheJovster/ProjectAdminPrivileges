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
        private float baseSpeed;

        public NavMeshAgent NavMeshAgent => navMeshAgent;

        private void Awake()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }
            baseSpeed = navMeshAgent.speed;
        }

        public void MoveToTarget(Vector3 targetPosition)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.SetDestination(targetPosition);
            }
        }

        public void MoveToTarget(Vector3 targetPosition, float speedMultiplier)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = baseSpeed * speedMultiplier;
                navMeshAgent.SetDestination(targetPosition);
            }
        }

        public void StopMovement()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.ResetPath();
            }
        }

        public void StopImmediate()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
            }
        }

        public void ResumeMovement()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = false;
            }
        }

        public void ResetSpeed()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = baseSpeed;
            }
        }

        public float DistanceToTarget(Vector3 targetPosition)
        {
            return Vector3.Distance(transform.position, targetPosition);
        }

        public float GetNormalizedSpeed()
        {
            if (navMeshAgent != null && navMeshAgent.speed > 0)
            {
                return navMeshAgent.velocity.magnitude / navMeshAgent.speed;
            }
            return 0f;
        }
    }
}

