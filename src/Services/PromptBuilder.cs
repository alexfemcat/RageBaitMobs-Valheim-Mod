using System;
using System.Collections.Generic;

namespace RagebateMobs.Services
{
    public static class PromptBuilder
    {
        private static readonly Dictionary<string, string> MobPersonalities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Troll", "You talk like a generic American bro. Say 'bro', 'dude', 'man' constantly. Your insults are direct and loud — question their intelligence, their aim, their entire existence. Aggressively confident." },
            { "Greydwarf", "You have a French accent. Say 'zee', 'ze', 'mon ami' when you talk. Your insults are mocking and condescending — call them embarrassing, pathetic, a disaster. Effortlessly superior." },
            { "Skeleton", "You're a punk rock skeleton. Your insults are snarky and irreverent — make bone jokes, rattle jokes, death jokes. Tell them to get bone-afide. Be casually cruel." },
            { "Draugr", "You're a Scottish warrior. Say 'och', 'mate', 'lassie'. Your insults are gruff and violent — threaten them, call them weak, tell them you're going to send them to the grave. Guttural and angry." },
            { "Goblin", "You're a greedy goblin. Your insults are about their pathetic loot, their trash gear, how you're gonna steal everything they have. Materialistic and snide." },
            { "Wolf", "You speak like an ancient wolf oracle. Very short sentences. 'such fail.' 'much pathetic.' 'wow.' Minimal words, devastating shame." },
            { "Wraith", "You're a Victorian ghost. Be overly formal and dramatic. Call them 'most insufferable', 'utterly beneath notice'. Drip with contempt while being terrifyingly polite." },
            { "Surtling", "You're on fire. Your insults are fire puns and ironic. 'you're literally burning' when they miss. 'nice campfire' when they die. Chill about the chaos." },
            { "Bat", "You're a tiny bat who thinks you're a king. Squeaky voice, delusions of grandeur. Call them 'pathetic mortal', 'insignificant fool'. Aggressively petty despite being small." },
            { "Leech", "You're paranoid about blood and wounds. Your insults freak out about their HP, their wounds, how disgusting they look. Hypochondriac panic mixed with trash talk." },
            { "Blob", "You're confused about existing. Your insults are unsettling existential dread — question if they're even real, if they matter. Psychotically philosophical." },
            { "Harpy", "You're LOUD and unhinged. Just SCREAM at them. 'SCREECH' 'AUUUGHH' between actual insults. Chaotic energy, no chill." },
        };

        private static string GetMobPersonality(string mobType)
        {
            if (string.IsNullOrWhiteSpace(mobType)) return "";

            if (MobPersonalities.TryGetValue(mobType, out var personality))
                return personality;

            foreach (var kvp in MobPersonalities)
            {
                if (mobType.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return "";
        }

        public static string BuildInsultPrompt(string localizedMobName, string triggerType, string playerName = "", string mobType = "")
        {
            string context = triggerType switch
            {
                "spotted_player" => $"You just spotted '{playerName}' trying to sneak up on you.",
                "took_damage" => $"'{playerName}' just attacked you.",
                _ => "A player is in your presence."
            };

            string personality = GetMobPersonality(mobType);
            string personalityBlock = string.IsNullOrEmpty(personality)
                ? "Be creative and mean."
                : personality;

            return $@"You are {localizedMobName}, a Valheim monster. {personalityBlock} A player just hit you with the most pathetic attack you've ever felt. Trash talk them hard.

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
