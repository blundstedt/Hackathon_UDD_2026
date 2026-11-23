using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Multiplayer
{
    [RequireComponent(typeof(RawImage))]
    public sealed class MenuBackground : MonoBehaviour
    {
        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();
        private void Fit()
        {
            var image = GetComponent<RawImage>();
            if (!image || !image.texture) return;
            var size = image.rectTransform.rect.size;
            if (size.y <= 0 || size.x <= 0) return;
            float ratio = size.x / size.y * image.texture.height / image.texture.width;
            float w = Mathf.Min(1, ratio), h = Mathf.Min(1, 1 / ratio);
            image.uvRect = new Rect((1 - w) / 2, (1 - h) / 2, w, h);
        }
    }
}
