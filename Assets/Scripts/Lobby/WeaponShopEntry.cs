using Hackaton.Multiplayer;
using Hackaton.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    public sealed class WeaponShopEntry : MonoBehaviour
    {
        [SerializeField] private Button header, equip;
        [SerializeField] private Image icon;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private Text title, itemName, price, action;
        [SerializeField] private CanvasGroup details;
        [SerializeField] private LayoutElement layout;
        [SerializeField] private WeaponPreview preview;
        [SerializeField] private NeonPlate equipPlate;
        [SerializeField] private float headerHeight = 62, detailHeight = 168;

        private WeaponOffer offer;
        private LobbyEquipmentStation station;
        private bool expanded, selected;
        private float fraction;
        private Color normalBorder;

        internal void Bind(WeaponOffer data, LobbyEquipmentStation equipment, int index)
        {
            offer = data;
            station = equipment;
            name = data.id;
            normalBorder = equipPlate.border;
            title.text = itemName.text = data.displayName;
            icon.sprite = data.icon;
            icon.enabled = data.icon;
            price.text = data.price == 0 ? "Gratis" : "$" + data.price;
            preview.Configure(data, index);
            header.onClick.AddListener(() => SetExpanded(!expanded));
            equip.onClick.AddListener(() => station.SelectWeapon(offer.playerPrefab));
        }

        internal void ResetSection()
        {
            SetExpanded(false);
            fraction = 0;
            ApplyHeight();
            ShowEquipment();
        }

        private void SetExpanded(bool show)
        {
            expanded = show;
            details.interactable = show;
            details.blocksRaycasts = show;
            if (show) details.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (offer == null) return;
            float target = expanded ? 1 : 0;
            if (fraction != target)
            {
                fraction = Mathf.MoveTowards(fraction, target, Time.unscaledDeltaTime * 5);
                ApplyHeight();
            }
            if (selected != (station.Equipped == offer.playerPrefab)) ShowEquipment();
        }

        private void ApplyHeight()
        {
            float progress = Mathf.SmoothStep(0, 1, fraction);
            layout.preferredHeight = headerHeight + progress * detailHeight;
            arrow.localRotation = Quaternion.Euler(0, 0, 180 * progress);
            if (fraction == 0) details.gameObject.SetActive(false);
        }

        private void ShowEquipment()
        {
            selected = station.Equipped == offer.playerPrefab;
            action.text = selected ? "Equipado" : offer.CanEquip ? "Equipar" : "No disponible";
            equip.interactable = !selected && offer.CanEquip;
            equipPlate.border = selected ? new Color32(112, 226, 175, 255) : normalBorder;
            equipPlate.SetVerticesDirty();
        }
    }
}
