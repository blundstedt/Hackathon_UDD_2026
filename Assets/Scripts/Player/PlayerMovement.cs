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
        private Vector3 knockback;

        internal void Push(Vector3 direction, float strength)
        {
            knockback += Vector3.ProjectOnPlane(direction, Vector3.up).normalized * strength;
            verticalSpeed = Mathf.Max(verticalSpeed, strength * .5f);
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
            knockback = Vector3.zero;
        }

        private void Update()
        {
            if (!controller.enabled) return;

            bool grounded = controller.isGrounded;
            if (grounded && verticalSpeed < 0f) verticalSpeed = -2f;
            if (grounded && input.JumpPressed)
                verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);

            Vector2 direction = Vector2.ClampMagnitude(input.Move, 1f);
            // Look turns the root in yaw only, so movement stays horizontal even looking up/down.
            Vector3 velocity = (transform.right * direction.x + transform.forward * direction.y)
                * (input.SprintHeld ? sprintSpeed : walkSpeed);
            verticalSpeed -= gravity * Time.deltaTime;
            velocity += knockback;
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, 16f * Time.deltaTime);
            velocity.y = verticalSpeed;
            CollisionFlags flags = controller.Move(velocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f) verticalSpeed = -2f;
        }
    }
}
