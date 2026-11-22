using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>Identidad y presentación de un objeto compartido por el equipo.</summary>
    [CreateAssetMenu(menuName = "Hackaton/Progresión/Objeto de desbloqueo")]
    public sealed class UnlockItem : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
    }
}
