using BepInEx.Configuration;

namespace RagebateMobs.Configuration
{
    public enum OutputMode { Shout, Chat }

    public class ModConfig
    {
        private readonly ConfigFile _config;

        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<OutputMode> OutputModeSetting { get; private set; }

        public ConfigEntry<string> LLMModel { get; private set; }
        public ConfigEntry<string> LMStudioApiUrl { get; private set; }

        public ConfigEntry<int> PerMobCooldownSeconds { get; private set; }
        public ConfigEntry<int> MaxSimultaneousInsults { get; private set; }

        public ConfigEntry<float> MinDamageThreshold { get; private set; }

        public ConfigEntry<bool> DebugLogging { get; private set; }

        public ModConfig(ConfigFile config)
        {
            _config = config;

            Enabled = _config.Bind("General", "Enabled", true,
                "Enable the mod entirely");
            OutputModeSetting = _config.Bind("General", "OutputMode", OutputMode.Shout,
                "How mobs deliver their insults (Shout = yellow speech bubble, Chat = server chat)");

            LLMModel = _config.Bind("API", "LLMModel", "meta-llama-3.1-8b-instruct-abliterated",
                "LLM model to use");
            LMStudioApiUrl = _config.Bind("API", "LMStudioApiUrl", "http://localhost:1234/v1",
                "Base URL for LM Studio API (must include /v1)");

            PerMobCooldownSeconds = _config.Bind("Cooldowns", "PerMobCooldownSeconds", 5,
                "Minimum seconds before the SAME mob speaks again");
            MaxSimultaneousInsults = _config.Bind("Cooldowns", "MaxSimultaneousInsults", 5,
                "Max insults broadcast on the same frame (prevents spam)");

            MinDamageThreshold = _config.Bind("Triggers", "MinDamageThreshold", 5f,
                "Minimum damage needed to trigger a taunt (0 = all damage triggers)");

            DebugLogging = _config.Bind("Debug", "DebugLogging", false,
                "Enable detailed logging for debugging");
        }
    }
}
