using HarmonyLib;
using RagebateMobs.Network;

namespace RagebateMobs.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class PlayerDeathPatch
    {
        private static int _callCount = 0;

        [HarmonyPostfix]
        public static void Postfix(Character __instance, HitData hit)
        {
            _callCount++;
            if (_callCount <= 3)
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] PlayerDeath ApplyDamage postfix fired (call #{_callCount})");

            if (!RagebateMobsPlugin.Config.Enabled.Value) return;
            if (__instance == null || !__instance.IsPlayer()) return;

            var player = __instance as Player;
            if (player == null) return;

            float dmg = hit?.GetTotalDamage() ?? 0f;
            if (dmg < 1f) return;

            var attacker = hit?.GetAttacker();
            if (attacker == null || attacker.IsPlayer()) return;

            var mobNv = attacker.GetComponent<ZNetView>();
            if (mobNv == null || mobNv.GetZDO() == null) return;

            string mobName = global::Localization.instance.Localize(attacker.m_name);
            if (string.IsNullOrWhiteSpace(mobName)) mobName = attacker.m_name;

            string mobType = attacker.name;
            string playerName = player.GetPlayerName() ?? player.GetHoverName();

            if (player.GetHealth() <= 0f)
            {
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} KILLED {playerName}, requesting death roast");
                RoastRpc.SendRequest(mobNv.GetZDO().m_uid, mobName, playerName, "player_died", mobType);
            }
        }
    }
}
