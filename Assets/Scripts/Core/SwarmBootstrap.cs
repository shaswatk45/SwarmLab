using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Auto-builds the whole lab at Play time so the user only has to press Play -
    /// no menus, no manual scene setup. If a "Swarm" object already exists (because
    /// the editor scene builder was used), it does nothing and defers to that setup.
    /// Everything it creates is runtime-only and disappears cleanly when Play stops.
    /// </summary>
    public static class SwarmBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if (Object.FindFirstObjectByType<SwarmManager>() != null) return; // already set up

            // --- Camera ---
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 35f, -45f);
            cam.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            cam.farClipPlane = 500f;

            // --- Light ---
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Lab_Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = new Color(0.8f, 0.95f, 1f);
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.08f, 0.1f);

            // --- Ground ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Lab_Ground";
            ground.transform.localScale = Vector3.one * 8f;
            ground.GetComponent<Renderer>().sharedMaterial =
                MakeMaterial(new Color(0.04f, 0.06f, 0.09f), false, 0f);

            // --- Swarm ---
            var swarmGo = new GameObject("Swarm");
            swarmGo.transform.position = new Vector3(0f, 12f, 0f);
            var manager = swarmGo.AddComponent<SwarmManager>();
            swarmGo.AddComponent<SwarmInteractor>();

            manager.agentMesh = GrabMesh(PrimitiveType.Capsule);
            manager.agentMaterial = MakeMaterial(new Color(0.1f, 0.9f, 0.9f), true, 2.0f);

            // Cinematic orbit camera.
            if (cam.GetComponent<CameraController>() == null) cam.gameObject.AddComponent<CameraController>();

            // Neon environment, obstacles, data logging + dashboard UI.
            var systems = new GameObject("Lab_Systems");
            systems.AddComponent<LabEnvironment>();
            systems.AddComponent<ScenarioManager>();
            systems.AddComponent<DataLogger>();
            systems.AddComponent<LabUI>();

            Debug.Log("SwarmLab ready. Keys 1-5 switch experiments, F morphs shapes, K kill, J jam, R reset, O toggle orbit.");
        }

        private static Mesh GrabMesh(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            var r = tmp.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
            Object.Destroy(tmp);
            return mesh;
        }

        private static Material MakeMaterial(Color color, bool emissive, float emissionStrength)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { enableInstancing = true };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * emissionStrength);
            }
            return mat;
        }
    }
}
