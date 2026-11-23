using Hackaton.Combat;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    public sealed class OfficeFloor : MonoBehaviour
    {
        [SerializeField] private NavMeshData pursuitMesh;
        private NavMeshDataInstance navigation;

        private void OnEnable()
        {
            if (pursuitMesh) navigation = NavMesh.AddNavMeshData(pursuitMesh);
        }

        private void OnDisable()
        {
            if (navigation.valid) navigation.Remove();
        }

        private void LateUpdate()
        {
            var session = FloorSession.Instance;
            if (!session || session.Traveling) return;
            int slot = 0;
            foreach (var player in session.Players)
            {
                if (player.Stats.Health == 0 || player.transform.position.y < -5f)
                    Recover(player, slot);
                slot++;
            }
        }

        private void Recover(FloorPlayer player, int slot)
        {
            var body = player.GetComponent<CharacterController>();
            var movement = player.GetComponent<PlayerMovement>();
            var impact = player.GetComponent<LargeKnockback>();
            bool moving = movement.enabled;
            bool simulating = impact && impact.enabled;
            if (simulating) impact.enabled = false;
            movement.enabled = false;
            body.enabled = false;
            player.transform.SetPositionAndRotation(transform.position +
                new Vector3(slot % 3 * 1.2f, .2f, slot / 3 * 1.2f), transform.rotation);
            body.enabled = true;
            movement.enabled = moving;
            if (simulating) impact.enabled = true;
            if (player.Stats.Health == 0) player.Stats.Heal(PlayerStats.MaxHealth);
        }
    }
}
