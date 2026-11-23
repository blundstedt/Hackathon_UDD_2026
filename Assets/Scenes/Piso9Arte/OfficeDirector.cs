using System.Collections;
using System.Collections.Generic;
using Hackaton.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    // One director belongs to the loaded floor; FloorSession owns the roster, travel and reward ledger.
    public sealed class OfficeDirector : MonoBehaviour
    {
        public enum Stage { Exploration, Pursuit, Window, Preparation, WaveOne, WaveTwo, Reconstruction, FinalCombat, Completed }
        [SerializeField] OfficeActor employee, supervisor, corruptor;
        [SerializeField] FloorObjective chest;
        [SerializeField] Transform breach, hunterSpawn;
        [SerializeField] Font signage;
        [SerializeField] Vector2Int[] rosters = { new(12,0), new(16,3), new(10,2) };
        [SerializeField, Min(1)] int concurrent = 7;
        [SerializeField, Min(.1f)] float spawnInterval = 1.15f;
        static readonly int[] entries = { 70,62,61,69,53,60,52,68,71 };
        static readonly Vector3 Arena = new(102.4f,0,89.6f);
        static readonly Vector3 CabinDoor = new(102.4f,0,91.57f);
        readonly List<OfficeActor> crew = new();
        readonly List<Transform> obstructions = new();
        NavMeshPath route;
        OfficeActor monster;
        OfficeTerminal terminal;
        Camera shot, playerCamera;
        int pendingEmployees, pendingDrones, reinforcements, cursor, encounters;
        float nextTick, nextSpawn, endAt;
        bool closing;
        public Stage Phase { get; private set; }
        public OfficeEffects Fx { get; private set; }
        public bool Defending => Phase == Stage.WaveOne || Phase == Stage.WaveTwo || Phase == Stage.FinalCombat;
        public bool AtElevator => Phase >= Stage.Preparation;
        public Vector3 Checkpoint => Cell(70);
        public bool CanSummon => Defending && reinforcements > 0 && crew.Count < Limit;
        int Limit => concurrent + Mathf.Min(5, FloorSession.Instance.PlayerCount - 1);
        static Vector3 Cell(int i) => new((i % 9) * 12.8f, 0, (i / 9) * 12.8f);

        void Awake()
        {
            route = new NavMeshPath();
            Fx = GetComponent<OfficeEffects>();
            chest.gameObject.SetActive(false);
        }

        void Start()
        {
            var screen = Fx.Block("Terminal del ascensor", CabinDoor + new Vector3(-3.9f,1.35f,.25f),
                new Vector3(1.7f,.85f,.38f),new Color(.08f,.14f,.17f),true);
            Fx.Block("Pedestal",screen.position + Vector3.down * .83f,new Vector3(.72f,.89f,.38f),new Color(.24f,.32f,.32f),true);
            terminal = screen.gameObject.AddComponent<OfficeTerminal>();
            terminal.Install(this,signage,CabinDoor);
            InstallOfficeMonitors();
            Show();
        }

        void InstallOfficeMonitors()
        {
            var head = employee.transform.Find("Model/Torso/Head");
            foreach (int sector in new[] { 10, 23, 37, 48, 60, 70 })
            {
                var site = Cell(sector) + new Vector3(3.4f,0,2.4f);
                Fx.Block("Punto de fichaje",site + Vector3.up * .7f,new Vector3(.42f,1.4f,.36f),
                    new Color(.22f,.34f,.31f));
                var display = Instantiate(head, site + Vector3.up * 1.69f,
                    Quaternion.LookRotation(Vector3.back),transform);
                display.name = "Control de asistencia";
                var properties = new MaterialPropertyBlock();
                properties.SetFloat("_Expression",sector % 3 == 0 ? 6 : 0);
                display.GetComponent<Renderer>().SetPropertyBlock(properties);
            }
        }

        void PlaceObstructions()
        {
            foreach (int sector in new[] { 15, 28, 40, 50, 61 })
            {
                var site = Cell(sector) + new Vector3(1.45f,0,.38f);
                var crate = Fx.Block("Expedientes pendientes",site + Vector3.up * .37f,
                    new Vector3(1.05f,.74f,.78f),new Color(.57f,.36f,.22f),true);
                var seal = Fx.Block("Precinto",site + Vector3.up * .75f,new Vector3(.16f,.035f,.79f),OfficeEffects.Ice);
                seal.SetParent(crate,true);
                obstructions.Add(crate);
            }
        }

        public Transform BlockingArchive(Transform hunter)
        {
            foreach (var crate in obstructions)
            {
                if (!crate) continue;
                Vector3 delta = crate.position-hunter.position;
                if (delta.sqrMagnitude < 3.6f && Vector3.Dot(delta,hunter.forward) > .25f)
                    return crate;
            }
            return null;
        }

        public void BreakArchive(Transform crate)
        {
            if (!crate || !obstructions.Remove(crate)) return;
            Fx.Burst(crate.position,OfficeEffects.Ice);
            Fx.Sound(ShiftSound.Crash,crate.position);
            Destroy(crate.gameObject);
        }

        void ClearArchives()
        {
            foreach (var crate in obstructions) if (crate) Destroy(crate.gameObject);
            obstructions.Clear();
        }

        void OnDisable()
        {
            RestoreCamera();
            Shader.SetGlobalFloat("_OfficeAlarm",0);
        }

        public FloorPlayer NearestPlayer(Vector3 position)
        {
            FloorPlayer nearest = null;
            float best = float.PositiveInfinity;
            foreach (var p in FloorSession.Instance.Players)
            {
                float d = (p.transform.position-position).sqrMagnitude;
                if (p.Stats.IsTargetable && d < best) { nearest = p; best = d; }
            }
            return nearest;
        }

        public void RestartPursuit()
        {
            if (Phase != Stage.Pursuit) return;
            monster.gameObject.SetActive(false);
            Destroy(monster.gameObject);
            ClearCrew();
            Fx.CancelShots();
            ClearArchives();
            encounters = 0;
            Set(Stage.Exploration,ShiftSound.Hum);
        }

        public bool Hear(Vector3 listener, out Vector3 origin)
        {
            origin = default;
            float nearest = float.PositiveInfinity;
            foreach (var p in FloorSession.Instance.Players)
            {
                if (!p.Stats.IsTargetable) continue;
                float speed = p.GetComponent<CharacterController>().velocity.magnitude;
                float audible = speed > 6 ? 16 : speed > 2 ? 6 : 0;
                float d = Vector3.Distance(listener,p.transform.position);
                if (d >= audible || d >= nearest) continue;
                origin = p.transform.position;
                nearest = d;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        public Vector3 PatrolPoint(Vector3 from, bool hunting)
        {
            for (int n = 0; n < 12; n++)
            {
                Vector3 waypoint = Cell(Random.Range(1,71));
                if (Vector3.Distance(from,waypoint) > (hunting ? 63 : 27)) continue;
                if (Walkable(from,waypoint,out var point)) return point;
            }
            return from;
        }

        bool Walkable(Vector3 from, Vector3 to, out Vector3 point)
        {
            point = default;
            if (!NavMesh.SamplePosition(from,out var a,3,NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(to,out var b,2,NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,route) ||
                route.status != NavMeshPathStatus.PathComplete) return false;
            point = b.position;
            return true;
        }

        OfficeActor Spawn(OfficeActor prefab, Vector3 position)
        {
            var actor = Instantiate(prefab,position,Quaternion.identity,transform);
            actor.Bind(this);
            return actor;
        }

        void Update()
        {
            var session = FloorSession.Instance;
            if (!session || session.Traveling || session.PlayerCount == 0 || Time.time < nextTick) return;
            nextTick = Time.time + .2f;
            if (Phase == Stage.Exploration)
            {
                foreach (var p in session.Players)
                {
                    if (!p.Stats.IsTargetable || Vector3.Distance(p.transform.position,transform.position) < 15) continue;
                    if (!NavMesh.SamplePosition(hunterSpawn.position,out var place,2,NavMesh.AllAreas)) return;
                    // The first time clock sounds locally. Bring the hunter in from a connected,
                    // concealed corridor, then seek the recorded sound even if its source leaves.
                    Vector3 arrival = place.position;
                    for (int i = 0; i < 72; i++)
                    {
                        var candidate = Cell((i * 23 + 40) % 72);
                        float distance = Vector3.Distance(candidate,p.transform.position);
                        if (distance < 15 || distance > 28 || !Walkable(p.transform.position,candidate,out var point)) continue;
                        if (Vector3.Distance(NearestPlayer(point).transform.position,point) < 12) continue;
                        if (!Physics.Linecast(p.BodyCenter,point + Vector3.up * 2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) continue;
                        float length = 0;
                        var corners = route.corners;
                        for (int j = 1; j < corners.Length; j++) length += Vector3.Distance(corners[j-1],corners[j]);
                        if (length > 48) continue;
                        arrival = point;
                        break;
                    }
                    monster = Spawn(corruptor,arrival);
                    monster.InvestigateAlarm(p.transform.position);
                    PlaceObstructions();
                    Set(Stage.Pursuit,ShiftSound.Hunt);
                    break;
                }
            }
            if (Phase == Stage.Pursuit)
            {
                var leader = NearestPlayer(Arena);
                if (leader && Vector3.Distance(leader.transform.position,Arena) < 6.6f &&
                    !Physics.Linecast(leader.BodyCenter,Arena + Vector3.up * 1.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                {
                    Set(Stage.Window,ShiftSound.Hum);
                    StartCoroutine(WindowEvent());
                    return;
                }
                if (leader && encounters < 4 && Vector3.Distance(leader.transform.position,transform.position) > 20 + encounters * 18)
                {
                    var spot = PatrolPoint(leader.transform.position,false);
                    var near = NearestPlayer(spot);
                    if (near && Vector3.Distance(near.transform.position,spot) > 8)
                    {
                        crew.Add(Spawn(encounters == 2 ? supervisor : employee,spot));
                        encounters++;
                    }
                }
            }
            if (!Defending || closing) return;
            if (Time.time >= nextSpawn && crew.Count < Limit && (pendingEmployees > 0 || pendingDrones > 0))
            {
                bool drone = pendingDrones > 0 && (pendingEmployees == 0 || pendingEmployees % 4 == 0);
                if (TryReinforcement(drone ? supervisor : employee))
                {
                    if (drone) pendingDrones--; else pendingEmployees--;
                }
                nextSpawn = Time.time + spawnInterval;
            }
            if (Phase == Stage.FinalCombat) { Show(); return; }
            if (pendingEmployees + pendingDrones == 0 && crew.Count == 0 && Time.time >= endAt)
            {
                if (Phase == Stage.WaveOne) BeginWave(1);
                else
                {
                    Set(Stage.Reconstruction,ShiftSound.Boss);
                    StartCoroutine(Rebuild());
                }
            }
            Show();
        }

        public void ActorDied(OfficeActor actor)
        {
            crew.Remove(actor);
            if (actor != monster || Phase != Stage.FinalCombat || closing) return;
            closing = true;
            ClearCrew();
            Fx.CancelShots();
            StartCoroutine(Finish());
        }

        IEnumerator Finish()
        {
            yield return new WaitForSeconds(1.35f);
            chest.gameObject.SetActive(true);
            Set(Stage.Completed,ShiftSound.Hum);
            Fx.Sound(ShiftSound.Clear,CabinDoor);
        }

        void ClearCrew()
        {
            foreach (var actor in crew) if (actor) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
            crew.Clear();
            pendingEmployees = pendingDrones = reinforcements = 0;
        }

        public string Activate(FloorPlayer player)
        {
            var session = FloorSession.Instance;
            if (Phase != Stage.Preparation || session.Traveling || !session.IsActive(player) ||
                !player.Stats.IsTargetable || Vector3.Distance(player.BodyCenter,terminal.transform.position) > 4)
                return "Terminal en espera";
            BeginWave(0);
            return "Reactivación del ascensor iniciada";
        }

        void BeginWave(int index)
        {
            int extra = Mathf.Clamp(FloorSession.Instance.PlayerCount-1,0,5);
            pendingEmployees = rosters[index].x + extra * 3;
            pendingDrones = rosters[index].y + (index > 0 ? extra : 0);
            reinforcements = index == 0 ? 0 : 3 + extra;
            nextSpawn = Time.time + 1;
            endAt = nextSpawn + 1;
            Set(index == 0 ? Stage.WaveOne : index == 1 ? Stage.WaveTwo : Stage.FinalCombat,
                index == 2 ? ShiftSound.Boss : ShiftSound.Defense);
            Fx.Sound(ShiftSound.Alert,CabinDoor);
        }

        bool TryReinforcement(OfficeActor prefab)
        {
            for (int n = 0; n < entries.Length; n++)
            {
                var candidate = Cell(entries[cursor++ % entries.Length]);
                var nearest = NearestPlayer(candidate);
                if (!nearest || Vector3.Distance(candidate,nearest.transform.position) < 7) continue;
                if (!Walkable(Arena,candidate,out var point)) continue;
                bool crowded = false;
                foreach (var actor in crew)
                    if (actor && (actor.transform.position-point).sqrMagnitude < 2.25f) { crowded = true; break; }
                if (crowded) continue;
                crew.Add(Spawn(prefab,point));
                Fx.Burst(point + Vector3.up, OfficeEffects.Ice, 4);
                return true;
            }
            return false;
        }

        public void Summon()
        {
            if (CanSummon && TryReinforcement(employee)) reinforcements--;
        }

        void Set(Stage next, ShiftSound track)
        {
            Phase = next;
            Fx.Track(track);
            Shader.SetGlobalFloat("_OfficeAlarm",next == Stage.Completed ? 0 : next == Stage.FinalCombat ? 1 : next == Stage.Exploration ? 0 : .48f);
            Show();
        }

        void Show()
        {
            string title;
            int progress = 0;
            switch (Phase)
            {
                case Stage.Exploration: title = "Oficina Corrupta  ·  Encuentra el ascensor de salida"; break;
                case Stage.Pursuit: title = "Evita al Corruptor  ·  Sigue las señales de salida"; break;
                case Stage.Window: title = "Incidente laboral en curso"; break;
                case Stage.Preparation: title = "Empleado reubicado  ·  Activa la terminal junto al ascensor"; break;
                case Stage.WaveOne: title = "1 / 3   Productividad obligatoria"; break;
                case Stage.WaveTwo: title = "2 / 3   Evaluación de desempeño"; progress = 30; break;
                case Stage.Reconstruction: title = "Reclamación de seguro rechazada  ·  Reconstruyendo empleado"; progress = 65; break;
                case Stage.FinalCombat: title = closing ? "Error crítico  ·  Empleado despedido" : "3 / 3   Reincorporación inmediata"; progress = 65; break;
                default: title = "Turno terminado  ·  Recoge el cofre y embarca"; progress = 100; break;
            }
            if (Phase == Stage.WaveOne || Phase == Stage.WaveTwo)
                title += "   ·   " + (crew.Count + pendingEmployees + pendingDrones) + " restantes";
            terminal.Show(title,progress,Phase == Stage.FinalCombat && !closing ? monster : null);
        }

        IEnumerator WindowEvent()
        {
            ClearCrew();
            Fx.CancelShots();
            ClearArchives();
            monster.Stage(OfficePose.Fall);
            var pane = breach.position;
            Vector3 start = new(Arena.x-4,0,Arena.z);
            Vector3 impact = pane + Vector3.up * .55f;
            Vector3 outside = impact + new Vector3(8,-14,1.5f);
            monster.transform.SetPositionAndRotation(start,Quaternion.LookRotation(Vector3.right));
            playerCamera = Camera.main;
            if (playerCamera)
            {
                shot = new GameObject("Cámara del incidente").AddComponent<Camera>();
                shot.transform.SetParent(transform);
                shot.CopyFrom(playerCamera);
                shot.transform.position = Arena + new Vector3(.4f,1.9f,-3.4f);
                playerCamera.enabled = false;
                shot.enabled = true;
            }
            for (float elapsed = 0; elapsed < 3.15f; elapsed += Time.deltaTime)
            {
                float run = Mathf.Clamp01(elapsed / 1.45f);
                float fall = Mathf.Clamp01((elapsed - 1.45f) / 1.7f);
                if (run >= 1 && breach.gameObject.activeSelf)
                {
                    breach.gameObject.SetActive(false);
                    Fx.Burst(impact + Vector3.up * 1.3f,OfficeEffects.Ice,30);
                    Fx.Sound(ShiftSound.Glass,impact,1);
                }
                Vector3 pos = Vector3.Lerp(start,impact,run * run);
                pos.x = Mathf.Lerp(pos.x,outside.x,fall);
                pos.z = Mathf.Lerp(pos.z,outside.z,fall);
                pos.y = Mathf.Lerp(pos.y,outside.y,fall * fall);
                monster.transform.position = pos;
                monster.SetPresentation(OfficePose.Fall,Mathf.Clamp01(run * .24f + fall));
                if (shot)
                {
                    // Cut to the outside of the facade as the chassis clears the frame.
                    if (fall > .12f) shot.transform.position = impact + new Vector3(10,1.6f,6);
                    shot.transform.LookAt(pos + Vector3.up * 1.6f);
                }
                yield return null;
            }
            monster.gameObject.SetActive(false);
            Fx.Sound(ShiftSound.Crash,impact,.85f);
            yield return new WaitForSeconds(.4f);
            RestoreCamera();
            Set(Stage.Preparation,ShiftSound.Hum);
        }

        void RestoreCamera()
        {
            if (shot) { shot.enabled = false; Destroy(shot.gameObject); }
            if (playerCamera) playerCamera.enabled = true;
            shot = playerCamera = null;
        }

        IEnumerator Rebuild()
        {
            var site = Arena + Vector3.right * 1.6f;
            if (Walkable(Arena,site,out var ground)) site = ground;
            else site = Arena;
            monster.gameObject.SetActive(true);
            monster.transform.rotation = Quaternion.LookRotation(Vector3.left);
            Fx.Ring(site,2.5f,2.5f);
            Fx.Sound(ShiftSound.Alert,site);
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                float step = Mathf.SmoothStep(0,1,t / 2.5f);
                monster.transform.position = Vector3.Lerp(breach.position + new Vector3(4,-5,0),site,step);
                monster.SetPresentation(OfficePose.Rebuild,step);
                yield return null;
            }
            monster.Reconstruct(site,FloorSession.Instance.PlayerCount);
            BeginWave(2);
        }
    }
}
