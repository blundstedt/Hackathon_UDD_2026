using UnityEngine;

namespace Hackaton.Player
{
    public sealed class TazaAttack : PlayerAttack
    {
        [Min(0)] public float magical_damage_multiplier = 1f;
        [SerializeField] private CombatProjectile projectile;
        protected override float Cooldown => 1.20f;

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            var shot = Instantiate(projectile, origin, Quaternion.LookRotation(direction));
            shot.Launch(transform, direction, Mathf.RoundToInt(5 * magical_damage_multiplier));
        }
    }
}
