using BepInEx.Configuration;

namespace RagebateMobs.Configuration
{
    public enum OutputMode { Shout, Chat }
    public class ModConfig
    {
        private readonly ConfigFile _config;
        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<string> LMStudioApiUrl { get; private set; }
        public ConfigEntry<string> Model { get; private set; }
        public ConfigEntry<OutputMode> OutputModeSetting { get; private set; }
        public ConfigEntry<string> LLMModel { get; private set; }
        public ConfigEntry<int> MaxSimultaneousInsults { get; private set; }
        public ConfigEntry<bool> DebugLogging { get; private set; }
        public ConfigEntry<int> PerMobCooldownSeconds { get; private set; }
        public ConfigEntry<float> MinDamageThreshold { get; private set; }

        public ModConfig(ConfigFile config)
        {
            _config = config;
            Enabled = _config.Bind("General", "Enabled", true, "Enable the mod");
            LMStudioApiUrl = _config.Bind("API", "LMStudioApiUrl", "http://localhost:1234", "Base URL for LM Studio API");
            Model = _config.Bind("API", "Model", "meta-llama-3.1-8b-instruct-abliterated", "LLM model to use");
            OutputModeSetting = _config.Bind("General", "OutputMode", OutputMode.Shout, "Shout or Chat output");
            PerMobCooldownSeconds = _config.Bind("Cooldowns", "PerMobCooldownSeconds", 60, "Per‑mob cooldown (seconds)");
            MinDamageThreshold = _config.Bind("Triggers", "MinDamageThreshold", 10f, "Minimum damage to trigger roast");
            LLMModel = _config.Bind("API", "LLMModel", "meta-llama-3.1-8b-instruct-abliterated", "LLM model name");
            MaxSimultaneousInsults = _config.Bind("General", "MaxSimultaneousInsults", 5, "Maximum simultaneous insults per mob");
            DebugLogging = _config.Bind("General", "DebugLogging", false, "Enable debug logging")
        }
    }
}
