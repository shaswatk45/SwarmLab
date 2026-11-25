using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SwarmLab
{
    /// <summary>
    /// Dresses the scene into a neon lab: a glowing grid floor, atmospheric fog, a
    /// bright boundary ring around the arena, and URP post-processing (bloom, vignette,
    /// colour grading) so every emissive thing glows. All of it is built at runtime and
    /// each risky step is guarded so it can never break Play.
    /// </summary>
    public class LabEnvironment : MonoBehaviour
    {
        private void Start()
        {
            float arena = 25f;
            var swarm = FindFirstObjectByType<SwarmManager>();
            if (swarm != null) arena = swarm.arenaRadius;

            try { SetupFog(); } catch { }
            try { SetupFloor(); } catch { }
            try { SetupBoundary(arena, swarm != null ? swarm.transform.position : Vector3.zero); } catch { }
            try { SetupPostFx(); } catch { }
        }

        private void SetupFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.02f, 0.04f, 0.07f);
        }

        private void SetupFloor()
        {
            var ground = GameObject.Find("Lab_Ground");
            if (ground == null) return;
            var tex = BuildGridTexture(256, 3);
            var mat = VizPrimitives.Emissive(new Color(0.03f, 0.05f, 0.08f), 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.02f, 0.04f, 0.07f));
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); mat.SetTextureScale("_BaseMap", new Vector2(10, 10)); }
            if (mat.HasProperty("_EmissionMap")) { mat.SetTexture("_EmissionMap", tex); mat.SetTextureScale("_EmissionMap", new Vector2(10, 10)); }
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(0.1f, 0.95f, 0.95f) * 1.3f);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private Texture2D BuildGridTexture(int size, int line)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            var dark = new Color(0.01f, 0.02f, 0.03f, 1f);
            var glow = new Color(0.3f, 1f, 1f, 1f);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = (x < line || y < line) ? glow : dark;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private void SetupBoundary(float arena, Vector3 centre)
        {
            var mat = VizPrimitives.Emissive(new Color(0.1f, 0.9f, 1f), 2.5f);
            int n = 160;
            var holder = new GameObject("Lab_Boundary");
            holder.transform.SetParent(transform, false);
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "post";
                Object.Destroy(post.GetComponent<Collider>());
                post.transform.SetParent(holder.transform, false);
                post.transform.position = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * arena + Vector3.up * 0.5f;
                post.transform.localScale = new Vector3(0.25f, 1.0f, 0.25f);
                post.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private void SetupPostFx()
        {
            var go = new GameObject("Lab_PostFX");
            go.transform.SetParent(transform, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.3f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.75f);

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.38f);
            vig.smoothness.Override(0.5f);
            vig.color.Override(new Color(0f, 0.02f, 0.04f));

            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.15f);
            ca.contrast.Override(12f);
            ca.saturation.Override(14f);

            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);

            // Make sure the camera actually runs post-processing.
            var cam = Camera.main;
            if (cam != null)
            {
                try { cam.GetUniversalAdditionalCameraData().renderPostProcessing = true; } catch { }
            }
        }
    }
}
