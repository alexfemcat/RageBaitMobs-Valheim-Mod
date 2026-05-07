using HarmonyLib;
using RagebateMobs.Network;
using RagebateMobs.Services;

namespace RagebateMobs.Patches
{
    // Phase 12.2 — Hype Man.
    // When a mob takes damage from a player and drops below 30% HP, ask a nearby same-type
    // buddy to fire a defending insult. The hyper mob is the broadcaster; the hurt mob is
    // passed as the candidate so the prompt can reference it as "your friend".
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class MobLowHealthPatch
    {
        private const float LowHealthThreshold = 0.30f;

        [HarmonyPostfix]
        public static void Postfix(Character __instance, HitData hit)
        {
            if (!RagebateMobsPlugin.Config.Enabled.Value) return;
            if (__instance == null || __instance.IsPlayer()) return;
            if (__instance.GetComponent<MonsterAI>() == null) return;

            float hp = __instance.GetHealth();
            if (hp <= 0f) return;
            float maxHp = __instance.GetMaxHealth();
            if (maxHp <= 0f) return;
            if (hp / maxHp >= LowHealthThreshold) return;

            var attacker = hit?.GetAttacker();
            if (attacker == null || !attacker.IsPlayer()) return;

            var hyper = NearbyMobScanner.FindNearbySameType(__instance, ScanHelpers.HypeRadius);
            if (hyper == null) return;

            var hyperNv = hyper.GetComponent<ZNetView>();
            if (hyperNv == null || hyperNv.GetZDO() == null) return;

            string hyperName = global::Localization.instance.Localize(hyper.m_name);
            if (string.IsNullOrWhiteSpace(hyperName)) hyperName = hyper.m_name;
            string hyperType = hyper.name;

            string hurtName = global::Localization.instance.Localize(__instance.m_name);
            if (string.IsNullOrWhiteSpace(hurtName)) hurtName = __instance.m_name;
            string hurtType = __instance.name;

            var player = attacker as Player;
            string playerName = player?.GetPlayerName() ?? attacker.GetHoverName() ?? "player";

            // Hyper mob is the broadcaster. Hurt mob is the candidate (friend reference).
            RoastRpc.SendRequest(
                hyperNv.GetZDO().m_uid, hyperName, playerName, "hype_man", hyperType,
                __instance.GetComponent<ZNetView>().GetZDO().m_uid, hurtName, hurtType);
        }
    }
}
