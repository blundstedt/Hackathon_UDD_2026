using System;
using System.Linq;
using System.Threading.Tasks;
using Hackaton.Player;
using Hackaton.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Start Play directly in Piso9 and focus the Game view. Uses the real E binding.
public static class VerifyElevatorPress
{
    static async Task Until(Func<bool> ready, string reason)
    {
        double deadline = EditorApplication.timeSinceStartup + 8;
        while (!ready())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(reason);
            await Task.Yield();
        }
    }

    static void Check(bool valid, string reason)
    {
        if (!valid) throw new InvalidOperationException(reason);
    }

    static void CheckLegend(ElevatorKeycap key, bool lit)
    {
        var data = new SerializedObject(key);
        var legend = (Renderer)data.FindProperty("legend").objectReferenceValue;
        var expected = (Material)data.FindProperty(lit ? "lit" : "dark").objectReferenceValue;
        Check(expected && legend.sharedMaterial == expected && key.Illuminated == lit, "Material de la leyenda");
    }

    public static async Task<string> All()
    {
        var session = FloorSession.Instance;
        Check(session && session.Current.number == 9 && session.PlayerCount == 1, "Iniciar Play en el Piso 9");
        var player = session.Players.Single();
        var input = player.GetComponent<PlayerInputReader>();
        Check(input.HasControl, "La vista Game necesita foco y cursor capturado");
        var panel = Object.FindObjectsByType<TowerFloorPanel>().Single(p => !p.IsExit);
        var blocked = panel.transform.Find("Piso 2").GetComponent<FloorButton>();
        var exit = panel.transform.Find("Piso 0").GetComponent<FloorButton>();
        CheckLegend(exit.Keycap,true);
        CheckLegend(blocked.Keycap,false);
        var hand = new SerializedObject(player.GetComponent<ElevatorHandPress>());
        var wrist = (Renderer)hand.FindProperty("wristExtension").objectReferenceValue;
        var tip = (Transform)hand.FindProperty("left.tip").objectReferenceValue;
        var eye = player.GetComponentInChildren<Camera>().transform;
        var body = player.GetComponent<CharacterController>();
        Vector3 origin = player.transform.position;
        Quaternion initialAim = eye.localRotation;
        var cabin = panel.transform.Find("Zona de embarque");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        bool down = false;
        void Queue()
        {
            if (InputState.currentUpdateType == InputUpdateType.Dynamic)
                InputSystem.QueueStateEvent(keyboard,down ? new KeyboardState(Key.E) : new KeyboardState());
        }
        void Move(Vector3 point)
        {
            body.enabled = false;
            player.transform.position = point;
            body.enabled = true;
            Physics.SyncTransforms();
        }
        async Task Begin(FloorButton button)
        {
            eye.LookAt(button.GetComponent<Collider>().bounds.center);
            int settle = Time.frameCount + 2;
            await Until(() => Time.frameCount >= settle, "Apuntar a la tecla");
            down = true;
            await Until(() => wrist.enabled, "E no inició el gesto de pulsación");
            down = false;
        }

        float oldScale = Time.timeScale;
        InputSystem.onBeforeUpdate += Queue;
        try
        {
            Time.timeScale = .5f;
            Move(cabin.TransformPoint(new Vector3(1.45f,.15f,17.45f)));
            Vector3 rest = blocked.Keycap.Surface;
            await Begin(blocked);
            await Until(() => Vector3.Dot(blocked.Keycap.Surface-rest,blocked.Keycap.Inward) > .001f,
                "La tecla bloqueada no se hundió");
            Check(Vector3.Distance(tip.position,blocked.Keycap.Surface) < .06f,"El dedo no alcanzó la tecla");
            await Until(() => input.HasControl && !wrist.enabled,"La mano no volvió a reposo");
            Check((blocked.Keycap.Surface-rest).sqrMagnitude < .000001f && !session.Traveling,"Retorno de la tecla bloqueada");
            await Begin(exit);
            eye.rotation = Quaternion.LookRotation(-exit.Keycap.Inward,Vector3.up);
            await Until(() => input.HasControl && !wrist.enabled,"El gesto no se canceló al apartar la mira");
            Check(!session.Traveling && session.Current.number == 9,"Viaje al perder el objetivo antes del contacto");
            return "Leyendas activa e inactiva, E en primera persona, contacto del dedo, recorrido y retorno de la tecla y cancelación del viaje.";
        }
        finally
        {
            InputSystem.onBeforeUpdate -= Queue;
            InputSystem.RemoveDevice(keyboard);
            Time.timeScale = oldScale;
            if (body) Move(origin);
            if (eye) eye.localRotation = initialAim;
        }
    }
}
