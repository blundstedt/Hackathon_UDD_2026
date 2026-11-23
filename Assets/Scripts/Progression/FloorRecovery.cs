using Hackaton.Combat;
using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Levels
{
    public static class FloorRecovery
    {
        public static void ReturnToCheckpoint(PlayerMovement movement, Transform checkpoint)
        {
            Vector3 safe = checkpoint.position;
            for (int i = 0; i < 4; i++)
            {
                safe = checkpoint.position + checkpoint.right * (i - 1.5f) * 2f;
                if (!Physics.CheckCapsule(safe + Vector3.up * .55f, safe + Vector3.up * 1.5f,
                    .43f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) break;
            }
            var launch = movement.GetComponent<LargeKnockback>();
            var capsule = movement.GetComponent<CharacterController>();
            bool moving = movement.enabled;
            bool canLaunch = launch.enabled;
            movement.enabled = false;
            launch.enabled = false;
            capsule.enabled = false;
            movement.transform.SetPositionAndRotation(safe + Vector3.up * .08f, checkpoint.rotation);
            capsule.enabled = true;
            launch.enabled = canLaunch;
            movement.enabled = moving;
            Physics.SyncTransforms();
        }
    }
}
