using Hackaton.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    public sealed class DayNightBed : MonoBehaviour
    {
        [SerializeField] private DayNightCycle cycle;
        [SerializeField] private Collider interactionArea;
        [SerializeField] private Text prompt;
        [SerializeField, Min(0.5f)] private float interactionDistance = 2.4f;

        private void Update()
        {
            var camera = Camera.main;
            var input = camera != null ? camera.GetComponentInParent<PlayerInputReader>() : null;
            bool canRest = input != null && input.HasControl && cycle.isActiveAndEnabled && CanInteract(camera, input.transform);
            if (canRest && Keyboard.current?.eKey.wasPressedThisFrame == true)
                cycle.AdvancePhase();

            if (canRest)
                prompt.text = cycle.IsNight ? "[E] Dormir hasta el día" : "[E] Dormir hasta la noche";
            if (prompt.gameObject.activeSelf != canRest)
                prompt.gameObject.SetActive(canRest);
        }

        private void OnDisable()
        {
            if (prompt != null) prompt.gameObject.SetActive(false);
        }

        private bool CanInteract(Camera camera, Transform player)
        {
            Vector3 nearest = interactionArea.ClosestPoint(player.position);
            if ((nearest - player.position).sqrMagnitude > interactionDistance * interactionDistance)
                return false;

            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            float reach = Vector3.Distance(ray.origin, player.position) + interactionDistance;
            if (!interactionArea.Raycast(ray, out RaycastHit bedHit, reach))
                return false;

            foreach (var hit in Physics.RaycastAll(ray, bedHit.distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(player))
                    return false;
            }
            return true;
        }
    }
}
