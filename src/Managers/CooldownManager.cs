using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Managers
{
    public class CooldownManager
    {
        private readonly Dictionary<ZDOID, float> _lastTalk = new Dictionary<ZDOID, float>();
        private readonly float _cooldown;

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
        }

        public void Clear() => _lastTalk.Clear();
    }
}
