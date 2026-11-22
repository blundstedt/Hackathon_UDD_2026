using UnityEngine;

namespace Hackaton.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 85f;
        private PlayerInputReader input;
        private CharacterController controller;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
        }

        private void Update()
        {
            if (controller != null && !controller.enabled) return;
            Vector2 delta = input.Look * mouseSensitivity;
            // Mouse delta is already displacement per frame; do not multiply by deltaTime.
            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
