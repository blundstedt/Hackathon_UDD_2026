using Hackaton.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace Hackaton.Player
{
    public abstract class PlayerAttack : MonoBehaviour
    {
        protected abstract float Cooldown { get; }
        private PlayerInputReader input;
        private PlayerView view;
        private float readyAt;
        public float RemainingFraction => Mathf.Clamp01((readyAt - Time.time) / Cooldown);

        protected void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            view = GetComponent<PlayerView>();
        }

        protected void Update()
        {
            if (input.AttackPressed) TryAttack();
        }

        public bool TryAttack()
        {
            if (!isActiveAndEnabled || Time.time < readyAt) return false;
            readyAt = Time.time + Cooldown;
            var pivot = view.AimPivot;
            Vector3 direction = pivot.forward;
            if (view.Mode == PlayerView.CameraMode.Behind)
            {
                Ray ray = view.ViewCamera.ViewportPointToRay(new Vector3(.5f, .5f));
                float distance = 80f;
                foreach (var hit in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(transform)) distance = Mathf.Min(distance, hit.distance);
                Vector3 aim = ray.GetPoint(distance) - pivot.position;
                if (Vector3.Dot(aim, pivot.forward) > 0) direction = aim.normalized;
            }
            Attack(pivot.position, direction);
            return true;
        }

        protected abstract void Attack(Vector3 origin, Vector3 direction);

        protected void Strike(Vector3 origin, Vector3 direction, float reach, float width, int damage, float push)
        {
            var rotation = Quaternion.LookRotation(direction, transform.up);
            var center = origin + direction * (reach * .5f);
            var halfSize = new Vector3(width, .48f, reach * .5f);
            var targets = Physics.OverlapBox(center, halfSize, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var struck = new HashSet<HitReceiver>();
            foreach (var collider in targets)
            {
                var stats = collider.GetComponentInParent<HitReceiver>();
                if (stats == null || stats.transform == transform || struck.Contains(stats)) continue;
                Vector3 point = collider.ClosestPoint(origin);
                if (Physics.Linecast(origin, point, out var obstacle, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && !obstacle.transform.IsChildOf(stats.transform) && !obstacle.transform.IsChildOf(transform)) continue;
                struck.Add(stats);
                stats.ReceiveHit(damage, point, direction, push);
            }
        }
    }
}
