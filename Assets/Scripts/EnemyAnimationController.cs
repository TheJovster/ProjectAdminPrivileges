using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Enemies.Animation
{
    public class EnemyAnimationController : MonoBehaviour
    {
        private Animator animator;

        // Animation parameter hashes
        private static readonly int speedHash = Animator.StringToHash("Speed");
        private static readonly int deathHash = Animator.StringToHash("Death");

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Sets the running speed animation parameter
        /// </summary>
        public void SetRun(float speed)
        {
            if (animator != null)
            {
                animator.SetFloat(speedHash, speed);
            }
        }

        /// <summary>
        /// Triggers the death animation
        /// </summary>
        public void TriggerDeath()
        {
            if (animator != null)
            {
                animator.SetTrigger(deathHash);
            }
        }

        /// <summary>
        /// Check if death animation is currently playing
        /// </summary>
        public bool IsPlayingDeath()
        {
            if (animator == null) return false;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsTag("Death");
        }
    }
}