using System;
using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Lobby
{
    [Serializable]
    public sealed class WeaponOffer
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public GameObject previewModel;
        public Material previewMaterial;
        public float previewYaw;
        public PlayerView playerPrefab;
        [Min(0)] public int price;
        public bool unlocked = true;

        public bool CanEquip => unlocked && price == 0 && playerPrefab;
    }

    [CreateAssetMenu(menuName = "Lobby/Catálogo de armas")]
    public sealed class WeaponCatalog : ScriptableObject
    {
        public WeaponOffer[] weapons = Array.Empty<WeaponOffer>();
        public WeaponOffer Find(PlayerView prefab) => Array.Find(weapons, w => w.playerPrefab == prefab);
    }
}

