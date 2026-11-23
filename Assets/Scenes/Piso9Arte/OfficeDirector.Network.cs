using System.Collections.Generic;
using Hackaton.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    // Fixed-size plain data. IDs last for one visit and survive the Corruptor's fall and rebuild.
    internal struct OfficeActorFrame : INetworkSerializeByMemcpy
    {
        public int id, health, maximum;
        public Vector3 position;
        public float yaw, speed, progress;
        public byte kind, pose, visible, boss;

        public OfficeActorFrame(OfficeActor actor)
        {
            id = actor.OfficeId;
            kind = (byte)actor.Role;
            health = actor.Health;
            maximum = actor.MaximumHealth;
            position = actor.transform.position;
            yaw = actor.transform.eulerAngles.y;
            speed = actor.ShownSpeed;
            progress = actor.ShownPhase;
            pose = (byte)actor.ShownPose;
            visible = (byte)(actor.gameObject.activeSelf ? 1 : 0);
            boss = (byte)(actor.IsBoss ? 1 : 0);
        }
    }

    public sealed partial class OfficeDirector
    {
        internal static OfficeDirector Active { get; private set; }
        internal static bool Online => CampaignRoom.Current && CampaignRoom.Current.IsOnline;
        internal static bool IsGuest => Online && !CampaignRoom.Current.IsAuthority;
        internal static NetworkTiza Publisher => Online && NetworkTiza.Local && NetworkTiza.Local.IsSpawned &&
            NetworkTiza.Local.IsServer ? NetworkTiza.Local : null;

        readonly List<OfficeActor> publishedActors = new();
        readonly Dictionary<int,OfficeActor> replicas = new();
        readonly HashSet<int> seenIds = new();
        readonly List<int> expiredIds = new();
        int nextActorId, replicaRemaining;
        float sendAt;
        Transform inactiveParent;

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        void LateUpdate()
        {
            if (IsGuest)
            {
                if (shot && monster) AimIncidentCamera(monster.transform.position);
                return;
            }
            var peer = Publisher;
            if (!peer || peer.NetworkManager.ConnectedClientsIds.Count < 2 || Time.unscaledTime < sendAt) return;
            sendAt = Time.unscaledTime + .1f;
            publishedActors.RemoveAll(actor => !actor);
            var frames = new OfficeActorFrame[publishedActors.Count];
            for (int i = 0; i < frames.Length; i++) frames[i] = new OfficeActorFrame(publishedActors[i]);
            byte archives = 0;
            for (int i = 0; i < obstructions.Count; i++)
                if (obstructions[i]) archives |= (byte)(1 << i);
            peer.OfficeStateRpc((byte)Phase,crew.Count + pendingEmployees + pendingDrones,closing,
                breach.gameObject.activeSelf,archives,frames);
        }

        internal void ReceiveState(Stage phase, int remaining, bool finishing, bool glass, byte archives, OfficeActorFrame[] actors)
        {
            if (!IsGuest || !terminal) return;
            seenIds.Clear();
            foreach (var frame in actors)
            {
                if (!replicas.TryGetValue(frame.id,out var actor))
                {
                    if (!inactiveParent)
                    {
                        inactiveParent = new GameObject("Montaje de enemigos").transform;
                        inactiveParent.SetParent(transform);
                        inactiveParent.gameObject.SetActive(false);
                    }
                    var role = (OfficeActor.Kind)frame.kind;
                    var prefab = role == OfficeActor.Kind.Employee ? employee : role == OfficeActor.Kind.Drone ? supervisor : corruptor;
                    actor = Instantiate(prefab,inactiveParent);
                    actor.GetComponent<NavMeshAgent>().enabled = false;
                    actor.transform.SetPositionAndRotation(frame.position, Quaternion.Euler(0,frame.yaw,0));
                    actor.transform.SetParent(transform,true);
                    replicas.Add(frame.id,actor);
                    if (role == OfficeActor.Kind.Corruptor) monster = actor;
                }
                seenIds.Add(frame.id);
                actor.ApplyFrame(frame);
            }
            expiredIds.Clear();
            foreach (var pair in replicas)
                if (!seenIds.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                    Destroy(pair.Value.gameObject);
                    expiredIds.Add(pair.Key);
                }
            foreach (int id in expiredIds) replicas.Remove(id);

            replicaRemaining = remaining;
            closing = finishing;
            breach.gameObject.SetActive(glass);
            chest.gameObject.SetActive(phase == Stage.Completed);
            if (phase != Phase)
            {
                if (Phase == Stage.Window) RestoreCamera();
                if (phase == Stage.Window) StartIncidentCamera();
                if (phase == Stage.Exploration || phase == Stage.Window || phase == Stage.Completed) Fx.CancelShots();
                ClearArchives();
                if (phase == Stage.Pursuit) PlaceObstructions();
                var music = phase switch
                {
                    Stage.Pursuit => ShiftSound.Hunt,
                    Stage.WaveOne or Stage.WaveTwo => ShiftSound.Defense,
                    Stage.FinalCombat or Stage.Reconstruction => ShiftSound.Boss,
                    _ => ShiftSound.Hum
                };
                Set(phase,music);
            }
            for (int i = 0; i < obstructions.Count; i++)
                if (obstructions[i]) obstructions[i].gameObject.SetActive((archives & (1 << i)) != 0);
            Show();
        }

        internal OfficeActor Replica(int id) => replicas.TryGetValue(id,out var actor) ? actor : null;
    }
}
