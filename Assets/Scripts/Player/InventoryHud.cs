using Hackaton.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Player
{
    internal sealed class InventoryHud
    {
        private readonly NeonPlate[] plates = new NeonPlate[3];
        private readonly Image[] icons = new Image[3];
        private readonly Text count;
        private int previousSlot = -1, previousCount = -1;
        private static readonly Color Pink = new Color32(255, 108, 215, 255);
        private static readonly Color Violet = new Color32(176, 111, 205, 255);

        internal InventoryHud(Transform canvas, Font font, InventoryItems items, PlayerAttack weapon)
        {
            var sprites = new[] { items.Primary(weapon), items.sandwich, items.potion };
            const float tileSize = 59, spacing = 7;
            float width = plates.Length * tileSize + (plates.Length - 1) * spacing;
            var group = Rect(canvas, "Inventario", 0, 0, width, 79);
            group.anchorMin = group.anchorMax = group.pivot = Vector2.right;
            group.anchoredPosition = new Vector2(-22, 21);
            for (int i = 0; i < plates.Length; i++)
            {
                var tile = Rect(group, "Casilla " + (i + 1), i * (tileSize + spacing), 0, tileSize, tileSize);
                plates[i] = Plate(tile, 7);
                var tab = Rect(tile, "Tecla", 15, 57, 29, 21);
                Plate(tab, 4);
                Label(tab, font, (i + 1).ToString(), 18);
                var icon = Rect(tile, "Objeto", 7, 7, 45, 45).gameObject.AddComponent<Image>();
                icon.sprite = sprites[i];
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icons[i] = icon;
            }
            count = Label(Rect(plates[1].transform, "Cantidad", 1, 0, 53, 21), font, "", 18);
            count.alignment = TextAnchor.MiddleRight;
            count.horizontalOverflow = HorizontalWrapMode.Wrap;
            count.resizeTextForBestFit = true;
            count.resizeTextMinSize = 9;
            count.resizeTextMaxSize = 18;
        }

        internal void Show(int slot, int units)
        {
            if (slot == previousSlot && units == previousCount) return;
            previousSlot = slot;
            previousCount = units;
            for (int i = 0; i < plates.Length; i++)
            {
                bool selected = slot == i + 1;
                plates[i].border = selected ? Pink : Violet;
                plates[i].stroke = selected ? 3.5f : 2f;
                plates[i].color = selected ? new Color32(77, 27, 79, 242) : new Color32(28, 16, 41, 226);
                plates[i].SetVerticesDirty();
                bool empty = i == 2 || i == 1 && units == 0;
                icons[i].color = empty ? new Color32(162, 122, 182, 130) : new Color32(255, 220, 255, 255);
            }
            count.text = units == 0 ? "" : units.ToString();
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static NeonPlate Plate(RectTransform parent, float corner)
        {
            var plate = parent.gameObject.AddComponent<NeonPlate>();
            plate.corner = corner;
            plate.color = new Color32(38, 18, 44, 235);
            plate.border = Violet;
            plate.raycastTarget = false;
            return plate;
        }

        private static Text Label(RectTransform parent, Font font, string value, int size)
        {
            var rect = Rect(parent, "Texto", 0, 0, parent.rect.width, parent.rect.height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.08f, .025f, .12f, .95f);
            return text;
        }
    }
}
