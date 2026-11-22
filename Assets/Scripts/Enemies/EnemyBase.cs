using System;
using System.Collections;
using Hackaton.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public class EnemyBase : HitReceiver
    {
        public enum EnemyState { Idle, Chase, Attack, Search, Dead }

        [Header("Health and movement")]
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(.1f)] private float moveSpeed = 3.5f;

        [Header("Perception")]
        [SerializeField, Min(.1f)] private float detectionRange = 14f;
        [SerializeField, Range(0, 360)] private float viewAngle = 110f;
        [SerializeField, Min(0)] private float eyeHeight = 1.5f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private LayerMask sightLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(.02f)] private float senseInterval = .15f;

        [Header("Attack")]
        [SerializeField, Min(0)] private int damage = 10;
        [SerializeField, Min(.1f)] private float attackRange = 1.6f;
        [SerializeField, Min(.05f)] private float attackInterval = 1f;

        [Header("Presentation")]
        [SerializeField] private EnemyVisual visual;
        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyState State { get; private set; }
        public int Health { get; private set; }
        protected int AttackDamage => damage;

        private NavMeshAgent agent;
        private CapsuleCollider body;
        private Collider[] candidates = new Collider[32];
        private Vector3 lastSeen;
        private float nextSense;
        private float nextAttack;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<CapsuleCollider>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = .05f;
            Health = maxHealth;
        }

        protected virtual void Update()
        {
            if (State == EnemyState.Dead || Time.time < nextSense) return;
            nextSense = Time.time + senseInterval;

            Transform target = FindVisibleTarget();
            if (target != null)
            {
                lastSeen = target.position;
                if (Vector3.Distance(transform.position, lastSeen) <= attackRange)
                {
                    State = EnemyState.Attack;
                    StopMoving();
                    if (Time.time >= nextAttack)
                    {
                        nextAttack = Time.time + attackInterval;
                        PerformAttack(target);
                    }
                }
                else
                {
                    State = EnemyState.Chase;
                    MoveTo(lastSeen);
                }
                return;
            }

            if (State == EnemyState.Chase || State == EnemyState.Attack)
                State = MoveTo(lastSeen) ? EnemyState.Search : EnemyState.Idle;
            else if (State == EnemyState.Search && SearchFinished())
            {
                StopMoving();
                State = EnemyState.Idle;
            }
        }

        protected virtual Transform FindVisibleTarget()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            int count;
            while ((count = Physics.OverlapSphereNonAlloc(eye, detectionRange, candidates,
                       targetLayers, QueryTriggerInteraction.Ignore)) == candidates.Length)
                Array.Resize(ref candidates, candidates.Length * 2);

            Transform nearest = null;
            float best = detectionRange * detectionRange;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = candidates[i];
                var player = candidate.GetComponentInParent<IPlayerTarget>();
                if (!(player is Component owner) || !player.IsTargetable) continue;

                Vector3 offset = candidate.bounds.center - eye;
                float distance = offset.sqrMagnitude;
                if (distance > best) continue;
                Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
                if (Vector3.Angle(transform.forward, horizontal) > viewAngle * .5f) continue;
                if (Physics.Linecast(eye, candidate.bounds.center, out var hit, sightLayers,
                        QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(owner.transform)) continue;

                best = distance;
                nearest = owner.transform;
            }
            return nearest;
        }

        private bool MoveTo(Vector3 position)
        {
            if (!agent.isOnNavMesh) return false;
            agent.isStopped = false;
            return agent.SetDestination(position);
        }

        private bool SearchFinished()
        {
            if (!agent.isOnNavMesh) return true;
            if (agent.pathPending) return false;
            return !agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid
                || agent.remainingDistance <= agent.stoppingDistance + .1f;
        }

        private void StopMoving()
        {
            if (!agent.isOnNavMesh) return;
            agent.isStopped = true;
            if (agent.hasPath || agent.pathPending) agent.ResetPath();
        }

        // Variants implement only the effect of an attack that is already in range and visible.
        protected virtual void PerformAttack(Transform target) { }

        public override void ReceiveHit(int amount, Vector3 point, Vector3 direction, float push)
            => ApplyDamage(amount, point);

        public void TakeDamage(int amount) => ApplyDamage(amount, transform.position + Vector3.up * eyeHeight);

        private void ApplyDamage(int amount, Vector3 point)
        {
            if (amount <= 0 || State == EnemyState.Dead) return;
            int received = Mathf.Min(amount, Health);
            Health -= received;
            if (healthBar != null) healthBar.ShowHit(point, received, (float)Health / maxHealth);
            if (Health == 0) Die();
        }

        private void Die()
        {
            State = EnemyState.Dead;
            StopMoving();
            agent.enabled = false;
            body.enabled = false;
            StopAllCoroutines();
            OnDeath();
            StartCoroutine(RemoveCorpse());
        }

        protected virtual void OnDeath()
        {
            if (visual != null) visual.LieDown();
        }

        private IEnumerator RemoveCorpse()
        {
            yield return new WaitForSeconds(2f);
            if (visual != null) yield return visual.FadeOut();
            Destroy(gameObject);
        }
    }
}
