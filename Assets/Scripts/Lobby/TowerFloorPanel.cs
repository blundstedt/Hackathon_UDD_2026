using Hackaton.Progression;
using UnityEngine;

/// <summary>Panel compartido por las cabinas. Una solicitud espera a todos los jugadores registrados.</summary>
[DefaultExecutionOrder(-400)]
public sealed class TowerFloorPanel : MonoBehaviour
{
    [SerializeField] private Renderer[] digits;
    [SerializeField] private Material illuminated;
    [SerializeField] private Material inactive;
    [SerializeField] private BoxCollider cabin;
    [SerializeField] private FloorSession sessionPrefab;
    private FloorSession session;
    private FloorPlayer requester;
    private int pending = -1;

    private void Awake()
    {
        session = FloorSession.Instance ? FloorSession.Instance : Instantiate(sessionPrefab);
    }

    private void OnEnable() => session.Progress.Changed += Refresh;
    private void Start() => Refresh();

    private void OnDisable()
    {
        if (!session) return;
        session.Progress.Changed -= Refresh;
        Cancel();
    }

    private void Refresh()
    {
        var current = session.Progress.ForScene(gameObject.scene.name);
        for (int i = 0; i < digits.Length; i++)
        {
            bool lit = current && (i == 0 || current.number == i ||
                (current.number == 0 && session.Progress.State(i) == FloorState.Available));
            if (digits[i]) digits[i].sharedMaterial = lit ? illuminated : inactive;
        }
    }

    public string Request(FloorPlayer player, int destination)
    {
        if (session.Traveling || !session.IsActive(player)) return "No disponible";
        var reason = session.TravelBlock(destination);
        if (reason != null) return reason;
        if (!Contains(player)) return "Entra al ascensor";
        if (pending < 0)
        {
            requester = player;
            pending = destination;
        }
        return null;
    }

    // Sample registered bodies in cabin-local space, including arrivals, exits and disconnects.
    // The box also works for CharacterControllers without Rigidbody trigger callbacks.
    private bool Contains(FloorPlayer player)
    {
        var local = cabin.transform.InverseTransformPoint(player.BodyCenter);
        return new Bounds(cabin.center, cabin.size).Contains(local);
    }

    private void LateUpdate()
    {
        if (pending < 0) return;
        if (session.Traveling || !session.IsActive(requester) || !Contains(requester) || session.TravelBlock(pending) != null)
        {
            Cancel();
            return;
        }

        int aboard = 0;
        foreach (var player in session.Players)
            if (Contains(player)) aboard++;

        if (aboard < session.PlayerCount)
        {
            session.Screen.WaitMessage($"Esperando jugadores... {aboard}/{session.PlayerCount}");
            return;
        }
        int destination = pending;
        Cancel();
        session.Travel(destination);
    }

    private void Cancel()
    {
        if (pending < 0) return;
        pending = -1;
        requester = null;
        session.Screen.WaitMessage(null);
    }
}
