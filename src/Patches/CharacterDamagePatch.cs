using HarmonyLib;
using RagebateMobs.Services;

namespace RagebateMobs.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class CharacterDamagePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Character __instance, HitData hit)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer() || !RagebateMobsPlugin.Config.Enabled.Value)
                return;

            var mob = __instance;
            if (mob == null || mob.IsPlayer())
                return;

            if (mob.GetComponent<MonsterAI>() == null)
                return;

            float damageAmount = hit?.GetTotalDamage() ?? 0f;
            if (damageAmount < RagebateMobsPlugin.Config.MinDamageThreshold.Value)
                return;

            var attacker = hit?.GetAttacker();
            if (attacker == null || !attacker.IsPlayer())
                return;

            if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mob))
                return;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName))
                mobName = mob.m_name;

            string playerName = (attacker as Player)?.GetPlayerName() ?? attacker.GetHoverName();
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "took_damage", playerName);

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (!string.IsNullOrWhiteSpace(insult))
                {
                    RagebateMobsPlugin.CooldownManager.RecordMobSpeak(mob);
                    RagebateMobsPlugin.OutputManager.BroadcastInsult(mob, insult);
                }
            });
        }
    }
}
