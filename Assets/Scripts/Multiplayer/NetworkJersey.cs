using Hackaton.Lobby;
using Unity.Netcode;
using UnityEngine;

namespace Hackaton.Multiplayer
{
    [RequireComponent(typeof(JerseyMannequin))]
    public sealed class NetworkJersey : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> worn = new(true);
        private JerseyMannequin mannequin;

        public override void OnNetworkSpawn()
        {
            mannequin = GetComponent<JerseyMannequin>();
            worn.OnValueChanged += Apply;
            mannequin.SetWorn(worn.Value);
        }

        public override void OnNetworkDespawn() => worn.OnValueChanged -= Apply;

        internal void Toggle()
        {
            if (IsServer) worn.Value = !worn.Value;
        }

        private void Apply(bool previous, bool current) => mannequin.SetWorn(current);
    }
}
