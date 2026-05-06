using HarmonyLib;
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

            if (!ZNet.instance || !ZNet.instance.IsServer() || !RagebateMobsPlugin.Config.Enabled.Value)
                return;

            if (!__instance.IsPlayer())
                return;

            float damageAmount = hit?.GetTotalDamage() ?? 0f;
            if (damageAmount < RagebateMobsPlugin.Config.MinDamageThreshold.Value)
                return;

            var mob = hit?.GetAttacker();
            if (mob == null || mob.IsPlayer())
                return;

            if (mob.GetComponent<MonsterAI>() == null)
                return;

            if (!RagebateMobsPlugin.CooldownManager.CanMobSpeak(mob))
                return;

            string mobName = global::Localization.instance.Localize(mob.m_name);
            if (string.IsNullOrWhiteSpace(mobName))
                mobName = mob.m_name;

            string playerName = (__instance as Player)?.GetPlayerName() ?? __instance.GetHoverName();
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "took_damage", playerName);

            RagebateMobsPlugin.CooldownManager.RecordMobSpeak(mob);
            RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName} hit {playerName} for {damageAmount:F1} dmg, generating roast");

            RagebateMobsPlugin.TaskManager.SafeFireAndForgetAsync(async () =>
            {
                var insult = await RagebateMobsPlugin.LLMService.GenerateInsultAsync(prompt);
                if (!string.IsNullOrWhiteSpace(insult))
                {
                    RagebateMobsPlugin.Logger.LogInfo($"[Ragebait] {mobName}: {insult}");
                    RagebateMobsPlugin.OutputManager.BroadcastInsult(mob, insult);
                }
                else
                {
                    RagebateMobsPlugin.Logger.LogWarning($"[Ragebait] Empty insult from LLM for {mobName}");
                }
            });
        }
    }
}
