using BepInEx.Logging;
using RagebateMobs.Configuration;

namespace RagebateMobs.Managers
{
    public class OutputManager
    {
        private readonly ModConfig _config;
        private readonly ManualLogSource _logger;

        public OutputManager(ModConfig config, ManualLogSource logger)
        {
            _config = config;
            _logger = logger;
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

            switch (_config.OutputMode.Value)
            {
                case OutputMode.Shout:
                    BroadcastAsShout(mob, insult);
                    break;
                case OutputMode.Chat:
                    BroadcastAsChat(mob, insult);
                    break;
            }

            if (_config.DebugLogging.Value)
                _logger.LogInfo($"[Ragebait] {mob.m_name} taunted: {insult}");
        }

        private void BroadcastAsShout(Character mob, string insult)
        {
            // Use vanilla Character.Say() - displays yellow speech bubble
            // This is a vanilla Valheim feature, no custom RPC needed
            mob.Say(insult);
        }

        private void BroadcastAsChat(Character mob, string insult)
        {
            // Use vanilla Chat system to broadcast to all players
            string mobDisplayName = Localization.instance.Localize(mob.m_name);
            string chatMessage = $"{mobDisplayName}: {insult}";

            if (Chat.instance != null)
            {
                Chat.instance.SendMessage(chatMessage);
            }
            else
            {
                _logger.LogWarning("[Ragebait] Chat instance not found!");
            }
        }
    }
}
