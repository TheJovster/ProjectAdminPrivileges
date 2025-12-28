using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Abilities
{
    /// <summary>
    /// Static utility class for path point distribution and calculations.
    /// </summary>
    public static class PathPointDistributor
    {
        /// <summary>
        /// Calculate total length of a path
        /// </summary>
        public static float CalculatePathLength(List<Vector3> path)
        {
            if (path == null || path.Count < 2) return 0f;

            float length = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                length += Vector3.Distance(path[i - 1], path[i]);
            }
            return length;
        }

        /// <summary>
        /// Trim path to maximum distance from start
        /// </summary>
        public static List<Vector3> TrimPathToMaxDistance(List<Vector3> path, float maxDistance)
        {
            if (path == null || path.Count < 2) return path;

            float currentLength = 0f;
            List<Vector3> trimmedPath = new List<Vector3>();
            trimmedPath.Add(path[0]);

            for (int i = 1; i < path.Count; i++)
            {
                float segmentLength = Vector3.Distance(path[i - 1], path[i]);

                if (currentLength + segmentLength > maxDistance)
                {
                    // Add partial segment to reach exactly maxDistance
                    float remainingDistance = maxDistance - currentLength;
                    Vector3 direction = (path[i] - path[i - 1]).normalized;
                    trimmedPath.Add(path[i - 1] + direction * remainingDistance);
                    break;
                }

                trimmedPath.Add(path[i]);
                currentLength += segmentLength;
            }

            return trimmedPath;
        }

        /// <summary>
        /// Distribute N points evenly along a path
        /// </summary>
        public static List<Vector3> GetEvenlySpacedPoints(List<Vector3> path, int count)
        {
            List<Vector3> result = new List<Vector3>();

            if (path == null || path.Count < 2 || count < 2)
            {
                Debug.LogWarning("[PathPointDistributor] Invalid input for GetEvenlySpacedPoints");
                return result;
            }

            float totalLength = CalculatePathLength(path);
            float spacing = totalLength / (count - 1);

            // Always add first point
            result.Add(path[0]);

            float accumulatedDistance = 0f;
            float targetDistance = spacing;
            int targetIndex = 1;

            for (int i = 1; i < path.Count && targetIndex < count - 1; i++)
            {
                float segmentLength = Vector3.Distance(path[i - 1], path[i]);

                // Check if target point(s) lie within this segment
                while (accumulatedDistance + segmentLength >= targetDistance && targetIndex < count - 1)
                {
                    // Interpolate position along segment
                    float t = (targetDistance - accumulatedDistance) / segmentLength;
                    Vector3 point = Vector3.Lerp(path[i - 1], path[i], t);
                    result.Add(point);

                    targetDistance += spacing;
                    targetIndex++;
                }

                accumulatedDistance += segmentLength;
            }

            // Always add last point
            result.Add(path[path.Count - 1]);

            return result;
        }
    }
}