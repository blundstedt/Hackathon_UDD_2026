using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hackaton.Player
{
    // Serialized in campaign accounts. Keep existing IDs when extending the palette.
    public enum TizaColor : byte { Beige, Rojo, Naranja, Amarillo, Verde, Azul, Morado, Rosa }

    [Serializable]
    public struct TizaLook : INetworkSerializable, IEquatable<TizaLook>
    {
        public TizaColor color;
        public bool jersey;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref jersey);
        }

        public bool Equals(TizaLook other) => color == other.color && jersey == other.jersey;
    }

    [DisallowMultipleComponent]
    public sealed class TizaAppearance : MonoBehaviour
    {
        private static readonly Color32[] Palette = {
            new(253, 230, 199, 255), new(212, 46, 61, 255), new(248, 121, 40, 255), new(247, 190, 45, 255),
            new(73, 180, 63, 255), new(63, 106, 218, 255), new(158, 71, 207, 255), new(240, 116, 176, 255)
        };
        public static Color Swatch(TizaColor color) => Palette[(uint)color < Palette.Length ? (int)color : 0];

        [SerializeField] private Material skin;
        [SerializeField] private GameObject jersey;
        private readonly List<(Renderer renderer, int index)> slots = new();
        private MaterialPropertyBlock block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        internal void Apply(TizaLook look)
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();
                foreach (var part in GetComponentsInChildren<Renderer>(true))
                {
                    var materials = part.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i] == skin) slots.Add((part, i));
                }
            }
            foreach (var (part, index) in slots)
            {
                part.GetPropertyBlock(block, index);
                block.SetColor(BaseColor, look.color == TizaColor.Beige ? skin.GetColor(BaseColor) : Swatch(look.color));
                part.SetPropertyBlock(block, index);
            }
            jersey.SetActive(look.jersey);
        }
    }
}
