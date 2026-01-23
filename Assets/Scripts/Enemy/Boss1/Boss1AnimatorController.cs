using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace ProjectAdminPrivileges.Enemy.Boss
{
    [RequireComponent(typeof(Animator))]
    public class Boss1AnimatorController : MonoBehaviour
    {
        private Animator animator;
        private Boss1AI boss1Ai;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int CastHash = Animator.StringToHash("Cast");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            boss1Ai = GetComponent<Boss1AI>();
        }

        private void Update()
        {
            float speed = boss1Ai.GetNormalizedSpeed();
            animator.SetFloat(SpeedHash, speed);
        }

        public void TriggerMeleeAttack()
        {
           animator.SetTrigger(AttackHash);
        }

        public void TriggerCast() 
        {
            animator.SetTrigger(CastHash);
        }

    }
}
