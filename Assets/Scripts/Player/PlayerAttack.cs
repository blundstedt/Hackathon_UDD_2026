using Hackaton.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace Hackaton.Player
{
    // Apply the attack on top of PlayerView's locomotion pose for this frame.
    [DefaultExecutionOrder(100)]
    public abstract class PlayerAttack : MonoBehaviour
    {
        protected abstract float Cooldown { get; }
        private PlayerInputReader input;
        private PlayerInventory inventory;
        private bool HoldingWeapon => !inventory || inventory.Selected == 1;
        private CharacterController controller;
        protected abstract float ImpactTime { get; }
        protected PlayerView View { get; private set; }
        private float startedAt;
        private bool animating;
        private bool impactPending;
        private float readyAt;
        public float RemainingFraction => Mathf.Clamp01((readyAt - Time.time) / Cooldown);

        protected void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            View = GetComponent<PlayerView>();
        }

        protected void Update()
        {
            if (HoldingWeapon && input.AttackPressed) TryAttack();
        }

        public bool TryAttack()
        {
            if (!HoldingWeapon) return false;
            var peer = Hackaton.Multiplayer.NetworkTiza.Local;
            if (!peer || peer.View != View) return StartSwing();
            if (!isActiveAndEnabled || !controller.enabled || impactPending || Time.time < readyAt) return false;
            peer.AttackRpc(Mathf.DeltaAngle(0f, View.AimPivot.localEulerAngles.x));
            return true;
        }

        internal bool StartSwing()
        {
            if (!HoldingWeapon || !isActiveAndEnabled || (controller != null && !controller.enabled) || impactPending || Time.time < readyAt) return false;
            startedAt = Time.time;
            OnAttackStarted();
            readyAt = startedAt + Cooldown;
            animating = impactPending = true;
            return true;
        }

        protected virtual void OnDisable()
        {
            animating = impactPending = false;
        }

        protected void LateUpdate()
        {
            if (!animating) return;
            if (!HoldingWeapon || controller != null && !controller.enabled)
            {
                animating = impactPending = false;
                return;
            }
            float elapsed = Time.time - startedAt;
            bool impact = impactPending && elapsed >= ImpactTime;
            Animate(impact ? ImpactTime : elapsed);
            if (impact)
            {
                impactPending = false;
                ResolveImpact();
            }
            else if (elapsed >= Cooldown)
                animating = false;
        }

        protected virtual void OnAttackStarted() { }
        protected abstract void Animate(float elapsed);

        protected static float Blend(float time, float from, float to) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, time));

        protected void Swing(float time, float windupTime, float restTime,
            Vector3 windup, Vector3 contact, Vector3 windupAngles, Vector3 contactAngles)
        {
            float swing = Blend(time, windupTime, ImpactTime);
            float recovery = 1f - Blend(time, ImpactTime, restTime);
            float anticipation = Blend(time, 0f, windupTime) * (1f - swing);
            MoveRightHand(windup * anticipation + contact * (swing * recovery),
                windupAngles * anticipation + contactAngles * (swing * recovery));
        }

        protected void MoveRightHand(Vector3 offset, Vector3 angles)
        {
            var hand = View.RightHand;
            var frame = View.HandFrame.rotation;
            hand.position += frame * offset;
            hand.rotation = frame * Quaternion.Euler(angles) * Quaternion.Inverse(frame) * hand.rotation;
        }

        protected virtual Vector3 AttackOrigin => View.AimPivot.position;

        private void ResolveImpact()
        {
            var pivot = View.AimPivot;
            Vector3 direction = pivot.forward;
            Vector3 origin = AttackOrigin;
            if (View.Mode == PlayerView.CameraMode.Behind || origin != pivot.position && View.Mode == PlayerView.CameraMode.FirstPerson)
            {
                Ray ray = View.ViewCamera.ViewportPointToRay(new Vector3(.5f, .5f));
                float distance = 80f;
                foreach (var hit in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(transform)) distance = Mathf.Min(distance, hit.distance);
                Vector3 aim = ray.GetPoint(distance) - origin;
                if (Vector3.Dot(aim, pivot.forward) > 0) direction = aim.normalized;
            }
            Attack(origin, direction);
        }

        protected abstract void Attack(Vector3 origin, Vector3 direction);

        protected void Strike(Vector3 origin, Vector3 direction, float reach, float width, int damage, Knockback impact)
        {
            var rotation = Quaternion.LookRotation(direction, transform.up);
            var center = origin + direction * (reach * .5f);
            var halfSize = new Vector3(width, .48f, reach * .5f);
            var targets = Physics.OverlapBox(center, halfSize, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var struck = new HashSet<HitReceiver>();
            foreach (var collider in targets)
            {
                var stats = collider.GetComponentInParent<HitReceiver>();
                if (stats == null || collider.transform.IsChildOf(transform) || struck.Contains(stats)) continue;
                Vector3 point = collider.ClosestPoint(origin);
                if (Physics.Linecast(origin, point, out var obstacle, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && !obstacle.transform.IsChildOf(stats.transform) && !obstacle.transform.IsChildOf(transform)) continue;
                struck.Add(stats);
                Vector3 away = Vector3.ProjectOnPlane((impact.large ? point : stats.transform.position) - transform.position, Vector3.up);
                stats.ReceiveHit(damage, point, away.sqrMagnitude > .0001f ? away : transform.forward, impact, transform);
            }
        }
    }
}
