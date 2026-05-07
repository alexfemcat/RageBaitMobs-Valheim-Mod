using HarmonyLib;
using RagebateMobs.Network;

namespace RagebateMobs.Patches
{
    // Fires on whoever owns the mob (typically a connected client).
    // Modded clients send a routed RPC to the server, which calls the LLM and broadcasts.
    [HarmonyPatch(typeof(MonsterAI), "DoAttack")]
    public static class MonsterAITargetingPatch
    {
        private static int _callCount = 0;

        [HarmonyPostfix]
        public static void Postfix(MonsterAI __instance)
        {
            _callCount++;
            if (_callCount <= 3 || _callCount % 200 == 0)
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] DoAttack postfix fired (call #{_callCount})");

            if (!RagebateMobsPlugin.Config.Enabled.Value) return;

            var mob = __instance.GetComponent<Character>();
            if (mob == null || mob.IsPlayer() || mob.IsDead()) return;

            var target = __instance.GetTargetCreature();
            if (target == null || !target.IsPlayer()) return;

            var nv = __instance.GetComponent<ZNetView>();
            if (nv == null || nv.GetZDO() == null) return;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName)) mobName = mob.m_name;

            string mobType = mob.name;

            string playerName = (target as Player)?.GetPlayerName() ?? "player";

            RoastRpc.SendRequest(nv.GetZDO().m_uid, mobName, playerName, "spotted_player", mobType);
        }
    }
}
