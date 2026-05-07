using HarmonyLib;
using RagebateMobs.Network;
using RagebateMobs.Services;

namespace RagebateMobs.Patches
{
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

            var player = __instance as Player;
            var mob = hit?.GetAttacker();
            if (mob == null || mob.IsPlayer()) return;
            if (mob.GetComponent<MonsterAI>() == null) return;

            var nv = mob.GetComponent<ZNetView>();
            if (nv == null || nv.GetZDO() == null) return;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName)) mobName = mob.m_name;

            string mobType = mob.name;
            string playerName = player?.GetPlayerName() ?? __instance.GetHoverName();

            // Look for a nearby same-type buddy and a nearby rival species so the server
            // can fire a call-and-response and/or a rivalry burst.
            var (buddyId, buddyName, buddyType) = ScanHelpers.FindBuddy(mob);
            var (rivalId, rivalName, rivalType) = ScanHelpers.FindRival(mob);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} hit {playerName} for {dmg:F1} dmg, requesting roast");
            RoastRpc.SendRequest(
                nv.GetZDO().m_uid, mobName, playerName, "took_damage", mobType,
                buddyId, buddyName, buddyType,
                rivalId, rivalName, rivalType);
        }
    }
}
