using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RagebateMobs.Managers
{
    public class KillCountManager
    {
        private readonly string _savePath;
        private readonly Dictionary<string, Dictionary<string, int>> _killCounts = new Dictionary<string, Dictionary<string, int>>();
        private readonly object _lock = new object();

        public KillCountManager(string savePath)
        {
            _savePath = savePath;
            Load();
        }

        public void RecordKill(string mobType, string playerName)
        {
            lock (_lock)
            {
                string mobKey = mobType.ToLowerInvariant();
                string playerKey = playerName.ToLowerInvariant();

                if (!_killCounts.ContainsKey(mobKey))
                    _killCounts[mobKey] = new Dictionary<string, int>();

                if (!_killCounts[mobKey].ContainsKey(playerKey))
                    _killCounts[mobKey][playerKey] = 0;

                _killCounts[mobKey][playerKey]++;
            }
        }

        public int GetKillCount(string mobType, string playerName)
        {
            lock (_lock)
            {
                string mobKey = mobType.ToLowerInvariant();
                string playerKey = playerName.ToLowerInvariant();

                if (_killCounts.TryGetValue(mobKey, out var playerDict))
                    if (playerDict.TryGetValue(playerKey, out int count))
                        return count;

                return 0;
            }
        }

        public string GetShameContext(string mobType, string playerName)
        {
            int kills = GetKillCount(mobType, playerName);

            if (kills == 0)
                return "";

            if (kills >= 50)
                return $"THIS ABSOLUTE PIECE OF GARBAGE HAS DIED TO YOU {kills} TIMES. THEY SHOULD JUST QUIT ALREADY.";

            if (kills >= 20)
                return $"You've killed this loser {kills} times already. When will they ever learn?";

            if (kills >= 10)
                return $"This idiot has died to you {kills} times. Pathetic really.";

            return $"Kill count: {kills} times now. They're getting what they deserve.";
        }

        public void ResetPlayerStats(string playerName)
        {
            lock (_lock)
            {
                string playerKey = playerName.ToLowerInvariant();

                foreach (var mobDict in _killCounts.Values)
                {
                    mobDict.Remove(playerKey);
                }
            }
            Save();
        }

        public void ResetAllStats()
        {
            lock (_lock)
            {
                _killCounts.Clear();
            }
            Save();
        }

        public Dictionary<string, Dictionary<string, int>> GetAllStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, Dictionary<string, int>>(_killCounts);
            }
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(_killCounts, Formatting.Indented);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Failed to save kill counts: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json);
                    if (data != null)
                    {
                        lock (_lock)
                        {
                            _killCounts.Clear();
                            foreach (var kvp in data)
                            {
                                _killCounts[kvp.Key] = new Dictionary<string, int>(kvp.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Failed to load kill counts: {ex.Message}");
            }
        }
    }
}
