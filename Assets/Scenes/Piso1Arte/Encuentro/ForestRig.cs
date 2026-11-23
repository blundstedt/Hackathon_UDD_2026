using Hackaton.Combat;
using Hackaton.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    // The model faces +Z. All offsets are local to the model; the agent owns the collision root.
    public sealed class ForestRig : MonoBehaviour
    {
        [SerializeField] private Transform head, leftArm, rightArm, leftHand, rightHand;
        [SerializeField] private Transform leftFoot, rightFoot, heart;
        [SerializeField] private MeshFilter visor;
        [SerializeField] private Mesh[] expressions;

        private ForestEnemy owner;
        private NavMeshAgent agent;
        private LargeKnockback launch;
        private Vector3 leftRest, rightRest;
        private float size, born, phase, gait, look, alertUntil, hurtUntil, stunUntil, hitSide;
        private float swingStart = float.NegativeInfinity, windup, deathTime = float.PositiveInfinity;
        private float nextBlink;
        private bool tracking, leftJab;
        private int lastFace = -1;

        // Angular offsets in degrees, displacement in unscaled model units.
        private struct Pose
        {
            public Vector3 body, neck, armL, armR, handL, handR, offset;
            public float legL, legR, spread, liftL, liftR;
        }

        private void Awake()
        {
            owner = GetComponentInParent<ForestEnemy>();
            agent = owner.GetComponent<NavMeshAgent>();
            launch = owner.GetComponent<LargeKnockback>();
            size = transform.localScale.x;
            leftRest = leftFoot.localPosition;
            rightRest = rightFoot.localPosition;
            born = Time.time;
            phase = Random.value * Mathf.PI * 2;
            nextBlink = born + Random.Range(1.5f, 4f);
        }

        public void Swing(float delay)
        {
            swingStart = Time.time;
            windup = Mathf.Max(.01f, delay);
            leftJab = !leftJab;
        }

        public void Flinch(Vector3 point)
        {
            hurtUntil = Time.time + .24f;
            hitSide = Mathf.Sign(owner.transform.InverseTransformPoint(point).x);
        }

        public void Stagger(float duration)
        {
            stunUntil = Time.time + duration;
            swingStart = float.NegativeInfinity;
        }

        public void Fall()
        {
            deathTime = Time.time;
            swingStart = float.NegativeInfinity;
        }

        private static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        private void LateUpdate()
        {
            float now = Time.time;
            bool dead = now >= deathTime;
            bool stunned = launch && launch.Active || now < stunUntil;
            bool sees = owner.State == EnemyBase.EnemyState.Chase || owner.State == EnemyBase.EnemyState.Attack;
            if (sees && !tracking) alertUntil = now + .32f;
            tracking = sees;

            Vector3 velocity = agent.enabled && agent.isOnNavMesh ? agent.velocity : Vector3.zero;
            float speed = velocity.magnitude;
            float blend = 1 - Mathf.Exp(-Time.deltaTime * 11f);
            gait = Mathf.Lerp(gait, stunned || dead ? 0 : Mathf.Clamp01(speed / agent.speed), blend);
            Vector3 direction = owner.transform.InverseTransformDirection(velocity);
            float heading = speed > .15f ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg : 0;
            look = Mathf.Lerp(look, Mathf.Clamp(heading,-38,38), blend);
            ForestRank rank = owner.Rank;
            bool goblin = rank == ForestRank.Goblin;
            bool boss = rank == ForestRank.Colossus;
            phase += Time.deltaTime * (goblin ? 3.1f + speed*3.6f : 1.7f + speed*(boss ? 2.15f : 2.7f));

            float walk = Mathf.Sin(phase) * gait;
            float breath = Mathf.Sin(now * (goblin ? 3.8f : 1.9f) + born);
            Pose pose = default;
            pose.offset = new Vector3(walk*(goblin ? .018f : .046f),
                .012f*breath + (goblin ? .052f : .031f)*Mathf.Abs(walk), 0);
            pose.body = new Vector3((goblin ? 11 : 5)*gait, walk*(goblin ? 6 : 9), -walk*(goblin ? 5 : 7));
            pose.neck = new Vector3(-pose.body.x*.62f - breath*1.5f, look*.7f - pose.body.y*.65f, -pose.body.z*.6f);
            pose.legL = -walk*(goblin ? 41 : 23);
            pose.legR = -pose.legL;
            pose.liftL = Mathf.Max(0, Mathf.Cos(phase))*gait*(goblin ? .083f : .052f);
            pose.liftR = Mathf.Max(0,-Mathf.Cos(phase))*gait*(goblin ? .083f : .052f);
            pose.armL = new Vector3(walk*24, 0, -5);
            pose.armR = new Vector3(-walk*(rank==ForestRank.Guardian ? 10 : 24), 0, 5);
            pose.handL.x = -16 - Mathf.Max(0,-walk)*15;
            pose.handR.x = rank==ForestRank.Guardian ? -37 : -16 - Mathf.Max(0,walk)*15;

            // The final part of windup is the forward stroke. The contact pose coincides with damage.
            float age = now - swingStart;
            float release = boss ? .78f : goblin ? .28f : .48f;
            float u = age / Mathf.Max(.01f, windup);
            float strike = Ease((u-.74f)/.26f);
            float ready = Ease(u/.62f)*(1-strike);
            float contact = strike*(1-Ease((age-windup)/release));
            if (ready + contact > 0) PoseAttack(ref pose, rank, ready, contact);

            float enter = Ease((now-born)/(boss ? 1.35f : .32f));
            float hit = Ease((hurtUntil-now)/.24f);
            float alert = Mathf.Sin(Mathf.PI*Mathf.Clamp01((alertUntil-now)/.32f));
            pose.offset.y -= (1-enter)*(boss ? 1 : .27f);
            pose.body.x += (1-enter)*(boss ? 47 : 19) - hit*16;
            pose.neck += new Vector3(-alert*12 + hit*9, hit*hitSide*17, hit*hitSide*8);
            pose.armL.z -= alert*10 + (1-enter)*22;
            pose.armR.z += alert*10 + (1-enter)*22;
            if (stunned && !dead)
            {
                pose.neck.z += Mathf.Sin(now*23)*8;
                pose.armL.z -= 22;
                pose.armR.z += 22;
            }

            float fall = Ease((now-deathTime)/(boss ? 1.08f : goblin ? .48f : .76f));
            if (dead) PoseDeath(ref pose, rank, fall);
            transform.localPosition = pose.offset*size;
            transform.localRotation = Quaternion.Euler(pose.body);
            head.localRotation = Quaternion.Euler(pose.neck);
            leftArm.localRotation = Quaternion.Euler(pose.armL);
            rightArm.localRotation = Quaternion.Euler(pose.armR);
            leftHand.localRotation = Quaternion.Euler(pose.handL);
            rightHand.localRotation = Quaternion.Euler(pose.handR);
            leftFoot.localRotation = Quaternion.Euler(pose.legL,0,-pose.spread);
            rightFoot.localRotation = Quaternion.Euler(pose.legR,0,pose.spread);
            leftFoot.localPosition = leftRest + Vector3.up*pose.liftL;
            rightFoot.localPosition = rightRest + Vector3.up*pose.liftR;

            if (heart)
                heart.localScale = Vector3.one * (1+.07f*breath+.35f*ready+.14f*contact)*(1-.75f*fall)*enter;
            if (now > nextBlink+.10f) nextBlink = now+Random.Range(2.2f,4.7f);
            int face = dead ? 6 : enter<.72f ? 5 : stunned ? 4 : hit>.02f ? 3 :
                alert>.02f ? 2 : ready+contact>.01f ? 1 : now>=nextBlink ? 7 : sees ? 1 : 0;
            if (face!=lastFace) { visor.sharedMesh=expressions[face]; lastFace=face; }
        }

        private void PoseAttack(ref Pose p, ForestRank rank, float ready, float impact)
        {
            switch (rank)
            {
                case ForestRank.Goblin:
                    float side = leftJab ? -1 : 1;
                    p.body += new Vector3(-9*ready+19*impact, side*(23*ready-27*impact), side*8*impact);
                    p.offset.z += -.065f*ready+.125f*impact;
                    Vector3 punch = new Vector3(36*ready-89*impact, -side*12*impact, side*12*ready);
                    Vector3 elbow = new Vector3(-61*ready+12*impact,0,0);
                    if (leftJab) { p.armL += punch; p.handL += elbow; p.armR.x += 27*impact; }
                    else { p.armR += punch; p.handR += elbow; p.armL.x += 27*impact; }
                    break;
                case ForestRank.Guardian:
                    p.offset += new Vector3(.065f*ready, -.075f*ready, .14f*impact);
                    p.body += new Vector3(8*ready+15*impact, 34*ready-25*impact, -7*ready);
                    p.armR += new Vector3(-26*ready-83*impact,-19*ready,8*ready);
                    p.handR += new Vector3(-29*ready+35*impact,14*ready,0);
                    p.armL += new Vector3(27*impact,12*ready,-15*impact);
                    p.neck.y -= 22*ready;
                    break;
                case ForestRank.Colossus:
                    p.offset.y += .061f*ready-.21f*impact;
                    p.body.x += -18*ready + 43*impact;
                    p.neck.x += 9*ready - 21*impact;
                    p.armL += new Vector3(-149*ready-11*impact,0,-27*ready-12*impact);
                    p.armR += new Vector3(-154*ready-7*impact,0,24*ready+10*impact);
                    p.handL.x -= 34*ready;
                    p.handR.x -= 38*ready;
                    p.spread += 9*ready+13*impact;
                    break;
            }
        }

        private static void PoseDeath(ref Pose p, ForestRank rank, float t)
        {
            bool goblin = rank == ForestRank.Goblin;
            bool boss = rank == ForestRank.Colossus;
            float drop = Ease(t/.42f);
            float tip = Ease((t-.16f)/.7f);
            float bounce = Mathf.Sin(Mathf.PI*Mathf.Clamp01((t-.73f)/.27f));
            p.offset = new Vector3(0, -.09f*drop + .32f*tip + .055f*bounce, 0);
            p.body = new Vector3((goblin ? -87 : boss ? 71 : 45)*tip, 6*tip,(goblin ? 17 : boss ? -23 : 61)*tip);
            p.neck = new Vector3(24*drop, (boss ? -14 : 18)*tip, 8*tip);
            p.armL = new Vector3(-38*drop,0,-48*tip);
            p.armR = new Vector3(-23*drop,0,42*tip);
            p.handL.x = -25*tip;
            p.handR.x = -19*tip;
            p.legL = 38*drop;
            p.legR = (goblin ? -30 : 37)*drop;
            p.spread = 18*tip;
            p.liftL = p.liftR = 0;
        }
    }
}

