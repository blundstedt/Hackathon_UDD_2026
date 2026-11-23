using UnityEngine;

namespace Hackaton.Combat
{
    public interface IPlayerTarget
    {
        bool IsTargetable { get; }
    }

    public abstract class HitReceiver : MonoBehaviour
    {
        public void ReceiveHit(int damage, Vector3 point, Vector3 direction, float push, Transform source = null)
            => ReceiveHit(damage, point, direction, Knockback.FromPush(push), source);

        public virtual void ReceiveHit(int damage, Vector3 point, Vector3 direction, Knockback impact, Transform source = null)
        {
            if (damage > 0) ApplyDamage(damage, point, source);
            if (impact.horizontal > 0f || impact.vertical > 0f) ApplyKnockback(impact, direction);
        }

        protected abstract void ApplyDamage(int damage, Vector3 point, Transform source);
        public abstract void ApplyKnockback(Knockback impact, Vector3 direction);
    }
}
