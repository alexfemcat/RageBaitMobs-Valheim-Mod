using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Managers
{
    public class CooldownManager
    {
        private readonly Dictionary<ZDOID, float> _lastTalk = new Dictionary<ZDOID, float>();
        private readonly Dictionary<string, float> _lastGroupTalk = new Dictionary<string, float>();
        private readonly float _cooldown;
        private const float CleanupThreshold = 300f;

        public CooldownManager(float perMobCooldownSeconds)
        {
            _cooldown = perMobCooldownSeconds;
        }

        public bool CanMobSpeak(ZDOID mobId)
        {
            if (mobId == ZDOID.None) return false;
            float now = Time.time;
            if (_lastTalk.TryGetValue(mobId, out var last) && now - last < _cooldown)
                return false;
            return true;
        }

        public void RecordMobSpeak(ZDOID mobId)
        {
            if (mobId == ZDOID.None) return;
            _lastTalk[mobId] = Time.time;
            if (_lastTalk.Count > 200)
                PruneStaleEntries(now: Time.time);
        }

        public bool CanGroupSpeak(string groupKey, float cooldownSeconds)
        {
            if (string.IsNullOrEmpty(groupKey)) return false;
            float now = Time.time;
            if (_lastGroupTalk.TryGetValue(groupKey, out var last) && now - last < cooldownSeconds)
                return false;
            return true;
        }

        public void RecordGroupSpeak(string groupKey)
        {
            if (string.IsNullOrEmpty(groupKey)) return;
            _lastGroupTalk[groupKey] = Time.time;
        }

        public void Clear()
        {
            _lastTalk.Clear();
            _lastGroupTalk.Clear();
        }

        private void PruneStaleEntries(float now)
        {
            var toRemove = new List<ZDOID>();
            foreach (var kvp in _lastTalk)
            {
                if (now - kvp.Value > CleanupThreshold)
                    toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove)
                _lastTalk.Remove(key);
        }
    }
}
