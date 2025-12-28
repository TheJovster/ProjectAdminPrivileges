using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    /// <summary>
    /// Interface for objects that have a surface type.
    /// Attach this component to any object that should produce surface-specific sounds (impacts, footsteps, etc.)
    /// </summary>
    public interface ISurface
    {
        SurfaceType GetSurfaceType();
    }

    /// <summary>
    /// Enum defining all possible surface types in the game.
    /// Used for selecting appropriate audio feedback.
    /// </summary>
    public enum SurfaceType
    {
        Concrete,
        Metal,
        Dirt,
        Flesh,
        Wood,
        Stone,
        Grass,
        Water,
        Default  // Fallback if no surface component found
    }
}
