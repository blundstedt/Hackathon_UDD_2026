using Hackaton.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ShopDoor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float reach = 3.2f;
        [SerializeField, Min(1f)] private float degreesPerSecond = 160f;
        private BoxCollider doorCollider;
        private bool open;
        private float angle;

        private void Awake() => doorCollider = GetComponent<BoxCollider>();

        private void Update()
        {
            if (Keyboard.current?.eKey.wasPressedThisFrame == true && CanInteract())
                open = !open;

            float next = Mathf.MoveTowards(angle, open ? 94f : 0f, degreesPerSecond * Time.deltaTime);
            if (next == angle) return;
            angle = next;
            transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        private bool CanInteract()
        {
            var camera = Camera.main;
            var player = camera != null ? camera.GetComponentInParent<PlayerInputReader>() : null;
            if (player == null || !player.HasControl) return false;

            var feet = player.transform.position;
            if ((doorCollider.ClosestPoint(feet) - feet).sqrMagnitude > reach * reach) return false;

            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f));
            float distance = Vector3.Distance(ray.origin, feet) + reach;
            if (!doorCollider.Raycast(ray, out var doorHit, distance)) return false;

            foreach (var hit in Physics.RaycastAll(ray, doorHit.distance, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != doorCollider && !hit.transform.IsChildOf(player.transform))
                    return false;
            }
            return true;
        }
    }
}

