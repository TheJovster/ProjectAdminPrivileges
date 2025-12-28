using UnityEngine;

namespace ProjectAdminPrivileges.PlayerCharacter
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Camera Position")]
        [SerializeField] private float distance = 20f;
        [SerializeField] private float height = 15f;
        [SerializeField] private float pitch = 45f; // Angle looking down (0-90)
        [SerializeField] private float yaw = 0f; // Rotation around target (0-360)

        [Header("Follow Settings")]
        [SerializeField] private float positionSmoothSpeed = 5f;
        [SerializeField] private bool smoothFollow = true;

        [Header("Look Ahead")]
        [SerializeField] private bool enableLookAhead = true;
        [SerializeField] private float lookAheadDistance = 5f;
        [SerializeField] private float lookAheadSmoothSpeed = 3f;

        [Header("Screen Shake")]
        [SerializeField] private float shakeDecay = 5f;

        private Vector3 currentLookAhead;
        private float currentLookAheadWeight = 0f;
        private bool lookAheadActive = false;

        private Vector3 shakeOffset;
        private float shakeIntensity;

        private void Start()
        {
            if (target == null)
            {
                Debug.LogError("[CameraController] No target assigned!");
            }

            SetFixedRotation();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdatePosition();
            ApplyScreenShake();
        }

        private void UpdatePosition()
        {
            Vector3 basePosition = CalculateCameraPosition(target.position);

            Vector3 lookAheadOffset = Vector3.zero;
            if (enableLookAhead && lookAheadActive)
            {
                currentLookAheadWeight = Mathf.Lerp(currentLookAheadWeight, 1f, lookAheadSmoothSpeed * Time.deltaTime);

                Vector3 aimDirection = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
                Vector3 targetLookAhead = aimDirection * lookAheadDistance;
                currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookAheadSmoothSpeed * Time.deltaTime);

                lookAheadOffset = currentLookAhead * currentLookAheadWeight;
            }
            else
            {
                currentLookAheadWeight = Mathf.Lerp(currentLookAheadWeight, 0f, lookAheadSmoothSpeed * Time.deltaTime);
                currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, lookAheadSmoothSpeed * Time.deltaTime);
            }

            Vector3 desiredPosition = basePosition + lookAheadOffset;

            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = desiredPosition;
            }
        }

        private void SetFixedRotation()
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Quaternion pitchRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.rotation = yawRotation * pitchRotation;
        }

        private Vector3 CalculateCameraPosition(Vector3 targetPosition)
        {
            float pitchRad = pitch * Mathf.Deg2Rad;
            float yawRad = yaw * Mathf.Deg2Rad;

            float horizontalDistance = distance * Mathf.Cos(pitchRad);

            Vector3 offset = new Vector3(
                horizontalDistance * Mathf.Sin(yawRad),
                height,
                horizontalDistance * Mathf.Cos(yawRad)
            );

            return targetPosition + offset;
        }

        private void ApplyScreenShake()
        {
            if (!GameManager.Instance.IsGameplayActive) return;

            if (shakeIntensity > 0.01f)
            {
                shakeOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * shakeIntensity;

                transform.position += shakeOffset;

                shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, shakeDecay * Time.deltaTime);
            }
            else
            {
                shakeIntensity = 0f;
                shakeOffset = Vector3.zero;
            }
        }

        public void SetLookAhead(bool enabled)
        {
            lookAheadActive = enabled;
        }

        public void Shake(float intensity)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        }

        public void SetDistance(float newDistance)
        {
            distance = Mathf.Max(5f, newDistance);
        }

        public void SetHeight(float newHeight)
        {
            height = Mathf.Max(5f, newHeight);
        }


        public void SetPitch(float newPitch)
        {
            pitch = Mathf.Clamp(newPitch, 10f, 89f);
            SetFixedRotation();
        }


        public void SetYaw(float newYaw)
        {
            yaw = newYaw % 360f;
            SetFixedRotation();
        }


        public bool IsLookAheadActive()
        {
            return currentLookAheadWeight > 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Gizmos.color = Color.cyan;
            Vector3 cameraPos = CalculateCameraPosition(target.position);
            Gizmos.DrawWireSphere(cameraPos, 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(cameraPos, target.position);

            if (enableLookAhead && lookAheadActive)
            {
                Gizmos.color = Color.green;
                Vector3 aimDirection = new Vector3(target.forward.x, 0f, target.forward.z).normalized;
                Vector3 lookAheadPos = target.position + aimDirection * lookAheadDistance;
                Gizmos.DrawLine(target.position, lookAheadPos);
                Gizmos.DrawWireSphere(lookAheadPos, 0.3f);
            }
        }
    }
}