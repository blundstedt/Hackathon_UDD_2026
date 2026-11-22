using System;
using UnityEngine;

namespace Hackaton.Combat
{
    [Serializable]
    public struct Knockback
    {
        [Min(0), Tooltip("Initial horizontal speed in metres per second.")]
        public float horizontal;
        [Min(0), Tooltip("Upward launch speed in metres per second.")]
        public float vertical;
        [Min(.01f), Tooltip("Time for horizontal speed to fall to zero. Control stays locked until landing.")]
        public float duration;
        [Min(0), Tooltip("Extra time grounded before movement resumes.")]
        public float recovery;

        public Knockback(float horizontal, float vertical, float duration, float recovery)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.duration = duration;
            this.recovery = recovery;
        }

        public static Knockback FromPush(float push) =>
            new Knockback(push, push * .5f, push / 16f, 0f);
    }

    // One state per character. Its existing movement owner calls Move exactly once per frame.
    public struct CharacterImpactMotion
    {
        private Vector3 horizontalVelocity;
        private float duration;
        private float remaining;
        private float recovery;
        public bool Active { get; private set; }

        public void Begin(Knockback hit, Vector3 direction, ref float verticalSpeed)
        {
            if (hit.horizontal <= 0f && hit.vertical <= 0f) return;
            horizontalVelocity = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * hit.horizontal;
            remaining = duration = Mathf.Max(.01f, hit.duration);
            recovery = hit.recovery;
            if (hit.vertical > 0f) verticalSpeed = hit.vertical;
            Active = true;
        }

        public void Move(CharacterController controller, Vector3 locomotion, ref float verticalSpeed,
            float gravity, float dt)
        {
            if (dt <= 0f) return;
            Vector3 displacement = locomotion * dt;
            if (Active)
            {
                float step = Mathf.Min(remaining, dt);
                displacement = horizontalVelocity * (step * (remaining - .5f * step) / duration);
                remaining -= step;
            }

            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            displacement.y = verticalSpeed * dt - .5f * gravity * dt * dt;
            verticalSpeed -= gravity * dt;
            CollisionFlags flags = controller.Move(displacement);
            if ((flags & CollisionFlags.Above) != 0 && displacement.y > 0f) verticalSpeed = 0f;
            if ((flags & CollisionFlags.Below) == 0 || displacement.y > 0f) return;
            verticalSpeed = -2f;
            if (Active && remaining <= 0f)
            {
                recovery -= dt;
                Active = recovery > 0f;
            }
        }
    }
}
