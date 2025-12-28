using UnityEngine;

namespace ProjectAdminPrivileges.Abilities
{
    public abstract class AbilityBase : MonoBehaviour
    {
        [SerializeField] protected AbilityData abilityData;

        protected float currentCooldown = 0f;

        public AbilityData Data => abilityData;
        public float CooldownRemaining => currentCooldown;
        public bool IsOnCooldown => currentCooldown > 0f;

        protected virtual void Update()
        {
            if (currentCooldown > 0f)
            {
                currentCooldown -= Time.deltaTime;
                if (currentCooldown < 0f)
                {
                    currentCooldown = 0f;
                }
            }
        }

        /// <summary>
        /// Check if ability can be activated (not on cooldown, game not paused, etc.)
        /// </summary>
        public virtual bool CanActivate()
        {
            if (IsOnCooldown) return false;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return false;
            return true;
        }

        /// <summary>
        /// Activate the ability at the target position
        /// </summary>
        public void TryActivate(Vector3 targetPosition)
        {
            if (!CanActivate())
            {
                Debug.Log($"[AbilityBase] Cannot activate {abilityData.AbilityName} - on cooldown or game paused");
                return;
            }

            Activate(targetPosition);
            StartCooldown();
        }

        /// <summary>
        /// Override this in subclasses to implement ability behavior
        /// </summary>
        protected abstract void Activate(Vector3 targetPosition);

        protected void StartCooldown()
        {
            currentCooldown = abilityData.Cooldown;
            Debug.Log($"[AbilityBase] {abilityData.AbilityName} on cooldown for {currentCooldown}s");
        }

        public void ResetCooldown()
        {
            currentCooldown = 0f;
            Debug.Log($"[AbilityBase] {abilityData.AbilityName} cooldown reset");
        }
    }
}