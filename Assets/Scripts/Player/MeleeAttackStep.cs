using System;
using Hackaton.Combat;
using UnityEngine;

namespace Hackaton.Player
{
    [Serializable]
    public struct MeleeAttackStep
    {
        [Min(.2f)] public float cooldown;
        public Knockback impact;

        public MeleeAttackStep(float cooldown, Knockback impact)
        {
            this.cooldown = cooldown;
            this.impact = impact;
        }
    }
}
