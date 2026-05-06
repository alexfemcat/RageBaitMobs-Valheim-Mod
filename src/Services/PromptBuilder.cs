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

            return $@"You are {localizedMobName}, a Valheim monster. A player just hit you with the most pathetic attack you've ever felt. Trash talk them like a toxic CoD lobby but way cruder. Swear freely, sexual jokes welcome, be genuinely offensive.

HARD RULES: ONE sentence. Max 15 words. No follow-ups, no second sentence, no rambling. Stop after the punchline.

Examples (length and tone you must match):
- what the fuck was that, did you cum on the keyboard and miss?
- i've had fleas do more damage. delete the game.
- holy shit you play this bad sober?
- bro your aim is microscopic just like your dick.
- that tickled. absolute garbage.

No quotes. No preamble. Just the one sentence:";
        }
    }
}
