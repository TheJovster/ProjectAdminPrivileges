using UnityEngine;
using Game; // For ProceduralMesh base class

namespace ProjectAdminPrivileges.Combat.Effects
{
    /// <summary>
    /// Generates a randomized procedural mesh for gore/gib effects.
    /// Inherits from ProceduralMesh to handle mesh lifecycle properly.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralGib : ProceduralMesh
    {
        [Header("Gib Shape")]
        [SerializeField] private GibType gibType = GibType.Generic;
        [SerializeField] private Vector3 baseSize = new Vector3(0.4f, 0.4f, 0.4f);

        [Header("Randomization")]
        [SerializeField, Range(0f, 0.5f)] private float randomness = 0.2f;
        [SerializeField] private bool randomizeOnEnable = true;

        [Header("Lifetime")]
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float fadeStartTime = 2f;

        private Material gibMaterial;
        private Color originalColor;

        protected override void Start()
        {
            base.Start();

            // Cache material for fading
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                gibMaterial = renderer.material;
                originalColor = gibMaterial.color;
            }
        }

        private void OnEnable()
        {
            if (randomizeOnEnable && Application.isPlaying)
            {
                UpdateMesh(); // Regenerate with new random values
            }

            if (Application.isPlaying)
            {
                StartCoroutine(LifetimeCoroutine());
            }
        }

        private System.Collections.IEnumerator LifetimeCoroutine()
        {
            // Reset material color
            if (gibMaterial != null)
            {
                gibMaterial.color = originalColor;
            }

            yield return new WaitForSeconds(fadeStartTime);

            // Fade out
            float fadeTime = lifetime - fadeStartTime;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);

                if (gibMaterial != null)
                {
                    Color c = gibMaterial.color;
                    c.a = alpha;
                    gibMaterial.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        protected override Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = $"ProceduralGib_{gibType}";

            switch (gibType)
            {
                case GibType.Generic:
                    CreateDeformedCube(mesh);
                    break;
                case GibType.Chunk:
                    CreateIrregularChunk(mesh);
                    break;
                case GibType.Splatter:
                    CreateFlatSplatter(mesh);
                    break;
            }

            return mesh;
        }

        /// <summary>
        /// Creates a cube with randomized vertex positions for organic look
        /// </summary>
        private void CreateDeformedCube(Mesh mesh)
        {
            // Base cube vertices
            Vector3[] baseVertices = new Vector3[]
            {
                // Front face
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                // Back face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f)
            };

            // Apply base size and randomness
            Vector3[] vertices = new Vector3[baseVertices.Length];
            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 scaledVertex = Vector3.Scale(baseVertices[i], baseSize);
                Vector3 randomOffset = new Vector3(
                    Random.Range(-randomness, randomness),
                    Random.Range(-randomness, randomness),
                    Random.Range(-randomness, randomness)
                );
                vertices[i] = scaledVertex + randomOffset;
            }

            // Cube triangles (36 indices for 12 triangles)
            // Each face needs counter-clockwise winding when viewed from outside
            int[] triangles = new int[]
            {
                // Front face (facing +Z)
                0, 1, 2, 0, 2, 3,
                // Back face (facing -Z)
                5, 4, 7, 5, 7, 6,
                // Left face (facing -X)
                4, 0, 3, 4, 3, 7,
                // Right face (facing +X)
                1, 5, 6, 1, 6, 2,
                // Top face (facing +Y)
                3, 2, 6, 3, 6, 7,
                // Bottom face (facing -Y)
                4, 5, 1, 4, 1, 0
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Creates a more irregular chunk shape
        /// </summary>
        private void CreateIrregularChunk(Mesh mesh)
        {
            // Use deformed cube but with more extreme randomness
            float originalRandomness = randomness;
            randomness *= 2f; // Double the chaos
            CreateDeformedCube(mesh);
            randomness = originalRandomness; // Restore
        }

        /// <summary>
        /// Creates a flatter splatter-like shape
        /// </summary>
        private void CreateFlatSplatter(Mesh mesh)
        {
            Vector3 flatSize = new Vector3(baseSize.x, baseSize.y * 0.2f, baseSize.z);
            Vector3 originalSize = baseSize;
            baseSize = flatSize;
            CreateDeformedCube(mesh);
            baseSize = originalSize;
        }
    }

    public enum GibType
    {
        Generic,    // Standard deformed cube
        Chunk,      // More irregular/chaotic
        Splatter    // Flatter, like a blood chunk
    }
}