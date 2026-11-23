using UnityEngine;

namespace Hackaton.Progression
{
    // The key's origin is its front surface. Local +Z points into the wall.
    public sealed class ElevatorKeycap : MonoBehaviour
    {
        [SerializeField] private Transform cap;
        [SerializeField] private Renderer legend;
        [SerializeField] private Renderer rim;
        [SerializeField] private Material lit, dark, darkRim;
        [SerializeField] private Color flash;
        [SerializeField, Range(.005f, .015f)] private float stroke = .009f;
        [SerializeField] private AudioSource click;
        private MaterialPropertyBlock properties;
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private bool available;
        public Vector3 Surface => cap.position;
        public Vector3 Inward => transform.forward;
        public bool Illuminated => available;
        public void LightKey(bool active)
        {
            available = active;
            legend.sharedMaterial = active ? lit : dark;
            rim.sharedMaterial = active ? lit : darkRim;
            Pose(0);
        }
        internal void Pose(float press)
        {
            cap.localPosition = Vector3.forward * (stroke * press);
            if (available && press > 0)
            {
                properties ??= new MaterialPropertyBlock();
                properties.SetColor(Emission, Color.Lerp(lit.GetColor(Emission), flash, press));
                legend.SetPropertyBlock(properties);
                rim.SetPropertyBlock(properties);
            }
            else
            {
                legend.SetPropertyBlock(null);
                rim.SetPropertyBlock(null);
            }
        }
        internal void Click(bool accepted)
        {
            click.pitch = accepted ? 1f : .80f;
            click.PlayOneShot(click.clip, accepted ? 1f : .65f);
        }
    }
}
