using Hackaton.Enemies;
using Unity.Netcode;
using UnityEngine.AI;

namespace Hackaton.Multiplayer
{
    public sealed class NetworkEnemy : NetworkBehaviour
    {
        private readonly NetworkVariable<int> health = new();
        private EnemyBase enemy;
        public override void OnNetworkSpawn()
        {
            enemy = GetComponent<EnemyBase>();
            if (IsServer) health.Value = enemy.Health;
            else
            {
                enemy.enabled = false;
                GetComponent<NavMeshAgent>().enabled = false;
                enemy.DisplayReplicaHealth(health.Value);
                health.OnValueChanged += ShowDamage;
            }
        }
        public override void OnNetworkDespawn() => health.OnValueChanged -= ShowDamage;
        private void LateUpdate()
        {
            if (IsSpawned && IsServer && health.Value != enemy.Health) health.Value = enemy.Health;
        }
        private void ShowDamage(int previous, int current) => enemy.DisplayReplicaHealth(current);
    }
}
