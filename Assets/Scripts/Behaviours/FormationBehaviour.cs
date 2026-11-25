using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Formation control. Each agent is assigned one slot in a target shape and steers
    /// to it (an "arrive" steering behaviour), so the swarm assembles into a sphere,
    /// ring, grid or heart. There is no central controller moving them - every agent
    /// only knows its own slot. Press F to morph to the next shape. The live metric is
    /// the average formation error (how far, on average, agents are from their slots).
    /// </summary>
    public class FormationBehaviour : SwarmBehaviour
    {
        public override string DisplayName => "Formation Control";
        public override string Description => "Agents assemble into shapes - sphere, ring, grid, heart. Press F to morph.";
        public override Color AgentColor => new Color(1f, 0.35f, 0.8f);
        public override string RealWorldUse => "Satellite constellations, collective transport, programmable drone light shows.";

        private enum Shape { Sphere, Ring, Grid, Heart }
        private static readonly string[] ShapeNames = { "Sphere", "Ring", "Grid", "Heart" };

        private Shape _shape = Shape.Sphere;
        private Vector3[] _targets;
        public float gain = 4f;

        public override string ExtraKeyHint => "[F] morph shape (" + ShapeNames[(int)_shape] + ")";

        public override void OnActivate(SwarmManager s) => BuildTargets(s);

        public override void OnExtraKey(SwarmManager s)
        {
            _shape = (Shape)(((int)_shape + 1) % 4);
            BuildTargets(s);
        }

        private void BuildTargets(SwarmManager s)
        {
            int n = s.Count;
            _targets = new Vector3[n];
            Vector3 c = s.transform.position;

            switch (_shape)
            {
                case Shape.Sphere:
                    for (int i = 0; i < n; i++)
                    {
                        float t = (i + 0.5f) / n;
                        float incl = Mathf.Acos(1f - 2f * t);
                        float azi = Mathf.PI * (1f + Mathf.Sqrt(5f)) * i;
                        _targets[i] = c + new Vector3(
                            Mathf.Sin(incl) * Mathf.Cos(azi),
                            Mathf.Cos(incl),
                            Mathf.Sin(incl) * Mathf.Sin(azi)) * 12f;
                    }
                    break;

                case Shape.Ring:
                    for (int i = 0; i < n; i++)
                    {
                        float a = 2f * Mathf.PI * i / n;
                        _targets[i] = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 15f;
                    }
                    break;

                case Shape.Grid:
                    int side = Mathf.CeilToInt(Mathf.Sqrt(n));
                    float span = 26f;
                    for (int i = 0; i < n; i++)
                    {
                        int gx = i % side;
                        int gz = i / side;
                        float x = (gx / (float)(side - 1) - 0.5f) * span;
                        float z = (gz / (float)(side - 1) - 0.5f) * span;
                        _targets[i] = c + new Vector3(x, 0f, z);
                    }
                    break;

                case Shape.Heart:
                    for (int i = 0; i < n; i++)
                    {
                        float a = 2f * Mathf.PI * i / n;
                        float hx = 16f * Mathf.Pow(Mathf.Sin(a), 3f);
                        float hy = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a)
                                   - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a);
                        _targets[i] = c + new Vector3(hx * 0.6f, hy * 0.6f, 0f);
                    }
                    break;
            }
        }

        public override Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours)
        {
            if (_targets == null || _targets.Length != swarm.Count) BuildTargets(swarm);

            Vector3 pos = swarm.positions[index];
            Vector3 to = _targets[index] - pos;
            float d = to.magnitude;
            // Arrive: full speed far away, easing to a stop on the slot.
            Vector3 desired = d > 0.01f ? (to / d) * Mathf.Min(swarm.maxSpeed, d * 3f) : Vector3.zero;
            Vector3 steer = (desired - swarm.velocities[index]) * gain;

            // Let the mouse drag the whole formation around.
            foreach (var (ipos, strength) in swarm.influencePoints)
            {
                Vector3 pull = ipos - pos;
                steer += pull.normalized * (strength / (pull.magnitude + 0.5f));
            }
            return steer;
        }

        public override string Metrics(SwarmManager s)
        {
            if (_targets == null) return null;
            float err = 0f;
            int cnt = 0;
            for (int i = 0; i < s.Count; i++)
            {
                if (!s.active[i]) continue;
                err += Vector3.Distance(s.positions[i], _targets[i]);
                cnt++;
            }
            if (cnt == 0) return null;
            return "Shape: " + ShapeNames[(int)_shape] + "   |   formation error: " + (err / cnt).ToString("F1") + " m";
        }
    }
}
