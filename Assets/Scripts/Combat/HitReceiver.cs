using UnityEngine;

namespace Hackaton.Combat
{
    public interface IPlayerTarget
    {
        bool IsTargetable { get; }
    }

    public abstract class HitReceiver : MonoBehaviour
    {
        public virtual void ReceiveHit(int damage, Vector3 point, Vector3 direction, float push, Transform source)
            => ReceiveHit(damage, point, direction, push);

        public abstract void ReceiveHit(int damage, Vector3 point, Vector3 direction, float push);
    }
}
