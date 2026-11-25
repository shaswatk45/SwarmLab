using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Distributed rendezvous / consensus. Each agent steers toward the average
    /// position of the neighbours it can see (a local averaging rule). With no leader
    /// and no global coordinate, the whole swarm provably converges to a single
    /// meeting point - the classic consensus result (Olfati-Saber et al., 2007).
    /// A small separation term stops them piling into an exact singularity.
    /// </summary>
    public class RendezvousBehaviour : SwarmBehaviour
    {
        public override string DisplayName => "Rendezvous (Consensus)";
        public override string Description => "Leaderless agents average their neighbours' positions and converge to one meeting point.";
        public override Color AgentColor => new Color(1f, 0.82f, 0.2f);
        public override string RealWorldUse => "Robots regrouping at a rally point; distributed clock/sensor synchronisation.";

        public float gatherWeight = 2.0f;
        public float separationWeight = 1.4f;
        public float separationRadius = 1.2f;

        public override Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours)
        {
            Vector3 pos = swarm.positions[index];
            float r2 = swarm.neighbourRadius * swarm.neighbourRadius;
            float sepR2 = separationRadius * separationRadius;

            Vector3 centroid = Vector3.zero;
            Vector3 separation = Vector3.zero;
            int count = 0;

            foreach (int j in neighbours)
            {
                if (j == index) continue;
                Vector3 to = swarm.positions[j] - pos;
                float d2 = to.sqrMagnitude;
                if (d2 > r2 || d2 < 1e-6f) continue;

                count++;
                centroid += swarm.positions[j];
                if (d2 < sepR2)
                    separation -= to / d2;
            }

            Vector3 steer = Vector3.zero;
            if (count > 0)
            {
                Vector3 toCentre = (centroid / count) - pos;
                steer += toCentre * gatherWeight + separation * separationWeight;
            }

            foreach (var (ipos, strength) in swarm.influencePoints)
            {
                Vector3 to = ipos - pos;
                float d = to.magnitude + 0.5f;
                steer += to.normalized * (strength / d);
            }

            return steer;
        }

        // Average distance from the swarm centroid - shrinks as they converge.
        public override string Metrics(SwarmManager s)
        {
            Vector3 c = Vector3.zero;
            int cnt = 0;
            for (int i = 0; i < s.Count; i++) { if (!s.active[i]) continue; c += s.positions[i]; cnt++; }
            if (cnt == 0) return null;
            c /= cnt;
            float spread = 0f;
            for (int i = 0; i < s.Count; i++) { if (!s.active[i]) continue; spread += Vector3.Distance(s.positions[i], c); }
            return "Spread: " + (spread / cnt).ToString("F1") + " m (converging)";
        }
    }
}
