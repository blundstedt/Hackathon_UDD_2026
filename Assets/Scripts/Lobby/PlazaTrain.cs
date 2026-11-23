using UnityEngine;

namespace Hackaton.Lobby
{
    // The track mesh and each carriage sample this same closed path in local metres.
    public sealed class PlazaTrain : MonoBehaviour
    {
        [SerializeField] private Vector2 halfSize = new(22f, 26f);
        [SerializeField, Min(1f)] private float cornerRadius = 6f;
        [SerializeField, Min(0f)] private float speed = 9f;
        [SerializeField, Min(1f)] private float carriageSpacing = 4.65f;
        [SerializeField, Range(0f, 1f)] private float departure = .84f;
        [SerializeField] private Transform[] carriages;
        private float distance;

        private float Radius => Mathf.Clamp(cornerRadius, .1f, Mathf.Min(halfSize.x, halfSize.y));
        public float Length => 4f * (halfSize.x + halfSize.y - 2f * Radius) + 2f * Mathf.PI * Radius;

        public Vector3 Sample(float metres, out Vector3 forward)
        {
            float r = Radius;
            float arc = Mathf.PI * .5f * r;
            float remaining = Mathf.Repeat(metres, Length);
            Vector3 start = new(halfSize.x, 0f, -halfSize.y + r);
            forward = Vector3.forward;
            for (int side = 0; side < 4; side++)
            {
                float straight = 2f * ((side % 2 == 0 ? halfSize.y : halfSize.x) - r);
                if (remaining < straight) return start + forward * remaining;
                remaining -= straight;
                Vector3 radial = Vector3.Cross(Vector3.up, forward);
                Vector3 corner = start + forward * straight - radial * r;
                if (remaining <= arc || side == 3)
                {
                    float angle = Mathf.Min(remaining, arc) / r;
                    Vector3 normal = radial * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                    forward = forward * Mathf.Cos(angle) - radial * Mathf.Sin(angle);
                    return corner + normal * r;
                }
                remaining -= arc;
                start = corner + forward * r;
                forward = -radial;
            }
            return start;
        }

        private void OnEnable()
        {
            distance = departure * Length;
            PositionCars();
        }
        private void Update()
        {
            distance = Mathf.Repeat(distance + speed * Time.deltaTime, Length);
            PositionCars();
        }

        private void PositionCars()
        {
            if (carriages == null) return;
            for (int i = 0; i < carriages.Length; i++)
            {
                if (!carriages[i]) continue;
                Vector3 pos = Sample(distance - i * carriageSpacing, out var heading);
                carriages[i].SetLocalPositionAndRotation(pos, Quaternion.LookRotation(heading));
            }
        }
    }
}
