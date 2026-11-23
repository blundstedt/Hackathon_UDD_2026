using System.Collections.Generic;
using Hackaton.Levels;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Paraiso
{
    // The floor owns its safe areas. Avatars belong to the campaign and may change with equipment.
    public sealed class CelestialRecovery : MonoBehaviour
    {
        [SerializeField] private BoxCollider[] safeAreas;
        [SerializeField] private float fallHeight = -6f;
        private readonly Dictionary<FloorPlayer, int> checkpoints = new();
        private FloorSession session;

        private void Start() => session = FloorSession.Instance;

        private void LateUpdate()
        {
            if (session.Traveling) return;
            foreach (var player in session.Players)
            {
                if (player.gameObject.scene != gameObject.scene) continue;
                if (!checkpoints.TryGetValue(player, out int latest))
                {
                    checkpoints.Add(player, 0);
                    var camera = player.GetComponent<PlayerView>().ViewCamera;
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.farClipPlane = 550f;
                }

                if (player.transform.position.y < fallHeight)
                {
                    FloorRecovery.ReturnToCheckpoint(player.GetComponent<PlayerMovement>(), safeAreas[latest].transform);
                    continue;
                }
                if (!player.GetComponent<CharacterController>().isGrounded) continue;
                for (int i = safeAreas.Length - 1; i > latest; i--)
                {
                    if (!safeAreas[i].bounds.Contains(player.transform.position)) continue;
                    checkpoints[player] = i;
                    break;
                }
            }
        }
    }
}
