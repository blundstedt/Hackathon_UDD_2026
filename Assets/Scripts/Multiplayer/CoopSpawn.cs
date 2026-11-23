using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Multiplayer
{
    public sealed class CoopSpawn : MonoBehaviour
    {
        [SerializeField] private Transform[] points;
        internal static Pose? Find(bool returningToLobby)
        {
            Physics.SyncTransforms();
            if (returningToLobby)
            {
                var cabin = FindAnyObjectByType<Hackaton.Lobby.LobbyCabinArrival>();
                if (cabin && cabin.TryArrival(out var arrival)) return arrival;
            }
            var lobby = FindAnyObjectByType<CoopSpawn>();
            if (lobby)
            {
                foreach (var point in lobby.points)
                    if (TryPose(point.position, point.rotation, out var pose)) return pose;
            }
            var anchor = FindAnyObjectByType<FloorSpawn>();
            if (!anchor) return null;
            for (int ring = 0; ring <= 4; ring++)
                for (int x = -ring; x <= ring; x++)
                    for (int z = -ring; z <= ring; z++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != ring) continue;
                        var p = anchor.transform.TransformPoint(new Vector3(x * 1.45f, 0, z * 1.45f));
                        if (TryPose(p, anchor.transform.rotation, out var pose)) return pose;
                    }
            return null;
        }
        public static bool TryPose(Vector3 point, Quaternion rotation, out Pose pose)
        {
            pose = default;
            if (!Physics.Raycast(point + Vector3.up * .8f, Vector3.down, out var hit, 2.7f, ~0, QueryTriggerInteraction.Ignore)
                || hit.normal.y < .78f) return false;
            var feet = hit.point + Vector3.up * .09f;
            if (Physics.CheckCapsule(feet + Vector3.up * .47f, feet + Vector3.up * 1.45f, .43f, ~0, QueryTriggerInteraction.Ignore)) return false;
            foreach (var peer in FindObjectsByType<TizaIdentity>())
                if ((peer.transform.position - feet).sqrMagnitude < 1.5f) return false;
            pose = new Pose(feet, rotation);
            return true;
        }
    }
}
