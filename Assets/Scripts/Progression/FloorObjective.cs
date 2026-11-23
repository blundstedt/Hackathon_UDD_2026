using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>Canjea la recompensa definida en el catálogo del piso en curso.</summary>
    public sealed class FloorObjective : FloorInteraction
    {
        [SerializeField] private string actionPrompt;
        [SerializeField] private string completedPrompt;

        internal event System.Action AvailabilityChanged;
        private void OnEnable() => AvailabilityChanged?.Invoke();
        private void OnDisable() => AvailabilityChanged?.Invoke();

        public override string Prompt(FloorSession session)
        {
            var floor = session.Current;
            bool completed = floor && session.Progress.State(floor.number) == FloorState.Completed;
            string configured = completed ? completedPrompt : actionPrompt;
            if (!string.IsNullOrEmpty(configured)) return configured;
            if (floor && floor.rewardItem)
                return completed ? "Entrega la llave en recepción · vuelve al lobby con el botón 0" : "[E] Recoger la llave";
            return completed ? "Nivel completado" : "[E] Completar nivel";
        }

        public override string Interact(FloorPlayer player) =>
            isActiveAndEnabled ? FloorSession.Instance.CompleteCurrent(player) : "Objetivo bloqueado";
    }
}
