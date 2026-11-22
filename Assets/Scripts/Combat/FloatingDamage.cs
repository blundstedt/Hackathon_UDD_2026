using UnityEngine;

namespace Hackaton.Combat
{
    public sealed class FloatingDamage : MonoBehaviour
    {
        private float verticalSpeed = .75f;
        private float gravity;

        internal static void Show(Vector3 point, int damage, Font font)
        {
            CreateLabel(point, $"-{damage}", Color.red, font, .85f);
        }

        internal static void ShowReward(Vector3 point, string value, Font font)
        {
            var label = CreateLabel(point, value, Color.white, font, 1.35f);
            label.verticalSpeed = 2.6f;
            label.gravity = 3.2f;
        }

        private static FloatingDamage CreateLabel(Vector3 point, string value, Color color, Font font, float duration)
        {
            var label = new GameObject(value, typeof(TextMesh), typeof(FloatingDamage));
            label.transform.position = point + Vector3.up * .15f;
            var text = label.GetComponent<TextMesh>();
            text.font = font;
            text.richText = true;
            text.fontSize = 64;
            text.characterSize = .045f;
            text.anchor = TextAnchor.LowerCenter;
            text.color = color;
            text.alignment = TextAlignment.Center;
            text.text = value;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            Destroy(label, duration);
            return label.GetComponent<FloatingDamage>();
        }

        private void LateUpdate()
        {
            transform.position += Vector3.up * (verticalSpeed * Time.deltaTime);
            verticalSpeed -= gravity * Time.deltaTime;
            if (Camera.main is Camera camera) transform.rotation = camera.transform.rotation;
        }
    }
}
