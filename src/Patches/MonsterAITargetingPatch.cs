using HarmonyLib;
using RagebateMobs.Services;
using System.Collections.Generic;

namespace RagebateMobs.Patches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateTargeting))]
    public static class MonsterAITargetingPatch
    {
        private static HashSet<int> _mobsJustAggro = new HashSet<int>();

        [HarmonyPostfix]
        public static void Postfix(MonsterAI __instance)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer() || !RagebateMobsPlugin.Config.Enabled.Value)
                return;

            var mob = __instance.m_character;
            if (mob == null || mob.IsPlayer())
                return;

            var target = __instance.m_targetCreature;
            if (target == null || !target.IsPlayer())
            {
                _mobsJustAggro.Remove(mob.GetInstanceID());
                return;
            }

            int mobId = mob.GetInstanceID();

            // Only trigger on initial aggro (target just acquired)
            if (_mobsJustAggro.Contains(mobId))
                return;

            _mobsJustAggro.Add(mobId);

            // Check cooldowns
            if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mob))
                return;

            // Get localized mob name
            string localizedMobName = Localization.instance.Localize(mob.m_name);
            string playerName = target.GetPlayerName();

            if (string.IsNullOrWhiteSpace(localizedMobName))
                localizedMobName = mob.m_name;

            // Build prompt and request insult
            string prompt = PromptBuilder.BuildInsultPrompt(localizedMobName, "spotted_player", playerName);

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
    }
}
