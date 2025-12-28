using UnityEngine;

namespace ProjectAdminPrivileges.Combat.Weapons
{
    /// <summary>
    /// Manual trail effect using LineRenderer instead of TrailRenderer.
    /// Attach to projectile. More control over appearance.
    /// </summary>
    public class ProjectileTrail : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private int trailLength = 10; // Number of points
        [SerializeField] private float trailLifetime = 0.2f; // How long each point lasts

        [Header("Appearance")]
        [SerializeField] private float startWidth = 0.25f;
        [SerializeField] private float endWidth = 0.08f;
        [SerializeField] private Gradient colorGradient;
        [SerializeField] private Material trailMaterial;

        private LineRenderer lineRenderer;
        private Vector3[] trailPositions;
        private float[] trailTimes;
        private bool isActive = false; // Prevents trail from starting before Fire()

        private void Awake()
        {
            // Setup LineRenderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 0; // Start with no points
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            lineRenderer.numCapVertices = 5;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (trailMaterial != null)
            {
                lineRenderer.material = trailMaterial;
            }

            if (colorGradient != null)
            {
                lineRenderer.colorGradient = colorGradient;
            }
            else
            {
                // Default gradient: yellow to transparent
                Gradient defaultGradient = new Gradient();
                defaultGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.yellow, 0.0f),
                        new GradientColorKey(Color.yellow, 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                lineRenderer.colorGradient = defaultGradient;
            }

            // Initialize arrays
            trailPositions = new Vector3[trailLength];
            trailTimes = new float[trailLength];

            ClearTrail();
        }

        private void OnEnable()
        {
            // Don't activate trail yet - let ActivateTrail() handle it after positioning
            isActive = false;

            // CRITICAL: Initialize arrays to current position to prevent stale data
            if (trailPositions != null && lineRenderer != null)
            {
                Vector3 currentPos = transform.position;
                for (int i = 0; i < trailLength; i++)
                {
                    trailPositions[i] = currentPos;
                    trailTimes[i] = 0;
                }
                lineRenderer.positionCount = 0;
            }
        }

        private void Update()
        {
            if (!isActive) return; // Don't update until activated by Fire()
            if (lineRenderer == null) return;

            // Add current position to trail
            AddPoint(transform.position);

            // Update all positions
            UpdateTrail();
        }

        private void AddPoint(Vector3 position)
        {
            // Shift old positions
            for (int i = trailLength - 1; i > 0; i--)
            {
                trailPositions[i] = trailPositions[i - 1];
                trailTimes[i] = trailTimes[i - 1];
            }

            // Add new position
            trailPositions[0] = position;
            trailTimes[0] = Time.time;
        }

        private void UpdateTrail()
        {
            int validPoints = 0;
            float currentTime = Time.time;

            // Count valid points (within lifetime)
            for (int i = 0; i < trailLength; i++)
            {
                if (currentTime - trailTimes[i] < trailLifetime)
                {
                    validPoints++;
                }
                else
                {
                    break; // Rest are older
                }
            }

            // Only draw trail if we have at least 2 points AND they're valid
            if (validPoints >= 2)
            {
                lineRenderer.positionCount = validPoints;
                for (int i = 0; i < validPoints; i++)
                {
                    lineRenderer.SetPosition(i, trailPositions[i]);
                }
            }
            else
            {
                // Not enough valid points - hide trail completely
                lineRenderer.positionCount = 0;
            }
        }

        public void ClearTrail()
        {
            if (lineRenderer == null) return;

            // Initialize all positions to current position
            // Prevents trail from drawing from pool location to spawn location
            Vector3 currentPos = transform.position;
            for (int i = 0; i < trailLength; i++)
            {
                trailPositions[i] = currentPos;
                trailTimes[i] = 0; // Set to 0 so they're invalid (too old)
            }

            lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// Activates trail rendering after projectile has been positioned.
        /// Called by Projectile.Fire()
        /// </summary>
        public void ActivateTrail()
        {
            ClearTrail(); // Clear with current position FIRST
            isActive = true; // THEN activate
        }

        private void OnDisable()
        {
            isActive = false; // Deactivate trail
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0; // Hide immediately
            }
        }

        /// <summary>
        /// Sets the color gradient for the trail.
        /// Called by Weapon.cs for per-weapon trail customization.
        /// </summary>
        public void SetGradient(Gradient gradient)
        {
            if (lineRenderer != null && gradient != null)
            {
                lineRenderer.colorGradient = gradient;
            }
        }

        /// <summary>
        /// Sets the width of the trail.
        /// Called by Weapon.cs for per-weapon trail customization.
        /// </summary>
        public void SetWidth(float start, float end)
        {
            startWidth = start;
            endWidth = end;
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = start;
                lineRenderer.endWidth = end;
            }
        }
    }
}