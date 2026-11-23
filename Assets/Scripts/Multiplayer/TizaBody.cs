using UnityEngine;
namespace Hackaton.Multiplayer
{
    public sealed class TizaBody : MonoBehaviour
    {
        public NetworkTiza Peer { get; internal set; }
    }
}
