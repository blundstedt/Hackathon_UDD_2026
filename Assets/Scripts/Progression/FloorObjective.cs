namespace Hackaton.Progression
{
    /// <summary>Objetivo de prueba. Un encuentro futuro puede llamar directamente a CompleteCurrent.</summary>
    public sealed class FloorObjective : FloorInteraction
    {
        public override string Prompt(FloorSession session) =>
            session.Current && session.Progress.State(session.Current.number) == FloorState.Completed ?
                "Nivel completado" : "[E] Completar nivel (prueba)";
        public override string Interact(FloorPlayer player) => FloorSession.Instance.CompleteCurrent(player);
    }
}
