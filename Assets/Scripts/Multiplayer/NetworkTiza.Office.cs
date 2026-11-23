using Hackaton.Levels;
using Unity.Netcode;
using UnityEngine;

namespace Hackaton.Multiplayer
{
    public sealed partial class NetworkTiza
    {
        // Scene travel despawns this player object, so packets belong to a single visit.
        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        internal void OfficeStateRpc(byte phase, int remaining, bool closing, bool glass, byte archives, OfficeActorFrame[] actors)
        {
            if (OfficeDirector.Active)
                OfficeDirector.Active.ReceiveState((OfficeDirector.Stage)phase,remaining,closing,glass,archives,actors);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        internal void OfficeSoundRpc(ShiftSound sound, Vector3 position, float volume)
        {
            if (OfficeDirector.Active) OfficeDirector.Active.Fx.Sound(sound,position,volume);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        internal void OfficeRingRpc(Vector3 center, float radius, float duration)
        {
            if (OfficeDirector.Active) OfficeDirector.Active.Fx.Ring(center,radius,duration);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        internal void OfficeBurstRpc(Vector3 position, Color color, int count)
        {
            if (OfficeDirector.Active) OfficeDirector.Active.Fx.Burst(position,color,count);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        internal void OfficeBoltRpc(int ownerId, Vector3 muzzle, Vector3 direction)
        {
            var office = OfficeDirector.Active;
            if (office) office.Fx.LaunchBolt(office.Replica(ownerId),muzzle,direction,0);
        }
    }
}
