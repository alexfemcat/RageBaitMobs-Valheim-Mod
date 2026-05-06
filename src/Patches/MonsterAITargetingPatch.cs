using HarmonyLib;
using RagebateMobs.Services;
using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Patches
{
    // Fires on every AI update tick - catches aggro + proximity
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    public static class MonsterAITargetingPatch
    {
        private static HashSet<int> _cooldownedMobs = new HashSet<int>();
        private static Dictionary<int, float> _mobNextTalkTime = new Dictionary<int, float>();

        [HarmonyPostfix]
        public static void Postfix(MonsterAI __instance, float dt)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer() || !RagebateMobsPlugin.Config.Enabled.Value)
                return;

            var mob = __instance.GetComponent<Character>();
            if (mob == null || mob.IsPlayer() || mob.IsDead())
                return;

            var target = __instance.GetTargetCreature();
            if (target == null || !target.IsPlayer())
                return;

            int mobId = mob.GetInstanceID();
            float now = Time.time;

            if (_mobNextTalkTime.TryGetValue(mobId, out float nextTime) && now < nextTime)
                return;

            _mobNextTalkTime[mobId] = now + RagebateMobsPlugin.Config.PerMobCooldownSeconds.Value;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName))
                mobName = mob.m_name;

            string playerName = (target as Player)?.GetPlayerName() ?? "player";
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "spotted_player", playerName);

            if (RagebateMobsPlugin.Config.DebugLogging.Value)
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} taunting {playerName}...");

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (!string.IsNullOrWhiteSpace(insult))
                    RagebateMobsPlugin.OutputManager.BroadcastInsult(mob, insult);
            });
        }
    }
}
