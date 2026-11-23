using System;
using Hackaton.Progression;
using UnityEngine;

namespace Hackaton.Player
{
    // Runs after locomotion, combat and the held item. Only the local interaction starts this pose.
    [DefaultExecutionOrder(220)]
    public sealed class ElevatorHandPress : MonoBehaviour
    {
        [Serializable]
        private sealed class Hand
        {
            public Transform root, palm, tip;
            public SkinnedMeshRenderer skin;
            public Transform[] joints;
            public Quaternion[] pointing;
            private Quaternion[] rest;
            public Vector3 RestScale { get; private set; }
            public void Capture()
            {
                RestScale = root.localScale;
                rest ??= new Quaternion[joints.Length];
                for (int i = 0; i < joints.Length; i++) rest[i] = joints[i].localRotation;
            }
            public void Fingers(float weight)
            {
                for (int i = 0; i < joints.Length; i++)
                    joints[i].localRotation = Quaternion.Slerp(rest[i], pointing[i], weight);
            }
        }

        [SerializeField] private Hand left;
        [SerializeField] private MeshRenderer wristExtension;
        [SerializeField, Range(.5f, .8f)] private float duration = .68f;
        [Tooltip("Compensación de perspectiva de la mano al alcanzar un botón lejano.")]
        [SerializeField, Range(1f, 4f)] private float maxPerspectiveScale = 2.95f;
        [SerializeField, Range(-180f, 180f)] private float palmRoll = 147f;
        private FloorPlayer player;
        private PlayerView view;
        private PlayerInputReader input;
        private PlayerInventory inventory;
        private PlayerAttack[] attacks;
        private FloorButton button;
        private bool pressing;
        private float elapsed;
        private bool confirmed;
        private void Awake()
        {
            player = GetComponent<FloorPlayer>();
            view = GetComponent<PlayerView>();
            input = GetComponent<PlayerInputReader>();
            inventory = GetComponent<PlayerInventory>();
            attacks = GetComponents<PlayerAttack>();
        }

        internal void Begin(FloorButton target)
        {
            if (pressing || !target.Keycap || inventory && inventory.Eating) return;
            foreach (var attack in attacks) if (attack.isActiveAndEnabled && attack.Swinging) return;
            button = target;
            pressing = true;
            left.Capture();
            wristExtension.sharedMaterial = left.skin.sharedMaterial;
            confirmed = false;
            elapsed = 0;
            input.Reaching = true;
        }

        private static float Ease(float start, float end, float time) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time));

        private void LateUpdate()
        {
            if (!pressing) return;
            if (!button || !button.isActiveAndEnabled || !player.isActiveAndEnabled || !view.isActiveAndEnabled ||
                !input.HasFocus || view.Mode != PlayerView.CameraMode.FirstPerson)
            {
                Release();
                return;
            }
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (t >= 1 && confirmed) { Release(); return; }
            var key = button.Keycap;
            float extension = Ease(0, .47f, t) * (1 - Ease(.66f, 1, t));
            float depth = Ease(.47f, .58f, t) * (1 - Ease(.67f, .81f, t));
            bool allowed = button.TravelBlock == null;
            key.Pose(depth * (allowed ? 1f : .48f));
            // PlayerView restores the locomotion pose of the hand root every frame.
            var wrist = left.root;
            Vector3 from = wrist.position;
            Quaternion idle = wrist.rotation;
            float distance = Vector3.Dot(key.Surface - view.ViewCamera.transform.position, view.ViewCamera.transform.forward);
            float framing = Mathf.Clamp(distance / .76f, 1f, maxPerspectiveScale);
            wrist.localScale = left.RestScale * Mathf.Lerp(1f, framing, extension);
            left.Fingers(extension);
            Vector3 inward = key.Inward;
            Vector3 index = (inward * .68f + key.transform.right * .64f + key.transform.up * .40f).normalized;
            Vector3 palmNormal = Vector3.ProjectOnPlane(-inward, index).normalized;
            Quaternion orientation = Quaternion.AngleAxis(-palmRoll, index) * Quaternion.LookRotation(palmNormal, index);
            Quaternion goal = orientation * Quaternion.Inverse(left.palm.rotation) * idle;
            wrist.rotation = Quaternion.Slerp(idle, goal, extension);
            Vector3 aim = key.Surface - (left.tip.position - wrist.position);
            Vector3 arc = (-key.transform.right * .07f - inward * .09f) * (4 * extension * (1 - extension));
            wrist.position = Vector3.Lerp(from, aim, extension) + arc;

            PoseWrist(extension);
            if (!confirmed && t >= .58f)
            {
                confirmed = true;
                if (player.AimTarget() != button || FloorSession.Instance.Traveling) { Release(); return; }
                key.Click(allowed);
                player.UseTarget(button);
            }
        }

        // Extend the wrist below the first-person framing as the hand reaches into the world.
        private void PoseWrist(float weight)
        {
            wristExtension.enabled = weight > .015f;
            if (!wristExtension.enabled) return;
            Vector3 start = left.palm.TransformPoint(new Vector3(0, .049f, 0));
            var eye = view.ViewCamera;
            Vector3 end = eye.ViewportToWorldPoint(new Vector3(-.06f, -.51f, .49f));
            Vector3 axis = (end - start) * weight;
            var tube = wristExtension.transform;
            tube.SetPositionAndRotation(start, Quaternion.LookRotation(axis, eye.transform.up));
            float radius = left.root.lossyScale.x * Mathf.SmoothStep(0, 1, weight);
            var parentScale = tube.parent.lossyScale;
            tube.localScale = new Vector3(radius / parentScale.x, radius / parentScale.y, axis.magnitude / parentScale.z);
        }

        private void Release()
        {
            if (!pressing) return;
            wristExtension.enabled = false;
            left.Fingers(0);
            left.root.localScale = left.RestScale;
            if (button) button.Keycap.Pose(0);
            pressing = false;
            button = null;
            input.Reaching = false;
            if (view && view.isActiveAndEnabled) view.RestoreSupportHand();
        }
        private void OnDisable() => Release();
    }
}
