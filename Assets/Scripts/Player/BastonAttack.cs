using UnityEngine;

namespace Hackaton.Player
{
    public sealed class BastonAttack : PlayerAttack
    {
        [Min(0)] public float melee_damage_multiplier = 1f;
        [SerializeField, Min(.1f)] private float staffLength = 1.55f;
        protected override float Cooldown => .35f;
        protected override float ImpactTime => .18f;

        protected override void Animate(float elapsed)
        {
            Swing(elapsed, .10f, Cooldown,
                new Vector3(.06f, .14f, -.12f), new Vector3(-.32f, -.02f, .32f),
                new Vector3(-36, 18, -12), new Vector3(52, -32, 15));
        }

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            Strike(origin, direction, staffLength, .55f, Mathf.RoundToInt(10 * melee_damage_multiplier), 3f);
        }
    }
}
