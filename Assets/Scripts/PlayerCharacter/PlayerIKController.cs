using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ProjectAdminPrivileges.PlayerCharacter
{
    public class PlayerIKController : MonoBehaviour
    {
        [Header("Two Bone IK Constraints")]
        [SerializeField] private TwoBoneIKConstraint leftHandIK;
        [SerializeField] private TwoBoneIKConstraint rightHandIK;

        [Header("Proxy Targets (Always Active)")]
        [SerializeField] private Transform leftHandProxy;  // Create empty GameObject child of player
        [SerializeField] private Transform rightHandProxy;
        [SerializeField] private Transform leftHintProxy;
        [SerializeField] private Transform rightHintProxy;

        [Header("Rig Builder")]
        [SerializeField] private RigBuilder rigBuilder;

        [Header("Default Weights")]
        [SerializeField, Range(0f, 1f)] private float defaultLeftHandWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultRightHandWeight = 0f;

        private bool ikActive = true;

        private void Awake()
        {
            if (rigBuilder == null)
            {
                rigBuilder = GetComponentInParent<RigBuilder>();
            }

            // Set constraints to use proxy targets (not weapon children)
            if (leftHandIK != null && leftHandProxy != null)
            {
                var data = leftHandIK.data;
                data.target = leftHandProxy;
                if (leftHintProxy != null) data.hint = leftHintProxy;
                leftHandIK.data = data;
            }

            if (rightHandIK != null && rightHandProxy != null)
            {
                var data = rightHandIK.data;
                data.target = rightHandProxy;
                if (rightHintProxy != null) data.hint = rightHintProxy;
                rightHandIK.data = data;
            }

            if (rigBuilder != null)
            {
                rigBuilder.Build();
            }
        }

        /// <summary>
        /// Update proxy positions to match weapon grips
        /// </summary>
        public void SetHandTargets(Transform leftGrip, Transform rightGrip, Transform leftHint = null, Transform rightHint = null)
        {
            if (leftHandProxy != null && leftGrip != null)
            {
                leftHandProxy.position = leftGrip.position;
                leftHandProxy.rotation = leftGrip.rotation;
            }

            if (rightHandProxy != null && rightGrip != null)
            {
                rightHandProxy.position = rightGrip.position;
                rightHandProxy.rotation = rightGrip.rotation;
            }

            if (leftHintProxy != null && leftHint != null)
            {
                leftHintProxy.position = leftHint.position;
                leftHintProxy.rotation = leftHint.rotation;
            }

            if (rightHintProxy != null && rightHint != null)
            {
                rightHintProxy.position = rightHint.position;
                rightHintProxy.rotation = rightHint.rotation;
            }
        }

        /// <summary>
        /// Continuously update proxy positions (call in Update or LateUpdate)
        /// </summary>
        public void UpdateProxyTargets(Transform leftGrip, Transform rightGrip, Transform leftHint = null, Transform rightHint = null)
        {
            if (leftGrip == null)
            {
                Debug.LogError("[IK] leftGrip is NULL!");
                return;
            }

            if (leftHandProxy == null)
            {
                Debug.LogError("[IK] leftHandProxy is NULL!");
                return;
            }

            Debug.Log($"[IK] Updating proxy from grip at {leftGrip.position} to proxy at {leftHandProxy.position}");

            leftHandProxy.position = leftGrip.position;
            leftHandProxy.rotation = leftGrip.rotation;

            if (rightHandProxy != null && rightGrip != null)
            {
                rightHandProxy.position = rightGrip.position;
                rightHandProxy.rotation = rightGrip.rotation;
            }

            if (leftHintProxy != null && leftHint != null)
            {
                leftHintProxy.position = leftHint.position;
            }

            if (rightHintProxy != null && rightHint != null)
            {
                rightHintProxy.position = rightHint.position;
            }
        }

        public void SetHandWeights(float leftWeight, float rightWeight)
        {
            if (leftHandIK != null)
            {
                leftHandIK.weight = Mathf.Clamp01(leftWeight);
            }

            if (rightHandIK != null)
            {
                rightHandIK.weight = Mathf.Clamp01(rightWeight);
            }
        }

        public void SetIKActive(bool active)
        {
            ikActive = active;

            if (leftHandIK != null)
            {
                leftHandIK.weight = active ? defaultLeftHandWeight : 0f;
            }

            if (rightHandIK != null)
            {
                rightHandIK.weight = active ? defaultRightHandWeight : 0f;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (leftHandProxy != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(leftHandProxy.position, 0.05f);
                Gizmos.DrawLine(leftHandProxy.position, leftHandProxy.position + leftHandProxy.forward * 0.1f);
            }

            if (rightHandProxy != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(rightHandProxy.position, 0.05f);
            }
        }
#endif
    }
}