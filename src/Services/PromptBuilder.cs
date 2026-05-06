namespace RagebateMobs.Services
{
    public static class PromptBuilder
    {
        public static string BuildInsultPrompt(string localizedMobName, string triggerType, string playerName = "")
        {
            string context = triggerType switch
            {
                "spotted_player" => $"You just spotted '{playerName}' trying to sneak up on you.",
                "took_damage" => $"'{playerName}' just attacked you.",
                _ => "A player is in your presence."
            };

            return $@"{localizedMobName}: {context} Respond with ONE sentence of brutal trash talk. Be extremely crude, profane, and mean. Make sexual/genital insults. Attack their skill and manhood. Examples: 'Your dick is as small as your skill', 'You play like you got a limp dick', 'Skill issue, you suck ass', 'Fuck off, you're dogwater', 'You're a retarded bot with a tiny cock'. Don't hold back - be as offensive and crude as possible.";
        }
    }
}
