using UnityEngine;

namespace Hackaton.Player
{
    public sealed class ResorteraAttack : PlayerAttack
    {
        [Min(0)] public float range_damage_multiplier = 1f;
        [SerializeField] private CombatProjectile projectile;
        protected override float Cooldown => .70f;

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            var shot = Instantiate(projectile, origin, Quaternion.LookRotation(direction));
            shot.Launch(transform, direction, Mathf.RoundToInt(8 * range_damage_multiplier));
        }
    }
}
