using HarmonyLib;
using RagebateMobs.Network;

namespace RagebateMobs.Patches
{
    // Fires on whoever owns the player character (the player's own client).
    // The modded client sends a routed RPC to the server, which calls the LLM and broadcasts.
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class CharacterDamagePatch
    {
        private static int _callCount = 0;

        [HarmonyPostfix]
        public static void Postfix(Character __instance, HitData hit)
        {
            _callCount++;
            if (_callCount <= 3)
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] ApplyDamage postfix fired (call #{_callCount})");

            if (!RagebateMobsPlugin.Config.Enabled.Value) return;
            if (__instance == null || !__instance.IsPlayer()) return;

            float dmg = hit?.GetTotalDamage() ?? 0f;
            if (dmg < RagebateMobsPlugin.Config.MinDamageThreshold.Value) return;

            var mob = hit?.GetAttacker();
            if (mob == null || mob.IsPlayer()) return;
            if (mob.GetComponent<MonsterAI>() == null) return;

            var nv = mob.GetComponent<ZNetView>();
            if (nv == null || nv.GetZDO() == null) return;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName)) mobName = mob.m_name;

            string mobType = mob.name;

            string playerName = (__instance as Player)?.GetPlayerName() ?? __instance.GetHoverName();

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} hit {playerName} for {dmg:F1} dmg, requesting roast");

            RoastRpc.SendRequest(nv.GetZDO().m_uid, mobName, playerName, "took_damage", mobType);
        }
    }
}
