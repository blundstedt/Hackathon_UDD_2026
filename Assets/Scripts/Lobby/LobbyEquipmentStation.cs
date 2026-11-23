using System;
using Hackaton.Multiplayer;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyEquipmentStation : MonoBehaviour
    {
        [SerializeField] private PlayerView player;
        [SerializeField] private WeaponCatalog catalog;
        [SerializeField, Min(.5f)] private float interactionDistance = 4f;

        private PlayerView equipped;
        private bool Online => CampaignRoom.Current && CampaignRoom.Current.IsOnline;
        internal WeaponCatalog Catalog => catalog;
        internal FloorPlayer LocalPlayer => Online ? NetworkTiza.Local?.Player :
            player ? player.GetComponent<FloorPlayer>() : null;
        internal PlayerView Equipped => Online && NetworkTiza.Local
            ? CampaignRoom.Current.Avatars[NetworkTiza.Local.Equipped] : equipped;

        private void Start()
        {
            if (Online || !FloorSession.Instance) return;
            var participant = LocalPlayer;
            if (!participant) return;
            var saved = FloorSession.Instance.Equipment(participant.Id);
            if (saved) Equip(saved);
        }

        internal bool InRange(FloorPlayer participant) => participant &&
            (participant.BodyCenter - transform.position).sqrMagnitude <= interactionDistance * interactionDistance;

        public bool CanEquip(FloorPlayer participant, PlayerView prefab) =>
            FloorSession.Instance && !FloorSession.Instance.Traveling && InRange(participant) &&
            catalog.Find(prefab)?.CanEquip == true;

        public void SelectWeapon(PlayerView prefab)
        {
            if (prefab == Equipped || !CanEquip(LocalPlayer, prefab)) return;
            if (Online)
            {
                int index = Array.IndexOf(CampaignRoom.Current.Avatars, prefab);
                if (index >= 0) NetworkTiza.Local.EquipRpc(index);
            }
            else Equip(prefab);
        }

        private void Equip(PlayerView prefab)
        {
            var previous = player;
            var previousTransform = previous.transform;
            var cameraMode = previous.Mode;
            var staging = new GameObject("Equipment staging");
            staging.SetActive(false);
            staging.transform.SetParent(transform, false);
            var replacement = Instantiate(prefab, staging.transform);
            replacement.gameObject.SetActive(false);
            replacement.transform.SetParent(previousTransform.parent, false);
            replacement.transform.SetPositionAndRotation(previousTransform.position, previousTransform.rotation);
            replacement.name = previous.name;
            replacement.MatchCamera(previous);
            replacement.GetComponent<PlayerStats>().CopyFrom(previous.GetComponent<PlayerStats>());
            Destroy(staging);

            if (FloorSession.Instance && previous.TryGetComponent<FloorPlayer>(out var participant))
            {
                replacement.GetComponent<FloorPlayer>().SetIdentity(participant.Id);
                FloorSession.Instance.Equip(participant.Id, prefab);
            }
            previous.gameObject.SetActive(false);
            player = replacement;
            equipped = prefab;
            replacement.gameObject.SetActive(true);
            replacement.SetCameraMode(cameraMode);
            Destroy(previous.gameObject);
        }
    }
}
