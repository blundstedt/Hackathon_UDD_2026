using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hackaton.Levels;
using Hackaton.Multiplayer;
using Hackaton.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Stage = Hackaton.Levels.OfficeDirector.Stage;

// Run in a disposable host slot with one connected guest. Enter on host, then Guest and Host concurrently.
public static class VerifyOfficeOnline
{
    static FloorSession Session => FloorSession.Instance;
    static FloorPlayer Local => NetworkTiza.Local.Player;
    static OfficeDirector Floor => Object.FindAnyObjectByType<OfficeDirector>();

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static async Task Until(Func<bool> ready, string reason, int seconds = 45)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (!ready())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(reason);
            await Task.Yield();
        }
    }

    public static string Enter()
    {
        Check(Session.Current.number == 0 && Session.PlayerCount == 2 && NetworkTiza.Local.IsServer,
            "Se necesita un host y un invitado en el lobby de prueba");
        var save = Session.Capture();
        if (!save.available.Contains(9)) save.available.Add(9);
        Session.Restore(save,CampaignRoom.Current.Avatars);
        CampaignRoom.Current.StartTravel("Piso9");
        return "Viaje de ambos participantes al Piso 9";
    }

    static void Move(Vector3 point)
    {
        var controller = Local.GetComponent<CharacterController>();
        controller.enabled = false;
        Local.transform.position = point;
        controller.enabled = true;
    }

    static async Task Use(FloorInteraction target, Func<bool> succeeded)
    {
        Move(target.transform.position + Vector3.back * 1.5f - Vector3.up * .7f);
        float retry = 0;
        await Until(() => {
            if (succeeded()) return true;
            if (Time.unscaledTime >= retry)
            {
                NetworkTiza.Local.Interact(target);
                retry = Time.unscaledTime + .4f;
            }
            return false;
        },"La interacción del invitado no llegó al host");
    }

    public static async Task<string> Guest()
    {
        Check(!CampaignRoom.Current.IsAuthority && Session.PlayerCount == 2 && Floor,"Requiere invitado en Piso9");
        var floor = Floor;
        var chest = Object.FindAnyObjectByType<FloorObjective>(FindObjectsInactive.Include);
        Check(!chest.isActiveAndEnabled,"Cofre visible antes de tiempo");
        int startMoney = Local.Stats.Money;
        Move(new Vector3(25.6f,.2f,0));
        await Until(() => floor.Phase == Stage.Pursuit,"Persecución ausente");
        var hunter = Object.FindObjectsByType<OfficeActor>().Single(a => a.Role == OfficeActor.Kind.Corruptor);
        Check(!hunter.GetComponent<NavMeshAgent>().enabled,"El invitado ejecuta su propia IA");
        int hp = hunter.Health;
        hunter.TakeDamage(hp);
        Check(hunter.Health == hp,"El invitado pudo cambiar la salud");
        Move(new Vector3(102.4f,.15f,89.6f));
        await Until(() => floor.Phase == Stage.Window,"Incidente ausente");
        await Until(() => floor.Phase == Stage.Preparation,"El incidente no terminó");
        Check(!hunter.gameObject.activeSelf,"El Corruptor no cayó");
        await Use(Object.FindAnyObjectByType<OfficeTerminal>(),() => floor.Phase == Stage.WaveOne);
        var phases = new HashSet<Stage>();
        var roles = new HashSet<OfficeActor.Kind>();
        await Until(() => {
            phases.Add(floor.Phase);
            foreach (var actor in Object.FindObjectsByType<OfficeActor>()) roles.Add(actor.Role);
            return floor.Phase == Stage.FinalCombat;
        },"Faltan oleadas",160);
        Check(phases.Contains(Stage.WaveTwo) && phases.Contains(Stage.Reconstruction) &&
            roles.Contains(OfficeActor.Kind.Employee) && roles.Contains(OfficeActor.Kind.Drone),"Faltaron etapas o enemigos");
        Check(hunter && hunter.IsBoss && hunter.Alive && !chest.isActiveAndEnabled,"Reconstrucción o cofre incorrectos");
        await Until(() => hunter && hunter.Health < hunter.MaximumHealth,"Vida del jefe sin sincronizar");
        await Until(() => floor.Phase == Stage.Completed && chest.isActiveAndEnabled,"Salida bloqueada");
        await Use(chest,() => Session.Progress.State(9) == FloorState.Completed);
        Check(Local.Stats.Money == startMoney + Session.Current.money,"Recompensa del invitado");
        return "Invitado: persecución, ventana, terminal remota, empleados, drones, dos oleadas, reconstrucción del mismo jefe, vida, cofre y recompensa.";
    }

    public static async Task<string> Host()
    {
        Check(CampaignRoom.Current.IsAuthority && Session.PlayerCount == 2 && Floor,"Requiere host en Piso9");
        var floor = Floor;
        int startMoney = Local.Stats.Money;
        await Until(() => floor.Phase == Stage.WaveOne,"Terminal del invitado",90);
        // Exercise actual budgets, deaths and phase gates, with enough time for each replicated wave to be observed.
        var firstSeen = new Dictionary<OfficeActor,float>();
        float oldScale = Time.timeScale;
        try
        {
            Time.timeScale = 2;
            await Until(() => {
                foreach (var actor in Object.FindObjectsByType<OfficeActor>())
                {
                    if (!actor.Alive || actor.Role == OfficeActor.Kind.Corruptor) continue;
                    if (!firstSeen.TryGetValue(actor,out float first)) firstSeen[actor] = Time.unscaledTime;
                    else if (Time.unscaledTime-first > .55f) actor.TakeDamage(actor.MaximumHealth);
                }
                return floor.Phase == Stage.FinalCombat;
            },"Presupuesto de oleadas bloqueado",150);
            var boss = Object.FindObjectsByType<OfficeActor>().Single(a => a.IsBoss);
            boss.TakeDamage(boss.MaximumHealth / 2);
            float until = Time.unscaledTime + 2;
            await Until(() => Time.unscaledTime >= until,"Pausa de replicación");
            boss.TakeDamage(boss.Health);
            await Until(() => Session.Progress.State(9) == FloorState.Completed,"El invitado no pudo cobrar");
            Check(Local.Stats.Money == startMoney + Session.Current.money,"Recompensa del host");
            return "Host: terminal activada por el invitado, oleadas agotadas, jefe derrotado y recompensa compartida.";
        }
        finally { Time.timeScale = oldScale; }
    }
    public static async Task<string> Return()
    {
        await Until(() => NetworkTiza.Local && Session.Current && Session.PlayerCount == 2,"Carga de los jugadores");
        Check(Session.Progress.State(9) == FloorState.Completed,"Se necesita el turno completado");
        bool host = NetworkTiza.Local.IsServer;
        int balance = Local.Stats.Money;
        float retry = 0;
        if (Floor)
        {
            var exit = Object.FindObjectsByType<TowerFloorPanel>().Single(panel => panel.IsExit);
            var zone = exit.transform.Find("Zona de embarque");
            var button = exit.transform.Find("Piso 0").GetComponent<FloorButton>();
            Move(zone.TransformPoint(new Vector3(host ? 1.45f : -.7f,.1f,17.45f)));
            await Until(() => {
                if (Session.Current && Session.Current.number == 0 && Session.PlayerCount == 2 && NetworkTiza.Local) return true;
                if (host && button && !Session.Traveling && Time.unscaledTime >= retry)
                {
                    NetworkTiza.Local.Interact(button);
                    retry = Time.unscaledTime + 1;
                }
                return false;
            },"Regreso conjunto al lobby");
        }
        if (!host)
        {
            var reception = Object.FindAnyObjectByType<FloorSecretary>();
            Move(reception.transform.position + reception.transform.forward * 2.5f);
            await Until(() => {
                if (Session.Progress.State(10) == FloorState.Available) return true;
                if (Time.unscaledTime >= retry) { NetworkTiza.Local.Interact(reception); retry = Time.unscaledTime + .5f; }
                return false;
            },"Entrega de la llave");
        }
        else await Until(() => Session.Progress.State(10) == FloorState.Available,"Llave compartida");
        Check(Local.Stats.Money == balance,"El ascensor duplicó la recompensa");
        return "Dos jugadores en el lobby, mismo saldo y piso 10 desbloqueado.";
    }
}
