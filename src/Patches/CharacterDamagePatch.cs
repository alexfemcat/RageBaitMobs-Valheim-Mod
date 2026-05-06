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
            if (!ShouldExecute())
                return;

            // Only trigger on mobs, not players
            var mob = __instance;
            if (mob == null || mob.IsPlayer())
                return;

            // Only trigger on mobs, not objects
            var monsterAI = mob.GetComponent<MonsterAI>();
            if (monsterAI == null)
                return;

            // Check minimum damage threshold
            float damageAmount = hit?.GetTotalDamage() ?? 0f;
            if (damageAmount < RagebateMobsPlugin.Config.MinDamageThreshold.Value)
                return;

            // Get attacker (should be a player)
            var attacker = hit?.GetAttacker();
            if (attacker == null || !attacker.IsPlayer())
                return;

            // Check cooldowns
            if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mob))
                return;

            // Get localized mob name
            string localizedMobName = Localization.instance.Localize(mob.m_name);
            string playerName = attacker.GetPlayerName();

            if (string.IsNullOrWhiteSpace(localizedMobName))
                localizedMobName = mob.m_name;

            // Build prompt and request insult
            string prompt = PromptBuilder.BuildInsultPrompt(localizedMobName, "took_damage", playerName);

            // Fire async request (non-blocking)
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

        private static bool ShouldExecute()
        {
            return ZNet.instance && ZNet.instance.IsServer() && RagebateMobsPlugin.Config.Enabled.Value;
        }
    }
}
