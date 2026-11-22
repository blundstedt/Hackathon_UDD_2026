using UnityEngine;

namespace Hackaton.Player
{
    public sealed class TazaAttack : PlayerAttack
    {
        [Min(0)] public float magical_damage_multiplier = 1f;
        [SerializeField] private CombatProjectile projectile;
        protected override float Cooldown => 1.20f;
        protected override float ImpactTime => .25f;

        protected override void Animate(float elapsed)
        {
            Swing(elapsed, .12f, .64f,
                new Vector3(.02f, -.04f, -.10f), new Vector3(-.20f, .12f, .36f),
                new Vector3(-16, 5, 0), new Vector3(84, -8, 3));
        }

        private Renderer mug;

        protected override Vector3 AttackOrigin
        {
            get
            {
                if (mug == null)
                {
                    foreach (var renderer in View.RightHand.GetComponentsInChildren<Renderer>())
                        if (renderer.name == "Mug02") { mug = renderer; break; }
                }
                if (mug == null) return View.RightHand.position;
                Bounds bounds = mug.localBounds;
                return mug.transform.TransformPoint(bounds.center + Vector3.up * bounds.extents.y);
            }
        }

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            var shot = Instantiate(projectile, origin, Quaternion.LookRotation(direction));
            shot.Launch(transform, direction, Mathf.RoundToInt(5 * magical_damage_multiplier));
        }
    }
}
