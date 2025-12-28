using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Abilities
{
    /// <summary>
    /// Helper class for recording painted paths with validation.
    /// Pure C# class - not a MonoBehaviour.
    /// </summary>
    public class PaintablePathRecorder
    {
        // Configuration
        private float minPaintDistance;
        private float maxPaintDistance;
        private float paintSampleRate;

        // State
        private List<Vector3> currentPath = new List<Vector3>();
        private bool isPainting = false;

        public bool IsPainting => isPainting;
        public List<Vector3> CurrentPath => new List<Vector3>(currentPath); // Return copy

        /// <summary>
        /// Constructor with configuration
        /// </summary>
        public PaintablePathRecorder(float minDistance = 10f, float maxDistance = 50f, float sampleRate = 0.1f)
        {
            minPaintDistance = minDistance;
            maxPaintDistance = maxDistance;
            paintSampleRate = sampleRate;
        }

        /// <summary>
        /// Start recording a new path
        /// </summary>
        public void StartPainting(Vector3 startPosition)
        {
            isPainting = true;
            currentPath.Clear();
            currentPath.Add(startPosition);
        }

        /// <summary>
        /// Update path with new position (only records if moved enough)
        /// </summary>
        public void UpdatePainting(Vector3 currentPosition)
        {
            if (!isPainting) return;

            // Only add point if moved far enough from last point
            if (currentPath.Count > 0)
            {
                float distanceFromLast = Vector3.Distance(currentPosition, currentPath[currentPath.Count - 1]);
                if (distanceFromLast < paintSampleRate) return;
            }

            currentPath.Add(currentPosition);
        }

        /// <summary>
        /// Finish painting and validate path.
        /// Returns validated path or null if invalid.
        /// </summary>
        public List<Vector3> FinishPainting(Vector3 endPosition)
        {
            if (!isPainting) return null;

            isPainting = false;

            // Add final position if not already there
            if (currentPath.Count == 0 || Vector3.Distance(endPosition, currentPath[currentPath.Count - 1]) > paintSampleRate)
            {
                currentPath.Add(endPosition);
            }

            // Validate path length
            float totalLength = PathPointDistributor.CalculatePathLength(currentPath);

            if (totalLength < minPaintDistance)
            {
                Debug.LogWarning($"[PaintablePathRecorder] Path too short: {totalLength:F1} < {minPaintDistance}");
                return null;
            }

            if (totalLength > maxPaintDistance)
            {
                Debug.LogWarning($"[PaintablePathRecorder] Path too long: {totalLength:F1} > {maxPaintDistance}. Trimming.");
                currentPath = PathPointDistributor.TrimPathToMaxDistance(currentPath, maxPaintDistance);
            }

            // Return copy of validated path
            return new List<Vector3>(currentPath);
        }

        /// <summary>
        /// Cancel painting
        /// </summary>
        public void CancelPainting()
        {
            isPainting = false;
            currentPath.Clear();
        }

        /// <summary>
        /// Get current path length (even if still painting)
        /// </summary>
        public float GetCurrentPathLength()
        {
            return PathPointDistributor.CalculatePathLength(currentPath);
        }
    }
}