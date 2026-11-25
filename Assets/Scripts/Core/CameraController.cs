using UnityEngine;
using UnityEngine.InputSystem;

namespace SwarmLab
{
    /// <summary>
    /// A simple cinematic orbit camera: it slowly circles the arena for a demo-friendly
    /// look, and the user can middle-drag to rotate, scroll to zoom, and press O to
    /// toggle the auto-orbit. Uses the new Input System. Attach to the main camera.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public Vector3 focus = new Vector3(0f, 8f, 0f);
        public float distance = 60f;
        public float yaw = 0f;
        public float pitch = 30f;
        public bool autoOrbit = true;
        public float autoSpeed = 6f;

        private void Start()
        {
            // Initialise from the current transform if it looks sensible.
            Vector3 d = transform.position - focus;
            if (d.sqrMagnitude > 1f) distance = d.magnitude;
            Apply();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool dragging = false;

            if (mouse != null)
            {
                if (mouse.middleButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * 0.2f;
                    pitch = Mathf.Clamp(pitch - delta.y * 0.2f, 5f, 85f);
                    dragging = true;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance - scroll * 0.03f, 18f, 130f);
            }

            if (kb != null && kb.oKey.wasPressedThisFrame) autoOrbit = !autoOrbit;
            if (autoOrbit && !dragging) yaw += autoSpeed * Time.deltaTime;

            Apply();
        }

        private void Apply()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus + rot * (Vector3.back * distance);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
