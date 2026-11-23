using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Hackaton.Multiplayer;
using Hackaton.Progression;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run in Play Mode via Unity Pipeline. Tests use their own directory outside the user's slots.
public static class VerifyCoop
{
    static CampaignRoom Room => CampaignRoom.Current;
    static SessionMenu Menu => Object.FindAnyObjectByType<SessionMenu>();
    static void Check(bool condition,string message) { if (!condition) throw new Exception(message); }
    static void Isolate()
    {
        var dir=Path.Combine(Application.temporaryCachePath,"CoopQA",Guid.NewGuid().ToString("N"));
        typeof(CampaignRoom).GetProperty("Slots").SetValue(Room,new CampaignSlots(dir));
    }
    public static string Slots()
    {
        var folder=Path.Combine(Application.temporaryCachePath,"CoopQA",Guid.NewGuid().ToString("N"));
        var store=new CampaignSlots(folder);
        var a=new CampaignSave(); a.available.Add(1); store.Write(1,a);
        var b=new CampaignSave(); b.completed.Add(3); store.Write(2,b);
        a.completed.Add(1); store.Write(1,a);
        Check(store.Read(1).completed.SequenceEqual(new[]{1}),"Recarga 1");
        Check(store.Read(2).completed.SequenceEqual(new[]{3}),"Recarga 2");
        string first=Path.Combine(folder,"partida-1.json"); File.WriteAllText(first,"{mal");
        Check(store.Read(1).available.Contains(1),"Recuperación desde copia");
        store.Delete(1);
        Check(store.Read(1)==null && store.Read(2).completed.Contains(3) && store.Read(3)==null,"Aislamiento");
        return "Guardado, recarga, recuperación y borrado aislados. "+folder;
    }
    public static string Interface()
    {
        var labels=Menu.GetComponentsInChildren<Button>().Select(b=>b.GetComponentInChildren<Text>().text).ToArray();
        Check(labels.Length==4 && labels.Contains("Hostear Partida") && labels.Contains("Salir Escritorio"),"Portada");
        Menu.HostPage(); Check(Menu.GetComponentsInChildren<Text>().Count(t=>t.text.StartsWith("PARTIDA "))==3,"Tres slots");
        Menu.Back(); Menu.JoinPage();
        Check(Menu.GetComponentInChildren<InputField>()!=null,"Campo de código");
        Menu.Back(); Menu.SettingsPage();
        Check(Menu.GetComponentsInChildren<Button>().Any(b=>b.name=="Audio"),"Opciones");
        Menu.Back();
        var texts=Menu.GetComponentsInChildren<Text>(true);
        Check(texts.All(t=>t.font && t.font.name.Contains("Dangrek")),"Fuente");
        return $"Cuatro acciones, tres slots, unión, opciones y {texts.Length} textos con Dangrek.";
    }
    public static string Host()
    {
        Isolate(); Menu.HostPage(); Menu.PlaySlot(1);
        return "Creación solicitada desde el menú, slot de prueba.";
    }
    public static string Offline()
    {
        Isolate(); Menu.HostPage(); Menu.ToggleLocal(); Menu.PlaySlot(1);
        return "Partida local solicitada.";
    }
    public static string Capacity()
    {
        var manager = NetworkManager.Singleton;
        if (!manager || !manager.IsHost || !Room.OwnsSave) throw new InvalidOperationException("Requiere un host de prueba");
        var disconnect = typeof(CampaignRoom).GetMethod("Disconnected", BindingFlags.NonPublic | BindingFlags.Instance);
        var temporary = new List<ulong>();
        try
        {
            for (int i = manager.ConnectedClientsIds.Count; i <= 4; i++)
            {
                ulong id = ulong.MaxValue - (ulong)i;
                var request = new NetworkManager.ConnectionApprovalRequest {
                    ClientNetworkId = id, Payload = Encoding.UTF8.GetBytes("qa-" + i) };
                var response = new NetworkManager.ConnectionApprovalResponse();
                manager.ConnectionApprovalCallback(request, response);
                if (response.Approved) temporary.Add(id);
                Check(response.Approved == (i < 4), "Aforo incorrecto");
                if (i == 4) Check(response.Reason.Contains("llena"), "Motivo de rechazo");
            }
        }
        finally
        {
            foreach (ulong id in temporary) disconnect.Invoke(Room, new object[] {id});
        }
        return "La aprobación del host admite cuatro identidades y rechaza la quinta.";
    }

    public static string ResumeOffline()
    {
        var save = Room.Slots.Read(1);
        if (save == null || !save.completed.Contains(1)) throw new Exception("Falta la prueba de Piso1");
        Room.Host(1, true);
        return "Continuando sin conexión el slot del host de prueba.";
    }

    public static string State()
    {
        var nm=NetworkManager.Singleton;
        return $"escena={SceneManager.GetActiveScene().name} busy={Room.Busy} mensaje={Room.LastMessage} codigo={Room.Code} " +
            $"host={nm && nm.IsHost} conectado={nm && nm.IsConnectedClient} id={(nm ? nm.LocalClientId : 0)} " +
            $"Tizas={Object.FindObjectsByType<NetworkTiza>().Length} camaras={Camera.allCamerasCount} oyentes={Object.FindObjectsByType<AudioListener>().Count(a=>a.enabled)} " +
            $"grupo={FloorSession.Instance?.PlayerCount} enemigos={Object.FindObjectsByType<Hackaton.Enemies.EnemyBase>().Length} " +
            string.Join(" | ",Object.FindObjectsByType<NetworkTiza>().Select(p=>$"{p.OwnerClientId}:{p.View?.transform.position}"));
    }
}
