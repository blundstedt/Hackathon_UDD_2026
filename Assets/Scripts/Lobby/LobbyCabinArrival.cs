using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    // Runs after the lobby restores equipment and registers the arriving avatars.
    [DefaultExecutionOrder(500)]
    public sealed class LobbyCabinArrival : MonoBehaviour
    {
        [SerializeField] private BoxCollider boardingZone;

        internal bool TryArrival(out Pose pose)
        {
            pose = default;
            if (!boardingZone) return false;
            var zone = boardingZone.transform;
            var size = boardingZone.size;
            var facing = Quaternion.LookRotation(-zone.forward, Vector3.up);
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = boardingZone.center;
                position.x += ((i % 2) - .5f) * size.x * .45f;
                position.z += ((i / 2) - .5f) * size.z * .45f;
                position.y -= size.y * .5f;
                if (Hackaton.Multiplayer.CoopSpawn.TryPose(zone.TransformPoint(position), facing, out pose)) return true;
            }
            return false;
        }

        private void Start()
        {
            if (Hackaton.Multiplayer.CampaignRoom.Current && Hackaton.Multiplayer.CampaignRoom.Current.IsOnline) return;
            var session = FloorSession.Instance;
            if (!session || !session.Traveling || !boardingZone) return;

            var zone = boardingZone.transform;
            var size = boardingZone.size;
            if (session.PlayerCount == 0) return;
            int columns = Mathf.Clamp(Mathf.FloorToInt(size.x / 1.4f), 1, session.PlayerCount);
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)session.PlayerCount / columns));
            int index = 0;
            foreach (var player in session.Players)
            {
                var body = player.GetComponent<CharacterController>();
                var position = boardingZone.center;
                position.x += ((index % columns + .5f) / columns - .5f) * (size.x - .4f);
                position.z += ((index / columns + .5f) / rows - .5f) * (size.z - .4f);
                position.y -= size.y * .5f;
                var feet = zone.TransformPoint(position);
                feet.y += body.height * .5f - body.center.y + body.skinWidth;
                body.enabled = false;
                player.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(-zone.forward, Vector3.up));
                body.enabled = true;
                index++;
            }
        }
    }
}
