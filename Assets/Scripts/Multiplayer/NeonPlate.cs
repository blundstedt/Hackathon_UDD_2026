using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Multiplayer
{
    // Two nested octagons keep the bevel sharp at any canvas resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NeonPlate : MaskableGraphic
    {
        public Color border = new Color32(183, 102, 181, 255);
        public float corner = 10;
        public float stroke = 2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Ring(vh, rect, corner, border);
            rect.xMin += stroke; rect.xMax -= stroke;
            rect.yMin += stroke; rect.yMax -= stroke;
            Ring(vh, rect, Mathf.Max(0, corner - stroke / 2), color);
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                vh.AddTriangle(i, next, i + 8);
                vh.AddTriangle(next, next + 8, i + 8);
            }
            for (int i = 1; i < 7; i++) vh.AddTriangle(8, 8 + i, 9 + i);
        }
        private static void Ring(VertexHelper vh, Rect r, float cut, Color tint)
        {
            float c = Mathf.Min(cut, Mathf.Min(r.width, r.height) * .25f);
            float l = r.xMin, b = r.yMin, t = r.yMax, h = r.xMax;
            foreach (var p in new[] {new Vector2(l, b+c),new Vector2(l,t-c),new Vector2(l+c,t),new Vector2(h-c,t),
                new Vector2(h,t-c),new Vector2(h,b+c),new Vector2(h-c,b),new Vector2(l+c,b)})
                vh.AddVert(p, tint, Vector2.zero);
        }
    }
}
