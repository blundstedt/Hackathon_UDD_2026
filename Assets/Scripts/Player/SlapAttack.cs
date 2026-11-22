using UnityEngine;

namespace Hackaton.Player
{
    public sealed class SlapAttack : PlayerAttack
    {
        protected override float Cooldown => .45f;
        protected override float ImpactTime => .16f;

        protected override void Animate(float elapsed)
        {
            Swing(elapsed, .08f, .40f,
                new Vector3(.09f, .06f, -.05f), new Vector3(-.54f, .11f, .24f),
                new Vector3(-8, 18, -18), new Vector3(8, -55, 40));
        }

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            Strike(origin, direction, 2f, .35f, 0, 7f);
        }
    }
}
