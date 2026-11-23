using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Multiplayer
{
    // A local instance belongs to one FloorSession. Network ownership and spawning remain independent.
    public sealed class CampaignIntro : MonoBehaviour
    {
        [Serializable]
        private struct Page
        {
            public Sprite image;
            public AudioClip narration;
            [TextArea] public string subtitle;
        }

        [SerializeField] private Page[] pages;
        [SerializeField] private Canvas overlay;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image current, incoming;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private GameObject captionBar;
        [SerializeField] private Button skip;
        [SerializeField] private AudioSource voice, music;
        private const float BlendSeconds = .4f, EndSeconds = .45f, MusicGain = .22f;
        private bool confirmed, started, ending;
        private CampaignRoom room;
        private readonly List<Canvas> hiddenHud = new();

        public bool Blocking { get; private set; } = true;

        public void ConfirmEntry() => confirmed = true;
        public void Cover()
        {
            overlay.enabled = true;
            foreach (var canvas in FindObjectsByType<Canvas>())
                if (canvas.renderMode != RenderMode.WorldSpace && !canvas.GetComponentInParent<SessionMenu>())
                    HideHud(canvas);
        }

        internal void HideHud(Canvas canvas)
        {
            if (!Blocking || !canvas || canvas == overlay || !canvas.enabled) return;
            hiddenHud.Add(canvas);
            canvas.enabled = false;
        }

        private void OnDisable()
        {
            foreach (var canvas in hiddenHud)
                if (canvas) canvas.enabled = true;
            hiddenHud.Clear();
        }

        private void Awake()
        {
            room = CampaignRoom.Current;
            overlay.enabled = false;
            skip.onClick.AddListener(Skip);
        }

        private void Update()
        {
            if (!confirmed || started || ending || !overlay.enabled) return;
            if (!room.LocalAvatarReady) return;
            started = true;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            music.volume = MusicGain * LocalGameSettings.Volume(LocalGameSettings.Setting.Music);
            music.Play();
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                incoming.sprite = page.image;
                incoming.enabled = true;
                incoming.color = new Color(1, 1, 1, 0);
                float elapsed = 0;
                while (elapsed < BlendSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    incoming.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, elapsed / BlendSeconds));
                    yield return null;
                }
                current.sprite = page.image;
                current.enabled = true;
                incoming.enabled = false;
                subtitle.text = page.subtitle;
                captionBar.SetActive(true);
                voice.clip = page.narration;
                voice.volume = LocalGameSettings.Volume(LocalGameSettings.Setting.Effects);
                voice.Play();
                while (voice.isPlaying)
                {
                    if (i == pages.Length - 1)
                    {
                        float left = (voice.clip.samples - voice.timeSamples) / (float)voice.clip.frequency;
                        music.volume = MusicGain * LocalGameSettings.Volume(LocalGameSettings.Setting.Music)
                            * Mathf.Clamp01((left + EndSeconds) / 2f);
                    }
                    yield return null;
                }
                captionBar.SetActive(false);
            }
            yield return Finish();
        }

        public void Skip()
        {
            if (ending || !confirmed || !started) return;
            StopAllCoroutines();
            voice.Stop();
            StartCoroutine(Finish());
        }

        private IEnumerator Finish()
        {
            ending = true;
            skip.interactable = false;
            float initial = music.volume;
            for (float elapsed = 0; elapsed < EndSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float gain = 1f - Mathf.SmoothStep(0, 1, elapsed / EndSeconds);
                group.alpha = gain;
                music.volume = initial * gain;
                yield return null;
            }
            Cancel();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Cancel()
        {
            Blocking = false;
            gameObject.SetActive(false);
            voice.Stop();
            music.Stop();
            Destroy(gameObject);
        }
    }
}
