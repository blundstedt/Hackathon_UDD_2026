using UnityEngine;

namespace Hackaton.Combat
{
    public sealed class FloatingDamage : MonoBehaviour
    {
        internal static void Show(Vector3 point, int damage, Font font)
        {
            var label = new GameObject("Daño", typeof(TextMesh), typeof(FloatingDamage));
            label.transform.position = point + Vector3.up * .15f;
            var text = label.GetComponent<TextMesh>();
            text.font = font;
            text.fontSize = 64;
            text.characterSize = .045f;
            text.anchor = TextAnchor.LowerCenter;
            text.color = Color.red;
            text.text = $"-{damage}";
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            Destroy(label, .85f);
        }

        private void LateUpdate()
        {
            transform.position += Vector3.up * (.75f * Time.deltaTime);
            if (Camera.main is Camera camera) transform.rotation = camera.transform.rotation;
        }
    }
}
