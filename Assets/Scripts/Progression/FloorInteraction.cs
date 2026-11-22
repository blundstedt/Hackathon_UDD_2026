using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>Destino del rayo de interacción de la cámara del jugador local.</summary>
    public abstract class FloorInteraction : MonoBehaviour
    {
        public abstract string Prompt(FloorSession session);
        public abstract string Interact(FloorPlayer player);
    }
}
