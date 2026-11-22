using Hackaton.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace Hackaton.Player
{
    public sealed class CombatProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 28f;
        [SerializeField] private float gravity = 3f;
        [SerializeField] private float hitRadius = .09f;
        [SerializeField] private float blastRadius;
        [SerializeField] private GameObject splash;
        private Transform owner;
        private Vector3 velocity;
        private int damage;

        internal void Launch(Transform source, Vector3 direction, int amount)
        {
            owner = source;
            velocity = direction * speed;
            damage = amount;
            Destroy(gameObject, 5f);
        }

        private void FixedUpdate()
        {
            Vector3 start = transform.position;
            float dt = Time.fixedDeltaTime;
            Vector3 acceleration = Vector3.down * gravity;
            Vector3 travel = velocity * dt + acceleration * (.5f * dt * dt);
            velocity += acceleration * dt;
            // Sphere casts omit colliders already touching the sphere at launch.
            foreach (var collider in Physics.OverlapSphere(start, hitRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsOwner(collider)) continue;
                Impact(collider, collider.ClosestPoint(start));
                return;
            }
            RaycastHit closest = default;
            foreach (var hit in Physics.SphereCastAll(start, hitRadius, travel.normalized, travel.magnitude,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!IsOwner(hit.collider) && (closest.collider == null || hit.distance < closest.distance))
                    closest = hit;
            }
            if (closest.collider != null) Impact(closest.collider, closest.point);
            else transform.position = start + travel;
        }

        private bool IsOwner(Collider collider) => owner != null && collider.transform.IsChildOf(owner);

        private void Impact(Collider collider, Vector3 point)
        {
            enabled = false;
            if (blastRadius > 0)
            {
                var effect = Instantiate(splash, point, Quaternion.identity);
                effect.transform.localScale = Vector3.one * (2 * blastRadius);
                Destroy(effect, .18f);
                var struck = new HashSet<HitReceiver>();
                foreach (var nearby in Physics.OverlapSphere(point, blastRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    var stats = nearby.GetComponentInParent<HitReceiver>();
                    if (stats == null || IsOwner(nearby) || !struck.Add(stats)) continue;
                    stats.ReceiveHit(damage, nearby.ClosestPoint(point), nearby.bounds.center - point, 4f);
                }
            }
            else
            {
                var stats = collider.GetComponentInParent<HitReceiver>();
                if (stats != null) stats.ReceiveHit(damage, point, velocity.normalized, 2f);
            }
            Destroy(gameObject);
        }
    }
}
