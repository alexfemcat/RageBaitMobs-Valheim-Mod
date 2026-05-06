using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RagebateMobs.Configuration;
using RagebateMobs.Managers;
using RagebateMobs.Network;
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
        public static TaskManager TaskManager { get; private set; }

        private static Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"[{Name}] Loading...");

            try
            {
                Config = new ModConfig(base.Config);
                LLMService = new LLMService(Config.LMStudioApiUrl.Value, Config.LLMModel.Value, Logger);
                CooldownManager = new CooldownManager(Config.PerMobCooldownSeconds.Value);
                TaskManager = new TaskManager(Logger);

                MainThreadDispatcher.Initialize();
                ApplyPatches();

                Logger.LogInfo($"[{Name}] Loaded. API: {Config.LMStudioApiUrl.Value}");
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
                _harmony.PatchAll(typeof(CharacterDamagePatch));
                _harmony.PatchAll(typeof(ZRoutedRpcAwakePatch));
                Logger.LogInfo("[Viking Ragebait] Patches applied (DoAttack, ApplyDamage, ZRoutedRpc.Awake)");
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
