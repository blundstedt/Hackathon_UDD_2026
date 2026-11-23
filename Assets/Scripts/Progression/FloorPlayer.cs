using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Progression
{
    /// <summary>Asocia un avatar a una identidad de sesión estable y reutiliza la entrada local del proyecto.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerStats), typeof(PlayerView))]
    public sealed class FloorPlayer : MonoBehaviour
    {
        [SerializeField] private string playerId = "local";
        [SerializeField, Min(0.5f)] private float reach = 3.2f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private PlayerInputReader input;
        private PlayerView view;
        private CharacterController body;
        private FloorSession session;
        private bool assignedIdentity;

        public string Id => playerId;
        // Supply the stable connection/account ID before enabling a spawned avatar.
        public void SetIdentity(string id)
        {
            if (isActiveAndEnabled || string.IsNullOrWhiteSpace(id))
                throw new System.InvalidOperationException("Asigna una identidad válida antes de activar al jugador.");
            playerId = id;
            assignedIdentity = true;
        }

        public PlayerStats Stats { get; private set; }
        public Vector3 BodyCenter => transform.TransformPoint(body.center);

        private void Awake()
        {
            Stats = GetComponent<PlayerStats>();
            input = GetComponent<PlayerInputReader>();
            view = GetComponent<PlayerView>();
            body = GetComponent<CharacterController>();
        }

        // A scene avatar may enable before the cabin creates the session on first entry.
        private void Start()
        {
            if (!session) Bind();
        }

        private void OnEnable() => Bind();

        private void Bind()
        {
            var owner = FloorSession.Instance;
            // Lobby display avatars also inherit this prefab, with local input disabled.
            // A remote avatar joins explicitly through SetIdentity before activation.
            if (owner && (input.enabled || assignedIdentity) && Stats.CanReceiveRewards && owner.Register(this))
                session = owner;
        }

        private void OnDisable()
        {
            if (session) session.Unregister(this);
            session = null;
        }

        internal FloorInteraction AimTarget()
        {
            var ray = view.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            float distance = reach + Vector3.Distance(ray.origin, BodyCenter);
            int count = Physics.RaycastNonAlloc(ray, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            FloorInteraction nearest = null;
            if (count == hits.Length) return null;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.distance >= distance || hit.transform.IsChildOf(transform)) continue;
                var target = hit.collider.GetComponent<FloorInteraction>();
                if (!target && hit.collider.isTrigger) continue;
                distance = hit.distance;
                nearest = target && target.isActiveAndEnabled && Vector3.Distance(hit.point, BodyCenter) <= reach ? target : null;
            }
            return nearest;
        }

        private void Update()
        {
            if (!session || session.Traveling || !input.enabled) return;
            var target = input.HasControl ? AimTarget() : null;
            if (target && input.InteractPressed)
            {
                var peer = Hackaton.Multiplayer.NetworkTiza.Local;
                if (peer && peer.Player == this) peer.Interact(target);
                else session.Screen.Notice(target.Interact(this));
            }
            session.Screen.Aim(target ? target.Prompt(session) : null);
        }
    }
}
