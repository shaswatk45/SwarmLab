using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Ant-style foraging (stigmergy). Agents leave the nest, find food, carry it home,
    /// and drop a pheromone trail on the way back. Searching agents are drawn along
    /// existing pheromone, so popular routes reinforce themselves - an emergent shortest
    /// path with no map and no communication beyond marks left in the environment
    /// (Dorigo & Stutzle). Green = nest, magenta = food, orange glow = pheromone trails.
    /// </summary>
    public class ForagingBehaviour : SwarmBehaviour
    {
        public override string DisplayName => "Foraging (Pheromone)";
        public override string Description => "Ants leave trails home from food; good routes reinforce themselves.";
        public override Color AgentColor => new Color(1f, 0.6f, 0.15f);
        public override string RealWorldUse => "Warehouse logistics & routing; ant-colony optimisation for delivery networks.";

        public float cellSize = 2f;
        public float speed = 6f;
        public float deposit = 0.6f;
        public float evaporation = 0.992f;
        public float trailWeight = 2.0f;
        public float foodSeekWeight = 1.2f;

        private Vector3 _nest;
        private Vector3[] _food;
        private bool[] _carrying;
        private int _delivered;

        private float[] _phero;
        private int _cols;
        private float _minX, _minZ;

        private Material _pheroMat, _nestMat, _foodMat;
        private readonly List<Matrix4x4> _pheroTiles = new List<Matrix4x4>();
        private readonly List<Matrix4x4> _markers = new List<Matrix4x4>();

        public override void OnActivate(SwarmManager s)
        {
            Vector3 c = s.transform.position;
            _nest = c;
            _food = new Vector3[5];
            for (int i = 0; i < _food.Length; i++)
            {
                float a = 2f * Mathf.PI * i / _food.Length + 0.3f;
                float r = s.arenaRadius * (0.7f + 0.2f * Mathf.Sin(i * 2.1f));
                _food[i] = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
            }

            _minX = c.x - s.arenaRadius;
            _minZ = c.z - s.arenaRadius;
            _cols = Mathf.CeilToInt(2f * s.arenaRadius / cellSize);
            _phero = new float[_cols * _cols];

            _carrying = new bool[s.Count];
            _delivered = 0;
        }

        private int Idx(int gx, int gz) => Mathf.Clamp(gz, 0, _cols - 1) * _cols + Mathf.Clamp(gx, 0, _cols - 1);
        private int GX(Vector3 p) => Mathf.Clamp(Mathf.FloorToInt((p.x - _minX) / cellSize), 0, _cols - 1);
        private int GZ(Vector3 p) => Mathf.Clamp(Mathf.FloorToInt((p.z - _minZ) / cellSize), 0, _cols - 1);

        private Vector3 PheroGradient(Vector3 p)
        {
            int gx = GX(p), gz = GZ(p);
            float right = _phero[Idx(gx + 1, gz)];
            float left = _phero[Idx(gx - 1, gz)];
            float up = _phero[Idx(gx, gz + 1)];
            float down = _phero[Idx(gx, gz - 1)];
            return new Vector3(right - left, 0f, up - down);
        }

        public override Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours)
        {
            if (_carrying == null || _carrying.Length != swarm.Count) OnActivate(swarm);

            Vector3 pos = swarm.positions[index];
            Vector3 flat = pos; flat.y = _nest.y;
            Vector3 dir;

            if (_carrying[index])
            {
                // Head home and lay a trail.
                _phero[Idx(GX(pos), GZ(pos))] = Mathf.Min(4f, _phero[Idx(GX(pos), GZ(pos))] + deposit);
                dir = (_nest - flat).normalized;
                if (HorizDist(flat, _nest) < 3f) { _carrying[index] = false; _delivered++; }
            }
            else
            {
                // Search: nearest food + follow pheromone + a little noise.
                Vector3 toFood = (NearestFood(flat) - flat);
                if (toFood.magnitude < 3f) { _carrying[index] = true; }
                Vector3 grad = PheroGradient(pos);
                dir = toFood.normalized * foodSeekWeight
                    + grad.normalized * trailWeight
                    + Random.onUnitSphere.WithY(0f) * 0.4f;
                dir = dir.normalized;
            }

            // Separation so they stream rather than pile up.
            Vector3 sep = Vector3.zero;
            foreach (int j in neighbours)
            {
                if (j == index) continue;
                Vector3 d = swarm.positions[j] - pos;
                float d2 = d.sqrMagnitude;
                if (d2 < 2.25f && d2 > 1e-6f) sep -= d / d2;
            }

            Vector3 steer = dir * speed - swarm.velocities[index];
            steer += sep * 2f;
            steer.y += (_nest.y - pos.y) * 2f; // keep foraging on a readable slab
            return steer;
        }

        private Vector3 NearestFood(Vector3 p)
        {
            Vector3 best = _food[0];
            float bd = float.MaxValue;
            foreach (var f in _food)
            {
                float d = HorizDist(p, f);
                if (d < bd) { bd = d; best = f; }
            }
            return best;
        }

        private static float HorizDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public override string Metrics(SwarmManager s)
        {
            int trail = 0;
            if (_phero != null) foreach (var v in _phero) if (v > 0.1f) trail++;
            return "Delivered: " + _delivered + "   |   active trail cells: " + trail;
        }

        public override void OnRender(SwarmManager s)
        {
            if (_phero == null) return;
            if (_pheroMat == null) _pheroMat = VizPrimitives.Emissive(new Color(1f, 0.5f, 0.05f), 1.4f);
            if (_nestMat == null) _nestMat = VizPrimitives.Emissive(new Color(0.2f, 1f, 0.3f), 2f);
            if (_foodMat == null) _foodMat = VizPrimitives.Emissive(new Color(1f, 0.2f, 0.7f), 2f);

            // Evaporate once per frame.
            for (int i = 0; i < _phero.Length; i++) _phero[i] *= evaporation;

            // Pheromone tiles.
            _pheroTiles.Clear();
            for (int i = 0; i < _phero.Length; i++)
            {
                if (_phero[i] < 0.08f) continue;
                int gx = i % _cols, gz = i / _cols;
                float x = _minX + (gx + 0.5f) * cellSize;
                float z = _minZ + (gz + 0.5f) * cellSize;
                float h = Mathf.Clamp(_phero[i], 0.1f, 3f);
                _pheroTiles.Add(Matrix4x4.TRS(new Vector3(x, 0.15f, z), Quaternion.identity,
                    new Vector3(cellSize * 0.8f, 0.1f, cellSize * 0.8f) * (0.6f + h * 0.2f)));
            }
            VizPrimitives.DrawInstanced(VizPrimitives.Cube, _pheroMat, _pheroTiles);

            // Nest.
            _markers.Clear();
            _markers.Add(Matrix4x4.TRS(_nest, Quaternion.identity, Vector3.one * 3f));
            VizPrimitives.DrawInstanced(VizPrimitives.Sphere, _nestMat, _markers);

            // Food.
            _markers.Clear();
            foreach (var f in _food)
                _markers.Add(Matrix4x4.TRS(f, Quaternion.identity, Vector3.one * 2.2f));
            VizPrimitives.DrawInstanced(VizPrimitives.Sphere, _foodMat, _markers);
        }
    }
}
