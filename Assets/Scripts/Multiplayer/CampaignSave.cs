using System;
using System.Collections.Generic;
using System.IO;
using Hackaton.Player;
using UnityEngine;

namespace Hackaton.Multiplayer
{
    [Serializable]
    public sealed class CampaignSave
    {
        public int version;
        public int slot;
        public long savedUtc;
        public List<int> available = new();
        public List<int> completed = new();
        public List<string> keys = new();
        public List<Account> accounts = new();

        [Serializable]
        public sealed class Account
        {
            public string id;
            public PlayerStats.Snapshot stats;
            public string equipment;
            public TizaLook appearance;
            public List<int> pendingRewards = new();
        }

        public int HighestFloor => Math.Max(1, Math.Max(available.Count == 0 ? 0 : Mathf.Max(available.ToArray()),
            completed.Count == 0 ? 0 : Mathf.Max(completed.ToArray())));
    }

    public sealed class CampaignSlots
    {
        private readonly string directory;
        public CampaignSlots(string directory) => this.directory = directory;

        private string PathFor(int slot)
        {
            if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, $"partida-{slot}.json");
        }

        public CampaignSave Read(int slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return null;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                CampaignSave data;
                try { data = JsonUtility.FromJson<CampaignSave>(File.ReadAllText(candidate)); }
                catch (ArgumentException) { continue; }
                if (data != null && data.version > 1)
                    throw new InvalidDataException("La partida requiere una versión más reciente del juego.");
                if (data != null && data.version == 1 && data.slot == slot && data.savedUtc > 0 &&
                    data.available != null && data.completed != null && data.keys != null && data.accounts != null)
                    return data;
            }
            throw new InvalidDataException($"No se pudo recuperar la partida {slot}.");
        }

        public void Write(int slot, CampaignSave data)
        {
            string path = PathFor(slot);
            Directory.CreateDirectory(directory);
            data.slot = slot;
            data.version = 1;
            data.savedUtc = DateTime.UtcNow.Ticks;
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(data, true));
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }

        public void Delete(int slot)
        {
            string path = PathFor(slot);
            foreach (string suffix in new[] { ".tmp", ".bak", "" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }
}
