using System.Collections;
using System.Collections.Generic;
using Hackaton.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hackaton.Progression
{
    /// <summary>Autoridad local de la partida. Conserva progreso, participantes y cuentas al cambiar de escena.</summary>
    public sealed class FloorSession : MonoBehaviour
    {
        [SerializeField] private FloorData[] floors;
        [SerializeField] private FloorTravelScreen screen;
        private readonly Dictionary<string, FloorPlayer> active = new();
        private readonly Dictionary<string, PlayerStats.Snapshot> accounts = new();
        private readonly Dictionary<string, List<FloorData>> owed = new();
        private readonly Dictionary<string, PlayerView> equipment = new();
        private readonly HashSet<string> participants = new();

        public PlayerView Equipment(string id) => equipment.TryGetValue(id, out var prefab) ? prefab : null;
        internal void Equip(string id, PlayerView prefab) => equipment[id] = prefab;

        public static FloorSession Instance { get; private set; }
        public FloorProgress Progress { get; private set; }
        public FloorTravelScreen Screen => screen;
        public IEnumerable<FloorPlayer> Players => active.Values;
        public int PlayerCount => active.Count;
        public FloorData Current => Progress.ForScene(SceneManager.GetActiveScene().name);
        public bool Traveling { get; private set; }

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Progress = new FloorProgress(floors);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Instance = null;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            var floor = Progress.ForScene(scene.name);
            if (floor && floor.number != 0) participants.Clear();
        }

        internal bool Register(FloorPlayer player)
        {
            if (!active.TryAdd(player.Id, player))
            {
                Debug.LogError($"Dos jugadores activos tienen el identificador {player.Id}.", player);
                return false;
            }
            if (accounts.TryGetValue(player.Id, out var snapshot)) player.Stats.Restore(snapshot);
            if (owed.Remove(player.Id, out var rewards))
                foreach (var reward in rewards) player.Stats.RewardDefeat(reward.money, reward.experience);
            var floor = Progress.ForScene(player.gameObject.scene.name);
            if (floor && floor.number != 0) participants.Add(player.Id);
            return true;
        }

        internal void Unregister(FloorPlayer player)
        {
            if (!IsActive(player)) return;
            accounts[player.Id] = player.Stats.Capture();
            active.Remove(player.Id);
        }

        public bool IsActive(FloorPlayer player) => player && active.TryGetValue(player.Id, out var registered) && registered == player;

        public string CompleteCurrent(FloorPlayer player)
        {
            if (Traveling || !IsActive(player) || !participants.Contains(player.Id)) return "No disponible";
            var floor = Current;
            if (!floor || floor.number == 0) return "No disponible";
            if (!Progress.Complete(floor, () => RewardParticipants(floor))) return "Nivel completado";
            return $"Nivel completado   +{floor.experience} EXP   +${floor.money}" +
                (floor.rewardItem ? $"   ·   {floor.rewardItem.displayName}" : "");
        }

        private void RewardParticipants(FloorData floor)
        {
            foreach (string id in participants)
            {
                if (active.TryGetValue(id, out var player)) player.Stats.RewardDefeat(floor.money, floor.experience);
                else
                {
                    if (!owed.TryGetValue(id, out var rewards)) owed[id] = rewards = new List<FloorData>();
                    rewards.Add(floor);
                }
            }
        }

        public string Deliver(FloorPlayer player) =>
            !Traveling && IsActive(player) && Current && Current.number == 0 ? Progress.Deliver() : "No disponible";

        // A null reason permits the trip. Recheck it immediately before the joint load.
        public string TravelBlock(int destination)
        {
            var current = Current;
            if (!current) return "No disponible";
            if (current.number == destination) return "Piso actual";
            if (current.number != 0 && destination != 0) return "No disponible";
            var state = Progress.State(destination);
            if (state == FloorState.Completed) return "Nivel completado";
            if (state == FloorState.Locked) return "No disponible";
            var floor = Progress.Get(destination);
            return !string.IsNullOrWhiteSpace(floor.sceneName) && Application.CanStreamedLevelBeLoaded(floor.sceneName)
                ? null : $"Piso {destination} aún no implementado";
        }

        internal void Travel(int destination)
        {
            if (Traveling || TravelBlock(destination) != null) return;
            Traveling = true;
            StartCoroutine(LoadScene(Progress.Get(destination).sceneName));
        }

        private IEnumerator LoadScene(string scene)
        {
            yield return screen.Cover();
            var operation = SceneManager.LoadSceneAsync(scene);
            if (operation != null)
            {
                yield return operation;
                yield return null; // Give Start and the first camera update an opaque frame.
            }
            else screen.Notice("No se pudo cargar el piso");
            yield return screen.Reveal();
            Traveling = false;
        }
    }
}
