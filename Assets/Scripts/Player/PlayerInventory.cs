using Hackaton.Multiplayer;
using Hackaton.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hackaton.Player
{
    // The authored grip contains only the primary weapon. Hands and their camera poses stay intact.
    [DefaultExecutionOrder(150)]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private InventoryItems items;
        [SerializeField] private Transform weaponGrip;
        [SerializeField] private Vector3 foodPosition, foodRotation;
        [SerializeField] private float foodScale = .76f;
        [SerializeField] private GripJoint[] foodGrip = System.Array.Empty<GripJoint>();
        private bool gripping;

        [System.Serializable]
        private sealed class GripJoint
        {
            public Transform bone;
            public Vector3 position, angles;
            private Vector3 restPosition;
            private Quaternion restRotation;

            public void Hold()
            {
                restPosition = bone.localPosition;
                restRotation = bone.localRotation;
                bone.SetLocalPositionAndRotation(position, Quaternion.Euler(angles));
            }

            public void Release() => bone.SetLocalPositionAndRotation(restPosition, restRotation);
        }

        private void SetGrip(bool hold)
        {
            if (gripping == hold) return;
            gripping = hold;
            foreach (var joint in foodGrip)
            {
                if (!joint.bone) continue;
                if (hold) joint.Hold();
                else joint.Release();
            }
        }

        private PlayerStats stats;
        private PlayerView view;
        private NetworkTiza peer;
        private GameObject held;
        private bool gripInitiallyVisible;
        private int selected = 1;
        private double localEnd;
        public int Selected => peer && !peer.IsOwner ? peer.ActiveSlot : selected;
        internal bool Eating => peer ? peer.BiteRemaining > 0 : localEnd > 0;
        internal InventoryItems Items => items;
        internal bool CanEat => stats.Completos > 0 && stats.Health > 0 && stats.Health < PlayerStats.MaxHealth;
        internal bool Traveling => FloorSession.Instance && FloorSession.Instance.Traveling;

        private void Awake()
        {
            gripInitiallyVisible = weaponGrip.gameObject.activeSelf;
            stats = GetComponent<PlayerStats>();
            view = GetComponent<PlayerView>();
            if (TryGetComponent<TizaBody>(out var body)) peer = body.Peer;
        }

        internal void ReadInput(PlayerInputReader input)
        {
            var kb = Keyboard.current;
            if (kb == null || !input.HasControl) return;
            if (kb.digit1Key.wasPressedThisFrame) Select(1);
            else if (kb.digit2Key.wasPressedThisFrame) Select(2);
            else if (kb.digit3Key.wasPressedThisFrame) Select(3);
            if (Selected == 2 && input.AttackPressed) Use();
        }

        internal void Select(int slot)
        {
            if (slot < 1 || slot > 3 || slot == selected || Traveling) return;
            selected = slot;
            localEnd = 0;
            if (peer) peer.SelectSlotRpc(slot);
        }

        internal void Use()
        {
            if (Selected != 2 || Traveling || !CanEat) return;
            if (peer) peer.EatRpc();
            else if (localEnd == 0) localEnd = Time.timeAsDouble + items.biteSeconds;
        }

        private void Update()
        {
            if (peer || localEnd == 0) return;
            if (Traveling || !CanEat) { localEnd = 0; return; }
            if (Time.timeAsDouble < localEnd) return;
            localEnd = 0;
            stats.EatCompleto(items.healing);
        }

        private void LateUpdate()
        {
            int slot = Selected;
            bool showGrip = slot == 1 && gripInitiallyVisible;
            if (weaponGrip.gameObject.activeSelf != showGrip) weaponGrip.gameObject.SetActive(showGrip);
            bool stocked = peer && !peer.IsOwner ? peer.HasCompleto : stats.Completos > 0;
            bool show = slot == 2 && stocked;
            SetGrip(show);
            if (show && !held)
            {
                held = Instantiate(items.completo, view.RightHand);
                held.transform.SetLocalPositionAndRotation(foodPosition, Quaternion.Euler(foodRotation));
                held.transform.localScale = Vector3.one * foodScale;
            }
            if (held && held.activeSelf != show) held.SetActive(show);
            if (!show) return;
            double left = peer ? peer.BiteRemaining : localEnd - Time.timeAsDouble;
            float t = left > 0 ? Mathf.Clamp01(1f - (float)left / items.biteSeconds) : 0f;
            // Finish the visible bite even when another unit will be equipped from the stack.
            float portion = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, .91f, t));
            held.transform.localScale = Vector3.one * (foodScale * portion);
            if (left <= 0 || !view.isActiveAndEnabled) return;
            float lift = Mathf.SmoothStep(0, 1, Mathf.Min(t / .38f, (1f - t) / .38f));
            float nibble = Mathf.Sin(t * Mathf.PI * 5f) * .024f * lift;
            var frame = view.HandFrame;
            view.RightHand.position += frame.TransformVector(new Vector3(-.30f, .24f + nibble, -.24f) * lift);
            view.RightHand.rotation = Quaternion.AngleAxis(18f * lift, frame.forward) * view.RightHand.rotation;
        }

        private void OnDisable()
        {
            localEnd = 0;
            SetGrip(false);
        }
    }
}
