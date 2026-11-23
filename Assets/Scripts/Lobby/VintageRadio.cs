using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class VintageRadio : FloorInteraction
    {
        private AudioSource source;

        private void Awake() => source = GetComponent<AudioSource>();

        public override string Prompt(FloorSession session) =>
            source.isPlaying ? null : "E - Reproducir radio";

        public override string Interact(FloorPlayer player)
        {
            if (!source.isPlaying) source.Play();
            return null;
        }
    }
}
