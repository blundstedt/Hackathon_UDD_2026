using Hackaton.Multiplayer;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CompletoCounter : FloorInteraction
    {
        [SerializeField] private InventoryItems items;
        public override string Prompt(FloorSession session) => $"E - Comprar completo italiano (${items.price})";
        public override string Interact(FloorPlayer player)
        {
            if (!player || CampaignRoom.Current && !CampaignRoom.Current.IsAuthority) return null;
            return player.Stats.BuyCompleto(items.price)
                ? "Completo italiano añadido al inventario."
                : "Dinero insuficiente.";
        }
    }
}
