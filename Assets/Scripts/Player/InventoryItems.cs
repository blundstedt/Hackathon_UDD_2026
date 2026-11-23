using UnityEngine;

namespace Hackaton.Player
{
    public sealed class InventoryItems : ScriptableObject
    {
        [Min(0)] public int price = 5;
        [Min(1)] public int healing = 25;
        [Range(1f, 1.5f)] public float biteSeconds = 1.2f;
        public GameObject completo;
        public Sprite hands, cane, sling, cup, sandwich, potion;

        public Sprite Primary(PlayerAttack attack) => attack switch
        {
            BastonAttack => cane,
            ResorteraAttack => sling,
            TazaAttack => cup,
            _ => hands
        };
    }
}
