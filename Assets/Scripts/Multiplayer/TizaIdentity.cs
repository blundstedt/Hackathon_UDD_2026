using Unity.Collections;
using Unity.Netcode;

namespace Hackaton.Multiplayer
{
    public sealed class TizaIdentity : NetworkBehaviour
    {
        public readonly NetworkVariable<FixedString64Bytes> Account = new();
        public readonly NetworkVariable<int> Equipment = new();
        public readonly NetworkVariable<int> ColorIndex = new();
        private int initialColor;
        private string initialAccount;
        private int initialEquipment;

        internal void Configure(string account, int equipment, int colorIndex)
        {
            initialAccount = account;
            initialColor = colorIndex;
            initialEquipment = equipment;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            Account.Value = initialAccount;
            ColorIndex.Value = initialColor;
            Equipment.Value = initialEquipment;
        }
    }
}
