using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Search & Rescue mission - the headline real-world use of swarm robotics. A set
    /// of "survivors" is scattered across the arena (hidden among obstacles). The swarm
    /// spreads out to search; when an agent spots a survivor it converges on them, and a
    /// survivor is rescued once agents reach them. The live mission metric is how many
    /// have been found and how long it took - exactly the number a real deployment cares
    /// about. Works together with the obstacles, so the swarm searches around them.
    /// </summary>
    public class SearchRescueBehaviour : SwarmBehaviour
    {
        public override string DisplayName => "Search & Rescue";
        public override string Description => "The swarm searches the area and converges on survivors to rescue them.";
        public override Color AgentColor => new Color(0.4f, 0.8f, 1f);
        public override string RealWorldUse => "Disaster response: covering large or dangerous areas to locate survivors fast.";

        public int survivorCount = 18;
        public float sightRadius = 13f;
        public float detectRadius = 2.5f;
        public float wanderWeight = 2.6f;
        public float seekWeight = 4f;

        private Vector3[] _survivors;
        private bool[] _found;
        private int _rescued;
        private float _startTime, _missionTime;
        private Vector3[] _wander;

        private Material _redMat, _greenMat;
        private readonly List<Matrix4x4> _red = new List<Matrix4x4>();
        private readonly List<Matrix4x4> _green = new List<Matrix4x4>();

        public override void OnActivate(SwarmManager s)
        {
            Vector3 c = s.transform.position;
            _survivors = new Vector3[survivorCount];
            _found = new bool[survivorCount];
            for (int i = 0; i < survivorCount; i++)
            {
                Vector3 p = c;
                for (int tries = 0; tries < 20; tries++)
                {
                    Vector2 r = Random.insideUnitCircle * (s.arenaRadius * 0.85f);
                    p = new Vector3(c.x + r.x, c.y, c.z + r.y);
                    if (!InsideObstacle(s, p)) break;
                }
                _survivors[i] = p;
            }
            _rescued = 0;
            _startTime = Time.time;
            _missionTime = 0f;

            _wander = new Vector3[s.Count];
            for (int i = 0; i < s.Count; i++) _wander[i] = Random.onUnitSphere.WithY(0f).normalized;
        }

        private static bool InsideObstacle(SwarmManager s, Vector3 p)
        {
            for (int o = 0; o < s.obstacles.Count; o++)
            {
                Vector3 to = p - s.obstacles[o].pos; to.y = 0f;
                if (to.magnitude < s.obstacles[o].radius + 3f) return true;
            }
            return false;
        }

        public override Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours)
        {
            if (_wander == null || _wander.Length != swarm.Count) OnActivate(swarm);

            Vector3 pos = swarm.positions[index];

            // Detect & rescue any survivor this agent reaches.
            int nearest = -1;
            float nearestD = sightRadius;
            for (int i = 0; i < _survivors.Length; i++)
            {
                if (_found[i]) continue;
                Vector3 to = _survivors[i] - pos; to.y = 0f;
                float d = to.magnitude;
                if (d < detectRadius) { _found[i] = true; _rescued++; continue; }
                if (d < nearestD) { nearestD = d; nearest = i; }
            }

            Vector3 dir;
            if (nearest >= 0)
            {
                // Spotted someone - converge.
                Vector3 to = _survivors[nearest] - pos; to.y = 0f;
                dir = to.normalized * seekWeight;
            }
            else
            {
                // Keep searching: wander + spread out.
                _wander[index] = (_wander[index] + Random.onUnitSphere.WithY(0f) * 0.3f).normalized;
                dir = _wander[index] * wanderWeight;
            }

            Vector3 sep = Vector3.zero;
            foreach (int j in neighbours)
            {
                if (j == index) continue;
                Vector3 to = swarm.positions[j] - pos;
                float d2 = to.sqrMagnitude;
                if (d2 < 4f && d2 > 1e-6f) sep -= to / d2;
            }

            Vector3 steer = dir + sep * 1.5f;
            steer.y += (swarm.transform.position.y - pos.y) * 1.5f; // keep near the search slab
            return steer;
        }

        public override string Metrics(SwarmManager s)
        {
            if (_rescued < _survivors.Length) _missionTime = Time.time - _startTime;
            string done = _rescued >= _survivors.Length ? "  ✔ ALL RESCUED" : "";
            return "Rescued: " + _rescued + " / " + _survivors.Length +
                   "   |   mission time " + _missionTime.ToString("F1") + "s" + done;
        }

        public override void OnRender(SwarmManager s)
        {
            if (_survivors == null) return;
            if (_redMat == null) _redMat = VizPrimitives.Emissive(new Color(1f, 0.15f, 0.15f), 2.2f);
            if (_greenMat == null) _greenMat = VizPrimitives.Emissive(new Color(0.2f, 1f, 0.3f), 2.2f);

            _red.Clear(); _green.Clear();
            float pulse = 1.6f + Mathf.Sin(Time.time * 4f) * 0.25f;
            for (int i = 0; i < _survivors.Length; i++)
            {
                var m = Matrix4x4.TRS(_survivors[i], Quaternion.identity,
                    Vector3.one * (_found[i] ? 1.8f : pulse));
                if (_found[i]) _green.Add(m); else _red.Add(m);
            }
            VizPrimitives.DrawInstanced(VizPrimitives.Sphere, _redMat, _red);
            VizPrimitives.DrawInstanced(VizPrimitives.Sphere, _greenMat, _green);
        }
    }
}
