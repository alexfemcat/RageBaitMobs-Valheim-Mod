using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Managers
{
    public class CooldownManager
    {
        private readonly Dictionary<ZDOID, float> _lastTalk = new Dictionary<ZDOID, float>();
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

        public void Clear() => _lastTalk.Clear();

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
