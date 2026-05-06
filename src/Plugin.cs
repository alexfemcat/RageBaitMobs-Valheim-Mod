using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RagebateMobs.Configuration;
using RagebateMobs.Managers;
using RagebateMobs.Patches;
using RagebateMobs.Services;

namespace RagebateMobs
{
    [BepInPlugin(GUID, Name, Version)]
    public class RagebateMobsPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.valheim.ragebatemobs";
        public const string Name = "Viking Ragebait";
        public const string Version = "1.0.0";

        public static ManualLogSource Logger { get; private set; }
        public static ModConfig Config { get; private set; }
        public static LLMService LLMService { get; private set; }
        public static CooldownManager CooldownManager { get; private set; }
        public static OutputManager OutputManager { get; private set; }
        public static TaskManager TaskManager { get; private set; }

        private static Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"[{Name}] Loading...");

            try
            {
                Config = new ModConfig(base.Config);
                LLMService = new LLMService(Config.LMStudioApiUrl.Value, Logger);
                CooldownManager = new CooldownManager(
                    Config.GlobalCooldownSeconds.Value,
                    Config.PerMobCooldownSeconds.Value
                );
                OutputManager = new OutputManager(Config, Logger);
                TaskManager = new TaskManager(Logger);

                ApplyPatches();

                Logger.LogInfo($"[{Name}] Loaded successfully! API: {Config.LMStudioApiUrl.Value}");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[{Name}] Failed to load: {ex}");
            }
        }

        private void ApplyPatches()
        {
            _harmony = new Harmony(GUID);

            try
            {
                _harmony.PatchAll(typeof(MonsterAITargetingPatch));
                Logger.LogInfo("[Viking Ragebait] MonsterAI.UpdateTargeting patched");

                _harmony.PatchAll(typeof(CharacterDamagePatch));
                Logger.LogInfo("[Viking Ragebait] Character.ApplyDamage patched");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[{Name}] Failed to apply patches: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            CooldownManager?.Clear();
            Logger.LogInfo("[Viking Ragebait] Unloaded.");
        }
    }
}
