using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Renderer))]
    [ExecuteInEditMode]
    public abstract class ProceduralMesh : MonoBehaviour
    {
        private Mesh    m_mesh;

		#region Properties

		public Mesh Mesh => m_mesh;

		#endregion

        protected virtual void Start()
        {
            UpdateMesh();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        protected virtual void Cleanup()
        {
            if (m_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_mesh);
                }
                else
                {
                    DestroyImmediate(m_mesh);
                }

                m_mesh = null;
            }
        }

        protected abstract Mesh CreateMesh();

        public virtual void UpdateMesh()
        {
            m_mesh = CreateMesh();
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.mesh = m_mesh;
            }

            SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                smr.sharedMesh = m_mesh;
            }
        }
    }
}