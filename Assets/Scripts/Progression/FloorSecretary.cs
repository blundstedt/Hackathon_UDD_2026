namespace Hackaton.Progression
{
    /// <summary>El collider de la recepcionista entrega una llave al registro compartido.</summary>
    public sealed class FloorSecretary : FloorInteraction
    {
        public override string Prompt(FloorSession session) => session.Progress.DeliveryPrompt();
        public override string Interact(FloorPlayer player) => FloorSession.Instance.Deliver(player);
    }
}
