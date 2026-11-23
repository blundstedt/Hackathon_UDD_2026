using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatsHud : MonoBehaviour
    {
        internal static readonly Color LevelColor = new Color32(207, 166, 255, 255);
        internal static readonly Color MoneyColor = new Color32(113, 236, 151, 255);
        private static readonly Color Red = new Color32(201, 53, 67, 255);
        private static readonly Color Track = new Color32(32, 26, 40, 225);
        private PlayerStats stats;
        [SerializeField] private Font hudFont;
        private Text level, money;
        private PlayerAttack attack;
        private RectTransform cooldownFill;
        private RectTransform healthFill, xpFill;

        private void Awake()
        {
            if (!enabled) return;
            stats = GetComponent<PlayerStats>();
            foreach (var candidate in GetComponents<PlayerAttack>())
                if (candidate.enabled) attack = candidate;
            var canvas = new GameObject("Atributos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(transform, false);
            var hudCanvas = canvas.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var room = Hackaton.Multiplayer.CampaignRoom.Current;
            if (room) room.HideHudDuringIntro(hudCanvas);
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            var summary = Panel(canvas.transform, "Resumen", Vector2.one, new Vector2(-12, -8), new Vector2(196, 57));
            level = Label(summary, "Nivel", 27, LevelColor, 0, 34);
            money = Label(summary, "Dinero", 23, MoneyColor, 27, 30);

            var health = Panel(canvas.transform, "Salud", Vector2.zero, new Vector2(22, 48), new Vector2(320, 24));
            var xp = Panel(canvas.transform, "Experiencia", Vector2.zero, new Vector2(22, 22), new Vector2(320, 20));
            healthFill = Bar(health, Red);
            xpFill = Bar(xp, LevelColor);
            if (attack != null)
            {
                var track = Panel(canvas.transform, "Recarga", Vector2.one * .5f, new Vector2(0, -16), new Vector2(34, 3));
                cooldownFill = Bar(track, Color.white);
            }
        }

        private void OnEnable()
        {
            stats.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => stats.Changed -= Refresh;

        private void LateUpdate()
        {
            if (cooldownFill == null) return;
            float remaining = attack.RemainingFraction;
            cooldownFill.parent.gameObject.SetActive(remaining > 0);
            cooldownFill.anchorMax = new Vector2(remaining, 1f);
        }

        private void Refresh()
        {
            level.text = stats.Level.ToString();
            money.text = "$" + stats.Money;
            bool max = stats.Level == PlayerStats.MaxLevel;
            healthFill.anchorMax = new Vector2((float)stats.Health / PlayerStats.MaxHealth, 1);
            xpFill.anchorMax = new Vector2(max ? 1 : (float)stats.Experience / stats.ExperienceToNextLevel, 1);
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Child(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Bar(RectTransform parent, Color color)
        {
            var background = parent.gameObject.AddComponent<Image>();
            background.color = Track;
            background.raycastTarget = false;
            var fill = Child(parent, "Relleno");
            var image = fill.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return fill;
        }

        private Text Label(RectTransform parent, string name, int size, Color color, float top, float height)
        {
            var rect = Panel(parent, name, Vector2.one, new Vector2(0, -top), new Vector2(parent.rect.width, height));
            var label = rect.gameObject.AddComponent<Text>();
            label.font = hudFont;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleRight;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = color;
            label.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            return label;
        }
    }
}
