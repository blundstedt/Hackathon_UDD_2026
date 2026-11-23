using System;
using System.Collections;
using Hackaton.Combat;
using Hackaton.Enemies;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    public enum ForestRank { Goblin, Guardian, Colossus }

    public sealed class ForestEnemy : EnemyBase
    {
        [SerializeField] private ForestRank rank;
        [SerializeField] private ForestRig rig;
        [SerializeField, Min(.1f)] private float impactRadius = 1.9f;
        [SerializeField, Min(.05f)] private float windup = .23f;
        [SerializeField] private Material runeMaterial;
        private LargeKnockback knockbackBody;
        private Coroutine swing;
        private LineRenderer warning;
        private float awakeAt;
        public ForestRank Rank => rank;
        public int FullHealth { get; private set; }
        public event Action<ForestEnemy> Defeated;

        protected override void Awake()
        {
            base.Awake();
            FullHealth = Health;
            knockbackBody = GetComponent<LargeKnockback>();
            awakeAt = Time.time + (rank == ForestRank.Colossus ? 1.35f : .35f);
        }

        protected override void Update()
        {
            if (Time.time >= awakeAt) base.Update();
        }

        protected override void PerformAttack(Transform target)
        {
            if (swing == null) swing = StartCoroutine(Strike(target));
        }

        private IEnumerator Strike(Transform target)
        {
            rig.Swing(windup);
            Vector3 origin = transform.position;
            bool slam = rank == ForestRank.Colossus;
            if (slam)
                warning = ForestRune.Create(transform.parent, origin, impactRadius, runeMaterial, new Color(1f,.5f,.24f));
            yield return new WaitForSeconds(windup);
            if (slam)
            {
                Destroy(warning.gameObject);
                warning = null;
                var session = FloorSession.Instance;
                if (session)
                    foreach (var player in session.Players) Hit(player.Stats, origin);
            }
            else if (target) Hit(target.GetComponent<PlayerStats>(), transform.position);
            swing = null;
        }

        private void Hit(PlayerStats player, Vector3 origin)
        {
            if (!player || !player.IsTargetable) return;
            var delta = player.transform.position - origin;
            if (delta.sqrMagnitude > impactRadius * impactRadius) return;
            Vector3 from = origin + Vector3.up * GetComponent<CharacterController>().height * .6f;
            Vector3 to = player.transform.position + Vector3.up;
            if (Physics.Linecast(from, to, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(player.transform) && !hit.transform.IsChildOf(transform)) return;
            player.ReceiveHit(AttackDamage, to, delta.normalized, rank == ForestRank.Goblin ? 0f : 1.6f, transform);
        }

        private void InterruptSwing()
        {
            if (swing != null) StopCoroutine(swing);
            swing = null;
            if (warning) Destroy(warning.gameObject);
            warning = null;
        }

        public override void ApplyKnockback(Knockback hit, Vector3 direction)
        {
            if (State != EnemyState.Dead && (hit.horizontal > 0 || hit.vertical > 0))
            {
                InterruptSwing();
                rig.Stagger(Mathf.Max(.25f,hit.recovery));
            }
            base.ApplyKnockback(hit, direction);
        }

        protected override void ApplyDamage(int amount, Vector3 point, Transform source)
        {
            if (amount > 0 && State != EnemyState.Dead) rig.Flinch(point);
            base.ApplyDamage(amount, point, source);
        }

        protected override void OnDeath()
        {
            InterruptSwing();
            rig.Fall();
            Defeated?.Invoke(this);
        }

        public void RecoverTo(Vector3 safe)
        {
            // Only needed when a physics launch carries an enemy beyond the meadow.
            knockbackBody.enabled = false;
            GetComponent<CharacterController>().enabled = true;
            transform.SetPositionAndRotation(safe, Quaternion.identity);
            GetComponent<NavMeshAgent>().Warp(safe);
            knockbackBody.enabled = true;
        }
    }
}

