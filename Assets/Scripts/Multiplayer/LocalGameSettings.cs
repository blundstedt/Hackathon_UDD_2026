using System;
using System.Linq;
using UnityEngine;

namespace Hackaton.Multiplayer
{
    public static class LocalGameSettings
    {
        private const string Prefix = "tiza.options.";
        public enum Setting { Master, Music, Effects, Resolution, Fullscreen, Quality, VSync, Sensitivity }
        public static event Action Changed;
        public static float Sensitivity => Read(Setting.Sensitivity, 10) / 10f;
        public static float Volume(Setting kind) => Read(kind, 10) / 10f;
        private static int Read(Setting key, int fallback) => PlayerPrefs.GetInt(Prefix + key, fallback);
        private static Vector2Int[] Sizes => Screen.resolutions.Select(r => new Vector2Int(r.width, r.height))
            .Append(new Vector2Int(Screen.width, Screen.height)).Distinct().OrderBy(r => r.x).ThenBy(r => r.y).ToArray();
        private static Vector2Int SelectedSize => new(PlayerPrefs.GetInt(Prefix + "width", Screen.width),
            PlayerPrefs.GetInt(Prefix + "height", Screen.height));

        public static string Label(Setting kind)
        {
            switch (kind)
            {
                case Setting.Resolution: return $"{SelectedSize.x} × {SelectedSize.y}";
                case Setting.Fullscreen: return Read(kind, Screen.fullScreen ? 1 : 0) == 1 ? "Completa" : "Ventana";
                case Setting.Quality: return QualitySettings.names[QualitySettings.GetQualityLevel()];
                case Setting.VSync: return QualitySettings.vSyncCount > 0 ? "Activado" : "Desactivado";
                case Setting.Sensitivity: return Sensitivity.ToString("0.0") + "×";
                default: return Mathf.RoundToInt(Volume(kind) * 100) + "%";
            }
        }

        public static void Step(Setting kind, int direction)
        {
            if (kind == Setting.Resolution)
            {
                var sizes = Sizes;
                var size = sizes[Mathf.Clamp(Array.IndexOf(sizes, SelectedSize) + direction, 0, sizes.Length - 1)];
                PlayerPrefs.SetInt(Prefix + "width", size.x);
                PlayerPrefs.SetInt(Prefix + "height", size.y);
            }
            else
            {
                int value = kind switch
                {
                    Setting.Quality => Mathf.Clamp(QualitySettings.GetQualityLevel() + direction, 0, QualitySettings.names.Length - 1),
                    Setting.Fullscreen => 1 - Read(kind, Screen.fullScreen ? 1 : 0),
                    Setting.VSync => QualitySettings.vSyncCount > 0 ? 0 : 1,
                    Setting.Sensitivity => Mathf.Clamp(Read(kind, 10) + direction, 1, 40),
                    _ => Mathf.Clamp(Read(kind, 10) + direction, 0, 10)
                };
                PlayerPrefs.SetInt(Prefix + kind, value);
            }
            Apply(kind == Setting.Fullscreen || kind == Setting.Resolution);
            PlayerPrefs.Save();
        }

        public static void Apply(bool display)
        {
            int quality = Mathf.Clamp(Read(Setting.Quality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            if (quality != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(quality);
            QualitySettings.vSyncCount = Read(Setting.VSync, QualitySettings.vSyncCount);
            AudioListener.volume = Volume(Setting.Master);
            if (display && (PlayerPrefs.HasKey(Prefix + "width") || PlayerPrefs.HasKey(Prefix + Setting.Fullscreen)))
                Screen.SetResolution(SelectedSize.x, SelectedSize.y,
                    Read(Setting.Fullscreen, Screen.fullScreen ? 1 : 0) == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            Changed?.Invoke();
        }
    }
}
