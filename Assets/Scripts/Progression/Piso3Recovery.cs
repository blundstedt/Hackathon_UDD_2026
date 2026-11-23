using Hackaton.Combat;
using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Progression
{
    [RequireComponent(typeof(FloorSpawn))]
    public sealed class Piso3Recovery : MonoBehaviour
    {
        private void LateUpdate()
        {
            var session = FloorSession.Instance;
            if (!session || session.Traveling) return;

            int slot = 0;
            foreach (var player in session.Players)
            {
                int currentSlot = slot++;
                if (player.gameObject.scene != gameObject.scene) continue;
                if (player.Stats.Health > 0 && player.transform.position.y > -1.5f) continue;

                // The existing knockback rig owns the body until it stands up on solid ground.
                if (player.TryGetComponent<LargeKnockback>(out var launch) && launch.Active) continue;
                var body = player.GetComponent<CharacterController>();
                body.enabled = false;
                float offset = (currentSlot % 4 - 1.5f) * 1.25f;
                player.transform.SetPositionAndRotation(transform.TransformPoint(offset, 0f, 0f), transform.rotation);
                body.enabled = true;
                player.Stats.Heal(PlayerStats.MaxHealth);
            }
        }
    }

#if UNITY_EDITOR
    // Direct Play starts without a campaign history. Use the same key delivery rules as the lobby.
    // AfterSceneLoad runs only for the starting scene. Trips that originate in the lobby keep their real state.
    internal static class Piso3EditorPreview
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PrepareDirectPlay()
        {
            var session = FloorSession.Instance;
            if (!session || !session.Current || session.Current.sceneName != "Piso3") return;

            for (int previous = 1; previous <= 2; previous++)
            {
                var floor = session.Progress.Get(previous);
                if (!floor || !session.Progress.Complete(floor, () => { })) return;
                session.Progress.Deliver();
            }
        }
    }
#endif
}
