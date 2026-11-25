using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// The heart of SwarmLab. It owns the agent data (position + velocity arrays),
    /// runs the simulation each frame, and draws all agents in a handful of GPU
    /// instanced draw calls so thousands of robots stay fast. The "thinking" is
    /// delegated to the active <see cref="SwarmBehaviour"/>. It also supports fault
    /// injection (killing agents, jamming communication) and exposes live metrics.
    /// </summary>
    [DisallowMultipleComponent]
    public class SwarmManager : MonoBehaviour
    {
        [Header("Swarm")]
        public int agentCount = 1000;
        public float arenaRadius = 25f;

        [Header("Motion")]
        public float maxSpeed = 8f;
        public float maxForce = 20f;
        public float neighbourRadius = 4f;

        [Header("Rendering")]
        public Mesh agentMesh;
        public Material agentMaterial;
        public float agentScale = 0.35f;

        // --- State the behaviours and interaction scripts read/write ---
        [HideInInspector] public Vector3[] positions;
        [HideInInspector] public Vector3[] velocities;
        [HideInInspector] public bool[] active;      // false = killed (fault injection)
        public int Count { get; private set; }

        /// <summary>External pull/push points set by the mouse interactor (world pos, strength).</summary>
        public readonly List<(Vector3 pos, float strength)> influencePoints = new List<(Vector3, float)>();

        /// <summary>Obstacles every experiment avoids (world pos, radius). Set by ScenarioManager.</summary>
        public readonly List<(Vector3 pos, float radius)> obstacles = new List<(Vector3, float)>();

        // --- Live metrics ---
        public int ActiveCount { get; private set; }
        public float Connectivity { get; private set; } // avg neighbours within radius per active agent
        public bool CommsJammed { get; private set; }

        private SwarmBehaviour _behaviour;
        private SpatialHash _grid;
        private readonly List<int> _neighbourBuffer = new List<int>(256);

        private const int BatchSize = 1023;
        private Matrix4x4[] _matrixBatch;

        public SwarmBehaviour ActiveBehaviour => _behaviour;

        private void Start()
        {
            Allocate();
            _grid = new SpatialHash(neighbourRadius);
            _matrixBatch = new Matrix4x4[BatchSize];
            SetBehaviour(new FlockingBehaviour());
        }

        /// <summary>Create arrays and scatter agents randomly inside the arena.</summary>
        public void Allocate()
        {
            Count = Mathf.Max(1, agentCount);
            positions = new Vector3[Count];
            velocities = new Vector3[Count];
            active = new bool[Count];
            for (int i = 0; i < Count; i++)
            {
                positions[i] = transform.position + Random.insideUnitSphere * arenaRadius * 0.8f;
                velocities[i] = Random.insideUnitSphere * maxSpeed * 0.5f;
                active[i] = true;
            }
            ActiveCount = Count;
            CommsJammed = false;
        }

        /// <summary>Swap the running experiment at runtime.</summary>
        public void SetBehaviour(SwarmBehaviour behaviour)
        {
            _behaviour = behaviour;
            if (_behaviour != null)
            {
                _behaviour.OnActivate(this);
                ApplyAgentColor(_behaviour.AgentColor);
            }
        }

        /// <summary>Recolour the swarm material (base + emission) for the active experiment.</summary>
        private void ApplyAgentColor(Color c)
        {
            if (agentMaterial == null) return;
            if (agentMaterial.HasProperty("_BaseColor")) agentMaterial.SetColor("_BaseColor", c);
            if (agentMaterial.HasProperty("_Color")) agentMaterial.SetColor("_Color", c);
            if (agentMaterial.HasProperty("_EmissionColor")) agentMaterial.SetColor("_EmissionColor", c * 2.2f);
        }

        /// <summary>Re-scatter the swarm and clear all faults.</summary>
        public void Respawn()
        {
            Allocate();
            if (_behaviour != null) _behaviour.OnActivate(this);
        }

        // ---- Fault injection -------------------------------------------------
        /// <summary>Deactivate a fraction of the currently-active agents.</summary>
        public void KillFraction(float fraction)
        {
            var live = new List<int>();
            for (int i = 0; i < Count; i++) if (active[i]) live.Add(i);
            int toKill = Mathf.RoundToInt(live.Count * Mathf.Clamp01(fraction));
            for (int k = 0; k < toKill; k++)
            {
                int r = Random.Range(0, live.Count);
                active[live[r]] = false;
                live.RemoveAt(r);
            }
            ActiveCount = live.Count;
        }

        public void ToggleJam() => CommsJammed = !CommsJammed;

        // ---------------------------------------------------------------------
        private void Update()
        {
            if (_behaviour == null) return;
            Simulate(Time.deltaTime);
            Render();
            _behaviour.OnRender(this); // experiment-specific extras
        }

        private void Simulate(float dt)
        {
            _grid.Rebuild(positions, active, Count);

            Vector3 centre = transform.position;
            long connSum = 0;
            float r2 = neighbourRadius * neighbourRadius;

            for (int i = 0; i < Count; i++)
            {
                if (!active[i]) continue;

                _grid.QueryNeighbours(positions[i], _neighbourBuffer);

                // Count genuine neighbours for the connectivity metric.
                if (!CommsJammed)
                {
                    foreach (int j in _neighbourBuffer)
                    {
                        if (j == i) continue;
                        if ((positions[j] - positions[i]).sqrMagnitude <= r2) connSum++;
                    }
                }
                else
                {
                    // Jammed: agents cannot sense neighbours at all.
                    _neighbourBuffer.Clear();
                }

                Vector3 steer = _behaviour.Steer(i, this, _neighbourBuffer);

                // Obstacle avoidance (horizontal) - shared by every experiment, so the
                // swarm navigates around real objects just like physical robots would.
                for (int o = 0; o < obstacles.Count; o++)
                {
                    Vector3 away = positions[i] - obstacles[o].pos;
                    away.y = 0f;
                    float d = away.magnitude;
                    float safe = obstacles[o].radius + 1.8f;
                    if (d < safe && d > 1e-4f)
                        steer += (away / d) * (safe - d) * maxForce * 1.6f;
                }

                // Soft containment near the arena edge.
                Vector3 offset = positions[i] - centre;
                float dist = offset.magnitude;
                if (dist > arenaRadius)
                    steer += -offset.normalized * (dist - arenaRadius) * maxForce;

                steer = Vector3.ClampMagnitude(steer, maxForce);
                velocities[i] += steer * dt;
                velocities[i] = Vector3.ClampMagnitude(velocities[i], maxSpeed);
                positions[i] += velocities[i] * dt;
            }

            Connectivity = ActiveCount > 0 ? (float)connSum / ActiveCount : 0f;
        }

        private void Render()
        {
            if (agentMesh == null || agentMaterial == null) return;

            var rp = new RenderParams(agentMaterial)
            {
                worldBounds = new Bounds(transform.position, Vector3.one * (arenaRadius * 4f))
            };

            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                if (!active[i]) continue;
                Quaternion rot = velocities[i].sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(velocities[i])
                    : Quaternion.identity;
                _matrixBatch[n++] = Matrix4x4.TRS(positions[i], rot, Vector3.one * agentScale);
                if (n == BatchSize)
                {
                    Graphics.RenderMeshInstanced(rp, agentMesh, 0, _matrixBatch, n);
                    n = 0;
                }
            }
            if (n > 0) Graphics.RenderMeshInstanced(rp, agentMesh, 0, _matrixBatch, n);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 0.9f, 0.25f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * arenaRadius * 2f);
        }
    }
}
