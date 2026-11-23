using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Hackaton.Player;
using Hackaton.Progression;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hackaton.Multiplayer
{
    [DefaultExecutionOrder(-1000)]
    public sealed class CampaignRoom : MonoBehaviour
    {
        public const string Menu = "MenuPrincipal", Lobby = "UrbanLobby_new";
        public static CampaignRoom Current { get; private set; }
        [SerializeField] private NetworkManager managerPrefab;
        [SerializeField] private NetworkObject tizaPrefab;
        [SerializeField] private FloorSession campaignPrefab;
        [SerializeField] private PlayerView[] avatars;
        private readonly Dictionary<ulong, string> identities = new();
        private readonly HashSet<ulong> reserved = new();
        private readonly Dictionary<ulong, int> colors = new();
        private NetworkManager manager;
        private FloorSession campaign;
        [SerializeField] private CampaignIntro introPrefab;
        private CampaignIntro intro;
        public bool IntroBlocking => intro && intro.Blocking;
        internal bool LocalAvatarReady => online ? NetworkTiza.Local && NetworkTiza.Local.View : campaign && campaign.PlayerCount > 0;
        internal void HideHudDuringIntro(Canvas canvas) { if (intro) intro.HideHud(canvas); }
        internal void CoverIntro() { if (intro) intro.Cover(); }
        private ISession session;
        private int slot;
        private bool host, closing, loading, online, quitting;
        private bool lockRequested, writingLock, returningToLobby;
        public bool Busy { get; private set; }
        public bool OwnsSave => host && slot != 0;
        public bool IsOnline => online;
        public bool IsAuthority => !online || host;
        public string Code => session?.Code;
        public CampaignSlots Slots { get; private set; }
        public string LastMessage { get; private set; }
        public event Action<string> Message;
        public PlayerView[] Avatars => avatars;
        public int RequiredPlayers => manager && manager.IsServer ? Mathf.Max(identities.Count, manager.ConnectedClientsIds.Count) : 0;

        private void Awake()
        {
            if (Current && Current != this) { gameObject.SetActive(false); Destroy(gameObject); return; }
            Current = this;
            DontDestroyOnLoad(gameObject);
            Slots = new CampaignSlots(Path.Combine(Application.persistentDataPath, "Partidas"));
            Application.runInBackground = true;
        }

        private void Tell(string value) { LastMessage = value; Message?.Invoke(value); }
        private void Prepare(bool hosting, bool network, int index, CampaignSave saved)
        {
            host = hosting;
            online = network;
            slot = index;
            campaign = Instantiate(campaignPrefab);
            intro = Instantiate(introPrefab, campaign.transform);
            if (saved != null) campaign.Restore(saved, avatars);
            campaign.Progress.Changed += OnProgress;
            if (!online) return;
            manager = Instantiate(managerPrefab);
            manager.ConnectionApprovalCallback = Approve;
            manager.OnServerStarted += ServerStarted;
            manager.OnClientStarted += ClientStarted;
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnClientStopped += Stopped;
        }
        private async Task Authenticate()
        {
            Tell("Autenticando con Unity…");
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(AuthenticationService.Instance.PlayerId);
        }
        public async void Host(int index, bool local)
        {
            if (Busy || campaign) return;
            Busy = true;
            try
            {
                var saved = Slots.Read(index);
                Prepare(true, !local, index, saved);
                if (!local)
                {
                    await Authenticate();
                    Tell("Abriendo sala…");
                    Attach(await MultiplayerService.Instance.CreateSessionAsync(
                        new SessionOptions { MaxPlayers = 4, IsPrivate = true }.WithRelayNetwork()));
                }
                Save();
                Tell(null);
                intro.ConfirmEntry();
                if (local) await SceneManager.LoadSceneAsync(Lobby);
                else StartTravel(Lobby);
            }
            catch (Exception ex) { await Failed(ex); }
            finally { Busy = false; }
        }
        public async void Join(string code)
        {
            if (Busy || campaign) return;
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0) { Tell("Introduce el código de la sala."); return; }
            Busy = true;
            try
            {
                Prepare(false, true, 0, null);
                await Authenticate();
                Tell("Conectando con el host…");
                Attach(await MultiplayerService.Instance.JoinSessionByCodeAsync(code));
                if (!manager.IsConnectedClient) throw new IOException("El host ha cerrado la conexión.");
                intro.ConfirmEntry();
                Tell(null);
            }
            catch (Exception ex) { await Failed(ex); }
            finally { Busy = false; }
        }
        private void Attach(ISession joined)
        {
            session = joined;
            session.Deleted += Lost;
            session.RemovedFromSession += Lost;
            session.SessionHostChanged += HostChanged;
        }
        private async Task Failed(Exception error)
        {
            string reason = RoomErrors.Describe(error, manager ? manager.DisconnectReason : null);
            await Close(false);
            Tell(reason);
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse result)
        {
            bool self = request.ClientNetworkId == NetworkManager.ServerClientId;
            string id = self ? "local" : Encoding.UTF8.GetString(request.Payload ?? Array.Empty<byte>());
            string reason = !self && (loading || SceneManager.GetActiveScene().name != Lobby) ? "Espera a que el grupo regrese al lobby." :
                identities.Count >= 4 ? "La sala está llena." :
                string.IsNullOrWhiteSpace(id) || id.Length > 54 || (!self && id == "local") || identities.ContainsValue(id)
                    ? "Identidad inválida o ya conectada." : null;
            result.Approved = reason == null;
            result.CreatePlayerObject = false;
            result.Pending = false;
            result.Reason = reason ?? "";
            if (!result.Approved) return;
            identities[request.ClientNetworkId] = id;
            for (int color = 0; color < 4; color++)
                if (!colors.ContainsValue(color)) { colors[request.ClientNetworkId] = color; break; }
        }
        private void ServerStarted() => manager.SceneManager.OnLoadEventCompleted += SceneReady;
        private void ClientStarted()
        {
            manager.SceneManager.OnLoad += (id, scene, mode, operation) =>
            {
                if (campaign && id == manager.LocalClientId) campaign.Screen.NetworkCover();
            };
            // Late join synchronizes previously loaded scenes without producing LoadEventCompleted.
            manager.SceneManager.OnSynchronizeComplete += id =>
            {
                if (campaign && id == manager.LocalClientId) campaign.Screen.NetworkReveal();
            };
            manager.SceneManager.OnLoadEventCompleted += (scene, mode, arrived, timedOut) =>
            {
                if (campaign) campaign.Screen.NetworkReveal();
            };
        }
        private void Connected(ulong id)
        {
            if (host && !loading && SceneManager.GetActiveScene().name == Lobby) Spawn(id);
        }
        private void Disconnected(ulong id)
        {
            identities.Remove(id);
            colors.Remove(id);
            reserved.Remove(id);
        }
        private void Stopped(bool wasHost) { if (!closing && !Busy && online) Lost(); }
        private void HostChanged(string _) { if (!host) Lost(); }
        private void Lost()
        {
            if (closing || quitting || !online) return;
            Leave("Se perdió la conexión con el host.");
        }
        public void StartTravel(string scene)
        {
            if (!host || loading) return;
            loading = true;
            returningToLobby = scene == Lobby && SceneManager.GetActiveScene().name != Menu;
            if (scene != Lobby) SetRoomLock(true);
            var result = manager.SceneManager.LoadScene(scene, LoadSceneMode.Single);
            if (result != SceneEventProgressStatus.Started)
            {
                loading = false;
                SetRoomLock(SceneManager.GetActiveScene().name != Lobby);
                throw new InvalidOperationException("No se pudo iniciar el viaje (" + result + ").");
            }
        }
        private async void SetRoomLock(bool locked)
        {
            lockRequested = locked;
            if (session == null || writingLock) return;
            writingLock = true;
            var room = session.AsHost();
            try
            {
                while (session == room && room.IsLocked != lockRequested)
                {
                    room.IsLocked = lockRequested;
                    await room.SavePropertiesAsync();
                }
            }
            catch (Exception e) { Debug.LogWarning("No se pudo actualizar el acceso a la sala: " + e.Message); }
            finally { writingLock = false; }
        }

        private void SceneReady(string name, LoadSceneMode mode, List<ulong> arrived, List<ulong> timedOut)
        {
            foreach (var id in timedOut) if (id != NetworkManager.ServerClientId) manager.DisconnectClient(id);
            loading = false;
            reserved.Clear();
            foreach (ulong id in arrived) Spawn(id);
            returningToLobby = false;
            campaign.EndNetworkTravel();
            SetRoomLock(name != Lobby);
        }
        private void Spawn(ulong id)
        {
            if (!manager.ConnectedClients.TryGetValue(id, out var client) || client.PlayerObject ||
                !identities.TryGetValue(id, out var identity) || !reserved.Add(id)) return;
            var pose = CoopSpawn.Find(returningToLobby);
            if (!pose.HasValue)
            {
                reserved.Remove(id);
                if (id != NetworkManager.ServerClientId) manager.DisconnectClient(id, "No hay un punto de aparición libre.");
                else Tell("No hay un punto de aparición libre.");
                return;
            }
            var instance = Instantiate(tizaPrefab, pose.Value.position, pose.Value.rotation);
            var tiza = instance.GetComponent<TizaIdentity>();
            int equipped = Array.IndexOf(avatars, campaign.Equipment(identity));
            tiza.Configure(identity, Mathf.Max(0, equipped), colors[id]);
            instance.SpawnAsPlayerObject(id, true);
        }
        public void Save()
        {
            if (OwnsSave && campaign) Slots.Write(slot, campaign.Capture());
        }
        private void OnProgress()
        {
            if (!host) return;
            try { Save(); } catch (Exception ex) { Tell("Error al guardar. " + ex.Message); }
            if (online) NetworkTiza.PublishCampaign();
        }
        public void Leave() => Leave(null);
        private async void Leave(string reason)
        {
            if (closing || quitting) return;
            Busy = true;
            try
            {
                if (OwnsSave) Save();
                await Close(true);
                Tell(reason);
            }
            catch (Exception ex) { Tell("No se pudo cerrar la partida. " + ex.Message); }
            finally { Busy = false; }
        }
        private async Task Close(bool goHome)
        {
            if (closing) return;
            closing = true;
            if (intro) intro.Cancel();
            try
            {
                if (session != null)
                {
                    var old = session;
                    session = null;
                    old.Deleted -= Lost;
                    old.RemovedFromSession -= Lost;
                    old.SessionHostChanged -= HostChanged;
                    try { if (host) await old.AsHost().DeleteAsync(); else await old.LeaveAsync(); }
                    catch (Exception ex) { Debug.LogWarning("Cierre de sesión: " + ex.Message); }
                }
                if (manager)
                {
                    manager.OnClientStopped -= Stopped;
                    if (manager.IsListening) manager.Shutdown();
                    while (manager && manager.ShutdownInProgress) await Task.Yield();
                    if (manager) Destroy(manager.gameObject);
                    manager = null;
                }
                if (campaign)
                {
                    campaign.Progress.Changed -= OnProgress;
                    Destroy(campaign.gameObject);
                    campaign = null;
                }
                identities.Clear();
                colors.Clear();
                reserved.Clear();
                slot = 0;
                host = online = loading = false;
                if ((goHome || SceneManager.GetActiveScene().name != Menu) && !quitting)
                    await SceneManager.LoadSceneAsync(Menu);
                await Task.Yield();
            }
            finally { closing = false; }
        }
        private void OnApplicationQuit()
        {
            quitting = true;
            if (OwnsSave) { try { Save(); } catch (Exception ex) { Debug.LogError(ex.Message); } }
        }
        private void OnDestroy() { if (Current == this) Current = null; }
    }
}
