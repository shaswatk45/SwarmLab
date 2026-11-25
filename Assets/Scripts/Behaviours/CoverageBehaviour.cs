using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Area coverage / exploration. The arena floor is divided into a grid; an agent
    /// marks a cell "explored" when it passes over it. Agents spread out (dispersion +
    /// wander) so the swarm sweeps the whole space. The live metric is the percentage
    /// of the area covered - the headline number for search-and-rescue style missions.
    /// Explored tiles light up faintly on the floor so you can watch the map fill in.
    /// </summary>
    public class CoverageBehaviour : SwarmBehaviour
    {
        public override string DisplayName => "Area Coverage";
        public override string Description => "The swarm disperses to explore; the floor lights up as area is covered.";
        public override Color AgentColor => new Color(0.3f, 1f, 0.5f);
        public override string RealWorldUse => "Environmental mapping & surveying, field monitoring, cleaning robots, recon.";

        public float cellSize = 2.5f;
        public float separationWeight = 1.8f;
        public float wanderWeight = 3.0f;

        private bool[] _visited;
        private int _cols;
        private float _minX, _minZ;
        private int _visitedCount, _total;
        private Vector3[] _wander;
        private float _startTime;

        private Material _tileMat;
        private readonly List<Matrix4x4> _tiles = new List<Matrix4x4>();

        public override void OnActivate(SwarmManager s)
        {
            Vector3 c = s.transform.position;
            _minX = c.x - s.arenaRadius;
            _minZ = c.z - s.arenaRadius;
            _cols = Mathf.CeilToInt(2f * s.arenaRadius / cellSize);
            _total = _cols * _cols;
            _visited = new bool[_total];
            _visitedCount = 0;
            _startTime = Time.time;

            _wander = new Vector3[s.Count];
            for (int i = 0; i < s.Count; i++)
                _wander[i] = Random.onUnitSphere.WithY(0f).normalized;
        }

        private int CellIndex(Vector3 p)
        {
            int gx = Mathf.Clamp(Mathf.FloorToInt((p.x - _minX) / cellSize), 0, _cols - 1);
            int gz = Mathf.Clamp(Mathf.FloorToInt((p.z - _minZ) / cellSize), 0, _cols - 1);
            return gz * _cols + gx;
        }

        public override Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours)
        {
            Vector3 pos = swarm.positions[index];

            // Mark the cell under this agent as explored.
            int cell = CellIndex(pos);
            if (!_visited[cell]) { _visited[cell] = true; _visitedCount++; }

            // Disperse from neighbours.
            Vector3 separation = Vector3.zero;
            float sepR2 = (cellSize * 1.5f) * (cellSize * 1.5f);
            foreach (int j in neighbours)
            {
                if (j == index) continue;
                Vector3 to = swarm.positions[j] - pos;
                float d2 = to.sqrMagnitude;
                if (d2 < sepR2 && d2 > 1e-6f) separation -= to / d2;
            }

            // Smooth random wander, kept on the horizontal plane.
            if (_wander == null || _wander.Length != swarm.Count) OnActivate(swarm);
            _wander[index] = (_wander[index] + Random.onUnitSphere.WithY(0f) * 0.3f).normalized;

            return separation * separationWeight + _wander[index] * wanderWeight;
        }

        public override string Metrics(SwarmManager s)
        {
            float pct = _total > 0 ? 100f * _visitedCount / _total : 0f;
            return "Coverage: " + pct.ToString("F0") + "%   |   time " + (Time.time - _startTime).ToString("F0") + "s";
        }

        public override void OnRender(SwarmManager s)
        {
            if (_tileMat == null) _tileMat = VizPrimitives.Emissive(new Color(0.0f, 0.35f, 0.45f), 1.2f);
            _tiles.Clear();
            Vector3 scale = new Vector3(cellSize * 0.92f, 0.1f, cellSize * 0.92f);
            for (int idx = 0; idx < _total; idx++)
            {
                if (!_visited[idx]) continue;
                int gx = idx % _cols;
                int gz = idx / _cols;
                float x = _minX + (gx + 0.5f) * cellSize;
                float z = _minZ + (gz + 0.5f) * cellSize;
                _tiles.Add(Matrix4x4.TRS(new Vector3(x, 0.12f, z), Quaternion.identity, scale));
            }
            VizPrimitives.DrawInstanced(VizPrimitives.Cube, _tileMat, _tiles);
        }
    }

    internal static class VecExt
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
