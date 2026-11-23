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

        public override string Interact(FloorPlayer player)
        {
            jersey.SetActive(!jersey.activeSelf);
            return null;
        }
    }
}
