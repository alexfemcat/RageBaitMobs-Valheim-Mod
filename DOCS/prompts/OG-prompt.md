Act as a Senior C# Developer and Valheim Modding Expert. I want to create a server-side-only BepInEx mod for Valheim that integrates a local LLM (via LM Studio) to make mobs "trash talk" players.

### Project Overview
- Title: "Viking Ragebait: The Toxic Mob Mod"
- Core Mechanic: When a mob targets a player or takes damage, it sends a request to a local LM Studio API to generate a "Call of Duty lobby" style insult.
- Target: Server-side only. Must be compatible with vanilla clients.

### Technical Requirements
1. Framework: BepInEx 5 + Harmony for patching.
2. Hook Points: 
    - MonsterAI.UpdateTargeting (for initial aggro).
    - Character.ApplyDamage (for reaction to being hit).
3. API Integration:
    - Target: http://localhost:1234/v1/chat/completions.
    - Performance: Must be ASYNC. Use TinyLlama-1.1B or Phi-3-mini for sub-second latency.
4. Logic & Optimization:
    - Identity Context: Extract the mob's internal name (e.g., $enemy_greydwarf).
    - Localization: You MUST use `Localization.instance.Localize(character.m_name)` to convert the internal string into a human-readable name (e.g., "Greydwarf") before passing it to the LLM.
    - Global Cooldown: Prevent API spam (e.g., 5 seconds between any mob speaking).
    - Individual Cooldown: Prevent one mob from yapping constantly (e.g., 60 seconds per specific NPC).
    - Toggleable Output: Include a config setting to switch between 'Shout' (Yellow text over head) and 'Normal' (Local chat).

### The Dynamic Persona Prompt
The mod should construct the prompt dynamically:
"You are a [LocalizedMobName] in the game Valheim. You absolutely loathe humans. You just spotted a player or got hit by one. Write a short, one-sentence insult in the style of a toxic 'Call of Duty' lobby. Use slang like 'skill issue', 'L + Ratio', 'get gud', 'touch grass', or 'dogwater'. Keep it under 15 words. Be petty and aggressive."

### Instructions for the Dev Plan
Please provide:
1. A Folder Structure for the project.
2. The `Project.csproj` with necessary Valheim/BepInEx references.
3. The `Plugin.cs` containing the Harmony patches and a configuration manager for the Shout/Chat toggle and cooldown timers.
4. An `LLMService.cs` class to handle the HttpClient requests to LM Studio asynchronously using JSON serialization.
5. Logic using `ZRoutedRpc.instance.InvokeRoutedRPC` to broadcast the message server-side, ensuring the 'sender' name is the Localized Mob Name.