using UnityEngine;

namespace Hackaton.Player
{
    public sealed class SlapAttack : PlayerAttack
    {
        protected override float Cooldown => .45f;

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            Strike(origin, direction, 2f, .35f, 0, 7f);
        }
    }
}
