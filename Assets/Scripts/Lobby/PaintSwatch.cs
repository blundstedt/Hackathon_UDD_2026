using Hackaton.Player;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PaintSwatch : FloorInteraction
    {
        [SerializeField] private TizaColor color;
        private Vector3 restPosition;
        private float press;

        private void OnEnable()
        {
            restPosition = transform.localPosition;
            var block = new MaterialPropertyBlock();
            var paint = GetComponent<Renderer>();
            paint.GetPropertyBlock(block);
            block.SetColor("_BaseColor", TizaAppearance.Swatch(color));
            paint.SetPropertyBlock(block);
        }

        public override string Prompt(FloorSession session) => $"E - Cambiar a {color}";

        public override string Interact(FloorPlayer player)
        {
            var look = player.Look;
            look.color = color;
            player.ChangeLook(look);
            return null;
        }

        // Depress only this viewer's selected paint. The individual trigger follows the paint.
        private void Update()
        {
            if (!Application.IsPlaying(gameObject)) return;
            var session = FloorSession.Instance;
            if (!session) return;
            var peer = Hackaton.Multiplayer.NetworkTiza.Local;
            var selected = peer && peer.Player ? peer.Player.Look.color : session.Look("local").color;
            float target = selected == color ? .018f : 0f;
            if (press == target) return;
            press = Mathf.MoveTowards(press, target, Time.deltaTime * .15f);
            transform.localPosition = restPosition + Vector3.forward * press;
        }
    }
}
