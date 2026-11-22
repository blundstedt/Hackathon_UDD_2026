using Hackaton.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    public sealed class SewerMerchant : MonoBehaviour
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Transform head;
        [SerializeField] private Collider faceTarget;
        [SerializeField] private Text prompt;
        [SerializeField, Min(0.1f)] private float radius = 8.48f;
        [SerializeField] private float openHeight = 0.32f;

        private float progress;
        private readonly RaycastHit[] hits = new RaycastHit[32];

        private void LateUpdate()
        {
            var camera = Camera.main;
            var player = camera ? camera.GetComponentInParent<PlayerInputReader>() : null;
            Vector3 offset = player ? player.transform.position - transform.position : Vector3.zero;
            offset.y = 0f;
            bool near = player && offset.sqrMagnitude <= radius * radius;
            progress = Mathf.MoveTowards(progress, near ? 1f : 0f, Time.deltaTime / (near ? 0.85f : 0.72f));
            // El retraso al subir también hace que la cabeza se esconda antes de bajar la tapa.
            hinge.localRotation = Quaternion.Euler(-54f * Smooth(0f, 0.86f), 0f, 0f);
            Vector3 position = head.localPosition;
            position.y = Mathf.Lerp(-0.62f, openHeight, Smooth(0.28f, 1f));
            head.localPosition = position;
            if (near && offset.sqrMagnitude > 0.01f)
                head.rotation = Quaternion.LookRotation(offset);

            bool show = near && progress > 0.75f && player.HasControl && IsAimedAt(camera, player.transform);
            if (prompt.gameObject.activeSelf != show)
                prompt.gameObject.SetActive(show);
        }

        private float Smooth(float start, float end)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, progress));
        }

        private bool IsAimedAt(Camera camera, Transform player)
        {
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (!faceTarget.Raycast(ray, out var faceHit, radius + Vector3.Distance(camera.transform.position, player.position)))
                return false;

            int count = Physics.RaycastNonAlloc(ray, hits, faceHit.distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(player))
                    return false;
            return count < hits.Length;
        }

        private void OnDisable()
        {
            if (prompt) prompt.gameObject.SetActive(false);
        }
    }
}
