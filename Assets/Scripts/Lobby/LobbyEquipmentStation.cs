using System;
using Hackaton.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyEquipmentStation : MonoBehaviour
    {
        [Serializable]
        private sealed class EquipmentOption
        {
            public string label;
            public Collider target;
            public PlayerView playerPrefab;
        }

        [SerializeField] private PlayerView player;
        [SerializeField] private EquipmentOption[] options;
        [SerializeField, Min(0.5f)] private float interactionDistance = 3.1f;
        [SerializeField] private GameObject reticle;
        [SerializeField] private Text prompt;

        private PlayerInputReader input;
        private EquipmentOption equipped;
        private EquipmentOption shownOption;
        private float stationReach;

        private void Awake()
        {
            input = player.GetComponent<PlayerInputReader>();
            equipped = options[0];
            foreach (var option in options)
            {
                float offset = Vector3.Distance(transform.position, option.target.bounds.center);
                stationReach = Mathf.Max(stationReach, offset + interactionDistance);
            }
        }

        private void Update()
        {
            bool nearby = input.HasControl
                && (transform.position - player.transform.position).sqrMagnitude <= stationReach * stationReach;
            if (reticle.activeSelf != nearby)
                reticle.SetActive(nearby);

            var target = nearby ? FindTarget() : null;
            if (target != null && target != equipped && Keyboard.current?.eKey.wasPressedThisFrame == true)
                Equip(target);

            ShowPrompt(target);
        }

        private EquipmentOption FindTarget()
        {
            var camera = player.ViewCamera;
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float reach = Vector3.Distance(ray.origin, player.transform.position) + interactionDistance;
            float closest = reach;
            EquipmentOption target = null;

            foreach (var hit in Physics.RaycastAll(ray, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                if (hit.distance >= closest || hit.transform.IsChildOf(player.transform))
                    continue;

                var candidate = Array.Find(options, option => option.target == hit.collider);
                if (candidate == null && hit.collider.isTrigger)
                    continue;

                closest = hit.distance;
                target = candidate;
            }

            if (target == null)
                return null;

            var position = player.transform.position;
            Vector3 nearest = target.target.ClosestPoint(position);
            return (nearest - position).sqrMagnitude <= interactionDistance * interactionDistance ? target : null;
        }

        private void Equip(EquipmentOption option)
        {
            var previous = player;
            var previousTransform = previous.transform;
            bool wasThirdPerson = previous.IsThirdPerson;

            // Configure the replacement before its movement and input components enable.
            var staging = new GameObject("Equipment staging");
            staging.SetActive(false);
            staging.transform.SetParent(transform, false);
            var replacement = Instantiate(option.playerPrefab, staging.transform);
            replacement.gameObject.SetActive(false);
            replacement.transform.SetParent(previousTransform.parent, false);
            replacement.transform.SetPositionAndRotation(previousTransform.position, previousTransform.rotation);
            replacement.name = previous.name;
            replacement.MatchCamera(previous);
            Destroy(staging);

            previous.gameObject.SetActive(false);
            player = replacement;
            input = replacement.GetComponent<PlayerInputReader>();
            equipped = option;
            shownOption = null;
            replacement.gameObject.SetActive(true);
            if (wasThirdPerson)
                replacement.SetThirdPerson(true);

            Destroy(previous.gameObject);
        }

        private void ShowPrompt(EquipmentOption option)
        {
            if (prompt.gameObject.activeSelf != (option != null))
                prompt.gameObject.SetActive(option != null);

            if (option == null || option == shownOption)
                return;

            prompt.text = option == equipped ? option.label + " equipado" : "[E] Equipar " + option.label;
            shownOption = option;
        }
    }
}

