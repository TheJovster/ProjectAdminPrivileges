using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    /// <summary>
    /// Component that defines the surface type of a GameObject.
    /// Attach this to floors, walls, enemies, props - anything that should produce surface-specific audio.
    /// </summary>
    public class Surface : MonoBehaviour, ISurface
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;

        public SurfaceType GetSurfaceType()
        {
            return surfaceType;
        }

        /// <summary>
        /// Attempts to get surface type from a RaycastHit.
        /// Returns SurfaceType.Default if no Surface component found.
        /// </summary>
        public static SurfaceType GetSurfaceTypeFromHit(RaycastHit hit)
        {
            ISurface surface = hit.collider.GetComponent<ISurface>();
            if (surface != null)
            {
                return surface.GetSurfaceType();
            }
            return SurfaceType.Default;
        }
    }
}
