using UnityEngine;

namespace Hackaton.Multiplayer
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class LocalAudioLevel : MonoBehaviour
    {
        [SerializeField] private bool music;
        private AudioSource source;
        private float authoredVolume;
        private void Awake()
        {
            source = GetComponent<AudioSource>();
            authoredVolume = source.volume;
        }
        private void OnEnable()
        {
            LocalGameSettings.Changed += Apply;
            Apply();
        }
        private void OnDisable() => LocalGameSettings.Changed -= Apply;
        private void Apply() => source.volume = authoredVolume * LocalGameSettings.Volume(
            music ? LocalGameSettings.Setting.Music : LocalGameSettings.Setting.Effects);
    }
}
