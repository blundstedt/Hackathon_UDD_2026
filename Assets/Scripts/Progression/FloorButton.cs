using UnityEngine;

namespace Hackaton.Progression
{
    public sealed class FloorButton : FloorInteraction
    {
        [SerializeField] private TowerFloorPanel panel;
        [SerializeField, Min(0)] private int destination;
        public override string Prompt(FloorSession session) => session.TravelBlock(destination) ?? $"[E] Ir al piso {destination}";
        public override string Interact(FloorPlayer player) => panel.Request(player, destination);
    }
}
