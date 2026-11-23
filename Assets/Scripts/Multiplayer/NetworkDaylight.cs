using Hackaton.Lobby;
using Unity.Netcode;

namespace Hackaton.Multiplayer
{
    public sealed class NetworkDaylight : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> night = new();
        private DayNightCycle cycle;
        public override void OnNetworkSpawn()
        {
            cycle = GetComponent<DayNightCycle>();
            if (IsServer) night.Value = cycle.IsNight;
            else { cycle.SetNight(night.Value); night.OnValueChanged += Apply; }
        }
        public override void OnNetworkDespawn() => night.OnValueChanged -= Apply;
        private void LateUpdate()
        {
            if (IsSpawned && IsServer && night.Value != cycle.IsNight) night.Value = cycle.IsNight;
        }
        private void Apply(bool oldValue, bool value) => cycle.SetNight(value);
    }
}
