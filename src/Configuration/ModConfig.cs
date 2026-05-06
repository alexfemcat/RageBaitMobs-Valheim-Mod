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
        public ConfigEntry<float> PerMobCooldownSeconds { get; private set; }
        public ConfigEntry<int> MaxSimultaneousInsults { get; private set; }
        public ConfigEntry<float> MinDamageThreshold { get; private set; }
        public ConfigEntry<bool> DebugLogging { get; private set; }

        public ConfigEntry<string> LLMModel { get; private set; }

        public ModConfig(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable the mod entirely"
            );

            LLMModel = config.Bind(
                "API",
                "LLMModel",
                "mistralai/ministral-3-3b",
                "LLM model to use (mistralai/ministral-3-3b recommended for authentic trash talk)"
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
                "http://localhost:1234/v1",
                "Base URL for LM Studio API (includes /v1 endpoint)"
            );

            PerMobCooldownSeconds = config.Bind(
                "Cooldowns",
                "PerMobCooldownSeconds",
                15f,
                "Minimum seconds before the SAME mob speaks again"
            );

            MaxSimultaneousInsults = config.Bind(
                "Cooldowns",
                "MaxSimultaneousInsults",
                5,
                "Max insults to broadcast simultaneously (prevents message spam on frame)"
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
