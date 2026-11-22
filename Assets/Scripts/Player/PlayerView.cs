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
        internal enum CameraMode { FirstPerson, Behind, Front }
        internal CameraMode Mode { get; private set; }
        private CharacterController controller;
        private PlayerMovement movement;
        private float stridePhase;
        private float bobAmplitude;
        private float airOffset;
        private float breathPhase;
        private float breathAmplitude;
        private float lastFallSpeed;
        private float landingAmplitude;
        private float landingTime;
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

            public void Apply(bool thirdPerson, Vector3 offset)
            {
                if (!IsConfigured) return;
                if (thirdPerson)
                    hand.SetLocalPositionAndRotation(restPosition + hand.parent.InverseTransformVector(offset), restRotation);
                else
                    hand.SetPositionAndRotation(cameraPose.position + offset, cameraPose.rotation);
            }

            public void Restore()
            {
                if (IsConfigured)
                    hand.SetLocalPositionAndRotation(restPosition, restRotation);
            }
        }

        internal Transform AimPivot => cameraPivot;
        internal Camera ViewCamera => playerCamera;

        internal void MatchCamera(PlayerView previous)
        {
            cameraPivot.localRotation = previous.cameraPivot.localRotation;
            playerCamera.clearFlags = previous.playerCamera.clearFlags;
            playerCamera.fieldOfView = previous.playerCamera.fieldOfView;
            playerCamera.nearClipPlane = previous.playerCamera.nearClipPlane;
            playerCamera.farClipPlane = previous.playerCamera.farClipPlane;
        }

        internal void SetCameraMode(CameraMode mode)
        {
            Mode = mode;
            bool external = mode != CameraMode.FirstPerson;
            SetCharacterVisibility(external);
            if (external)
            {
                Vector3 target = cameraPivot.position - transform.up * 0.35f;
                followPosition = GetFollowPosition(target);
                followVelocity = Vector3.zero;
            }
            else
                playerCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (isActiveAndEnabled) ApplyHandPoses();
        }

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            characterRenderers = characterVisual.GetComponentsInChildren<Renderer>(true);
            normalShadowModes = new ShadowCastingMode[characterRenderers.Length];
            for (int i = 0; i < characterRenderers.Length; i++)
                normalShadowModes[i] = characterRenderers[i].shadowCastingMode;

            mainHand = new SharedHandPose(sharedFirstPersonHand, firstPersonHandPose);
            supportHand = new SharedHandPose(sharedFirstPersonSupportHand, firstPersonSupportHandPose);
        }

        private void OnEnable() => SetCameraMode(CameraMode.FirstPerson);

        private void OnDisable()
        {
            stridePhase = bobAmplitude = airOffset = 0f;
            breathPhase = breathAmplitude = lastFallSpeed = landingAmplitude = landingTime = 0f;
            SetCameraMode(CameraMode.FirstPerson);
            SetCharacterVisibility(true);
        }

        private void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (input.HasControl && keyboard != null && keyboard.vKey.wasPressedThisFrame)
                CycleCamera();

            UpdateHandMotion();
            ApplyHandPoses();
            if (Mode == CameraMode.FirstPerson) return;

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

        private void CycleCamera()
        {
            SetCameraMode(Mode switch
            {
                CameraMode.FirstPerson => CameraMode.Behind,
                CameraMode.Behind => CameraMode.Front,
                _ => CameraMode.FirstPerson
            });
        }

        private Vector3 GetFollowPosition(Vector3 target)
        {
            float pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
            float yaw = Mode == CameraMode.Front ? 180f : 0f;
            Quaternion orbit = transform.rotation * Quaternion.Euler(Mathf.Clamp(pitch + 19f, -30f, 55f), yaw, 0f);
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

        private void UpdateHandMotion()
        {
            bool moving = controller.enabled && movement.isActiveAndEnabled;
            Vector3 velocity = moving ? controller.velocity : Vector3.zero;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            float walk = Mathf.Clamp01(speed / Mathf.Max(0.01f, movement.WalkSpeed));
            float run = Mathf.InverseLerp(movement.WalkSpeed, movement.SprintSpeed, speed);
            bool grounded = moving && controller.isGrounded;
            float targetAmplitude = grounded ? walk * Mathf.Lerp(0.025f, 0.052f, run) : 0f;
            float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
            bobAmplitude = Mathf.Lerp(bobAmplitude, targetAmplitude, blend);
            float frequency = walk * Mathf.Lerp(1.55f, 2.35f, run);
            if (grounded)
                stridePhase = Mathf.Repeat(stridePhase + frequency * Time.deltaTime * 2f * Mathf.PI, 2f * Mathf.PI);

            // Lag on ascent, float on descent. A long fall can never wind up extra displacement.
            float targetAir = grounded ? 0f : Mathf.Clamp(-velocity.y * 0.012f, -0.065f, 0.085f);
            airOffset = Mathf.Lerp(airOffset, targetAir, blend);

            float idle = grounded ? 1f - Mathf.SmoothStep(0f, 1f, speed / 0.5f) : 0f;
            breathAmplitude = Mathf.Lerp(breathAmplitude, 0.009f * idle, blend);
            breathPhase = Mathf.Repeat(breathPhase + Time.deltaTime * (2f * Mathf.PI / 4f), 2f * Mathf.PI);

            // The controller loses its downward velocity on contact, so sample it while airborne.
            if (grounded && lastFallSpeed > 1.5f)
            {
                landingAmplitude = 0.11f * Mathf.InverseLerp(1.5f, 14f, lastFallSpeed);
                landingTime = 0f;
            }
            lastFallSpeed = moving && !grounded ? Mathf.Max(0f, -velocity.y) : 0f;
            landingTime = Mathf.Min(1f, landingTime + Time.deltaTime);
        }

        private void ApplyHandPoses()
        {
            bool thirdPerson = Mode != CameraMode.FirstPerson;
            Vector3 up = thirdPerson ? transform.up : cameraPivot.up;
            float rebound = -landingAmplitude * Mathf.Exp(-10f * landingTime) * Mathf.Sin(22f * landingTime);
            float height = airOffset + bobAmplitude * Mathf.Sin(stridePhase) + breathAmplitude * Mathf.Sin(breathPhase) + rebound;
            Vector3 offset = up * height;
            // Keep both grips together, including when equipment replaces one of the hand roots.
            mainHand.Apply(thirdPerson, offset);
            supportHand.Apply(thirdPerson, offset);
        }
    }
}
