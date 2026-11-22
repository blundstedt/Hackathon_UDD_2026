using Hackaton.Combat;
using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Enemies
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Font damageFont;
        [SerializeField, Min(.1f)] private float visibleDuration = 3f;
        private float hideAt;

        internal void ShowHit(Vector3 point, int damage, float healthFraction)
        {
            fill.anchorMax = new Vector2(healthFraction, 1f);
            FloatingDamage.Show(point, damage, damageFont);
            hideAt = Time.time + visibleDuration;
            gameObject.SetActive(healthFraction > 0f);
        }

        internal void ShowReward(Vector3 point, int money, int experience, string prefix)
        {
            string green = ColorUtility.ToHtmlStringRGB(PlayerStatsHud.MoneyColor);
            string violet = ColorUtility.ToHtmlStringRGB(PlayerStatsHud.LevelColor);
            FloatingDamage.ShowReward(point, $"<color=#{green}>{prefix}${money}</color>\n" +
                $"<color=#{violet}>+{experience} EXP</color>", damageFont);
        }

        private void LateUpdate()
        {
            if (Time.time >= hideAt)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Camera.main is Camera camera) transform.rotation = camera.transform.rotation;
        }
    }
}
