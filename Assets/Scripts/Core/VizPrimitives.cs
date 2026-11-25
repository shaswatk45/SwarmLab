using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Small shared helper so experiments can draw extra GPU-instanced geometry
    /// (food markers, pheromone cells, explored tiles) without each one re-inventing
    /// mesh/material grabbing. All meshes are Unity's built-in primitives; materials
    /// are URP/Lit (or Standard) with instancing + emission enabled.
    /// </summary>
    public static class VizPrimitives
    {
        private static Mesh _sphere, _cube, _capsule, _quad;

        public static Mesh Sphere { get { if (_sphere == null) _sphere = Grab(PrimitiveType.Sphere); return _sphere; } }
        public static Mesh Cube { get { if (_cube == null) _cube = Grab(PrimitiveType.Cube); return _cube; } }
        public static Mesh Capsule { get { if (_capsule == null) _capsule = Grab(PrimitiveType.Capsule); return _capsule; } }
        public static Mesh Quad { get { if (_quad == null) _quad = Grab(PrimitiveType.Quad); return _quad; } }

        private static Mesh Grab(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            var r = tmp.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
            Object.Destroy(tmp);
            return mesh;
        }

        /// <summary>Create an emissive instanced material.</summary>
        public static Material Emissive(Color color, float emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { enableInstancing = true };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * emission);
            return mat;
        }

        private static readonly Matrix4x4[] _batch = new Matrix4x4[1023];

        /// <summary>Draw a list of transforms with one mesh + material, batched to 1023.</summary>
        public static void DrawInstanced(Mesh mesh, Material material, List<Matrix4x4> matrices)
        {
            if (mesh == null || material == null || matrices == null || matrices.Count == 0) return;
            var rp = new RenderParams(material) { worldBounds = new Bounds(Vector3.zero, Vector3.one * 2000f) };
            int drawn = 0;
            while (drawn < matrices.Count)
            {
                int n = Mathf.Min(1023, matrices.Count - drawn);
                for (int j = 0; j < n; j++) _batch[j] = matrices[drawn + j];
                Graphics.RenderMeshInstanced(rp, mesh, 0, _batch, n);
                drawn += n;
            }
        }
    }
}
