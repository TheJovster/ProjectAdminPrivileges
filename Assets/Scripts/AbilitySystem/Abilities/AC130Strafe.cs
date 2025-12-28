using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAdminPrivileges.Combat.Weapons;

namespace ProjectAdminPrivileges.Abilities
{
    public class AC130Strafe : AbilityBase, IPaintableAbility
    {
        [Header("Strafe Settings")]
        [SerializeField] private int damagePointCount = 10;
        [SerializeField] private float tickInterval = 0.1f;
        [SerializeField] private float damageRadius = 3f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Painting Settings")]
        [SerializeField] private float minPaintDistance = 10f;
        [SerializeField] private float maxPaintDistance = 50f;
        [SerializeField] private float paintSampleRate = 0.1f;
        [SerializeField] private Color paintingColor = Color.yellow;
        [SerializeField] private Color confirmedColor = Color.red;
        [SerializeField] private float lineWidth = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject impactVFXPrefab;
        [SerializeField] private LineRenderer paintLineRenderer;
        [SerializeField] private GameObject warningDecalPrefab;

        [Header("Audio")]
        [SerializeField] private Audio.AudioClipData strafeLoopSound;
        [SerializeField] private Audio.AudioClipData impactSound;

        [Header("Camera")]
        [SerializeField] private float cameraShakeIntensity = 0.3f;
        [SerializeField] private bool shakePerImpact = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugSpheres = true;

        // Helper instance
        private PaintablePathRecorder pathRecorder;

        // State
        private List<GameObject> warningDecals = new List<GameObject>();

        // IPaintableAbility implementation
        public bool IsPainting => pathRecorder != null && pathRecorder.IsPainting;

        private void Start()
        {
            // Initialize path recorder
            pathRecorder = new PaintablePathRecorder(minPaintDistance, maxPaintDistance, paintSampleRate);

            // Setup LineRenderer if not assigned
            if (paintLineRenderer == null)
            {
                GameObject lineObj = new GameObject("PaintLine");
                lineObj.transform.SetParent(transform);
                paintLineRenderer = lineObj.AddComponent<LineRenderer>();

                paintLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                paintLineRenderer.startWidth = lineWidth;
                paintLineRenderer.endWidth = lineWidth;
                paintLineRenderer.positionCount = 0;
                paintLineRenderer.enabled = false;
                paintLineRenderer.useWorldSpace = true;
            }
        }

        protected override void Activate(Vector3 targetPosition)
        {
            // This gets called by base class, but we override behavior with painting
            // Do nothing here - painting handles activation
            Debug.LogWarning("[AC130Strafe] Activate() called directly - should use painting interface instead");
        }

        // IPaintableAbility implementation
        public void StartPainting(Vector3 startPos)
        {
            pathRecorder.StartPainting(startPos);

            if (paintLineRenderer != null)
            {
                paintLineRenderer.enabled = true;
                paintLineRenderer.startColor = paintingColor;
                paintLineRenderer.endColor = paintingColor;
                paintLineRenderer.positionCount = 1;
                paintLineRenderer.SetPosition(0, startPos + Vector3.up * 0.5f);
            }

            Debug.Log($"[AC130Strafe] Started painting at {startPos}");
        }

        public void UpdatePainting(Vector3 currentPos)
        {
            pathRecorder.UpdatePainting(currentPos);

            // Update LineRenderer to show current path
            if (paintLineRenderer != null)
            {
                List<Vector3> currentPath = pathRecorder.CurrentPath;
                paintLineRenderer.positionCount = currentPath.Count;
                for (int i = 0; i < currentPath.Count; i++)
                {
                    paintLineRenderer.SetPosition(i, currentPath[i] + Vector3.up * 0.5f);
                }
            }
        }

        public void FinishPainting(Vector3 endPos)
        {
            List<Vector3> validatedPath = pathRecorder.FinishPainting(endPos);

            if (validatedPath == null)
            {
                // Path invalid - cancel
                CancelPainting();
                return;
            }

            // Change line color to confirmed
            if (paintLineRenderer != null)
            {
                paintLineRenderer.startColor = confirmedColor;
                paintLineRenderer.endColor = confirmedColor;
            }

            // Spawn warning decals
            SpawnWarningDecals(validatedPath);

            // Execute strafe
            StartCoroutine(ExecuteStrafeAlongPath(validatedPath));

            // Start cooldown after painting confirmed
            StartCooldown();

            Debug.Log($"[AC130Strafe] Finished painting. Path length: {PathPointDistributor.CalculatePathLength(validatedPath):F1}");
        }

        public void CancelPainting()
        {
            pathRecorder.CancelPainting();

            if (paintLineRenderer != null)
            {
                paintLineRenderer.enabled = false;
            }

            ClearWarningDecals();

            Debug.Log("[AC130Strafe] Painting cancelled");
        }

        private IEnumerator ExecuteStrafeAlongPath(List<Vector3> path)
        {
            // Wait before executing
            yield return new WaitForSeconds(1f);

            // Clear warning decals
            ClearWarningDecals();

            // Play strafe loop sound
            if (strafeLoopSound != null && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayAudio(strafeLoopSound, path[0], 1.2f);
            }

            // Get evenly spaced damage points
            List<Vector3> damagePoints = PathPointDistributor.GetEvenlySpacedPoints(path, damagePointCount);

            // Execute damage at each point
            foreach (Vector3 point in damagePoints)
            {
                ExecuteDamageAtPoint(point);

                // Spawn impact VFX
                if (impactVFXPrefab != null)
                {
                    GameObject impact = Instantiate(impactVFXPrefab, point + Vector3.up * 0.5f, Quaternion.identity);

                    ParticleSystem ps = impact.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Play();
                        Destroy(impact, ps.main.duration + 0.5f);
                    }
                    else
                    {
                        Destroy(impact, 2f);
                    }
                }

                // Play impact sound
                if (impactSound != null && Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlayAudio(impactSound, point, 0.6f);
                }

                // Camera shake
                if (shakePerImpact)
                {
                    ShakeCamera();
                }

                yield return new WaitForSeconds(tickInterval);
            }

            // Hide line after completion
            if (paintLineRenderer != null)
            {
                paintLineRenderer.enabled = false;
            }

            Debug.Log($"[AC130Strafe] Strafe complete");
        }

        private void ExecuteDamageAtPoint(Vector3 position)
        {
            Collider[] hitColliders = Physics.OverlapSphere(position, damageRadius, enemyLayer);
            int enemiesHit = 0;

            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamageAtPoint(abilityData.Damage, position);
                    enemiesHit++;
                }
            }

            if (enemiesHit > 0 && showDebugSpheres)
            {
                Debug.Log($"[AC130Strafe] Hit {enemiesHit} enemies at {position}");
            }
        }

        private void SpawnWarningDecals(List<Vector3> path)
        {
            ClearWarningDecals();

            if (warningDecalPrefab == null) return;

            List<Vector3> decalPoints = PathPointDistributor.GetEvenlySpacedPoints(path, damagePointCount);

            foreach (Vector3 point in decalPoints)
            {
                GameObject decal = Instantiate(warningDecalPrefab, point + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
                warningDecals.Add(decal);
            }
        }

        private void ClearWarningDecals()
        {
            foreach (GameObject decal in warningDecals)
            {
                if (decal != null)
                {
                    Destroy(decal);
                }
            }
            warningDecals.Clear();
        }

        private void ShakeCamera()
        {
            if (Camera.main != null)
            {
                var cameraController = Camera.main.GetComponentInParent<PlayerCharacter.CameraController>();
                if (cameraController != null)
                {
                    cameraController.Shake(cameraShakeIntensity);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugSpheres) return;

            // Show current path while painting
            if (pathRecorder != null && pathRecorder.IsPainting)
            {
                List<Vector3> currentPath = pathRecorder.CurrentPath;
                if (currentPath.Count > 1)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 1; i < currentPath.Count; i++)
                    {
                        Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                    }
                }
            }

            // Show damage radius at evenly spaced points
            if (pathRecorder != null && !pathRecorder.IsPainting && pathRecorder.CurrentPath.Count > 0)
            {
                List<Vector3> damagePoints = PathPointDistributor.GetEvenlySpacedPoints(pathRecorder.CurrentPath, damagePointCount);

                Gizmos.color = Color.red;
                foreach (Vector3 point in damagePoints)
                {
                    Gizmos.DrawWireSphere(point, damageRadius);
                }
            }
        }
    }
}