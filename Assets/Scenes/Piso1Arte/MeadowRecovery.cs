using Hackaton.Combat;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Levels
{
    // Piso 1 owns recovery positions. The session retains rewards and completion.
    public sealed class MeadowRecovery : MonoBehaviour
    {
        private void LateUpdate()
        {
            var session = FloorSession.Instance;
            if (!session || session.Traveling) return;
            int slot = 0;
            foreach (var player in session.Players)
            {
                Vector3 position = player.transform.position;
                float x = position.x / 39f;
                float z = position.z / 33f;
                if (player.Stats.Health == 0 || position.y < -4f || x * x + z * z > 1f)
                    Recover(player, slot);
                slot++;
            }
        }

        private void Recover(FloorPlayer player, int slot)
        {
            var controller = player.GetComponent<CharacterController>();
            var movement = player.GetComponent<PlayerMovement>();
            var knockback = player.GetComponent<LargeKnockback>();
            bool wasMoving = movement.enabled;
            bool wasSimulating = knockback && knockback.enabled;
            if (wasSimulating) knockback.enabled = false;
            movement.enabled = false;
            controller.enabled = false;
            Vector3 offset = new Vector3(slot % 4 * 1.3f, .25f, slot / 4 * 1.3f);
            player.transform.SetPositionAndRotation(transform.TransformPoint(offset), transform.rotation);
            controller.enabled = true;
            movement.enabled = wasMoving;
            if (wasSimulating) knockback.enabled = true;
            if (player.Stats.Health == 0) player.Stats.Heal(PlayerStats.MaxHealth);
        }
    }
}
