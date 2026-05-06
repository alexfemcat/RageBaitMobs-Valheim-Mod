using BepInEx.Configuration;

namespace RagebateMobs.Configuration
{
    public enum OutputMode
    {
        Shout,  // Yellow speech bubble over mob head
        Chat    // Server chat message
    }

    public class ModConfig
    {
        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<OutputMode> OutputMode { get; private set; }
        public ConfigEntry<string> LMStudioApiUrl { get; private set; }
        public ConfigEntry<float> GlobalCooldownSeconds { get; private set; }
        public ConfigEntry<float> PerMobCooldownSeconds { get; private set; }
        public ConfigEntry<float> MinDamageThreshold { get; private set; }
        public ConfigEntry<bool> DebugLogging { get; private set; }

        public ModConfig(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable the mod entirely"
            );

            OutputMode = config.Bind(
                "General",
                "OutputMode",
                Configuration.OutputMode.Shout,
                "How mobs deliver their insults (Shout = yellow speech bubble, Chat = server chat)"
            );

            LMStudioApiUrl = config.Bind(
                "API",
                "LMStudioUrl",
                "http://localhost:1234",
                "URL where LM Studio is running (default: localhost)"
            );

            GlobalCooldownSeconds = config.Bind(
                "Cooldowns",
                "GlobalCooldownSeconds",
                8f,
                "Minimum seconds between ANY mob speaking (prevents spam)"
            );

            PerMobCooldownSeconds = config.Bind(
                "Cooldowns",
                "PerMobCooldownSeconds",
                15f,
                "Minimum seconds before the SAME mob speaks again (max 15s)"
            );

            MinDamageThreshold = config.Bind(
                "Triggers",
                "MinDamageThreshold",
                5f,
                "Minimum damage needed to trigger a taunt (0 = all damage triggers)"
            );

            DebugLogging = config.Bind(
                "Debug",
                "DebugLogging",
                false,
                "Enable detailed logging for debugging"
            );
        }
    }
}
