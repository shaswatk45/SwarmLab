using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Classic Reynolds "boids" flocking (SIGGRAPH 1987). Each agent follows three
    /// local rules relative to the neighbours it can see:
    ///   1. Separation - steer away from crowding.
    ///   2. Alignment  - match the average heading of neighbours.
    ///   3. Cohesion   - steer toward the average position of neighbours.
    /// No agent has a global view; the lifelike flock is an emergent result. The
    /// mouse "influence points" are added on top so the user can herd the swarm.
    /// </summary>
    public class FlockingBehaviour : SwarmBehaviour
    {
        public override string DisplayName => "Flocking (Boids)";
        public override string Description => "Separation, alignment and cohesion produce lifelike flocking from local rules only.";

        public override string RealWorldUse => "Drone shows & UAV swarms holding cohesive movement; crowd and traffic modelling.";

        public float separationWeight = 2.2f;
        public float alignmentWeight = 1.1f;
        public float cohesionWeight = 1.0f;
        public float separationRadius = 2.0f;

        public override Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours)
        {
            Vector3 pos = swarm.positions[index];
            float r2 = swarm.neighbourRadius * swarm.neighbourRadius;
            float sepR2 = separationRadius * separationRadius;

            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int count = 0;

            foreach (int j in neighbours)
            {
                if (j == index) continue;
                Vector3 to = swarm.positions[j] - pos;
                float d2 = to.sqrMagnitude;
                if (d2 > r2 || d2 < 1e-6f) continue;

                count++;
                alignment += swarm.velocities[j];
                cohesion += swarm.positions[j];
                if (d2 < sepR2)
                    separation -= to / d2; // push away, stronger when closer
            }

            Vector3 steer = Vector3.zero;
            if (count > 0)
            {
                alignment = (alignment / count) - swarm.velocities[index];
                cohesion = ((cohesion / count) - pos);
                steer += separation * separationWeight
                       + alignment * alignmentWeight
                       + cohesion * cohesionWeight;
            }

            // Mouse herding: attract (positive strength) or repel (negative).
            foreach (var (ipos, strength) in swarm.influencePoints)
            {
                Vector3 to = ipos - pos;
                float d = to.magnitude + 0.5f;
                steer += to.normalized * (strength / d);
            }

            return steer;
        }
    }
}
