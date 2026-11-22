using System.Collections.Generic;
using UnityEngine;

namespace Hackaton.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class LargeKnockback : MonoBehaviour
    {
        [Header("Launch (multipliers of the attack speeds)")]
        [SerializeField, Min(0)] private float horizontalForce = .26f;
        [SerializeField, Min(0)] private float verticalForce = .92f;
        [SerializeField, Min(0)] private float spin = 13f;
        [SerializeField, Range(0, 30)] private float variationDegrees = 12f;

        [Header("Landing")]
        [SerializeField, Range(0, .5f)] private float elasticity = .28f;
        [SerializeField, Range(0, 1)] private float groundFriction = .65f;
        [SerializeField, Min(.01f)] private float stoppedSpeed = .38f;
        [SerializeField, Min(.2f)] private float maxRecoveryTime = 4f;

        [Header("Rig")]
        [SerializeField] private bool useRagdoll = true;
        [SerializeField, Tooltip("Optional. Limits joint discovery to this model.")] private Transform rig;
        [SerializeField] private Animator animator;
        [SerializeField, Tooltip("Layer 0 state, e.g. Base Layer.GetUp")] private string getUpState = "Base Layer.GetUp";
        [SerializeField, Min(.05f)] private float getUpDuration = .4f;

        private enum Phase { Ready, Flying, Rising }
        private Phase phase;
        public bool Active => phase != Phase.Ready;

        private CharacterController controller;
        private CapsuleCollider capsule;
        private Rigidbody rigRootBody;
        private bool rootWasKinematic;
        private Collider[] suspendedShell;
        private Body[] bodies;
        private RaycastHit[] supportHits = new RaycastHit[8];
        private PhysicsMaterial material;
        private bool animatorWasEnabled, ragdoll, fallen;
        private float age, settled, riseTime, minimumRest;
        private Vector3 landing, riseStart;
        private Quaternion upright, riseRotation;

        public static LargeKnockback For(CharacterController owner) =>
            owner.TryGetComponent<LargeKnockback>(out var existing) ? existing : owner.gameObject.AddComponent<LargeKnockback>();

        // Replicate the owner's body poses for remote players. PhysX is not deterministic.
        // Only the character's simulation owner calls this entry point.
        public void Launch(Knockback hit, Vector3 direction)
        {
            if (!isActiveAndEnabled || fallen) return;
            if (phase == Phase.Rising)
            {
                capsule.enabled = false;
                if (animatorWasEnabled) animator.enabled = false;
                foreach (var part in bodies) part.Activate(material, spin);
            }
            else if (!Active)
            {
                controller = GetComponent<CharacterController>();
                if (!controller.enabled) return;
                controller.enabled = false;
                upright = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                if (animator == null) animator = GetComponentInChildren<Animator>();
                animatorWasEnabled = animator != null && animator.enabled;
                if (animatorWasEnabled) animator.enabled = false;
                PrepareBodies();
                fallen = false;
            }

            phase = Phase.Flying;
            age = settled = 0f;
            minimumRest = Mathf.Max(.12f, hit.recovery);
            Vector3 forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = upright * Vector3.forward;
            forward = Quaternion.AngleAxis(Random.Range(-variationDegrees, variationDegrees), Vector3.up) * forward;
            Vector3 launch = forward * (hit.horizontal * horizontalForce) + Vector3.up * (hit.vertical * verticalForce);
            Vector3 tumble = (Vector3.Cross(Vector3.up, forward) + Random.insideUnitSphere * .55f).normalized * spin;
            // A shared change in linear velocity keeps the joints together on repeated hits.
            Vector3 delta = launch - bodies[0].rb.linearVelocity;
            foreach (var part in bodies)
            {
                part.ResetFlightDamping();
                part.rb.WakeUp();
                part.rb.AddForce(delta, ForceMode.VelocityChange);
            }
            bodies[0].rb.AddTorque(tumble, ForceMode.VelocityChange);
        }

        private void PrepareBodies()
        {
            if (material == null) material = new PhysicsMaterial("Knockback contact");
            material.bounciness = elasticity;
            material.dynamicFriction = material.staticFriction = groundFriction;
            material.bounceCombine = PhysicsMaterialCombine.Maximum;

            var members = new List<Rigidbody>();
            if (useRagdoll)
            {
                Transform model = rig != null ? rig : transform;
                foreach (Joint joint in model.GetComponentsInChildren<Joint>())
                {
                    var parent = joint.connectedBody;
                    var child = joint.GetComponent<Rigidbody>();
                    if (parent == null || child == null || parent.transform == transform ||
                        !parent.transform.IsChildOf(model) || child.transform == transform) continue;
                    if (!members.Contains(parent)) members.Add(parent);
                    if (!members.Contains(child)) members.Add(child);
                }
            }
            // Parent-first order also lets recovery reconstruct the fallen world pose.
            members.Sort((a, b) => Depth(a.transform).CompareTo(Depth(b.transform)));
            ragdoll = members.Count > 0;
            if (ragdoll)
            {
                rigRootBody = GetComponent<Rigidbody>();
                if (rigRootBody != null)
                {
                    rootWasKinematic = rigRootBody.isKinematic;
                    rigRootBody.isKinematic = true;
                }
                suspendedShell = System.Array.FindAll(GetComponentsInChildren<Collider>(), shape =>
                    shape.enabled && !shape.isTrigger && shape != controller &&
                    shape.GetComponentInParent<Rigidbody>() == rigRootBody);
                foreach (var shape in suspendedShell) shape.enabled = false;
            }
            else
            {
                if (capsule == null)
                {
                    capsule = gameObject.AddComponent<CapsuleCollider>();
                    capsule.enabled = false;
                }
                capsule.center = controller.center;
                capsule.radius = controller.radius;
                capsule.height = controller.height;
                if (!TryGetComponent<Rigidbody>(out var root))
                {
                    root = gameObject.AddComponent<Rigidbody>();
                    root.isKinematic = true;
                }
                members.Add(root);
            }

            bodies = new Body[members.Count];
            for (int i = 0; i < members.Count; i++)
                bodies[i] = new Body(members[i], ragdoll ? null : capsule);
            foreach (var part in bodies) part.Activate(material, spin);
        }

        private static int Depth(Transform node)
        {
            int depth = 0;
            while (node.parent != null) { depth++; node = node.parent; }
            return depth;
        }

        private void FixedUpdate()
        {
            if (phase != Phase.Flying || fallen) return;
            age += Time.fixedDeltaTime;
            float speed = 0f;
            foreach (var part in bodies)
                speed = Mathf.Max(speed, part.rb.linearVelocity.magnitude, part.rb.angularVelocity.magnitude * .45f);

            if (age < .2f || !FindSupport(out var ground))
            {
                settled = 0f;
                return;
            }

            // A deadline on a slope brakes the body only while supported.
            if (age >= maxRecoveryTime)
                foreach (var part in bodies)
                {
                    part.rb.linearDamping = 20f;
                    part.rb.angularDamping = 20f;
                }

            settled = speed <= stoppedSpeed ? settled + Time.fixedDeltaTime : 0f;
            if (settled < minimumRest && !(age >= maxRecoveryTime && speed < 1f)) return;

            Vector3 centre = bodies[0].rb.worldCenterOfMass;
            Vector3 offset = upright * Vector3.Scale(controller.center, transform.lossyScale);
            float halfHeight = Mathf.Max(controller.height * Mathf.Abs(transform.lossyScale.y) * .5f,
                controller.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)));
            landing = new Vector3(centre.x, ground.y + halfHeight + .015f, centre.z) - offset;
            riseStart = transform.position;
            riseRotation = transform.rotation;
            if (ragdoll) foreach (var part in bodies) part.SaveWorldPose();
            foreach (var part in bodies) part.Restore();
            if (ragdoll)
            {
                transform.SetPositionAndRotation(landing, upright);
                foreach (var part in bodies) part.StartPoseBlend();
                // Keep a hittable shape during the get-up animation.
                PrepareRecoveryCapsule();
            }
            else capsule.enabled = true;
            riseTime = 0f;
            phase = Phase.Rising;
        }

        private void PrepareRecoveryCapsule()
        {
            if (capsule == null) capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = controller.center;
            capsule.height = controller.height;
            capsule.radius = controller.radius;
            capsule.enabled = true;
        }

        private bool FindSupport(out Vector3 point)
        {
            foreach (var part in bodies)
            foreach (var shape in part.shapes)
            {
                Bounds bounds = shape.bounds;
                int count;
                while ((count = Physics.RaycastNonAlloc(bounds.center, Vector3.down, supportHits, bounds.extents.y + .08f,
                           Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == supportHits.Length)
                    System.Array.Resize(ref supportHits, count * 2);
                for (int i = 0; i < count; i++)
                {
                    var hit = supportHits[i];
                    if (hit.normal.y < .55f || hit.transform.IsChildOf(transform)) continue;
                    point = hit.point;
                    return true;
                }
            }
            point = default;
            return false;
        }

        private void Update()
        {
            if (phase != Phase.Rising || fallen) return;
            riseTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, riseTime / getUpDuration);
            if (!ragdoll)
            {
                Quaternion orientation = Quaternion.Slerp(riseRotation, upright, t);
                Vector3 offset = Vector3.Scale(controller.center, transform.lossyScale);
                Vector3 centre = Vector3.Lerp(riseStart + riseRotation * offset, landing + upright * offset, t);
                transform.SetPositionAndRotation(centre - orientation * offset, orientation);
            }
            else if (!animatorWasEnabled || !animator.enabled)
            {
                float blend = Mathf.Clamp01(riseTime / (getUpDuration * .35f));
                foreach (var part in bodies) part.BlendPose(blend);
            }
            if (animatorWasEnabled && !animator.enabled && riseTime >= getUpDuration * .35f)
            {
                animator.enabled = true;
                int state = Animator.StringToHash(getUpState);
                if (animator.runtimeAnimatorController != null && animator.HasState(0, state))
                    animator.CrossFadeInFixedTime(state, .1f, 0);
            }
            if (riseTime >= getUpDuration) Finish();
        }

        private void RestoreShell()
        {
            if (!ragdoll) return;
            foreach (var shape in suspendedShell) shape.enabled = true;
            if (rigRootBody != null) rigRootBody.isKinematic = rootWasKinematic;
        }

        private void Finish()
        {
            capsule.enabled = false;
            if (animatorWasEnabled) animator.enabled = true;
            RestoreShell();
            controller.enabled = true;
            phase = Phase.Ready;
        }

        public void KeepFallen() => fallen = Active;

        private void OnDisable()
        {
            if (!Active) return;
            if (phase == Phase.Flying)
            {
                foreach (var part in bodies) part.Restore();
                if (ragdoll) foreach (var part in bodies) part.RestorePose();
                transform.rotation = upright;
            }
            if (capsule != null) capsule.enabled = false;
            if (animatorWasEnabled) animator.enabled = true;
            RestoreShell();
            controller.enabled = !fallen;
            fallen = false;
            phase = Phase.Ready;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        private sealed class Body
        {
            public readonly Rigidbody rb;
            public readonly Collider[] shapes;
            private readonly PhysicsMaterial[] materials;
            private readonly bool[] enabledShapes;
            private readonly bool kinematic, gravity, collisions;
            private readonly RigidbodyConstraints constraints;
            private readonly CollisionDetectionMode detection;
            private readonly float linearDrag, angularDrag, maxSpin;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private Vector3 fallenPosition;
            private Quaternion fallenRotation;
            private readonly Joint[] joints;
            private readonly Vector2[] breakLimits;

            public Body(Rigidbody body, CapsuleCollider proxy)
            {
                rb = body;
                localPosition = rb.transform.localPosition;
                localRotation = rb.transform.localRotation;
                joints = proxy == null ? rb.GetComponents<Joint>() : System.Array.Empty<Joint>();
                breakLimits = new Vector2[joints.Length];
                for (int i = 0; i < joints.Length; i++)
                    breakLimits[i] = new Vector2(joints[i].breakForce, joints[i].breakTorque);
                gravity = rb.useGravity;
                kinematic = rb.isKinematic;
                collisions = rb.detectCollisions;
                constraints = rb.constraints;
                detection = rb.collisionDetectionMode;
                linearDrag = rb.linearDamping;
                angularDrag = rb.angularDamping;
                maxSpin = rb.maxAngularVelocity;
                shapes = proxy != null ? new Collider[] { proxy } :
                    System.Array.FindAll(rb.GetComponentsInChildren<Collider>(), c =>
                        c.GetComponentInParent<Rigidbody>() == rb && !c.isTrigger && !(c is CharacterController));
                materials = new PhysicsMaterial[shapes.Length];
                enabledShapes = new bool[shapes.Length];
                for (int i = 0; i < shapes.Length; i++)
                {
                    materials[i] = shapes[i].sharedMaterial;
                    enabledShapes[i] = shapes[i].enabled;
                }
            }

            public void Activate(PhysicsMaterial contact, float spin)
            {
                foreach (var shape in shapes)
                {
                    shape.sharedMaterial = contact;
                    shape.enabled = true;
                }
                foreach (var joint in joints) joint.breakForce = joint.breakTorque = Mathf.Infinity;
                rb.constraints = RigidbodyConstraints.None;
                rb.isKinematic = false;
                rb.useGravity = rb.detectCollisions = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.maxAngularVelocity = Mathf.Max(maxSpin, spin);
            }

            public void ResetFlightDamping()
            {
                rb.linearDamping = .12f;
                rb.angularDamping = .65f;
            }

            public void Restore()
            {
                for (int i = 0; i < joints.Length; i++)
                {
                    joints[i].breakForce = breakLimits[i].x;
                    joints[i].breakTorque = breakLimits[i].y;
                }
                rb.linearVelocity = rb.angularVelocity = Vector3.zero;
                rb.collisionDetectionMode = detection;
                rb.isKinematic = kinematic;
                rb.useGravity = gravity;
                rb.detectCollisions = collisions;
                rb.constraints = constraints;
                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;
                rb.maxAngularVelocity = maxSpin;
                for (int i = 0; i < shapes.Length; i++)
                {
                    shapes[i].enabled = enabledShapes[i];
                    shapes[i].sharedMaterial = materials[i];
                }
            }

            public void SaveWorldPose() => rb.transform.GetPositionAndRotation(out fallenPosition, out fallenRotation);

            public void StartPoseBlend()
            {
                rb.transform.SetPositionAndRotation(fallenPosition, fallenRotation);
                rb.transform.GetLocalPositionAndRotation(out fallenPosition, out fallenRotation);
            }

            public void BlendPose(float t) => rb.transform.SetLocalPositionAndRotation(
                Vector3.Lerp(fallenPosition, localPosition, t), Quaternion.Slerp(fallenRotation, localRotation, t));

            public void RestorePose() => rb.transform.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}
