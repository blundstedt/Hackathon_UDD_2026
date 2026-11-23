using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hackaton.Combat;
using Hackaton.Levels;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Run All in a fresh Play session in Piso9. Distances are in metres.
public static class VerifyOfficeMaze
{
    const float Cell = 12.8f;
    const float Radius = 1.55f;
    const float Height = 3.8f;
    static FloorSession Session => FloorSession.Instance;
    static FloorPlayer Player => Session.Players.Single();
    static TowerFloorPanel Exit => Object.FindObjectsByType<TowerFloorPanel>().Single(panel => panel.IsExit);

    static Vector3 Center(int index) => new Vector3((index % 9) * Cell, 0, (index / 9) * Cell);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static bool Clear(Vector3 start, Vector3 end)
    {
        var d = end - start;
        var hits = Physics.CapsuleCastAll(start + Vector3.up * (Radius + .055f),
            start + Vector3.up * (Height - Radius + .055f), Radius, d.normalized, d.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        return hits.All(hit => hit.collider.GetComponentInParent<FloorPlayer>());
    }

    static List<int>[] Inspect(out int connections)
    {
        var graph = Enumerable.Range(0,72).Select(i => new List<int>()).ToArray();
        connections = 0;
        for (int i = 0; i < graph.Length; i++)
        {
            foreach (int j in new[] {i + 1, i + 9})
            {
                if (j >= graph.Length || (j == i + 1 && j % 9 == 0)) continue;
                Vector3 start = Center(i), end = Center(j);
                if (!Clear(start,end)) continue;
                var path = new NavMeshPath();
                Require(NavMesh.SamplePosition(start,out var a,.8f,NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(end,out var b,.8f,NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path) &&
                    path.status==NavMeshPathStatus.PathComplete &&
                    path.corners.Zip(path.corners.Skip(1),Vector3.Distance).Sum()<Cell+1f,
                    $"La navegación no sigue el paso físico {i}–{j}");
                graph[i].Add(j);
                graph[j].Add(i);
                connections++;
            }
        }
        return graph;
    }

    static List<int> Route(List<int>[] graph, int goal)
    {
        var parent = new Dictionary<int,int> {{0,-1}};
        var queue = new Queue<int>();
        queue.Enqueue(0);
        while(queue.Count > 0)
        {
            int from = queue.Dequeue();
            foreach(int to in graph[from])
                if(parent.TryAdd(to,from)) queue.Enqueue(to);
        }
        Require(parent.Count == graph.Length, $"Sólo {parent.Count} sectores accesibles");
        var route = new List<int>();
        for(int i=goal; i>=0; i=parent[i]) route.Add(i);
        route.Reverse();
        return route;
    }

    static void Walk(Vector3 target)
    {
        var body=Player.GetComponent<CharacterController>();
        var motion=new CharacterImpactMotion();
        float vy=0f;
        for(int i=0;i<600;i++)
        {
            var d=Vector3.ProjectOnPlane(target-Player.transform.position,Vector3.up);
            if(d.magnitude<.12f) return;
            motion.Move(body,d.normalized*4.5f,ref vy,20f,1f/60f);
        }
        throw new Exception("Paso bloqueado hacia "+target+" en "+Player.transform.position);
    }

    static string Use(FloorInteraction target)
    {
        var camera=Player.GetComponentInChildren<Camera>();
        camera.transform.LookAt(target.GetComponent<Collider>().bounds.center);
        var aim=typeof(FloorPlayer).GetMethod("AimTarget",BindingFlags.NonPublic|BindingFlags.Instance);
        Require(aim.Invoke(Player,null) as FloorInteraction == target,"Interacción fuera de alcance: "+target.name);
        return target.Interact(Player);
    }

    static void MoveTo(Vector3 position)
    {
        var body=Player.GetComponent<CharacterController>();
        body.enabled=false;Player.transform.position=position;body.enabled=true;Physics.SyncTransforms();
    }

    static async Task WaitForFloor(int id)
    {
        var deadline=EditorApplication.timeSinceStartup+45;
        while(!Session.Current || Session.Current.number!=id || Session.Traveling || Session.PlayerCount!=1)
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException($"Viaje a {id}, piso {Session.Current?.number}, cargando {Session.Traveling}, avatares {Session.PlayerCount}");
            await Task.Yield();
        }
    }

    static async Task WaitForRecovery(Vector3 safe)
    {
        double deadline=EditorApplication.timeSinceStartup+3;
        while(Vector3.Distance(Player.transform.position,safe)>1f || Player.Stats.Health==0)
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Recuperación en recepción");
            await Task.Yield();
        }
    }

    static async Task Until(Func<bool> condition, string reason, double timeout = 35)
    {
        double deadline = EditorApplication.timeSinceStartup + timeout;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception(reason);
            await Task.Yield();
        }
    }

    public static async Task<string> Skirmish()
    {
        Require(Session.Current.number == 9 && Session.PlayerCount == 1,"Iniciar Play directamente en Piso9");
        var director = Object.FindAnyObjectByType<OfficeDirector>();
        OfficeActor Place(string name, Vector3 point)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<OfficeActor>("Assets/Scenes/Piso9Arte/" + name + ".prefab");
            var actor = Object.Instantiate(prefab,point,Quaternion.identity);
            actor.Bind(director);
            return actor;
        }
        var body = Player.GetComponent<CharacterController>();
        var employee = Place("Empleado Corrupto",Center(0) + Vector3.forward * 2);
        OfficeActor drone = null;
        try
        {
            MoveTo(Center(0)+Vector3.up*.15f);
            Player.transform.rotation = Quaternion.identity;
            var camera = Player.GetComponentInChildren<Camera>();
            camera.transform.LookAt(employee.transform.position + Vector3.up * 1.25f);
            var hands = Player.GetComponent<SlapAttack>();
            Require(hands && hands.TryAttack(),"Manos vacías del avatar");
            await Until(() => employee.Health < employee.MaximumHealth,"La bofetada no dañó al empleado",5);
            var nav = employee.GetComponent<NavMeshAgent>();
            await Until(() => nav.updatePosition && nav.isOnNavMesh,"Recuperación de knockback",12);
            nav.Warp(body.transform.position + Vector3.forward*1.4f);
            employee.transform.rotation = Quaternion.Euler(0,180,0);
            Physics.SyncTransforms();
            int before = Player.Stats.Health;
            await Until(() => Player.Stats.Health < before,"Golpe del empleado",6);
            employee.TakeDamage(employee.Health);
            Require(!employee.Alive,"Muerte del empleado");
            var stand = body.transform.position;
            drone = Place("Dron Supervisor",stand + Vector3.right * 4);
            drone.transform.LookAt(stand);
            bool sawProjectile = false;
            int droneHP = drone.Health;
            before = Player.Stats.Health;
            await Until(() => {
                sawProjectile |= Object.FindObjectsByType<CombatProjectile>().Length > 0;
                return Player.Stats.Health < before;
            },"El proyectil del dron no alcanzó al jugador",14);
            Require(sawProjectile && drone.Health == droneHP,"Proyectil visible y exclusión del emisor");
            drone.TakeDamage(drone.Health);
            Require(!drone.Alive,"Muerte del dron");
            return "Bofetada FPS, daño y recuperación de knockback del empleado, ataque corto, disparo visible con impacto y muerte de ambos arquetipos.";
        }
        finally
        {
            if (employee) Object.Destroy(employee.gameObject);
            if (drone) Object.Destroy(drone.gameObject);
        }
    }

    public static async Task<string> All()
    {
        Application.runInBackground = true;
        Require(Session.Current.number == 9 && Session.PlayerCount == 1, "Iniciar Play directamente en Piso9");
        var director = Object.FindAnyObjectByType<OfficeDirector>();
        var terminal = Object.FindAnyObjectByType<OfficeTerminal>();
        var chest = Object.FindAnyObjectByType<FloorObjective>(FindObjectsInactive.Include);
        int money = Player.Stats.Money, xp = Player.Stats.Experience;
        Require(!chest.isActiveAndEnabled && chest.Interact(Player) == "Objetivo bloqueado", "Cofre prematuro");
        Require(Exit.Request(Player,0) != null && terminal.Interact(Player) == "Terminal en espera", "Bloqueo previo");
        var graph = Inspect(out int edges);
        var route = Route(graph,71);
        Require(edges > 72, "El laberinto necesita circuitos alternativos");
        var origin = Player.transform.position;
        MoveTo(Vector3.down * 9);
        await WaitForRecovery(origin);
        Walk(Center(route[1]));
        Walk(Center(route[2]));
        await Until(() => director.Phase == OfficeDirector.Stage.Pursuit,"No comenzó la persecución");
        var hunter = Object.FindObjectsByType<OfficeActor>().Single(a => a.Role == OfficeActor.Kind.Corruptor);
        var agent = hunter.GetComponent<NavMeshAgent>();
        int hp = hunter.Health;
        hunter.ReceiveHit(hp*2,hunter.transform.position,Vector3.right,new Knockback(20,15,.5f,.2f,true),Player.transform);
        Require(hunter.Health == hp && agent.updatePosition, "El perseguidor recibió daño o impulso");
        // Put a player inside the frontal arc. Observe the real windup, dodge, then let the second swing connect.
        agent.Warp(Center(40));
        hunter.transform.rotation = Quaternion.identity;
        MoveTo(Center(40) + Vector3.forward * 1.8f);
        await Until(() => hunter.transform.Find("Model/Torso/ArmR").localEulerAngles.x > 280 &&
            hunter.transform.Find("Model/Torso/ArmR").localEulerAngles.x < 336,"Anticipación del perseguidor");
        MoveTo(Center(40) + Vector3.back * 4.3f);
        float dodgedAt = Time.time;
        await Until(() => Time.time > dodgedAt + .8f,"Esquiva");
        Require(Player.Stats.Health == PlayerStats.MaxHealth,"El golpe fallido hizo daño");
        bool eliminated = false;
        void ObserveDeath() { if (Player.Stats.Health == 0) eliminated = true; }
        Player.Stats.Changed += ObserveDeath;
        try
        {
            await Until(() => {
                if (hunter && !eliminated)
                    MoveTo(hunter.transform.position + hunter.transform.forward * 1.55f);
                return eliminated;
            },"El ataque conectado no eliminó al jugador",12);
        }
        finally { Player.Stats.Changed -= ObserveDeath; }
        await WaitForRecovery(origin);
        foreach (int cell in route) Walk(Center(cell));
        await Until(() => director.Phase == OfficeDirector.Stage.Window,"No se activó la ventana");
        hunter = Object.FindObjectsByType<OfficeActor>().Single(a => a.Role == OfficeActor.Kind.Corruptor);
        var identity = hunter.GetEntityId();
        await Until(() => director.Phase == OfficeDirector.Stage.Preparation,"No terminó la caída");
        Require(!hunter.gameObject.activeSelf,"El perseguidor sigue activo");
        Require(!chest.isActiveAndEnabled,"Premio antes de la defensa");
        MoveTo(terminal.transform.position + Vector3.back * 1.75f - Vector3.up * 1.15f);
        Require(Use(terminal).Contains("iniciada") && terminal.Interact(Player) == "Terminal en espera","Activación única");
        int employees = 0, drones = 0;
        var waves = new HashSet<OfficeDirector.Stage>();
        // Advance real spawners, budgets and death handlers. Weapon-specific impacts have a separate suite.
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 3;
            await Until(() => {
                waves.Add(director.Phase);
                foreach (var actor in Object.FindObjectsByType<OfficeActor>())
                {
                    if (actor.Role == OfficeActor.Kind.Corruptor || !actor.Alive) continue;
                    if (actor.Role == OfficeActor.Kind.Drone) drones++; else employees++;
                    actor.ReceiveHit(actor.Health,actor.transform.position,Vector3.forward,0,Player.transform);
                }
                return director.Phase == OfficeDirector.Stage.FinalCombat;
            },"Oleadas bloqueadas",100);
            Require(waves.Contains(OfficeDirector.Stage.WaveOne) && waves.Contains(OfficeDirector.Stage.WaveTwo) &&
                employees >= 28 && drones >= 3,"Composición de las dos primeras oleadas");
            Require(hunter.GetEntityId() == identity && hunter.IsBoss && hunter.Alive,"Identidad del jefe");
            int thirdEmployees = 0, thirdDrones = 0;
            await Until(() => {
                foreach (var add in Object.FindObjectsByType<OfficeActor>())
                {
                    if (!add.Alive || add.Role == OfficeActor.Kind.Corruptor) continue;
                    if (add.Role == OfficeActor.Kind.Drone) thirdDrones++; else thirdEmployees++;
                    add.TakeDamage(add.MaximumHealth);
                }
                return thirdEmployees >= 10 && thirdDrones >= 2;
            },"Dotación inicial de la última oleada");
            Require(director.Phase == OfficeDirector.Stage.FinalCombat && !chest.isActiveAndEnabled,
                "Limpiar la dotación debe dejar al jefe activo y el cofre cerrado");
            hunter.TakeDamage(hunter.MaximumHealth / 3);
            Require(hunter.Health < .7f * hunter.MaximumHealth,"Etapa 2");
            hunter.TakeDamage(hunter.MaximumHealth / 3);
            Require(hunter.Health <= .35f * hunter.MaximumHealth,"Furia");
            var bossAgent = hunter.GetComponent<NavMeshAgent>();
            bossAgent.Warp(Center(71));
            hunter.transform.rotation = Quaternion.identity;
            MoveTo(Center(71)+Vector3.forward*1.8f);
            Player.Stats.Heal(PlayerStats.MaxHealth);
            int before = Player.Stats.Health;
            await Until(() => Player.Stats.Health < before, "El jefe no atacó");
            Require(Player.Stats.Health > 0,"Golpe letal habilitado en modo jefe");
            hunter.TakeDamage(hunter.Health);
            await Until(() => director.Phase == OfficeDirector.Stage.Completed,"No abrió la salida");
            float open = Time.time;
            await Until(() => Time.time > open + 1.1f,"Puertas");
        }
        finally { Time.timeScale = previousScale; }
        Require(chest.isActiveAndEnabled && Session.Progress.State(10)==FloorState.Locked,"Disponibilidad del cofre o llave");
        MoveTo(chest.transform.position + Vector3.back * 1.7f - Vector3.up * .7f);
        string reward = Use(chest);
        Require(reward.Contains(Session.Progress.Get(10).requiredItem.displayName), reward);
        Use(chest);
        Require(Player.Stats.Money==money+Session.Current.money && Player.Stats.Experience==xp+Session.Current.experience,"Cobro único");
        var zone = Exit.transform.Find("Zona de embarque").GetComponent<BoxCollider>();
        MoveTo(zone.transform.TransformPoint(new Vector3(1.45f,.1f,17.45f)));
        Require(Use(Exit.transform.Find("Piso 0").GetComponent<FloorButton>())==null,"Solicitud de regreso");
        await WaitForFloor(0);
        var secretary=Object.FindAnyObjectByType<FloorSecretary>();
        MoveTo(secretary.transform.position+secretary.transform.forward*2.5f);
        Use(secretary);
        Require(Session.Progress.State(10)==FloorState.Available && Session.Progress.State(2)==FloorState.Locked,"Llave de campaña");
        return $"72 sectores, {edges} pasos, {route.Count-1} tramos a pie. Esquiva, eliminación, caída y retorno del mismo jefe. {employees} empleados y {drones} drones antes del jefe. {reward}. Regreso y canje del piso 10.";
    }
}
