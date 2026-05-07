using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Services
{
    public static class NearbyMobScanner
    {
        public static Character FindNearbySameType(Character original, float radius)
        {
            if (original == null) return null;
            var all = Character.GetAllCharacters();
            if (all == null) return null;

            Vector3 pos = original.transform.position;
            Character best = null;
            float bestDist = float.MaxValue;

            foreach (var c in all)
            {
                if (c == null || c == original) continue;
                if (c.IsPlayer() || c.IsDead()) continue;
                if (c.name != original.name) continue;
                if (c.GetComponent<MonsterAI>() == null) continue;
                var nv = c.GetComponent<ZNetView>();
                if (nv == null || nv.GetZDO() == null) continue;

                float d = Vector3.Distance(pos, c.transform.position);
                if (d <= radius && d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        public static Character FindNearbyByTypes(Character original, IEnumerable<string> typeNames, float radius)
        {
            if (original == null || typeNames == null) return null;
            var all = Character.GetAllCharacters();
            if (all == null) return null;

            var typeSet = new HashSet<string>(typeNames);
            Vector3 pos = original.transform.position;
            Character best = null;
            float bestDist = float.MaxValue;

            foreach (var c in all)
            {
                if (c == null || c == original) continue;
                if (c.IsPlayer() || c.IsDead()) continue;
                if (!typeSet.Contains(c.name)) continue;
                if (c.GetComponent<MonsterAI>() == null) continue;
                var nv = c.GetComponent<ZNetView>();
                if (nv == null || nv.GetZDO() == null) continue;

                float d = Vector3.Distance(pos, c.transform.position);
                if (d <= radius && d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        public static int CountNearbySameType(Character original, float radius)
        {
            if (original == null) return 0;
            var all = Character.GetAllCharacters();
            if (all == null) return 0;

            Vector3 pos = original.transform.position;
            int count = 0;
            foreach (var c in all)
            {
                if (c == null || c == original) continue;
                if (c.IsPlayer() || c.IsDead()) continue;
                if (c.name != original.name) continue;
                if (Vector3.Distance(pos, c.transform.position) <= radius) count++;
            }
            return count;
        }
    }
}
