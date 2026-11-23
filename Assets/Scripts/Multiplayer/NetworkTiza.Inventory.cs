using Hackaton.Player;
using Unity.Netcode;

namespace Hackaton.Multiplayer
{
    public sealed partial class NetworkTiza
    {
        private readonly NetworkVariable<int> activeSlot = new(1);
        private readonly NetworkVariable<bool> hasCompleto = new();
        private readonly NetworkVariable<double> biteEnds = new();
        private PlayerInventory inventory;
        internal int ActiveSlot => activeSlot.Value;
        internal bool HasCompleto => hasCompleto.Value;
        internal double BiteRemaining => biteEnds.Value - NetworkManager.ServerTime.Time;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SelectSlotRpc(int slot)
        {
            if (!inventory || slot < 1 || slot > 3 || activeSlot.Value == slot) return;
            activeSlot.Value = slot;
            biteEnds.Value = 0;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EatRpc()
        {
            if (!inventory || inventory.Traveling || activeSlot.Value != 2 || !inventory.CanEat || biteEnds.Value != 0) return;
            biteEnds.Value = NetworkManager.ServerTime.Time + inventory.Items.biteSeconds;
        }

        private void UpdateInventory()
        {
            if (biteEnds.Value > 0)
            {
                if (inventory.Traveling || !inventory.CanEat) biteEnds.Value = 0;
                else if (BiteRemaining <= 0)
                {
                    biteEnds.Value = 0;
                    player.Stats.EatCompleto(inventory.Items.healing);
                }
            }
            hasCompleto.Value = player.Stats.Completos > 0;
        }
    }
}
