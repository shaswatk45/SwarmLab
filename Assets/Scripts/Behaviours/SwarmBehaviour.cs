using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Base class for every experiment (flocking, rendezvous, coverage, foraging,
    /// formation). The SwarmManager does not know which algorithm is running - it
    /// just asks the active behaviour how each agent should steer. Experiments can
    /// optionally draw their own extras (food, pheromone, explored area) and report
    /// a live metric line, all without touching the manager or UI.
    /// </summary>
    public abstract class SwarmBehaviour
    {
        /// <summary>Name shown in the HUD.</summary>
        public abstract string DisplayName { get; }

        /// <summary>One-line description shown in the lab HUD.</summary>
        public abstract string Description { get; }

        /// <summary>
        /// Return a steering acceleration for agent <paramref name="index"/>.
        /// <paramref name="neighbours"/> are nearby ACTIVE agents from the spatial
        /// hash (still includes self and agents outside the radius - filter as needed).
        /// </summary>
        public abstract Vector3 Steer(int index, SwarmManager swarm, List<int> neighbours);

        /// <summary>Called once when this experiment becomes active (set up state).</summary>
        public virtual void OnActivate(SwarmManager swarm) { }

        /// <summary>Called once per frame after agents are drawn (draw extras here).</summary>
        public virtual void OnRender(SwarmManager swarm) { }

        /// <summary>Optional extra HUD line (e.g. "Coverage: 42%"). Null = nothing.</summary>
        public virtual string Metrics(SwarmManager swarm) => null;

        /// <summary>Optional hint shown in the HUD for an experiment-specific key.</summary>
        public virtual string ExtraKeyHint => null;

        /// <summary>Optional response to the experiment's own key (F). Default: nothing.</summary>
        public virtual void OnExtraKey(SwarmManager swarm) { }

        /// <summary>Tint the swarm takes while this experiment runs.</summary>
        public virtual Color AgentColor => new Color(0.1f, 0.9f, 0.95f);

        /// <summary>Real-world application this experiment maps to (shown in the UI).</summary>
        public virtual string RealWorldUse => "";
    }
}
