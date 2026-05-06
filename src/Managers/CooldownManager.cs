using System;
using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Managers
{
    public class CooldownManager
    {
        private float _lastGlobalTalkTime;
        private readonly Dictionary<int, float> _perMobLastTalkTime = new Dictionary<int, float>();
        private readonly float _globalCooldown;
        private readonly float _perMobCooldown;

        public CooldownManager(float globalCooldownSeconds, float perMobCooldownSeconds)
        {
            _globalCooldown = globalCooldownSeconds;
            _perMobCooldown = perMobCooldownSeconds;
            _lastGlobalTalkTime = float.MinValue;
        }

        public bool CanMobSpeak(Character mob)
        {
            if (mob == null)
                return false;

            float now = Time.time;

            // Check global cooldown
            if (now - _lastGlobalTalkTime < _globalCooldown)
                return false;

            // Check per-mob cooldown
            int mobId = mob.GetInstanceID();
            if (_perMobLastTalkTime.TryGetValue(mobId, out float lastTime))
            {
                if (now - lastTime < _perMobCooldown)
                    return false;
            }

            return true;
        }

        public void RecordMobSpeak(Character mob)
        {
            if (mob == null)
                return;

            float now = Time.time;
            _lastGlobalTalkTime = now;

            int mobId = mob.GetInstanceID();
            _perMobLastTalkTime[mobId] = now;
        }

        public void Clear()
        {
            _perMobLastTalkTime.Clear();
            _lastGlobalTalkTime = float.MinValue;
        }
    }
}
