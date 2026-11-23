using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hackaton.Combat;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Run All in a fresh Play session in UrbanLobby_new. Distances are in metres.
public static class VerifyOfficeMaze
{
    const float Cell = 12.8f;
    const float Radius = 1.55f;
    const float Height = 3.8f;
    static FloorSession Session => FloorSession.Instance;
    static FloorPlayer Player => Session.Players.Single();
    static TowerFloorPanel Panel => Object.FindObjectsByType<TowerFloorPanel>().Single(panel => !panel.IsExit);
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

    static async Task UseCabin(int destination)
    {
        var zone=Panel.transform.Find("Zona de embarque").GetComponent<BoxCollider>();
        MoveTo(zone.transform.TransformPoint(new Vector3(1.8f,.12f,17.6f)));
        string reason = Use(Panel.transform.Find("Piso "+destination).GetComponent<FloorButton>());
        Require(reason == null, reason);
        await WaitForFloor(destination);
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

    public static async Task<string> All()
    {
        Application.runInBackground = true;
        Require(Session.Current.number==0 && Session.Progress.State(2)==FloorState.Locked && Session.Progress.State(10)==FloorState.Locked,"Iniciar en el lobby en una partida nueva");
        await UseCabin(9);
        await UseCabin(0);
        Require(Session.Progress.State(9)==FloorState.Available && Session.Progress.State(10)==FloorState.Locked,"Abandono anticipado");
        await UseCabin(9);
        var safe=Object.FindAnyObjectByType<FloorSpawn>().transform.position;
        MoveTo(Vector3.down*9);
        await WaitForRecovery(safe);
        Physics.SyncTransforms();
        var graph=Inspect(out int edges);
        Require(edges==83,"Pasos encontrados: "+edges);
        var box=Object.FindAnyObjectByType<FloorObjective>();
        var p=box.transform.position;
        int goal=Mathf.RoundToInt(p.x/Cell)+9*Mathf.RoundToInt(p.z/Cell);
        var zone=Exit.transform.Find("Zona de embarque").GetComponent<BoxCollider>();
        Require(goal==71 && zone.bounds.Contains(p),"El módulo debe estar dentro de la cabina de la esquina opuesta");
        var route=Route(graph,goal);
        foreach(var index in route) Walk(Center(index));
        Vector3 approach=Vector3.MoveTowards(p,Center(goal),1.85f);
        Walk(approach);
        int xp=Player.Stats.Experience, money=Player.Stats.Money;
        string reward=Use(box);
        Require(reward.Contains(Session.Progress.Get(10).requiredItem.displayName),"Llave incorrecta: "+reward);
        Require(Player.Stats.Experience==xp+Session.Current.experience && Player.Stats.Money==money+Session.Current.money,"Recompensa incorrecta");
        int paid=Player.Stats.Money, earned=Player.Stats.Experience;
        Use(box);
        Require(Player.Stats.Money==paid && Player.Stats.Experience==earned,"Cobro duplicado");
        Player.Stats.TakeDamage(PlayerStats.MaxHealth);
        await WaitForRecovery(safe);
        Require(Player.Stats.Money==paid && Player.Stats.Experience==earned && Session.Progress.State(9)==FloorState.Completed,"Recuperación altera el progreso");
        foreach(var index in route) Walk(Center(index));
        Walk(zone.transform.TransformPoint(new Vector3(1.45f,.1f,17.45f)));
        Require(Use(Exit.transform.Find("Piso 0").GetComponent<FloorButton>())==null,"Salida al lobby denegada");
        await WaitForFloor(0);
        Require(Player.Stats.Money==paid && Player.Stats.Experience==earned && Session.Progress.State(10)==FloorState.Locked,"El viaje altera recompensa o llave");
        Require(Session.Progress.State(1)==FloorState.Available && Session.Progress.State(2)==FloorState.Locked,"Progreso ajeno al piso alterado");
        var secretary=Object.FindAnyObjectByType<FloorSecretary>();
        MoveTo(secretary.transform.position+secretary.transform.forward*2.5f);
        Use(secretary);
        Require(Session.Progress.State(10)==FloorState.Available && Session.Progress.State(2)==FloorState.Locked,"Recepción debe desbloquear únicamente el piso 10");
        return $"72 sectores, {edges} conexiones. {route.Count-1} tramos a pie entre cabinas. {reward}. Llave del 10 canjeada.";
    }

    public static async Task<string> ExitOnly()
    {
        Application.runInBackground = true;
        await UseCabin(9);
        var zone=Exit.transform.Find("Zona de embarque").GetComponent<BoxCollider>();
        int expected=Player.Stats.Experience+Session.Current.experience;
        MoveTo(zone.transform.TransformPoint(new Vector3(1.45f,.1f,17.45f)));
        Require(Use(Exit.transform.Find("Piso 0").GetComponent<FloorButton>())==null,"Petición de salida rechazada");
        await WaitForFloor(0);
        Require(Player.Stats.Experience==expected && Session.Progress.State(9)==FloorState.Completed,"Premio automático de embarque incorrecto");
        var secretary=Object.FindAnyObjectByType<FloorSecretary>();
        MoveTo(secretary.transform.position+secretary.transform.forward*2.5f);
        Use(secretary);
        Require(Session.Progress.State(10)==FloorState.Available && Session.Progress.State(2)==FloorState.Locked,"Llave de embarque incorrecta");
        return "Embarque de salida concede una recompensa y la llave para el piso 10.";
    }
}
