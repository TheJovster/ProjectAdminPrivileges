using UnityEngine;
using ProjectAdminPrivileges.PlayerCharacter;

namespace ProjectAdminPrivileges.PlayerCharacter
{
    public class PlayerAnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerIKController ikController;
        [SerializeField] private RuntimeAnimatorController baseRuntimeAnimator;

        private static readonly int moveXHash = Animator.StringToHash("MoveX");
        private static readonly int moveYHash = Animator.StringToHash("MoveY");
        private static readonly int weaponTypeHash = Animator.StringToHash("WeaponType");
        private static readonly int fireHash = Animator.StringToHash("Fire");
        private static readonly int reloadHash = Animator.StringToHash("Reload");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (ikController == null)
            {
                ikController = GetComponent<PlayerIKController>();
            }
        }

        public void UpdateMovementAnimation(float moveX, float moveY)
        {
            animator.SetFloat(moveXHash, moveX);
            animator.SetFloat(moveYHash, moveY);
        }

        public void TriggerFire()
        {
            animator.SetTrigger(fireHash);
        }

        public void SetWeaponType(int weaponType)
        {
            animator.SetInteger(weaponTypeHash, weaponType);
        }

        public void TriggerReload()
        {
            animator.SetTrigger(reloadHash);

            // Disable IK during reload (let animation control hands)
/*            if (ikController != null)
            {
                ikController.SetIKActive(false);
            }*/
        }

        public void SetWeaponAnimations(AnimatorOverrideController animatorOverrideController) 
        {
            if(animatorOverrideController != null) 
            {
                animator.runtimeAnimatorController = animatorOverrideController;
            }
            else 
            {
                animator.runtimeAnimatorController = baseRuntimeAnimator;
            }
        }

        // Called by Animation Event at end of reload animation
        public void OnReloadComplete()
        {
/*            if (ikController != null)
            {
                ikController.SetIKActive(true);
            }*/
        }

        // Called by Animation Event at end of weapon switch animation (if you have one)
        public void OnWeaponSwitchComplete()
        {
/*            if (ikController != null)
            {
                ikController.SetIKActive(true);
            }*/
        }
    }
}
