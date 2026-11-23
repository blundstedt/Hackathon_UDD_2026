using System;
using System.Collections;
using Hackaton.Combat;
using Hackaton.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(CharacterController))]
    public class EnemyBase : HitReceiver
    {
        public enum EnemyState { Idle, Chase, Attack, Search, Dead }

        [Header("Health and movement")]
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(.01f)] private float impactGravity = 20f;

        [SerializeField, Min(1)] private float patrolRadius = 9f;

        [Header("Perception")]
        [SerializeField, Min(0)] private float proximityRange = 2.5f;
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

        [Header("Rewards per player")]
        [SerializeField, Min(0)] private int moneyReward = 10;
        [SerializeField, Min(0)] private int experienceReward = 10;
        [SerializeField, Min(0)] private int killerBonusMoney;
        [SerializeField, Min(0)] private int killerBonusExperience;

        [Header("Presentation")]
        [SerializeField] private EnemyVisual visual;
        [SerializeField] private EnemyHealthBar healthBar;

        public EnemyState State { get; protected set; }
        public int Health { get; private set; }
        internal void DisplayReplicaHealth(int value)
        {
            int damage = Health - value;
            Health = value;
            if (damage > 0 && healthBar)
                healthBar.ShowHit(transform.position + Vector3.up * eyeHeight, damage, (float)value / maxHealth);
            if (value == 0 && State != EnemyState.Dead)
            {
                State = EnemyState.Dead;
                body.enabled = false;
                if (visual) visual.LieDown();
            }
        }

        public int MaximumHealth => maxHealth;

        protected void SetMaximumHealth(int amount) => Health = maxHealth = Mathf.Max(1, amount);
        protected int AttackDamage => damage;

        protected NavMeshAgent agent;
        private CharacterController body;
        private CharacterImpactMotion impact;
        private LargeKnockback launch;
        private bool navigatingAfterLaunch;
        private bool previousUpdateRotation;
        private float verticalSpeed;
        private Collider[] candidates = new Collider[32];
        private Vector3 lastSeen;
        private float nextSense;
        private float nextAttack;
        private float nextPatrolAttempt;
        private NavMeshPath patrolPath;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<CharacterController>();
            launch = LargeKnockback.For(body);
            patrolPath = new NavMeshPath();
            agent.speed = moveSpeed;
            agent.stoppingDistance = .05f;
            Health = maxHealth;
        }

        protected virtual void Update()
        {
            if (State == EnemyState.Dead)
            {
                if (body.enabled)
                {
                    impact.Move(body, Vector3.zero, ref verticalSpeed, impactGravity, Time.deltaTime);
                    if (body.isGrounded) body.enabled = false;
                }
                return;
            }
            if (launch.Active) return;
            if (navigatingAfterLaunch)
            {
                navigatingAfterLaunch = false;
                agent.updateRotation = previousUpdateRotation;
                ResumeNavigation();
            }
            if (impact.Active)
            {
                impact.Move(body, Vector3.zero, ref verticalSpeed, impactGravity, Time.deltaTime);
                if (impact.Active) return;
                ResumeNavigation();
            }
            if (Time.time < nextSense) return;
            nextSense = Time.time + senseInterval;

            Think();
        }

        protected virtual void Think()
        {
            Transform target = FindVisibleTarget();
            if (target != null)
            {
                lastSeen = target.position;
                if (Vector3.Distance(transform.position, lastSeen) <= attackRange)
                {
                    State = EnemyState.Attack;
                    StopMoving();
                    Vector3 facing = Vector3.ProjectOnPlane(lastSeen - transform.position, Vector3.up);
                    if (facing.sqrMagnitude > .001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                            Quaternion.LookRotation(facing), agent.angularSpeed * senseInterval);
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

            bool lostTarget = State == EnemyState.Chase || State == EnemyState.Attack;
            State = EnemyState.Search;
            if (lostTarget && MoveTo(lastSeen)) return;
            if (Time.time >= nextPatrolAttempt && agent.isOnNavMesh && SearchFinished())
                PickPatrolDestination();
        }

        private void PickPatrolDestination()
        {
            nextPatrolAttempt = Time.time + .5f;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * patrolRadius;
                Vector3 sample = transform.position + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(sample, out var hit, agent.height, filter)
                    || (hit.position - transform.position).sqrMagnitude < 1f
                    || !agent.CalculatePath(hit.position, patrolPath)
                    || patrolPath.status != NavMeshPathStatus.PathComplete) continue;

                agent.isStopped = false;
                agent.SetPath(patrolPath);
                return;
            }
        }

        protected virtual Transform FindVisibleTarget()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            float range = Mathf.Max(detectionRange, proximityRange);
            int count;
            while ((count = Physics.OverlapSphereNonAlloc(eye, range, candidates,
                       targetLayers, QueryTriggerInteraction.Ignore)) == candidates.Length)
                Array.Resize(ref candidates, candidates.Length * 2);

            Transform nearest = null;
            float best = range * range;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = candidates[i];
                var player = candidate.GetComponentInParent<IPlayerTarget>();
                if (!(player is Component owner) || !player.IsTargetable) continue;

                Vector3 offset = candidate.bounds.center - eye;
                float distance = offset.sqrMagnitude;
                if (distance > best) continue;
                Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
                if (distance > proximityRange * proximityRange && (distance > detectionRange * detectionRange
                    || Vector3.Angle(transform.forward, horizontal) > viewAngle * .5f)) continue;
                if (Physics.Linecast(eye, candidate.bounds.center, out var hit, sightLayers,
                        QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(owner.transform)) continue;

                best = distance;
                nearest = owner.transform;
            }
            return nearest;
        }

        protected bool MoveTo(Vector3 position)
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

        protected void StopMoving()
        {
            if (!agent.isOnNavMesh) return;
            agent.isStopped = true;
            if (agent.hasPath || agent.pathPending) agent.ResetPath();
        }

        // Variants implement only the effect of an attack that is already in range and visible.
        protected virtual void PerformAttack(Transform target) { }

        public override void ApplyKnockback(Knockback hit, Vector3 direction)
        {
            if (State == EnemyState.Dead || (!body.enabled && !launch.Active)) return;
            if (hit.horizontal <= 0f && hit.vertical <= 0f) return;
            if (hit.large && launch.isActiveAndEnabled)
            {
                if (!navigatingAfterLaunch)
                {
                    StopMoving();
                    previousUpdateRotation = agent.updateRotation;
                    agent.updatePosition = agent.updateRotation = false;
                    navigatingAfterLaunch = true;
                }
                impact = default;
                verticalSpeed = 0f;
                launch.Launch(hit, direction);
                return;
            }
            if (launch.Active) return;
            if (!impact.Active)
            {
                StopMoving();
                // Keep the simulated agent at its last safe position while the capsule handles collisions.
                agent.updatePosition = false;
                verticalSpeed = 0f;
            }
            impact.Begin(hit, direction, ref verticalSpeed);
        }

        private void ResumeNavigation()
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(transform.position, out var ground, agent.height, filter))
                agent.Warp(ground.position);
            else if (agent.isOnNavMesh)
            {
                // A launch may clear the walkable boundary. Rejoin it at the closest reachable edge.
                Vector3 safe = agent.nextPosition;
                if (NavMesh.Raycast(safe, transform.position, out var edge, filter)) safe = edge.position;
                agent.Warp(safe);
            }
            agent.updatePosition = true;
            nextSense = 0f;
        }

        public void TakeDamage(int amount) => ApplyDamage(amount, transform.position + Vector3.up * eyeHeight, null);

        protected override void ApplyDamage(int amount, Vector3 point, Transform source)
        {
            if (amount <= 0 || State == EnemyState.Dead) return;
            int received = Mathf.Min(amount, Health);
            Health -= received;
            if (healthBar != null) healthBar.ShowHit(point, received, (float)Health / maxHealth);
            if (Health == 0) Die(source);
        }

        private void Die(Transform source)
        {
            State = EnemyState.Dead;
            launch.KeepFallen();
            StopMoving();
            agent.enabled = false;
            // An airborne corpse can finish falling while the usual death and despawn run.
            body.enabled = impact.Active && !body.isGrounded;
            verticalSpeed = Mathf.Min(0f, verticalSpeed);
            StopAllCoroutines();
            var killer = source != null ? source.GetComponentInParent<PlayerStats>() : null;
            foreach (var player in FindObjectsByType<PlayerStats>())
            {
                if (!player.CanReceiveRewards) continue;
                if (moneyReward > 0 || experienceReward > 0) player.RewardDefeat(moneyReward, experienceReward);
                if (player == killer && (killerBonusMoney > 0 || killerBonusExperience > 0))
                {
                    player.RewardDefeat(killerBonusMoney, killerBonusExperience);
                    if (healthBar != null) healthBar.ShowReward(transform.position + Vector3.up * 2.8f,
                        killerBonusMoney, killerBonusExperience, "Bono: ");
                }
            }
            if (healthBar != null && (moneyReward > 0 || experienceReward > 0)) healthBar.ShowReward(transform.position + Vector3.up * 1.8f,
                moneyReward, experienceReward, "");
            OnDeath();
            StartCoroutine(RemoveCorpse());
        }

        protected virtual void OnDeath()
        {
            if (visual != null && !launch.Active) visual.LieDown();
        }

        private IEnumerator RemoveCorpse()
        {
            yield return new WaitForSeconds(2f);
            if (visual != null) yield return visual.FadeOut();
            if (TryGetComponent<Unity.Netcode.NetworkObject>(out var replica) && replica.IsSpawned) replica.Despawn();
            else Destroy(gameObject);
        }
    }
}
