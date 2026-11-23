using Hackaton.Multiplayer;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    public sealed class JerseyMannequin : FloorInteraction
    {
        [SerializeField] private GameObject jersey;

        public override string Prompt(FloorSession session) =>
            jersey.activeSelf ? "E - Quitar camiseta" : "E - Poner camiseta";

        internal void SetWorn(bool worn) => jersey.SetActive(worn);

        public override string Interact(FloorPlayer player)
        {
            if (TryGetComponent<NetworkJersey>(out var network) && network.IsSpawned)
                network.Toggle();
            else
                SetWorn(!jersey.activeSelf);
            return null;
        }
    }
}
