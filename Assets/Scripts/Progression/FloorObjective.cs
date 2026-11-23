using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>Canjea la recompensa definida en el catálogo del piso en curso.</summary>
    public sealed class FloorObjective : FloorInteraction
    {
        [SerializeField] private string actionPrompt = "[E] Completar nivel (prueba)";
        [SerializeField] private string completedPrompt = "Nivel completado";

        public override string Prompt(FloorSession session) =>
            session.Current && session.Progress.State(session.Current.number) == FloorState.Completed ?
                completedPrompt : actionPrompt;
        public override string Interact(FloorPlayer player) => FloorSession.Instance.CompleteCurrent(player);
    }
}
