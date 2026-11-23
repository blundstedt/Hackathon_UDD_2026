using System;
using Hackaton.Lobby;
using Hackaton.Player;
using Hackaton.Progression;
using Unity.Netcode;
using UnityEngine;

namespace Hackaton.Multiplayer
{
    // NGO synchronizes this root. Each peer assembles the real authored avatar for its role.
    [DefaultExecutionOrder(250)]
    public sealed partial class NetworkTiza : NetworkBehaviour
    {
        public static NetworkTiza Local { get; private set; }
        private TizaIdentity identity;
        private FloorPlayer player;
        private PlayerView view;
        private bool statsDirty;
        private static readonly Color[] BodyColors = { new Color32(218, 83, 132, 255), new Color32(86, 145, 242, 255),
            new Color32(246, 166, 70, 255), new Color32(108, 196, 160, 255) };
        public PlayerView View => view;
        public FloorPlayer Player => player;
        public int Equipped => identity.Equipment.Value;

        protected override void OnNetworkPostSpawn()
        {
            identity = GetComponent<TizaIdentity>();
            if (IsOwner) Local = this;
            identity.Equipment.OnValueChanged += ChangedEquipment;
            Assemble(identity.Equipment.Value);
            if (!IsServer && IsOwner) RequestStateRpc();
        }

        private void Assemble(int index)
        {
            var catalog = CampaignRoom.Current.Avatars;
            var pose = view ? new Pose(view.transform.position, view.transform.rotation) : new Pose(transform.position, transform.rotation);
            if (view) RemoveAvatar();
            var staging = new GameObject("Montaje de Tiza");
            staging.SetActive(false);
            view = Instantiate(catalog[index], staging.transform);
            inventory = view.GetComponent<PlayerInventory>();
            if (IsServer) { activeSlot.Value = 1; biteEnds.Value = 0; }
            player = view.GetComponent<FloorPlayer>();
            view.gameObject.AddComponent<TizaBody>().Peer = this;
            foreach (var part in view.GetComponentsInChildren<Renderer>(true))
            {
                if (part.name != "Cuerpo") continue;
                var tint = new MaterialPropertyBlock();
                part.GetPropertyBlock(tint);
                tint.SetColor("_BaseColor", BodyColors[identity.ColorIndex.Value]);
                tint.SetColor("_Color", BodyColors[identity.ColorIndex.Value]);
                part.SetPropertyBlock(tint);
            }
            player.SetIdentity(identity.Account.Value.ToString());
            view.transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (!IsOwner)
            {
                view.GetComponent<PlayerInputReader>().enabled = false;
                view.GetComponent<PlayerMovement>().enabled = false;
                view.GetComponent<FirstPersonLook>().enabled = false;
                view.GetComponent<PlayerStatsHud>().enabled = false;
                view.ViewCamera.enabled = false;
                foreach (var listener in view.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            view.ViewCamera.clearFlags = CameraClearFlags.Skybox;
            view.transform.SetParent(null, true);
            if (!IsOwner) view.enabled = false; // Restores authored hands and third-person renderers.
            Destroy(staging);
            if (IsServer)
            {
                FloorSession.Instance.Equip(player.Id, catalog[index]);
                player.Stats.Changed += DirtyStats;
                statsDirty = true;
            }
        }

        private void RemoveAvatar()
        {
            if (!view) return;
            if (IsServer && player && player.Stats) player.Stats.Changed -= DirtyStats;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        public override void OnNetworkDespawn()
        {
            if (identity) identity.Equipment.OnValueChanged -= ChangedEquipment;
            if (Local == this) Local = null;
            RemoveAvatar();
        }
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        public void RecoverRpc(Vector3 position, Quaternion rotation)
        {
            if (!view) return;
            var controller = view.GetComponent<CharacterController>();
            controller.enabled = false;
            view.transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
        }
        private void ChangedEquipment(int before, int after) => Assemble(after);
        private void DirtyStats() => statsDirty = true;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
        public void AttackRpc(float pitch)
        {
            if (!view || float.IsNaN(pitch) || float.IsInfinity(pitch)) return;
            if (!IsOwner) view.AimPivot.localRotation = Quaternion.Euler(Mathf.Clamp(pitch, -85, 85), 0, 0);
            foreach (var attack in view.GetComponents<PlayerAttack>())
                if (attack.isActiveAndEnabled) attack.StartSwing();
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        public void PushRpc(float horizontal, float vertical, float duration, float recovery, bool large, Vector3 direction)
        {
            if (view) view.GetComponent<PlayerMovement>().Push(
                new Hackaton.Combat.Knockback(horizontal, vertical, duration, recovery, large), direction);
        }
        private void LateUpdate()
        {
            if (!IsSpawned || !view) return;
            if (IsOwner) transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
            else view.transform.SetPositionAndRotation(transform.position, transform.rotation);
            if (IsServer && inventory) UpdateInventory();
            if (IsServer && statsDirty)
            {
                statsDirty = false;
                if (!IsOwner) SendStatsRpc(player.Stats.Health, player.Stats.Money, player.Stats.Level, player.Stats.Experience, player.Stats.Completos);
            }
        }
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void SendStatsRpc(int health, int money, int level, int experience, int completos)
        {
            if (!IsServer && player) player.Stats.Restore(new PlayerStats.Snapshot {
                health = health, money = money, level = level, experience = experience, completos = completos });
        }

        public static void PublishCampaign()
        {
            if (!Local || !Local.IsServer) return;
            string data = JsonUtility.ToJson(FloorSession.Instance.Capture());
            foreach (var client in Local.NetworkManager.ConnectedClients.Values)
                if (client.PlayerObject && client.PlayerObject.TryGetComponent<NetworkTiza>(out var peer) && !peer.IsOwner)
                    peer.ReceiveStateRpc(data);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestStateRpc() => ReceiveStateRpc(JsonUtility.ToJson(FloorSession.Instance.Capture()));
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void ReceiveStateRpc(string json)
        {
            if (!IsServer && FloorSession.Instance)
                FloorSession.Instance.Restore(JsonUtility.FromJson<CampaignSave>(json), CampaignRoom.Current.Avatars);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RestRpc()
        {
            if (!player) return;
            foreach (var bed in FindObjectsByType<DayNightBed>())
                if (bed.TryRest(player.transform.position)) return;
        }

        public void Interact(FloorInteraction target)
        {
            var point = target.GetComponent<Collider>().ClosestPoint(player.BodyCenter);
            // Keep adjacent elevator keys distinct in the server overlap query.
            if (target is FloorButton button) UseRpc(button.Keycap.Surface, target.GetType().Name, button.Destination);
            else UseRpc(point, target.GetType().Name, -1);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void UseRpc(Vector3 point, string kind, int destination)
        {
            if (!player || FloorSession.Instance.Traveling || Vector3.Distance(player.BodyCenter, point) > 3.6f) return;
            foreach (var c in Physics.OverlapSphere(point, .18f, ~0, QueryTriggerInteraction.Collide))
            {
                if (!c.TryGetComponent<FloorInteraction>(out var target) || !target.isActiveAndEnabled || target.GetType().Name != kind) continue;
                if (target is FloorButton button && button.Destination != destination) continue;
                Vector3 near = c.ClosestPoint(player.BodyCenter);
                if (Vector3.Distance(near, player.BodyCenter) > 3.6f) continue;
                var message = target.Interact(player);
                if (!string.IsNullOrEmpty(message)) NoticeRpc(message);
                return;
            }
        }
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void NoticeRpc(string message) => FloorSession.Instance.Screen.Notice(message);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EquipRpc(int index)
        {
            var station = FindAnyObjectByType<LobbyEquipmentStation>();
            var catalog = CampaignRoom.Current.Avatars;
            if (station && index >= 0 && index < catalog.Length && station.CanEquip(player, catalog[index]))
                identity.Equipment.Value = index;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void WaitMessageRpc(string message) => FloorSession.Instance.Screen.WaitMessage(message.Length == 0 ? null : message);
    }
}
