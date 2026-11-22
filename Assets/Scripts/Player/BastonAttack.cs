using Hackaton.Combat;
using UnityEngine;

namespace Hackaton.Player
{
    public sealed class BastonAttack : PlayerAttack
    {
        [Min(0)] public float melee_damage_multiplier = 1f;
        [SerializeField, Min(.1f)] private float staffLength = 1.55f;
        [SerializeField, Min(0)] private int baseDamage = 10;
        [SerializeField, Min(.01f), Tooltip("Maximum seconds between the start of two attacks.")]
        private float comboWindow = 1f;
        [SerializeField] private MeleeAttackStep first = new(.35f, new Knockback(2f, 0f, .16f, .02f));
        [SerializeField] private MeleeAttackStep second = new(.35f, new Knockback(3f, 0f, .20f, .04f));
        [SerializeField] private MeleeAttackStep finisher = new(.35f, new Knockback(15f, 8f, .8f, .15f, true));
        private int step;
        private int nextStep;
        private float lastAttack;

        private MeleeAttackStep Current => step == 0 ? first : step == 1 ? second : finisher;
        protected override float Cooldown => Current.cooldown;
        protected override float ImpactTime => .18f;

        protected override void OnAttackStarted()
        {
            step = Time.time - lastAttack > comboWindow ? 0 : nextStep;
            nextStep = (step + 1) % 3;
            lastAttack = Time.time;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            nextStep = 0;
        }

        protected override void Animate(float elapsed)
        {
            Swing(elapsed, .10f, Cooldown,
                new Vector3(.06f, .14f, -.12f), new Vector3(-.32f, -.02f, .32f),
                new Vector3(-36, 18, -12), new Vector3(52, -32, 15));
        }

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            Strike(origin, direction, staffLength, .55f, Mathf.RoundToInt(baseDamage * melee_damage_multiplier), Current.impact);
        }
    }
}
