using UnityEngine;

namespace Hackaton.Player
{
    public sealed class ResorteraAttack : PlayerAttack
    {
        [Min(0)] public float range_damage_multiplier = 1f;
        [SerializeField] private CombatProjectile projectile;
        protected override float Cooldown => .70f;
        protected override float ImpactTime => .36f;
        private const float FullDrawTime = .32f;
        private Transform pocketGrip;
        private Transform pocket;
        private Transform leftGrip;
        private Vector3 pocketRest;
        private Band rightBand;
        private Band leftBand;

        protected override Vector3 AttackOrigin => pocketGrip.position;

        protected override void Animate(float elapsed)
        {
            if (pocketGrip == null)
            {
                foreach (var part in View.RightHand.GetComponentsInChildren<Transform>(true))
                    if (part.name == "Grip_Pouch") { pocketGrip = part; break; }

                var fork = pocketGrip.parent;
                pocket = fork.Find("Cuna_Cuero");
                pocketRest = pocket.localPosition;
                rightBand = new Band(fork.Find("Elastico_D"), 1f);
                leftBand = new Band(fork.Find("Elastico_I"), -1f);
                leftGrip = View.LeftHand.Find("Agarre.I");
            }

            float reach = Blend(elapsed, 0f, .13f);
            float pull = Blend(elapsed, .13f, FullDrawTime);
            float release = Blend(elapsed, FullDrawTime, ImpactTime);
            float recover = 1f - Blend(elapsed, ImpactTime, .64f);
            MoveRightHand(new Vector3(-.24f, .02f, .12f) * (reach * recover), Vector3.zero);

            Vector3 drawn = View.HandFrame.rotation * new Vector3(-.09f, -.04f, -.32f) * pull;
            Vector3 localPull = pocket.parent.InverseTransformVector(drawn * (1f - release));
            pocket.localPosition = pocketRest + localPull;
            rightBand.Stretch(localPull);
            leftBand.Stretch(localPull);

            // The fingers stay back as the pocket snaps forward.
            View.LeftHand.position += (pocketGrip.position + drawn - leftGrip.position) * (reach * recover);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (pocket == null) return;
            pocket.localPosition = pocketRest;
            rightBand.Stretch(Vector3.zero);
            leftBand.Stretch(Vector3.zero);
        }

        // Each imported strip runs mostly along local X, from the fork to the leather.
        // Keep its outer tip fixed and map the inner tip onto the moving pocket.
        private sealed class Band
        {
            private readonly Transform strip;
            private readonly Vector3 rest;
            private readonly Vector3 scale;
            private readonly Quaternion rotation;
            private readonly Vector3 anchor;
            private readonly Vector3 span;

            public Band(Transform strip, float side)
            {
                this.strip = strip;
                rest = strip.localPosition;
                scale = strip.localScale;
                rotation = strip.localRotation;
                Bounds bounds = strip.GetComponent<MeshFilter>().sharedMesh.bounds;
                anchor = bounds.center + new Vector3(side * bounds.extents.x, bounds.extents.y * .8f, 0);
                span = 2f * (bounds.center - anchor);
            }

            public void Stretch(Vector3 pull)
            {
                Vector3 original = Vector3.Scale(span, scale);
                Vector3 target = original + Quaternion.Inverse(rotation) * pull;
                Vector3 stretched = original;
                stretched.x = Mathf.Sign(original.x) * Mathf.Sqrt(Mathf.Max(.000001f,
                    target.sqrMagnitude - original.y * original.y - original.z * original.z));
                Vector3 newScale = new Vector3(scale.x * stretched.x / original.x, scale.y, scale.z);
                Quaternion turn = rotation * Quaternion.FromToRotation(stretched, target);
                strip.localScale = newScale;
                strip.SetLocalPositionAndRotation(rest + rotation * Vector3.Scale(anchor, scale)
                    - turn * Vector3.Scale(anchor, newScale), turn);
            }
        }

        protected override void Attack(Vector3 origin, Vector3 direction)
        {
            var shot = Instantiate(projectile, origin, Quaternion.LookRotation(direction));
            shot.Launch(transform, direction, Mathf.RoundToInt(8 * range_damage_multiplier));
        }
    }
}
