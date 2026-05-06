using System;
using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Managers
{
    public class CooldownManager
    {
        private readonly Dictionary<int, float> _perMobLastTalkTime = new Dictionary<int, float>();
        private readonly float _perMobCooldown;

        public CooldownManager(float perMobCooldownSeconds)
        {
            _perMobCooldown = perMobCooldownSeconds;
        }

        public bool CanMobSpeak(Character mob)
        {
            if (mob == null)
                return false;

            float now = Time.time;
            int mobId = mob.GetInstanceID();

            // Check per-mob cooldown
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
            int mobId = mob.GetInstanceID();
            _perMobLastTalkTime[mobId] = now;
        }

        public void Clear()
        {
            _perMobLastTalkTime.Clear();
        }
    }
}
