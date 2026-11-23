using Hackaton.Player;
using Unity.Collections;
using Unity.Netcode;

namespace Hackaton.Multiplayer
{
    public sealed class TizaIdentity : NetworkBehaviour
    {
        public readonly NetworkVariable<FixedString64Bytes> Account = new();
        public readonly NetworkVariable<int> Equipment = new();
        public readonly NetworkVariable<TizaLook> Appearance = new();
        private TizaLook initialLook;
        private string initialAccount;
        private int initialEquipment;

        internal void Configure(string account, int equipment, TizaLook look)
        {
            initialAccount = account;
            initialLook = look;
            initialEquipment = equipment;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            Account.Value = initialAccount;
            Appearance.Value = initialLook;
            Equipment.Value = initialEquipment;
        }
    }
}
