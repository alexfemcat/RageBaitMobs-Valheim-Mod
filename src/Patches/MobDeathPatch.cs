using HarmonyLib;
using RagebateMobs.Network;

namespace RagebateMobs.Patches
{
    // Fires last-words insult when a player kills a mob.
    // Postfix on Character.ApplyDamage: if recipient is a non-player mob, attacker is a
    // player, and the mob's HP is now <= 0, send a player_killed_mob roast request.
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class MobDeathPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Character __instance, HitData hit)
        {
            if (!RagebateMobsPlugin.Config.Enabled.Value) return;
            if (__instance == null || __instance.IsPlayer()) return;
            if (__instance.GetComponent<MonsterAI>() == null) return;
            if (__instance.GetHealth() > 0f) return;

            var attacker = hit?.GetAttacker();
            if (attacker == null || !attacker.IsPlayer()) return;

            var nv = __instance.GetComponent<ZNetView>();
            if (nv == null || nv.GetZDO() == null) return;

            var player = attacker as Player;
            string playerName = player?.GetPlayerName() ?? attacker.GetHoverName() ?? "player";

            string mobName = global::Localization.instance.Localize(__instance.m_name);
            if (string.IsNullOrWhiteSpace(mobName)) mobName = __instance.m_name;
            string mobType = __instance.name;

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {playerName} KILLED {mobName}, requesting last words");
            RoastRpc.SendRequest(nv.GetZDO().m_uid, mobName, playerName, "player_killed_mob", mobType);
        }
    }
}
