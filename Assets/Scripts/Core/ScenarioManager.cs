using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Builds the physical environment: obstacle columns the swarm must navigate around
    /// (registered with the SwarmManager so every experiment avoids them), plus distant
    /// backdrop pillars for depth. Also exposes add / clear so the user can drop obstacles
    /// live - demonstrating that the swarm adapts to a changing environment, like real
    /// robots in a warehouse or disaster site.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        private SwarmManager _swarm;
        private GameObject _holder;
        private Material _obsMat;
        private readonly List<GameObject> _obstacleObjects = new List<GameObject>();

        public int ObstacleCount => _obstacleObjects.Count;

        private void Start()
        {
            _swarm = FindFirstObjectByType<SwarmManager>();
            _holder = new GameObject("Lab_Obstacles");
            _holder.transform.SetParent(transform, false);
            _obsMat = VizPrimitives.Emissive(new Color(0.55f, 0.6f, 0.7f), 0.35f);

            try { BuildDefaultLayout(); } catch { }
            try { BuildBackdrop(); } catch { }
        }

        private void BuildDefaultLayout()
        {
            if (_swarm == null) return;
            Vector3 c = _swarm.transform.position;
            // A scattered "debris field" inside the arena.
            Vector2[] spots =
            {
                new Vector2(-12, 6), new Vector2(10, 10), new Vector2(3, -12),
                new Vector2(-8, -8), new Vector2(15, -3), new Vector2(-16, -2)
            };
            foreach (var p in spots)
                AddObstacle(new Vector3(c.x + p.x, c.y, c.z + p.y), 2.5f);
        }

        /// <summary>Create one obstacle column and register it for avoidance.</summary>
        public void AddObstacle(Vector3 pos, float radius)
        {
            if (_swarm == null) return;
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.name = "Obstacle";
            var collider = col.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            col.transform.SetParent(_holder.transform, false);
            // Cylinder default height is 2 (scale.y is half-height), so y-scale 9 => ~18 tall.
            col.transform.position = new Vector3(pos.x, _swarm.transform.position.y - 2f, pos.z);
            col.transform.localScale = new Vector3(radius * 2f, 9f, radius * 2f);
            col.GetComponent<Renderer>().sharedMaterial = _obsMat;
            _obstacleObjects.Add(col);
            _swarm.obstacles.Add((new Vector3(pos.x, _swarm.transform.position.y, pos.z), radius));
        }

        public void ClearObstacles()
        {
            foreach (var go in _obstacleObjects) if (go != null) Object.Destroy(go);
            _obstacleObjects.Clear();
            if (_swarm != null) _swarm.obstacles.Clear();
        }

        private void BuildBackdrop()
        {
            if (_swarm == null) return;
            Vector3 c = _swarm.transform.position;
            var mat = VizPrimitives.Emissive(new Color(0.08f, 0.3f, 0.4f), 0.8f);
            var holder = new GameObject("Lab_Backdrop");
            holder.transform.SetParent(transform, false);
            int n = 28;
            float ring = _swarm.arenaRadius + 22f;
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Pillar";
                var collider = pillar.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);
                pillar.transform.SetParent(holder.transform, false);
                float h = Random.Range(18f, 40f);
                pillar.transform.position = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring + Vector3.up * (h * 0.5f - 2f);
                pillar.transform.localScale = new Vector3(2.5f, h, 2.5f);
                pillar.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
    }
}
