using Hackaton.Combat;
using UnityEngine;

namespace Hackaton.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7.5f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField, Min(0.01f)] private float gravity = 20f;
        private CharacterController controller;
        private PlayerInputReader input;
        private float verticalSpeed;
        private CharacterImpactMotion impact;

        internal void Push(Knockback hit, Vector3 direction)
        {
            impact.Begin(hit, direction, ref verticalSpeed);
        }

        internal float WalkSpeed => walkSpeed;
        internal float SprintSpeed => sprintSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
        }

        private void OnDisable()
        {
            verticalSpeed = 0f;
            impact = default;
        }

        private void Update()
        {
            if (!controller.enabled) return;

            if (!impact.Active && controller.isGrounded && input.JumpPressed)
                verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);

            Vector2 direction = Vector2.ClampMagnitude(input.Move, 1f);
            // Look turns the root in yaw only, so movement stays horizontal even looking up/down.
            Vector3 velocity = (transform.right * direction.x + transform.forward * direction.y)
                * (input.SprintHeld ? sprintSpeed : walkSpeed);
            impact.Move(controller, velocity, ref verticalSpeed, gravity, Time.deltaTime);
        }
    }
}
