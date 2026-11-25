using UnityEngine;
using UnityEngine.InputSystem;

namespace SwarmLab
{
    /// <summary>
    /// Desktop interaction + the lab dashboard. Raycasts the mouse onto the arena to
    /// herd the swarm, switches between the five experiments, triggers fault injection
    /// (kill / jam), and draws the live metrics HUD. Uses the new Input System so no
    /// project settings need changing. The same intents map onto XR controllers later.
    /// </summary>
    [RequireComponent(typeof(SwarmManager))]
    public class SwarmInteractor : MonoBehaviour
    {
        public float attractStrength = 60f;
        public float repelStrength = 90f;
        public float killFraction = 0.3f;

        private SwarmManager _swarm;
        private Plane _ground;
        private Camera _cam;
        private DataLogger _logger;
        private ScenarioManager _scenario;

        private void Awake()
        {
            _swarm = GetComponent<SwarmManager>();
            _ground = new Plane(Vector3.up, transform.position);
            _cam = Camera.main;
            _logger = FindFirstObjectByType<DataLogger>();
            _scenario = FindFirstObjectByType<ScenarioManager>();
        }

        private Vector3 MouseGroundPoint()
        {
            if (_cam == null || Mouse.current == null) return _swarm.transform.position;
            Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            return _ground.Raycast(ray, out float e) ? ray.GetPoint(e) : _swarm.transform.position;
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;

            var mouse = Mouse.current;
            var kb = Keyboard.current;

            _swarm.influencePoints.Clear();
            if (_cam != null && mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                Ray ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
                if (_ground.Raycast(ray, out float enter))
                {
                    Vector3 p = ray.GetPoint(enter);
                    float s = mouse.leftButton.isPressed ? attractStrength : -repelStrength;
                    _swarm.influencePoints.Add((p, s));
                }
            }

            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) _swarm.SetBehaviour(new FlockingBehaviour());
            if (kb.digit2Key.wasPressedThisFrame) _swarm.SetBehaviour(new RendezvousBehaviour());
            if (kb.digit3Key.wasPressedThisFrame) _swarm.SetBehaviour(new CoverageBehaviour());
            if (kb.digit4Key.wasPressedThisFrame) _swarm.SetBehaviour(new ForagingBehaviour());
            if (kb.digit5Key.wasPressedThisFrame) _swarm.SetBehaviour(new FormationBehaviour());
            if (kb.digit6Key.wasPressedThisFrame) _swarm.SetBehaviour(new SearchRescueBehaviour());
            if (kb.fKey.wasPressedThisFrame && _swarm.ActiveBehaviour != null) _swarm.ActiveBehaviour.OnExtraKey(_swarm);
            if (kb.kKey.wasPressedThisFrame) _swarm.KillFraction(killFraction);
            if (kb.jKey.wasPressedThisFrame) _swarm.ToggleJam();
            if (kb.rKey.wasPressedThisFrame) _swarm.Respawn();
            if (kb.eKey.wasPressedThisFrame && _logger != null) _logger.Export();
            if (kb.bKey.wasPressedThisFrame && _scenario != null) _scenario.AddObstacle(MouseGroundPoint(), 2.5f);
            if (kb.nKey.wasPressedThisFrame && _scenario != null) _scenario.ClearObstacles();
        }
    }
}
