using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hackaton.Progression
{
    public enum FloorState { Locked, Available, Completed }

    /// <summary>Libro de progreso de una partida. Las llaves pertenecen al equipo.</summary>
    public sealed class FloorProgress
    {
        private readonly SortedDictionary<int, FloorData> floors = new();
        private readonly Dictionary<int, FloorState> states = new();
        private readonly Dictionary<UnlockItem, int> items = new();
        public event Action Changed;

        public FloorProgress(IEnumerable<FloorData> catalog, string previewScene = null)
        {
            foreach (var floor in catalog)
            {
                if (!floor || !floors.TryAdd(floor.number, floor))
                {
                    Debug.LogError("El catálogo contiene una referencia vacía o un número de piso repetido.");
                    continue;
                }
                bool available = floor.number == 0 || floor.initiallyAvailable ||
                    (!string.IsNullOrEmpty(previewScene) && floor.sceneName == previewScene);
                states.Add(floor.number, available ? FloorState.Available : FloorState.Locked);
            }
        }

        public FloorData Get(int number) => floors.TryGetValue(number, out var floor) ? floor : null;
        public FloorData ForScene(string scene)
        {
            foreach (var floor in floors.Values)
                if (!string.IsNullOrEmpty(scene) && floor.sceneName == scene) return floor;
            return null;
        }

        public FloorState State(int number) => states.TryGetValue(number, out var state) ? state : FloorState.Locked;

        internal bool Complete(FloorData floor, Action grantPlayerRewards)
        {
            if (!floor || floor.number == 0 || State(floor.number) != FloorState.Available) return false;
            states[floor.number] = FloorState.Completed;
            foreach (var next in floors.Values)
                if (next.prerequisite == floor && !next.requiredItem && State(next.number) == FloorState.Locked)
                    states[next.number] = FloorState.Available;
            if (floor.rewardItem)
            {
                items.TryGetValue(floor.rewardItem, out int count);
                items[floor.rewardItem] = count + 1;
            }
            grantPlayerRewards();
            Changed?.Invoke();
            return true;
        }

        private FloorData NextDelivery()
        {
            foreach (var floor in floors.Values)
                if (State(floor.number) == FloorState.Locked && floor.prerequisite &&
                    State(floor.prerequisite.number) == FloorState.Completed && floor.requiredItem &&
                    items.TryGetValue(floor.requiredItem, out int count) && count > 0)
                    return floor;
            return null;
        }

        public string DeliveryPrompt()
        {
            if (NextDelivery()) return "Presiona E para entregar el objeto";
            bool delivered = false;
            foreach (var floor in floors.Values)
            {
                if (floor.requiredItem && State(floor.number) != FloorState.Locked) delivered = true;
            }
            return delivered ? "Este piso ya está desbloqueado" : "Necesitas el objeto de desbloqueo";
        }

        internal string Deliver()
        {
            var floor = NextDelivery();
            if (!floor) return DeliveryPrompt();
            items[floor.requiredItem]--;
            states[floor.number] = FloorState.Available;
            Changed?.Invoke();
            return $"¡Piso {floor.number} desbloqueado!";
        }
    }
}
