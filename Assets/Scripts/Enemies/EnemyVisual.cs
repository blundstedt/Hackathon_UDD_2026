using System.Collections;
using UnityEngine;

namespace Hackaton.Enemies
{
    public class EnemyVisual : MonoBehaviour
    {
        [SerializeField, Min(0)] private float proneHeight = .5f;
        [SerializeField, Min(0)] private float fadeDuration = .35f;

        public virtual void LieDown()
        {
            Vector3 position = transform.localPosition;
            position.y = proneHeight;
            transform.localPosition = position;
            transform.localRotation *= Quaternion.Euler(90, 0, 0);
        }

        // The capsule uses a transparent URP material. Custom models can override their presentation.
        public virtual IEnumerator FadeOut()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            int tint = Shader.PropertyToID("_BaseColor");
            var colors = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                var materials = renderers[i].sharedMaterials;
                colors[i] = new Color[materials.Length];
                for (int j = 0; j < materials.Length; j++)
                    if (materials[j] != null && materials[j].HasProperty(tint))
                        colors[i][j] = materials[j].GetColor(tint);
            }

            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                float opacity = 1f - elapsed / fadeDuration;
                for (int i = 0; i < renderers.Length; i++)
                {
                    for (int j = 0; j < colors[i].Length; j++)
                    {
                        Color color = colors[i][j];
                        color.a *= opacity;
                        renderers[i].GetPropertyBlock(block, j);
                        block.SetColor(tint, color);
                        renderers[i].SetPropertyBlock(block, j);
                    }
                }
                yield return null;
            }
        }
    }
}
