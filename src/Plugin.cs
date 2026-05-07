using System.IO;
using System.Threading;
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
        public static KillCountManager KillCountManager { get; private set; }
        public static BettingManager BettingManager { get; private set; }
        public static SemaphoreSlim LlmSemaphore { get; private set; }

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

                string savePath = Path.Combine(Paths.ConfigPath, "ragebatemobs_kills.json");
                KillCountManager = new KillCountManager(savePath);
                BettingManager = new BettingManager();

                LlmSemaphore = new SemaphoreSlim(Config.MaxSimultaneousInsults.Value);

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
            ApplyOne(typeof(MonsterAITargetingPatch), "MonsterAI.DoAttack");
            ApplyOne(typeof(CharacterDamagePatch), "Character.ApplyDamage");
            ApplyOne(typeof(PlayerDeathPatch), "Character.ApplyDamage (death)");
            ApplyOne(typeof(MobDeathPatch), "Character.ApplyDamage (mob death)");
            ApplyOne(typeof(MobLowHealthPatch), "Character.ApplyDamage (low-hp hype)");
            ApplyOne(typeof(ZNetAwakePatch), "ZNet.Awake");
        }

        private void ApplyOne(System.Type patchType, string label)
        {
            try
            {
                _harmony.PatchAll(patchType);
                Logger.LogInfo($"[Viking Ragebait] Patched {label}");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[Viking Ragebait] Failed to patch {label}: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            CooldownManager?.Clear();
            KillCountManager?.Save();
            BettingManager?.Clear();
            LlmSemaphore?.Dispose();
            Logger.LogInfo("[Viking Ragebait] Unloaded.");
        }
    }
}
