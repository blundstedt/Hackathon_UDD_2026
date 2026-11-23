using Hackaton.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hackaton.Lobby
{
    public sealed class WeaponShopMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private WeaponShopEntry entryPrefab;
        [SerializeField] private Button close;

        private static WeaponShopMenu active;
        private static int closedFrame = -1;
        public static bool CapturesInput => active || closedFrame == Time.frameCount;

        private LobbyEquipmentStation station;
        private WeaponShopEntry[] entries;
        private GameObject fallbackEvents;
        private CursorLockMode previousLock;
        private bool previousVisibility;

        public void Open(LobbyEquipmentStation equipment)
        {
            if (active || !equipment.InRange(equipment.LocalPlayer)) return;
            station = equipment;
            previousLock = Cursor.lockState;
            previousVisibility = Cursor.visible;
            active = this;
            gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (!EventSystem.current && !fallbackEvents)
            {
                // Direct lobby Play Mode has no persistent event system from MenuPrincipal.
                fallbackEvents = new GameObject("Eventos de tienda", typeof(EventSystem), typeof(InputSystemUIInputModule));
                fallbackEvents.transform.SetParent(transform, false);
            }
            if (entries == null)
            {
                close.onClick.AddListener(Close);
                var offers = equipment.Catalog.weapons;
                entries = new WeaponShopEntry[offers.Length];
                for (int i = 0; i < entries.Length; i++)
                {
                    entries[i] = Instantiate(entryPrefab, scroll.content);
                    entries[i].Bind(offers[i], equipment, i);
                }
            }
            foreach (var entry in entries) entry.ResetSection();
            Resize();
            Canvas.ForceUpdateCanvases();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }

        private void Resize()
        {
            var size = ((RectTransform)panel.parent).rect.size;
            float height = Mathf.Clamp(scroll.content.rect.height + 222f, 420f, 660f);
            panel.sizeDelta = Vector2.Min(new Vector2(700, height), size - new Vector2(32, 36));
        }

        private void Update()
        {
            if (active != this) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                !station.InRange(station.LocalPlayer) || !FloorSession.Instance ||
                FloorSession.Instance.Traveling)
            {
                Close();
                return;
            }
            Resize();
        }

        public void Close() => gameObject.SetActive(false);

        private void OnDisable()
        {
            if (active != this) return;
            active = null;
            closedFrame = Time.frameCount;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisibility;
        }
    }
}
