#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SwarmLab.EditorTools
{
    /// <summary>
    /// One-click scene setup so there is almost no manual dragging in the editor.
    /// Menu:  SwarmLab > Build Lab Scene.
    /// It creates the ground, camera, light, the swarm object (with the manager and
    /// interactor), and a GPU-instanced material, then selects the swarm so you can
    /// tweak numbers in the Inspector. Safe to run again - it reuses existing objects.
    /// </summary>
    public static class SwarmSceneBuilder
    {
        [MenuItem("SwarmLab/Build Lab Scene")]
        public static void BuildScene()
        {
            // --- Ground ---
            GameObject ground = GameObject.Find("Lab_Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Lab_Ground";
            }
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 8f; // Plane is 10m, so ~80m wide
            var groundMat = MakeMaterial("Lab_Ground_Mat", new Color(0.04f, 0.06f, 0.09f), false, 0f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // --- Camera ---
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 35f, -45f);
            cam.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            cam.farClipPlane = 500f;

            // --- Light ---
            if (GameObject.Find("Lab_Light") == null)
            {
                var lightGo = new GameObject("Lab_Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = new Color(0.8f, 0.95f, 1f);
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Dark ambient for the neon look.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.08f, 0.1f);

            // --- Swarm object ---
            GameObject swarmGo = GameObject.Find("Swarm");
            if (swarmGo == null) swarmGo = new GameObject("Swarm");
            swarmGo.transform.position = new Vector3(0f, 12f, 0f);

            var manager = swarmGo.GetComponent<SwarmManager>();
            if (manager == null) manager = swarmGo.AddComponent<SwarmManager>();
            if (swarmGo.GetComponent<SwarmInteractor>() == null) swarmGo.AddComponent<SwarmInteractor>();

            manager.agentMesh = GetBuiltinMesh(PrimitiveType.Capsule);
            manager.agentMaterial = MakeMaterial("Agent_Mat", new Color(0.1f, 0.9f, 0.9f), true, 2.0f);

            Selection.activeGameObject = swarmGo;
            EditorUtility.SetDirty(manager);
            Debug.Log("SwarmLab: lab scene built. Press Play. Keys: [1] Flocking  [2] Rendezvous  [R] Respawn. Left-drag attract, right-drag repel.");
        }

        /// <summary>Grab a reference to one of Unity's built-in primitive meshes.</summary>
        private static Mesh GetBuiltinMesh(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            return mesh;
        }

        /// <summary>
        /// Create (or overwrite) a material asset. Picks URP/Lit if the project uses
        /// URP, otherwise the Built-in Standard shader, so this works in either setup.
        /// </summary>
        private static Material MakeMaterial(string name, Color color, bool emissive, float emissionStrength)
        {
            string path = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets", "Materials");
            string assetPath = path + "/" + name + ".mat";

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = shader != null;
            if (!urp) shader = Shader.Find("Standard");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.enableInstancing = true; // required for GPU-instanced rendering
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                Color e = color * emissionStrength;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", e);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
#endif
