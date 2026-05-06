using BepInEx.Logging;
using RagebateMobs.Configuration;

namespace RagebateMobs.Managers
{
    public class OutputManager
    {
        private readonly ModConfig _config;
        private readonly ManualLogSource _logger;
        private int _insultsThisFrame = 0;
        private int _lastFrameCount = 0;

        public OutputManager(ModConfig config, ManualLogSource logger)
        {
            _config = config;
            _logger = logger;
        }

        private void ResetFrameCounter()
        {
            // Reset if we're on a new frame
            if (_lastFrameCount != UnityEngine.Time.frameCount)
            {
                _lastFrameCount = UnityEngine.Time.frameCount;
                _insultsThisFrame = 0;
            }
        }

        public void BroadcastInsult(Character mob, string insult)
        {
            if (mob == null || string.IsNullOrWhiteSpace(insult))
                return;

            // Server-side only check
            if (!ZNet.instance || !ZNet.instance.IsServer())
            {
                _logger.LogWarning("[Ragebait] OutputManager called on non-server! Aborting.");
                return;
            }

            ResetFrameCounter();

            // Check if we've hit the max insults per frame
            if (_insultsThisFrame >= _config.MaxSimultaneousInsults.Value)
            {
                if (_config.DebugLogging.Value)
                    _logger.LogInfo($"[Ragebait] Hit max simultaneous insults ({_config.MaxSimultaneousInsults.Value}) this frame");
                return;
            }

            switch (_config.OutputMode.Value)
            {
                case OutputMode.Shout:
                    BroadcastAsShout(mob, insult);
                    break;
                case OutputMode.Chat:
                    BroadcastAsChat(mob, insult);
                    break;
            }

            _insultsThisFrame++;

            if (_config.DebugLogging.Value)
                _logger.LogInfo($"[Ragebait] {mob.m_name} taunted: {insult}");
        }

        private void BroadcastAsShout(Character mob, string insult)
        {
            // Broadcast via ZRoutedRpc so all clients see the speech bubble
            mob.GetComponent<ZNetView>()?.InvokeRPC(ZNetView.Everybody, "OnNPCText", insult, 2.0f, 5.0f, Talker.Type.Shout, false);
        }

        private void BroadcastAsChat(Character mob, string insult)
        {
            string mobDisplayName = global::Localization.instance.Localize(mob.m_name);
            Chat.instance?.AddString(mobDisplayName, insult, Talker.Type.Normal);
        }
    }
}
