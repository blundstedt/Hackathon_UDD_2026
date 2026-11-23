using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    public sealed class JerseyMannequin : FloorInteraction
    {
        public override string Prompt(FloorSession session) => "E - Equipar polera";
        public override string PromptFor(FloorSession session, FloorPlayer player) =>
            player.Look.jersey ? "E - Quitar polera" : Prompt(session);

        public override string Interact(FloorPlayer player)
        {
            var look = player.Look;
            look.jersey = !look.jersey;
            player.ChangeLook(look);
            return null;
        }
    }
}
