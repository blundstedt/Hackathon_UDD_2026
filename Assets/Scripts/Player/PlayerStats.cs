using System;
using UnityEngine;

namespace Hackaton.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour
    {
        public const int MaxHealth = 100;
        public const int MaxLevel = 18;

        [SerializeField] private bool trainingDummy;
        [SerializeField] private Font damageFont;

        internal void ReceiveHit(int damage, Vector3 point, Vector3 direction, float push)
        {
            if (trainingDummy)
            {
                if (damage > 0) FloatingDamage.Show(point, damage, damageFont);
                return;
            }
            TakeDamage(damage);
            if (TryGetComponent<PlayerMovement>(out var movement) && movement.isActiveAndEnabled)
                movement.Push(direction, push);
        }

        public int Health { get; private set; } = MaxHealth;
        public int Money { get; private set; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNextLevel => Level == MaxLevel ? 0 : 100 * Level;
        public event Action Changed;

        public void TakeDamage(int amount)
        {
            if (trainingDummy || amount <= 0 || Health == 0) return;
            Health = Math.Max(0, Health - amount);
            Changed?.Invoke();
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || Health == MaxHealth) return;
            Health += Math.Min(amount, MaxHealth - Health);
            Changed?.Invoke();
        }

        public bool TrySpend(int cost)
        {
            if (cost < 0 || cost > Money) return false;
            Money -= cost;
            Changed?.Invoke();
            return true;
        }

        // Called once by the defeated enemy for the player receiving its reward.
        public void RewardDefeat(int money, int experience)
        {
            if (money < 0 || experience < 0) throw new ArgumentOutOfRangeException();
            Money += Math.Min(money, int.MaxValue - Money);
            long available = (long)Experience + experience;
            while (Level < MaxLevel && available >= ExperienceToNextLevel)
            {
                available -= ExperienceToNextLevel;
                Level++;
            }
            Experience = Level == MaxLevel ? 0 : (int)available;
            Changed?.Invoke();
        }

        internal void CopyFrom(PlayerStats previous)
        {
            Health = previous.Health;
            Money = previous.Money;
            Level = previous.Level;
            Experience = previous.Experience;
            Changed?.Invoke();
        }
    }
}
