using System;
using System.Collections;
using System.Collections.Generic;
using Hackaton.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace Hackaton.Levels
{
    [DisallowMultipleComponent]
    public sealed class ForestArena : MonoBehaviour
    {
        [Serializable] private class SpawnGroup
        {
            public ForestEnemy prefab;
            [Min(1)] public int count = 1;
            public Transform[] gates;
            [Min(.1f)] public float radius = 4;
            [Min(.05f)] public float interval = .55f;
            [Min(1)] public int maxActive = 19;
        }
        [Serializable] private class Wave
        {
            public string title;
            public SpawnGroup[] groups;
        }

        [SerializeField] private Wave[] waves;
        [SerializeField] private float arrivalSeconds = 6, restSeconds = 5;
        [SerializeField] private Material runeMaterial;
        [SerializeField] private ForestChest chest;
        [SerializeField] private Transform corruption, anomalies;
        [SerializeField] private ForestHud hud;
        [SerializeField] private Transform chestSite;
        private readonly HashSet<ForestEnemy> active = new();
        private readonly List<ForestEnemy> recovery = new();
        private NavMeshPath path;
        private NavMeshData navData;
        private NavMeshDataInstance nav;
        private Vector3 centre;
        private int unspawned;
        private float nextRecovery;
        private ForestEnemy boss;
        private bool dropped, cleared;
        public bool CanClaim => cleared && dropped;

        private void Awake()
        {
            // Bake once from scene colliders, before players and enemies begin moving.
            path = new NavMeshPath();
            var bounds = new Bounds(Vector3.zero, new Vector3(81,18,69));
            var sources = new List<NavMeshBuildSource>();
            Physics.SyncTransforms();
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);
            sources.RemoveAll(s => s.component is Collider c && (c.isTrigger || c is CharacterController || !c.enabled));
            navData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),sources,bounds,Vector3.zero,Quaternion.identity);
            if (navData) nav = NavMesh.AddNavMeshData(navData);
        }

        private IEnumerator Start()
        {
            if (!nav.valid || !NavMesh.SamplePosition(Vector3.zero,out var origin,4,NavMesh.AllAreas))
            {
                Debug.LogError("El claro no tiene una superficie de navegación central.",this);
                yield break;
            }
            centre = origin.position;
            while (!FloorSession.Instance || FloorSession.Instance.PlayerCount == 0) yield return null;
            hud.SetStatus("Bosque Mágico Corrupto", "Prepárate para defender el claro");
            yield return new WaitForSeconds(arrivalSeconds);
            for (int i=0; i<waves.Length; i++)
            {
                Wave wave = waves[i];
                foreach (var group in wave.groups) unspawned += group.count;
                hud.SetStatus($"Oleada {i+1}/{waves.Length}", wave.title);
                if (i>=1) corruption.gameObject.SetActive(true);
                if (i==2)
                {
                    anomalies.gameObject.SetActive(true);
                    FloorSession.Instance.Screen.Notice("El Coloso de Raíces está despertando");
                    yield return new WaitForSeconds(1.5f);
                }
                RefreshCount();
                foreach (var group in wave.groups) StartCoroutine(Spawn(group));
                while (unspawned>0 || active.Count>0) yield return null;
                hud.SetStatus($"Oleada {i+1} completada", i+1<waves.Length ? "Prepárate para la siguiente oleada" : "Abre el cofre con E");
                if (i+1<waves.Length) yield return new WaitForSeconds(restSeconds);
            }
            cleared = true;
            hud.MarkChest(chest.transform);
        }

        private IEnumerator Spawn(SpawnGroup group)
        {
            var wait = new WaitForSeconds(group.interval);
            var footprint = group.prefab.GetComponent<CharacterController>().radius;
            int gate = UnityEngine.Random.Range(0,group.gates.Length);
            for (int sent=0; sent<group.count;)
            {
                Vector3 anchor = group.gates[gate].position;
                gate = (gate+1)%group.gates.Length;
                if (active.Count>=group.maxActive || !FindSpawn(anchor,group.radius,footprint,out Vector3 position))
                { yield return wait; continue; }
                var ring = ForestRune.Create(transform,position,footprint+.45f,runeMaterial,
                    group.prefab.Rank == ForestRank.Colossus ? new Color(.95f,.38f,.8f) : new Color(.3f,1,.7f));
                yield return new WaitForSeconds(.58f);
                Destroy(ring.gameObject);
                if (active.Count>=group.maxActive || !Vacant(position,footprint)) { yield return wait; continue; }
                var enemy = Instantiate(group.prefab,position,Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(centre-position,Vector3.up).normalized),transform);
                active.Add(enemy);
                enemy.Defeated += OnDefeated;
                if (enemy.Rank == ForestRank.Colossus) { boss=enemy; hud.TrackBoss(enemy); }
                unspawned--;
                sent++;
                RefreshCount();
                yield return wait;
            }
        }

        private bool FindSpawn(Vector3 anchor,float radius,float footprint,out Vector3 position)
        {
            for (int i=0; i<10; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle*radius;
                var guess = anchor + new Vector3(offset.x,0,offset.y);
                if (!NavMesh.SamplePosition(guess,out var hit,3,NavMesh.AllAreas)) continue;
                if (!NavMesh.FindClosestEdge(hit.position,out var edge,NavMesh.AllAreas) || edge.distance<footprint*.75f) continue;
                if (!Vacant(hit.position,footprint)) continue;
                if (NavMesh.CalculatePath(centre,hit.position,NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete)
                { position=hit.position; return true; }
            }
            position=default;
            return false;
        }

        private static bool Vacant(Vector3 pos,float radius)
        {
            foreach (var player in FloorSession.Instance.Players)
                if ((player.transform.position-pos).sqrMagnitude<49) return false;
            float r = radius+.1f;
            return !Physics.CheckCapsule(pos+Vector3.up*(r+.20f),pos+Vector3.up*(r+.9f),r,
                Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        }

        private void OnDefeated(ForestEnemy enemy)
        {
            if (!active.Remove(enemy)) return;
            enemy.Defeated -= OnDefeated;
            if (enemy==boss && !dropped)
            {
                dropped=true;
                chest.gameObject.SetActive(true);
                hud.MarkChest(chestSite);
                FloorSession.Instance.Screen.Notice("El Coloso ha caído. El cofre está en el claro.");
            }
            RefreshCount();
        }

        private void RefreshCount() => hud.SetRemaining(unspawned+active.Count);

        private void Update()
        {
            if (Time.time<nextRecovery) return;
            nextRecovery = Time.time+1;
            recovery.Clear();
            foreach (var enemy in active)
            {
                var p=enemy.transform.position;
                if (p.y < -3 || p.x*p.x/(39*39)+p.z*p.z/(33*33)>1) recovery.Add(enemy);
            }
            foreach (var enemy in recovery)
                if (FindSpawn(centre,9,enemy.GetComponent<CharacterController>().radius,out var point)) enemy.RecoverTo(point);
        }

        private void OnDestroy()
        {
            foreach (var enemy in active) if (enemy) enemy.Defeated -= OnDefeated;
            if (nav.valid) nav.Remove();
            if (navData) Destroy(navData);
        }
    }
}

