using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Hackaton.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform characterVisual;
        [SerializeField] private GameObject firstPersonVisual;
        [SerializeField] private Transform sharedFirstPersonHand;
        [SerializeField] private Transform firstPersonHandPose;
        [SerializeField] private Transform sharedFirstPersonSupportHand;
        [SerializeField] private Transform firstPersonSupportHandPose;
        [SerializeField, Min(0.5f)] private float followDistance = 3.6f;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.1f;
        [SerializeField] private LayerMask obstructionLayers = Physics.DefaultRaycastLayers;

        private const float CollisionRadius = 0.18f;
        private const float WallClearance = 0.06f;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[16];
        private PlayerInputReader input;
        private Renderer[] characterRenderers;
        private ShadowCastingMode[] normalShadowModes;
        private Vector3 followPosition;
        private Vector3 followVelocity;
        private bool thirdPerson;
        private SharedHandPose mainHand;
        private SharedHandPose supportHand;

        private readonly struct SharedHandPose
        {
            private readonly Transform hand;
            private readonly Transform cameraPose;
            private readonly Vector3 restPosition;
            private readonly Quaternion restRotation;

            public SharedHandPose(Transform hand, Transform cameraPose)
            {
                this.hand = hand;
                this.cameraPose = cameraPose;
                restPosition = hand != null ? hand.localPosition : Vector3.zero;
                restRotation = hand != null ? hand.localRotation : Quaternion.identity;
            }

            private bool IsConfigured => hand != null && cameraPose != null;

            public bool Contains(Transform target) => IsConfigured && target.IsChildOf(hand);

            public void FollowCamera()
            {
                if (IsConfigured)
                    hand.SetPositionAndRotation(cameraPose.position, cameraPose.rotation);
            }

            public void Restore()
            {
                if (IsConfigured)
                    hand.SetLocalPositionAndRotation(restPosition, restRotation);
            }
        }

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            characterRenderers = characterVisual.GetComponentsInChildren<Renderer>(true);
            normalShadowModes = new ShadowCastingMode[characterRenderers.Length];
            for (int i = 0; i < characterRenderers.Length; i++)
                normalShadowModes[i] = characterRenderers[i].shadowCastingMode;

            mainHand = new SharedHandPose(sharedFirstPersonHand, firstPersonHandPose);
            supportHand = new SharedHandPose(sharedFirstPersonSupportHand, firstPersonSupportHandPose);
        }

        private void OnEnable() => UseFirstPerson();

        private void OnDisable()
        {
            UseFirstPerson();
            SetCharacterVisibility(true);
        }

        private void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (input.HasControl && keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                if (thirdPerson)
                    UseFirstPerson();
                else
                    UseThirdPerson();
            }

            if (!thirdPerson)
            {
                FollowFirstPersonHandPoses();
                return;
            }

            Vector3 target = cameraPivot.position - transform.up * 0.35f;
            Vector3 desiredPosition = GetFollowPosition(target);
            followPosition = Vector3.SmoothDamp(followPosition, desiredPosition, ref followVelocity, followSmoothTime);

            // Constrain the smoothed path as well, so a quick turn cannot cut through a wall.
            Vector3 safePosition = ConstrainToClearPath(target, followPosition);
            Quaternion rotation = (target - safePosition).sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(target - safePosition, transform.up)
                : cameraPivot.rotation;
            playerCamera.transform.SetPositionAndRotation(safePosition, rotation);
        }

        private void UseFirstPerson()
        {
            thirdPerson = false;
            playerCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            FollowFirstPersonHandPoses();
            SetCharacterVisibility(false);
        }

        private void UseThirdPerson()
        {
            thirdPerson = true;
            Vector3 target = cameraPivot.position - transform.up * 0.35f;
            followPosition = GetFollowPosition(target);
            followVelocity = Vector3.zero;
            SetCharacterVisibility(true);
        }

        private Vector3 GetFollowPosition(Vector3 target)
        {
            float pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
            Quaternion orbit = transform.rotation * Quaternion.Euler(Mathf.Clamp(pitch + 19f, -30f, 55f), 0f, 0f);
            Vector3 desiredPosition = target + orbit * (Vector3.back * followDistance);
            return ConstrainToClearPath(target, desiredPosition);
        }

        private Vector3 ConstrainToClearPath(Vector3 origin, Vector3 destination)
        {
            Vector3 displacement = destination - origin;
            float distance = displacement.magnitude;
            if (distance <= 0.001f) return origin;

            Vector3 direction = displacement / distance;
            int count = Physics.SphereCastNonAlloc(origin, CollisionRadius, direction, obstructionHits, distance,
                obstructionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = obstructionHits[i];
                if (!hit.collider.transform.IsChildOf(transform))
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - WallClearance));
            }

            return origin + direction * distance;
        }

        private void SetCharacterVisibility(bool visible)
        {
            if (firstPersonVisual != null)
                firstPersonVisual.SetActive(!visible && isActiveAndEnabled);

            if (visible)
            {
                mainHand.Restore();
                supportHand.Restore();
            }

            for (int i = 0; i < characterRenderers.Length; i++)
            {
                var target = characterRenderers[i].transform;
                if (mainHand.Contains(target) || supportHand.Contains(target))
                    continue;

                ShadowCastingMode normalMode = normalShadowModes[i];
                characterRenderers[i].shadowCastingMode = visible || normalMode == ShadowCastingMode.Off
                    ? normalMode
                    : ShadowCastingMode.ShadowsOnly;
                characterRenderers[i].forceRenderingOff = !visible && normalMode == ShadowCastingMode.Off;
            }
        }

        private void FollowFirstPersonHandPoses()
        {
            if (!isActiveAndEnabled) return;
            mainHand.FollowCamera();
            supportHand.FollowCamera();
        }
    }
}
