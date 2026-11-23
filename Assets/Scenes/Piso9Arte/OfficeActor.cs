using Hackaton.Combat;
using Hackaton.Enemies;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    public sealed class OfficeActor : EnemyBase
    {
        public enum Kind { Employee, Drone, Corruptor }
        enum Action { None, Claw, Slam, Rush, Bolt, Summon, Break }
        [SerializeField] Kind kind;
        [SerializeField] int clawDamage = 17, slamDamage = 22, rushDamage = 20, boltDamage = 11;
        OfficeDirector floor;
        OfficeRig rig;
        OfficePose pose;
        void Pose(OfficePose next)
        {
            pose = next;
            State = next switch
            {
                OfficePose.Patrol => EnemyState.Idle,
                OfficePose.Search => EnemyState.Search,
                OfficePose.Strike or OfficePose.Slam or OfficePose.Summon => EnemyState.Attack,
                _ => EnemyState.Chase
            };
        }
        Action attack;
        FloorPlayer marked;
        Transform crate;
        Vector3 memory, aim, rushEnd;
        bool knowsPosition, boss, staging, committed, heard;
        float memoryUntil, readyAt, actionStart, impactAt, finishAt, hurtAt = -10, deathAt, nextStep, nextPatrol;
        int sequence;
        internal int OfficeId { get; set; }
        internal OfficePose ShownPose { get; private set; }
        internal float ShownSpeed { get; private set; }
        internal float ShownPhase { get; private set; }
        OfficeActorFrame replicaFrame;
        bool replica;
        public Kind Role => kind;
        public bool IsBoss => boss;
        public bool Alive => State != EnemyState.Dead;
        bool Hunter => kind == Kind.Corruptor && !boss;
        int Tier => Health > MaximumHealth * .7f ? 1 : Health > MaximumHealth * .35f ? 2 : 3;
        Vector3 Eyes => transform.position + Vector3.up * (kind == Kind.Corruptor ? 2.55f : 1.45f);

        protected override void Awake()
        {
            base.Awake();
            rig = new OfficeRig(transform, kind);
            agent.angularSpeed = 360;
            agent.acceleration = 20;
        }

        public void Bind(OfficeDirector owner) => floor = owner;

        public void InvestigateAlarm(Vector3 position)
        {
            memory = position;
            knowsPosition = heard = true;
            memoryUntil = Time.time + 9;
            MoveTo(position);
        }

        public void Stage(OfficePose special)
        {
            staging = true;
            attack = Action.None;
            StopMoving();
            agent.enabled = false;
            GetComponent<CharacterController>().enabled = false;
            SetPresentation(special, 0);
        }

        public void SetPresentation(OfficePose special, float t) => Animate(special, 0, t);

        void Animate(OfficePose shown, float speed, float progress)
        {
            ShownPose = shown;
            ShownSpeed = speed;
            ShownPhase = progress;
            rig.Animate(shown, speed, progress);
        }

        internal void ApplyFrame(OfficeActorFrame frame)
        {
            bool snap = !replica || !gameObject.activeSelf || (transform.position-frame.position).sqrMagnitude > 36;
            replica = true;
            replicaFrame = frame;
            gameObject.SetActive(frame.visible != 0);
            if (!gameObject.activeSelf) return;
            if (snap) transform.SetPositionAndRotation(frame.position, Quaternion.Euler(0,frame.yaw,0));
            if (!boss && frame.boss != 0) { boss = true; rig.Rebuild(); }
            if (MaximumHealth != frame.maximum) SetMaximumHealth(frame.maximum);
            if (Health != frame.health) DisplayReplicaHealth(frame.health);
        }

        public void Reconstruct(Vector3 point, int players)
        {
            transform.SetPositionAndRotation(point, Quaternion.LookRotation(Vector3.left));
            rig.Rebuild();
            SetMaximumHealth(MaximumHealth + 140 * Mathf.Max(0, players - 1));
            boss = true;
            knowsPosition = false;
            staging = false;
            agent.enabled = true;
            agent.Warp(point);
            GetComponent<CharacterController>().enabled = true;
            readyAt = Time.time + 1;
            Pose(OfficePose.Fury);
        }

        public override void ReceiveHit(int damage, Vector3 point, Vector3 direction, Knockback impact, Transform source = null)
        {
            if (Hunter || staging || !Alive || (source && source.GetComponent<OfficeActor>())) return;
            // The default lobby loadout has a zero-damage slap. A valid physical strike shorts office electronics.
            if (damage == 0 && impact.large && source && source.GetComponent<SlapAttack>()) damage = 7;
            base.ReceiveHit(damage, point, direction, impact, source);
        }

        protected override void ApplyDamage(int damage, Vector3 point, Transform source)
        {
            if (replica || Hunter || staging || !Alive) return;
            hurtAt = Time.time;
            base.ApplyDamage(damage, point, source);
        }

        public override void ApplyKnockback(Knockback impact, Vector3 direction)
        {
            if (replica || Hunter || staging || !Alive) return;
            if (boss)
            {
                impact.large = false;
                impact.vertical = 0;
                impact.horizontal *= .12f;
                impact.duration = .12f;
            }
            attack = Action.None;
            base.ApplyKnockback(impact, direction);
        }

        protected override void OnDeath()
        {
            deathAt = Time.time;
            attack = Action.None;
            rig.Animate(OfficePose.Shutdown, 0, 0);
            if (!floor) return;
            floor.Fx.Burst(Eyes, OfficeEffects.Magenta);
            floor.Fx.Sound(ShiftSound.Down, Eyes);
            floor.ActorDied(this);
        }

        protected override void Update()
        {
            if (replica)
            {
                float blend = 1f - Mathf.Exp(-20f * Time.deltaTime);
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position,replicaFrame.position,blend),
                    Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,replicaFrame.yaw,0),blend));
                Animate((OfficePose)replicaFrame.pose,replicaFrame.speed,replicaFrame.progress);
                return;
            }
            if (staging || !floor) return;
            base.Update();
            float t = Mathf.InverseLerp(actionStart, finishAt, Time.time);
            var shown = pose;
            if (!Alive) { shown = OfficePose.Shutdown; t = Mathf.Clamp01((Time.time - deathAt) / .9f); }
            else if (Time.time - hurtAt < .18f) { shown = OfficePose.Hurt; t = (Time.time - hurtAt) / .18f; }
            float speed = agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0;
            Animate(shown, speed, t);
            if (Alive && kind == Kind.Corruptor && speed > 1 && Time.time >= nextStep)
            {
                nextStep = Time.time + (speed > 4 ? .46f : .72f);
                floor.Fx.Sound(ShiftSound.Step, transform.position, .38f);
            }
        }

        bool Clear(FloorPlayer player)
        {
            if (!player || !player.Stats.IsTargetable) return false;
            return !Physics.Linecast(Eyes, player.BodyCenter, out var hit, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(player.transform);
        }

        FloorPlayer Sense(out float distance)
        {
            FloorPlayer nearest = null;
            distance = Hunter ? 30 : 25;
            foreach (var player in FloorSession.Instance.Players)
            {
                var delta = player.transform.position - transform.position;
                float d = delta.magnitude;
                if (d >= distance || (Hunter && d > 3.5f && Vector3.Angle(transform.forward,delta) > 66) || !Clear(player)) continue;
                distance = d;
                nearest = player;
            }
            return nearest;
        }

        protected override void Think()
        {
            if (!floor || staging || !agent.isOnNavMesh) return;
            if (attack != Action.None) { AdvanceAttack(); return; }
            if (Hunter && Time.time >= readyAt && (crate = floor.BlockingArchive(transform)))
            {
                Face(crate.position);
                StartAction(Action.Break);
                return;
            }
            var seen = Sense(out float distance);
            if (seen)
            {
                bool first = !knowsPosition || heard;
                heard = false;
                knowsPosition = true;
                memory = seen.transform.position;
                memoryUntil = Time.time + 4;
                if (first && Hunter)
                {
                    readyAt = Time.time + .58f;
                    floor.Fx.Sound(ShiftSound.Alert, Eyes);
                }
                if (first && Hunter || Hunter && pose == OfficePose.Detect && Time.time < readyAt)
                {
                    Face(memory);
                    Pose(OfficePose.Detect);
                    return;
                }
                Pose(boss && Tier == 3 ? OfficePose.Fury : OfficePose.Chase);
                agent.speed = Hunter ? 5.85f : boss ? 3.8f + Tier * .55f : kind == Kind.Drone ? 4f : 3.65f;
                float reach = Hunter || boss ? 2.65f : 1.7f;
                if (Time.time >= readyAt)
                {
                    Action choice = SelectAttack(distance, reach);
                    if (choice != Action.None) { Begin(choice, seen); return; }
                }
                if (kind == Kind.Drone && distance < 5)
                {
                    var retreat = transform.position + (transform.position-memory).normalized * 3;
                    if (!agent.Raycast(retreat, out _)) MoveTo(retreat);
                    else StopMoving();
                }
                else if (distance > (kind == Kind.Drone ? 9 : reach * .79f)) MoveTo(memory);
                else { StopMoving(); Face(memory); }
                return;
            }
            if (Hunter && floor.Hear(transform.position, out var noise))
            {
                memory = noise; knowsPosition = true; heard = true; memoryUntil = Time.time + 3;
            }
            if (knowsPosition && Time.time < memoryUntil)
            {
                Pose(OfficePose.Search);
                agent.speed = Hunter ? 4.4f : 3.5f;
                MoveTo(memory);
                return;
            }
            knowsPosition = false;
            // During the alarm, attackers converge on the terminal even when the team breaks sight.
            if (floor.Defending && floor.NearestPlayer(transform.position) is FloorPlayer defender)
            {
                MoveTo(defender.transform.position);
                Pose(OfficePose.Chase);
                return;
            }
            if (agent.pathPending || (agent.hasPath && agent.remainingDistance > 1) || Time.time < nextPatrol) return;
            nextPatrol = Time.time + 2;
            agent.speed = Hunter ? 3.5f : 2.25f;
            Pose(OfficePose.Patrol);
            MoveTo(floor.PatrolPoint(transform.position, Hunter));
        }

        Action SelectAttack(float range, float reach)
        {
            if (Hunter) return range < reach ? Action.Claw : Action.None;
            if (kind == Kind.Employee) return range < reach ? Action.Claw : Action.None;
            if (kind == Kind.Drone) return range > 14 ? Action.None :
                sequence % 5 == 3 && floor.CanSummon ? Action.Summon : Action.Bolt;
            if (Tier >= 2 && sequence % 5 == 3 && floor.CanSummon) return Action.Summon;
            if (range < reach && sequence % 3 != 1) return Action.Claw;
            if (range < 5.5f) return Action.Slam;
            if (Tier >= 2 && range < 13 && sequence % 3 == 1) return Action.Rush;
            return range < 18 ? Action.Bolt : Action.None;
        }

        void Face(Vector3 point)
        {
            var dir = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (dir.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        void Begin(Action choice, FloorPlayer target)
        {
            marked = target;
            Face(target.transform.position);
            aim = target.BodyCenter;
            StartAction(choice);
        }

        void StartAction(Action choice)
        {
            StopMoving();
            attack = choice;
            committed = false;
            sequence++;
            actionStart = Time.time;
            float windup = choice == Action.Slam || choice == Action.Summon ? .95f : Hunter ? .62f : .55f;
            impactAt = actionStart + windup;
            finishAt = impactAt + (choice == Action.Rush ? .85f : .35f);
            Pose(choice == Action.Summon ? OfficePose.Summon : choice == Action.Slam ? OfficePose.Slam : OfficePose.Strike);
            if (choice == Action.Slam || choice == Action.Summon) floor.Fx.Ring(transform.position, choice == Action.Slam ? 5.5f : 2, windup);
            floor.Fx.Sound(ShiftSound.Pulse, Eyes, Hunter || boss ? .72f : .22f);
        }

        bool Connects(FloorPlayer player, float range, float cone)
        {
            if (!Clear(player)) return false;
            Vector3 delta = player.BodyCenter - transform.position;
            if (delta.y < -.4f || delta.y > (kind == Kind.Corruptor ? 3.6f : 2.35f)) return false;
            delta.y = 0;
            return delta.magnitude <= range && Vector3.Angle(transform.forward,delta) <= cone;
        }

        void AdvanceAttack()
        {
            if (Time.time < impactAt) return;
            if (!committed)
            {
                committed = true;
                switch (attack)
                {
                    case Action.Claw:
                        if (Connects(marked, Hunter || boss ? 2.95f : 1.85f, 52))
                        {
                            if (Hunter) marked.Stats.Eliminate();
                            else Hit(marked, boss ? clawDamage : 7, boss ? 5 : 1);
                            floor.Fx.Burst(marked.BodyCenter, OfficeEffects.Magenta, 4);
                        }
                        break;
                    case Action.Break:
                        floor.BreakArchive(crate);
                        crate = null;
                        break;
                    case Action.Slam:
                        foreach (var player in FloorSession.Instance.Players)
                            if (Connects(player,5.5f,180)) Hit(player,slamDamage,7);
                        floor.Fx.Burst(transform.position + Vector3.up * .25f, OfficeEffects.Magenta);
                        floor.Fx.Sound(ShiftSound.Crash, Eyes);
                        break;
                    case Action.Bolt:
                        floor.Fx.Bolt(this, Eyes + transform.forward * .6f, (aim - Eyes).normalized,
                            boss ? boltDamage : 7);
                        break;
                    case Action.Summon:
                        floor.Summon();
                        floor.Fx.Burst(Eyes, OfficeEffects.Magenta);
                        break;
                    case Action.Rush:
                        rushEnd = transform.position + transform.forward * 9f;
                        if (agent.Raycast(rushEnd, out var edge)) rushEnd = edge.position;
                        agent.speed = 13; agent.acceleration = 65;
                        MoveTo(rushEnd);
                        break;
                }
            }
            if (attack == Action.Rush && Connects(marked,2.4f,75))
            {
                Hit(marked,rushDamage,6);
                marked = null;
                StopMoving();
            }
            if (Time.time < finishAt) return;
            StopMoving();
            agent.acceleration = 20;
            readyAt = Time.time + (kind == Kind.Drone ? 1.9f : boss ? 1.75f - Tier * .32f : 1f);
            attack = Action.None;
            Pose(OfficePose.Search);
        }

        void Hit(FloorPlayer player, int amount, float push) =>
            player.Stats.ReceiveHit(amount,player.BodyCenter,player.transform.position-transform.position,push,transform);
    }
}
