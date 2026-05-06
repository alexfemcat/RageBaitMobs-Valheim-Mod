using HarmonyLib;
using RagebateMobs.Services;
using System.Collections.Generic;
using UnityEngine;

namespace RagebateMobs.Patches
{
    [HarmonyPatch(typeof(MonsterAI), "DoAttack")]
    public static class MonsterAITargetingPatch
    {
        private static Dictionary<int, float> _mobNextTalkTime = new Dictionary<int, float>();
        private static int _callCount = 0;

        [HarmonyPostfix]
        public static void Postfix(MonsterAI __instance)
        {
            _callCount++;

            // Log every 100 calls so we know the patch is firing at all
            if (_callCount % 100 == 0)
                RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] DoAttack fired {_callCount} times");

            if (ZNet.instance == null) { RagebateMobsPlugin.Logger.LogWarning("[Ragebait] ZNet.instance is null"); return; }
            if (!ZNet.instance.IsServer()) { RagebateMobsPlugin.Logger.LogWarning("[Ragebait] Not server"); return; }
            if (!RagebateMobsPlugin.Config.Enabled.Value) return;

            var mob = __instance.GetComponent<Character>();
            if (mob == null) { RagebateMobsPlugin.Logger.LogWarning("[Ragebait] mob is null"); return; }
            if (mob.IsPlayer()) return;
            if (mob.IsDead()) return;

            var target = __instance.GetTargetCreature();
            if (target == null) { RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] {mob.m_name} has no target"); return; }
            if (!target.IsPlayer()) { RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] target is not player"); return; }

            int mobId = mob.GetInstanceID();
            float now = Time.time;

            if (_mobNextTalkTime.TryGetValue(mobId, out float nextTime) && now < nextTime)
                return;

            _mobNextTalkTime[mobId] = now + RagebateMobsPlugin.Config.PerMobCooldownSeconds.Value;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName)) mobName = mob.m_name;

            string playerName = (target as Player)?.GetPlayerName() ?? "player";
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "spotted_player", playerName);

            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} taunting {playerName}...");

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (!string.IsNullOrWhiteSpace(insult))
                    RagebateMobsPlugin.OutputManager.BroadcastInsult(mob, insult);
                else
                    RagebateMobsPlugin.Logger.LogWarning("[Ragebait] Empty insult from LLM");
            });
        }
    }
}
