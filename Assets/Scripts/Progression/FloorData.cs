using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>El número identifica al piso. El sucesor referencia al anterior como requisito.</summary>
    [CreateAssetMenu(menuName = "Hackaton/Progresión/Piso")]
    public sealed class FloorData : ScriptableObject
    {
        [Min(0)] public int number;
        public string displayName;
        [Tooltip("Nombre único de una escena habilitada en la compilación. Vacío para un piso futuro.")]
        public string sceneName;
        public bool initiallyAvailable;
        public FloorData prerequisite;
        public UnlockItem requiredItem;
        [Min(0)] public int experience;
        [Min(0)] public int money;
        public UnlockItem rewardItem;
    }
}
