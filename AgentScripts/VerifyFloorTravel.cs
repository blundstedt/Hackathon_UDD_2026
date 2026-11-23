using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hackaton.Player;
using Hackaton.Lobby;
using Hackaton.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Unity Pipeline: run All in a fresh Play session started in UrbanLobby_new.
public static class VerifyFloorTravel
{
    private static FloorSession Session => FloorSession.Instance;
    private static FloorPlayer Local => Session.Players.Single(p => p.Id == "local");
    private static TowerFloorPanel Panel => Object.FindAnyObjectByType<TowerFloorPanel>();
    private static FloorButton Button(int number) => Panel.transform.Find($"Piso {number}").GetComponent<FloorButton>();
    private static FloorSecretary Secretary => Object.FindAnyObjectByType<FloorSecretary>();
    private static Text Hint => Session.Screen.GetComponentsInChildren<Text>().Single(t => t.name == "Interacción");
    private static readonly List<string> checks = new();

    private static void Check(bool valid, string label)
    {
        if (!valid) throw new Exception(label + $" [piso {Session.Current.number}, grupo {Session.PlayerCount}, texto '{Hint.text}']");
        checks.Add(label);
    }

    private static async Task Until(Func<bool> ready, string reason)
    {
        double deadline = EditorApplication.timeSinceStartup + 18;
        while (!ready())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(reason);
            await Task.Yield();
        }
    }

    private static void Move(FloorPlayer player, Vector3 position)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = position;
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    // The meadow puts its objective across the clearing. Walk the real capsule to interaction range.
    private static void WalkToObjective(FloorObjective objective)
    {
        var controller = Local.GetComponent<CharacterController>();
        var motion = new Hackaton.Combat.CharacterImpactMotion();
        Vector3 target = objective.transform.position - Vector3.forward * 2f;
        float vertical = 0f;
        for (int i = 0; i < 900; i++)
        {
            Vector3 delta = Vector3.ProjectOnPlane(target - Local.transform.position, Vector3.up);
            if (delta.magnitude < .15f) return;
            motion.Move(controller, delta.normalized * 4.5f, ref vertical, 20f, 1f / 60f);
        }
        throw new Exception("El controlador no puede llegar a la caja por la pradera");
    }

    private static void Board(FloorPlayer player, TowerFloorPanel panel = null)
    {
        var zone = (panel ? panel : Panel).transform.Find("Zona de embarque").GetComponent<BoxCollider>();
        Move(player,zone.transform.TransformPoint(new Vector3(zone.center.x+1.4f,0.2f,zone.center.z)));
    }

    private static string Press(FloorInteraction target)
    {
        var eye = Local.GetComponentInChildren<Camera>();
        var rotation = eye.transform.rotation;
        eye.transform.LookAt(target.GetComponent<Collider>().bounds.center);
        var aim = typeof(FloorPlayer).GetMethod("AimTarget",BindingFlags.NonPublic|BindingFlags.Instance);
        var seen = aim.Invoke(Local,null) as FloorInteraction;
        eye.transform.rotation = rotation;
        Check(seen == target, "Rayo llega a " + target.name);
        return target.Interact(Local);
    }

    private static FloorPlayer Join(string id)
    {
        var stage = new GameObject("Participante de prueba");
        stage.SetActive(false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerTizaManosVacias.prefab");
        var go = Object.Instantiate(prefab,stage.transform);
        var player = go.GetComponent<FloorPlayer>();
        player.SetIdentity(id);
        go.GetComponent<PlayerInputReader>().enabled = false;
        go.GetComponent<PlayerMovement>().enabled = false;
        go.GetComponent<PlayerView>().enabled = false;
        go.GetComponentInChildren<Camera>().gameObject.SetActive(false);
        go.GetComponent<PlayerStatsHud>().enabled = false;
        go.transform.SetPositionAndRotation(Local.transform.position + Vector3.right*12,Quaternion.identity);
        go.transform.SetParent(null,true);
        Object.Destroy(stage);
        return player;
    }

    private static async Task Arrive(int number)
    {
        await Until(() => Session.Current.number == number && !Session.Traveling && Session.PlayerCount > 0,
            "No se llegó al piso "+number);
        Check(Object.FindObjectsByType<FloorSession>().Length == 1,"Una sesión al llegar al piso "+number);
    }

    // Separate fresh session. This path drives the same E binding as a human player.
    public static async Task<string> KeyboardFlow()
    {
        checks.Clear();
        Check(Local.GetComponent<PlayerInputReader>().HasControl, "Vista Game enfocada y cursor capturado");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        bool down = false;
        void QueueKey()
        {
            if (InputState.currentUpdateType == InputUpdateType.Dynamic)
                InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(Key.E) : new KeyboardState());
        }
        async Task Frames(int count)
        {
            int first = Time.frameCount;
            await Until(() => Time.frameCount >= first + count, "No avanzan los frames");
        }
        async Task Use(FloorInteraction target)
        {
            var eye = Local.GetComponentInChildren<Camera>();
            eye.transform.LookAt(target.GetComponent<Collider>().bounds.center);
            await Frames(2);
            down = true;
            await Frames(2);
            down = false;
            await Frames(2);
        }

        InputSystem.onBeforeUpdate += QueueKey;
        try
        {
            Board(Local);
            await Use(Button(1));
            await Arrive(1);
            var objective = Object.FindAnyObjectByType<FloorObjective>();
            WalkToObjective(objective);
            await Use(objective);
            Check(Session.Progress.State(1) == FloorState.Completed, "E recoge la llave en la pradera");
            Board(Local);
            await Use(Button(0));
            await Arrive(0);
            var secretary = Secretary;
            Move(Local, secretary.transform.position + secretary.transform.forward * 2.5f);
            await Use(secretary);
            Check(Session.Progress.State(2) == FloorState.Available, "E entrega la llave a la secretaria");
        }
        finally
        {
            InputSystem.onBeforeUpdate -= QueueKey;
            InputSystem.RemoveDevice(keyboard);
        }
        return string.Join("\n", checks);
    }
    public static async Task<string> All()
    {
        checks.Clear();
        Check(Session.Current.number==0 && Session.PlayerCount==1 && Local.GetComponent<PlayerInputReader>().enabled && Session.Progress.State(1)==FloorState.Available
            && Session.Progress.State(2)==FloorState.Locked,"Inicio y exclusión del muñeco de práctica");
        var authority = Session;
        var data = Session.Progress.Get(1);
        int initialMoney = Local.Stats.Money, initialXp = Local.Stats.Experience;
        Board(Local);
        Check(Press(Button(0))=="Piso actual" && Press(Button(2))=="No disponible","Botones actual y bloqueado");
        var extras = new[]{Join("dos"),Join("tres"),Join("cuatro")};
        Board(extras[0]); Board(extras[1]);
        Press(Button(1)); Press(Button(1));
        await Until(()=>Hint.text=="Esperando jugadores... 3/4","No se mostró 3/4");
        Check(!Session.Traveling,"Espera 3/4 sin carga duplicada");
        var lateJoin = Join("incorporación");
        await Until(() => Hint.text == "Esperando jugadores... 3/5", "No contó al jugador que se incorporó durante la espera");
        lateJoin.gameObject.SetActive(false);
        await Until(() => Hint.text == "Esperando jugadores... 3/4", "No descontó la baja durante la espera");
        Check(!Session.Traveling, "Altas y bajas conservan la solicitud mientras falta un jugador");
        Move(Local, Panel.transform.TransformPoint(new Vector3(0,0.2f,9)));
        await Until(()=>Hint.text!="Esperando jugadores... 3/4","No se canceló la espera");
        Board(extras[2]); Board(Local);
        await Task.Yield();
        Check(!Session.Traveling && Session.Current.number==0,"Salir de la cabina cancela la solicitud");
        Move(extras[2],Panel.transform.TransformPoint(new Vector3(0,0.2f,9)));
        Press(Button(1));
        await Until(()=>Hint.text=="Esperando jugadores... 3/4","Espera después de cancelar");
        Board(extras[2]);
        await Until(()=>Session.Traveling,"Cuarto jugador no inició el viaje");
        Check(Session.Screen.GetComponentInChildren<CanvasGroup>(true).gameObject.activeSelf,"Cortina de carga activa");
        await Arrive(1);
        Check(Session.TravelBlock(2)=="No disponible","Solo el botón 0 navega desde el nivel");
        Board(Local);
        var other=Join("dos");
        Press(Button(0));
        await Until(()=>Hint.text=="Esperando jugadores... 1/2","No esperó en el regreso");
        other.gameObject.SetActive(false);
        await Arrive(0);
        Check(Session.Progress.State(1)==FloorState.Available && Local.Stats.Money==initialMoney &&
            Local.Stats.Experience==initialXp,"Abandono sin recompensas y regreso al desconectar");
        var secretary = Secretary;
        Move(Local,secretary.transform.position+secretary.transform.forward*2.5f);
        Check(Press(secretary)=="Necesitas el objeto de desbloqueo","Secretaria sin llave");
        var station = Object.FindAnyObjectByType<LobbyEquipmentStation>();
        var options = (Array)typeof(LobbyEquipmentStation).GetField("options",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(station);
        typeof(LobbyEquipmentStation).GetMethod("Equip",BindingFlags.NonPublic|BindingFlags.Instance)
            .Invoke(station,new[]{options.GetValue(1)});
        var equipped = Session.Equipment(Local.Id);
        Check(equipped && Local.GetComponent<BastonAttack>().enabled, "Equipamiento asociado al jugador");
        Board(Local);
        Press(Button(1));
        await Arrive(1);
        Check(Local.GetComponent<BastonAttack>().enabled,"Equipamiento al aparecer en el piso");
        var teammate = Join("tres");
        var offline = Join("ausente");
        offline.gameObject.SetActive(false);
        var objective = Object.FindAnyObjectByType<FloorObjective>();
        WalkToObjective(objective);
        Check(Press(objective).Contains(data.rewardItem.displayName),"Finalización mediante el objetivo");
        Check(Session.Progress.State(1)==FloorState.Completed && Session.Progress.State(2)==FloorState.Locked &&
            Local.Stats.Money==initialMoney+data.money && Local.Stats.Experience==initialXp+data.experience &&
            teammate.Stats.Money==data.money && teammate.Stats.Experience==data.experience,"Premios individuales y llave de equipo");
        Check(Press(objective)=="Nivel completado" && teammate.Stats.Money==data.money,"Segunda finalización sin duplicados");
        offline.gameObject.SetActive(true);
        Check(offline.Stats.Money==data.money && offline.Stats.Experience==data.experience,"Participante reconectado cobra una vez");
        offline.gameObject.SetActive(false);
        teammate.gameObject.SetActive(false);
        Board(Local); Press(Button(0));
        await Arrive(0);
        Check(Local.GetComponent<BastonAttack>().enabled,"Equipamiento restaurado en el lobby");
        Check(Session==authority && Local.Stats.Money==initialMoney+data.money && Local.Stats.Experience==initialXp+data.experience,
            "Autoridad y saldo conservados entre escenas");
        Board(Local);
        Check(Press(Button(1))=="Nivel completado","Piso completado no repetible");
        secretary=Secretary;
        Move(Local,secretary.transform.position+secretary.transform.forward*2.5f);
        Check(secretary.Prompt(Session)=="Presiona E para entregar el objeto","Llave pendiente de entrega");
        Check(Press(secretary)=="¡Piso 2 desbloqueado!" && Session.Progress.State(2)==FloorState.Available,"Entrega efectiva");
        Check(!Press(secretary).Contains("¡Piso") && Session.Progress.State(3)==FloorState.Locked,"Entrega duplicada rechazada");
        var serialized = new SerializedObject(Panel);
        var digit = (Renderer)serialized.FindProperty("digits").GetArrayElementAtIndex(2).objectReferenceValue;
        Check(digit.sharedMaterial==serialized.FindProperty("illuminated").objectReferenceValue,"Botón 2 actualizado por evento");
        Board(Local);
        Press(Button(2)); await Arrive(2);
        var entry=GameObject.Find("Ascensor del atrio").GetComponent<TowerFloorPanel>();
        var entryZone=entry.transform.Find("Zona de embarque").GetComponent<BoxCollider>();
        Check(entryZone.bounds.Contains(Local.BodyCenter) && Local.GetComponent<BastonAttack>().enabled,
            "Llegada al atrio en la cabina con el equipo del lobby");
        int beforeMoney=Local.Stats.Money;
        Board(Local,entry); Press(entry.transform.Find("Piso 0").GetComponent<FloorButton>());
        await Arrive(0);
        Check(Session.Progress.State(2)==FloorState.Available && Local.Stats.Money==beforeMoney,"Salida temprana del atrio");
        Board(Local); Press(Button(2)); await Arrive(2);
        var diamond=Object.FindAnyObjectByType<FloorObjective>();
        var floor2=Session.Current;
        var exit=GameObject.Find("Ascensor de las recompensas").GetComponent<TowerFloorPanel>();
        var companion=Join("explorador");
        float ground=diamond.transform.parent.position.y;
        Move(Local,new Vector3(diamond.transform.position.x,ground+.1f,diamond.transform.position.z-2.4f));
        int EarnedXp(PlayerStats s)=>50*(s.Level-1)*s.Level+s.Experience;
        int beforeXp=EarnedXp(Local.Stats);
        Check(Press(diamond).Contains("Nivel completado") && floor2.rewardItem && !Session.Traveling,"El diamante completa el piso 2 y entrega la llave del piso 3");
        Check(Local.Stats.Money==beforeMoney+floor2.money && EarnedXp(Local.Stats)==beforeXp+floor2.experience &&
            companion.Stats.Money==floor2.money && EarnedXp(companion.Stats)==floor2.experience,"Premio compartido del piso 2");
        Check(Press(diamond)=="Nivel completado" && Local.Stats.Money==beforeMoney+floor2.money &&
            Session.Progress.State(3)==FloorState.Locked,"Diamante de un solo cobro y requisito del piso 3 cumplido");
        Board(Local,exit);
        Press(exit.transform.Find("Piso 0").GetComponent<FloorButton>());
        await Until(()=>Hint.text=="Esperando jugadores... 1/2","La cabina final debe esperar al grupo");
        Board(companion,exit); await Arrive(0);
        Check(Local.Stats.Money==beforeMoney+floor2.money && EarnedXp(Local.Stats)==beforeXp+floor2.experience,
            "Saldo y EXP del diamante de vuelta en el lobby");
        secretary=Secretary; Move(Local,secretary.transform.position+secretary.transform.forward*2.5f);
        Check(Press(secretary).Contains("¡Piso 3") && Session.Progress.State(3)==FloorState.Available && Session.Progress.State(9)==FloorState.Locked,"La llave del piso 2 desbloquea el piso 3 en recepción");
        var next=Session.Progress.Get(3);
        Check(next.rewardItem && next.rewardItem==Session.Progress.Get(9).requiredItem &&
            Session.Progress.Get(9).prerequisite==next,"El piso 3 entrega la llave para ir al piso 9");
        if (string.IsNullOrEmpty(next.sceneName))
            Check(Session.TravelBlock(3)=="Piso 3 aún no implementado","Sin viaje a una escena pendiente");
        return string.Join("\n",checks);
    }
}
