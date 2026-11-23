using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Hackaton.Lobby
{
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        [Header("Tiempo de juego")]
        [SerializeField] private bool automatic = true;
        [SerializeField] private bool startAtNight;
        [SerializeField, Min(0.1f)] private float dayMinutes = 10f;
        [SerializeField, Min(0.1f)] private float nightMinutes = 5f;

        [Header("Iluminación nocturna")]
        [SerializeField] private Light sun;
        [SerializeField] private Material nightSky;
        [SerializeField] private Volume nightVolume;
        [SerializeField] private Light[] cityLamps = System.Array.Empty<Light>();
        [SerializeField, Min(0f)] private float moonIntensity = 0.32f;
        [SerializeField] private Color moonColor = new(0.59f, 0.72f, 1f);
        [SerializeField] private Color nightAmbientSky = new(0.24f, 0.30f, 0.43f);
        [SerializeField] private Color nightFog = new(0.10f, 0.14f, 0.23f);

        private float elapsedSeconds;
        private Daylight daylight;
        private bool initialized;

        public bool IsNight { get; private set; }
        private bool ControlsActiveScene => initialized && SceneManager.GetActiveScene() == gameObject.scene;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Initialize();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            if (ControlsActiveScene)
            {
                if (IsNight)
                {
                    RestoreDaylight();
                    SetCityLighting(false);
                }
                IsNight = false;
                initialized = false;
            }
        }

        private void OnActiveSceneChanged(Scene previous, Scene active)
        {
            if (active == gameObject.scene)
                Initialize();
        }

        private void Initialize()
        {
            if (initialized || nightSky == null || sun == null || SceneManager.GetActiveScene() != gameObject.scene)
                return;

            daylight = new Daylight(sun);
            initialized = true;
            SetNight(startAtNight);
        }

        private void Update()
        {
            if (!ControlsActiveScene || !automatic ||
                (Hackaton.Multiplayer.CampaignRoom.Current && !Hackaton.Multiplayer.CampaignRoom.Current.IsAuthority)) return;

            elapsedSeconds += Time.deltaTime;
            float phaseSeconds = 60f * Mathf.Max(0.1f, IsNight ? nightMinutes : dayMinutes);
            if (elapsedSeconds >= phaseSeconds)
                AdvancePhase();
        }

        public void AdvancePhase() => SetNight(!IsNight);

        public void SetNight(bool night)
        {
            if (!ControlsActiveScene) return;

            elapsedSeconds = 0f;
            SetCityLighting(night);
            if (IsNight == night) return;

            IsNight = night;
            if (night)
            {
                RenderSettings.skybox = nightSky;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = nightAmbientSky;
                RenderSettings.ambientEquatorColor = nightAmbientSky * 0.65f;
                RenderSettings.ambientGroundColor = nightAmbientSky * 0.37f;
                RenderSettings.reflectionIntensity = daylight.ReflectionIntensity * 0.35f;
                RenderSettings.fogColor = nightFog;
                sun.color = moonColor;
                sun.intensity = moonIntensity;
            }
            else
            {
                RestoreDaylight();
            }
        }

        private void SetCityLighting(bool night)
        {
            if (nightVolume != null) nightVolume.weight = night ? 1f : 0f;
            foreach (var lamp in cityLamps)
                if (lamp != null) lamp.enabled = night;
        }

        private void RestoreDaylight()
        {
            RenderSettings.skybox = daylight.Skybox;
            RenderSettings.ambientMode = daylight.AmbientMode;
            RenderSettings.ambientSkyColor = daylight.Sky;
            RenderSettings.ambientEquatorColor = daylight.Equator;
            RenderSettings.ambientGroundColor = daylight.Ground;
            RenderSettings.fogColor = daylight.Fog;
            RenderSettings.reflectionIntensity = daylight.ReflectionIntensity;
            sun.color = daylight.SunColor;
            sun.intensity = daylight.SunIntensity;
        }

        private readonly struct Daylight
        {
            public readonly Material Skybox;
            public readonly AmbientMode AmbientMode;
            public readonly Color Sky, Equator, Ground, Fog, SunColor;
            public readonly float ReflectionIntensity, SunIntensity;

            public Daylight(Light sun)
            {
                Skybox = RenderSettings.skybox;
                AmbientMode = RenderSettings.ambientMode;
                Sky = RenderSettings.ambientSkyColor;
                Equator = RenderSettings.ambientEquatorColor;
                Ground = RenderSettings.ambientGroundColor;
                Fog = RenderSettings.fogColor;
                ReflectionIntensity = RenderSettings.reflectionIntensity;
                SunColor = sun.color;
                SunIntensity = sun.intensity;
            }
        }
    }
}
