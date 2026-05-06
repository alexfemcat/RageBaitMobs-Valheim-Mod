# Viking Ragebait: The Toxic Mob Mod - Development Plan

**Project:** Server-side BepInEx mod integrating local LLM (LM Studio) for mob trash-talk
**Target Framework:** .NET Framework 4.7.2 (Valheim/BepInEx compatible)
**LLM Integration:** LM Studio API (http://localhost:1234/v1/chat/completions)

---

## Phase 1: Project Setup & Infrastructure

### Folder Structure & Dependencies
- [ ] Create project root directory structure:
  - [ ] `/RagebateMobs/` (root)
  - [ ] `/RagebateMobs/src/` (C# source files)
  - [ ] `/RagebateMobs/DOCS/` (documentation)
  - [ ] `/RagebateMobs/bin/` (build output)
  - [ ] `/RagebateMobs/obj/` (build artifacts)
  - [ ] `/RagebateMobs/.gitignore` (exclude bin, obj, .vs)

### Core Project Files
- [ ] **RagebateMobs.csproj** - Configure project file with:
  - [ ] Target Framework: `net472` (Mono-compatible) for Linux/PufferPanel
  - [ ] Add BepInEx 5 NuGet reference
  - [ ] Add Harmony NuGet reference
  - [ ] Add Valheim assembly references (extract from server install or use NuGet if available)
  - [ ] Configure output directory: `bin/Release/net472/`
  - [ ] **Linux note:** Use forward slashes in all paths, avoid hardcoded Windows paths
  - [ ] PostBuildEvent: Copy DLL to local BepInEx/plugins folder (optional, for testing)
  - [ ] Example minimal .csproj structure:
    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net472</TargetFramework>
        <OutputType>Library</OutputType>
        <LangVersion>latest</LangVersion>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="BepInEx" Version="5.4.x" />
        <PackageReference Include="Harmony" Version="2.x" />
      </ItemGroup>
    </Project>
    ```

---

## Phase 2: Core LLM Integration

### LLMService.cs - API Communication Layer
- [ ] Create `src/Services/LLMService.cs` with:
  - [ ] HttpClient static instance (avoid socket exhaustion)
  - [ ] Async method: `GenerateInsultAsync(string mobName, string playerName, string context)`
  - [ ] Build JSON request payload for LM Studio API:
    ```
    {
      "model": "local-model",
      "messages": [{"role": "user", "content": "<dynamic prompt>"}],
      "temperature": 0.8,
      "max_tokens": 50
    }
    ```
  - [ ] Parse JSON response and extract generated text
  - [ ] Error handling (timeout, connection failure, malformed response)
  - [ ] Logging for debugging API calls (optional)

### LLM Prompt Builder
- [ ] Create `src/Services/PromptBuilder.cs` with:
  - [ ] Method: `BuildInsultPrompt(string localizedMobName, string triggerType)` 
    - `triggerType` = "spotted_player" or "took_damage"
  - [ ] Dynamic persona prompt construction:
    - Base: "You are a [LocalizedMobName] in the game Valheim. You absolutely loathe humans."
    - Context: "You just [spotted a player / got hit by one]."
    - Instruction: "Write a short, one-sentence insult in the style of a toxic 'Call of Duty' lobby."
    - Slang guide: "Use slang like 'skill issue', 'L + Ratio', 'get gud', 'touch grass', or 'dogwater'."
    - Constraints: "Keep it under 15 words. Be petty and aggressive."

---

## Phase 3: Configuration & Cooldown Management

### Plugin Configuration System
- [ ] Create `src/Configuration/ModConfig.cs` with:
  - [ ] Config property: `OutputMode` (enum: Shout, Chat)
  - [ ] Config property: `GlobalCooldownSeconds` (default: 5)
  - [ ] Config property: `PerMobCooldownSeconds` (default: 60)
  - [ ] Config property: `LMStudioApiUrl` (default: "http://localhost:1234")
  - [ ] Config property: `Enabled` (toggle for entire mod)
  - [ ] Config property: `MinDamageThreshold` (only trigger on significant damage)
  - [ ] Load/save to BepInEx ConfigFile

### Cooldown Manager
- [ ] Create `src/Managers/CooldownManager.cs` with:
  - [ ] Property: `LastGlobalTalkTime` (DateTime)
  - [ ] Dictionary: `perMobLastTalkTime` (Keyed by Character/NPC instance or InstanceID)
  - [ ] Method: `CanMobSpeak(Character mob)` → bool
    - Check if global cooldown elapsed
    - Check if specific mob cooldown elapsed
    - Return true only if both are ready
  - [ ] Method: `RecordMobSpeak(Character mob)` → void
    - Update LastGlobalTalkTime
    - Update perMobLastTalkTime[mob]

---

## Phase 4: Core Plugin & Harmony Patches

### Plugin.cs - Main Entry Point
- [ ] Create `src/Plugin.cs` with:
  - [ ] Class: `RagebateMobsPlugin : BaseUnityPlugin`
  - [ ] BepInEx plugin attributes:
    - [ ] GUID: "com.valheim.ragebatemobs"
    - [ ] Name: "Viking Ragebait"
    - [ ] Version: "1.0.0"
  - [ ] Initialize ModConfig on Awake()
  - [ ] Initialize CooldownManager singleton
  - [ ] Apply Harmony patches on Awake()
  - [ ] Add logging utility methods

### Harmony Patch: MonsterAI.UpdateTargeting
- [ ] Create `src/Patches/MonsterAITargetingPatch.cs` with:
  - [ ] Postfix patch on `MonsterAI.UpdateTargeting()`
  - [ ] Check if mob just acquired a player target
  - [ ] Check cooldowns via CooldownManager
  - [ ] If cooldowns pass:
    - [ ] Localize mob name: `Localization.instance.Localize(character.m_name)`
    - [ ] Extract player name from target
    - [ ] Call `LLMService.GenerateInsultAsync()` (async/fire-and-forget)
    - [ ] On response, call broadcast method (next section)
    - [ ] Record speak time in CooldownManager

### Harmony Patch: Character.ApplyDamage
- [ ] Create `src/Patches/CharacterDamagePatch.cs` with:
  - [ ] Postfix patch on `Character.ApplyDamage()`
  - [ ] Check if damaged character is a mob (not player)
  - [ ] Check if damage exceeds MinDamageThreshold config
  - [ ] Check cooldowns via CooldownManager
  - [ ] If cooldowns pass:
    - [ ] Localize mob name
    - [ ] Extract attacker (player) name
    - [ ] Call `LLMService.GenerateInsultAsync()` with "took_damage" context
    - [ ] On response, call broadcast method
    - [ ] Record speak time in CooldownManager

---

## Phase 5: Message Broadcasting & Output

### Broadcast/Output Manager (SERVER-SIDE ONLY)
- [ ] Create `src/Managers/OutputManager.cs` with:
  - [ ] Method: `BroadcastInsult(string mobName, string insult)` → void
  - [ ] **CRITICAL:** Use ONLY vanilla Valheim features that work on unmodded clients
  - [ ] Logic branch based on `ModConfig.OutputMode`:
    - [ ] **Shout Mode (Preferred for vanilla clients):**
      - [ ] Use `Character.Say(insult)` on the mob character
      - [ ] Displays yellow speech bubble over mob's head (vanilla feature)
      - [ ] Visible to all connected players automatically
      - [ ] No custom RPC needed — vanilla feature
    - [ ] **Chat Mode:**
      - [ ] Use `Chat.instance.SendMessage(insult)` or similar
      - [ ] Broadcasts to all players' chat logs
      - [ ] Format: `[MOB NAME]: insult text`
      - [ ] Vanilla feature, no client mod required
  - [ ] **Server-side validation:**
    - [ ] Only run on `if (!ZNet.instance.IsServer()) return;`
    - [ ] Never instantiate client-side UI
    - [ ] Never reference UnityEngine.UI or client-only components

### Vanilla Feature Broadcasting (NO Custom RPC Needed)
- [ ] **IMPORTANT: Do NOT use custom ZRoutedRpc for vanilla clients!**
- [ ] Use vanilla `Character.Say()` and `Chat.instance.SendMessage()` only
- [ ] These are built-in Valheim features that work on unmodded clients
- [ ] No need for custom network protocols or RPC handlers
- [ ] Example (Shout Mode):
  ```csharp
  var mob = /* get character */;
  string insult = /* LLM response */;
  mob.Say(insult);  // Server-side only, vanilla clients see it
  ```
- [ ] Example (Chat Mode):
  ```csharp
  Chat.instance.SendMessage($"[{mobName}]: {insult}");
  // Appears in all players' chat logs, no mod required
  ```

---

## Phase 6: Integration & Error Handling

### Async Task Management
- [ ] Create `src/Managers/TaskManager.cs` with:
  - [ ] Method: `SafeFireAndForgetAsync(Task task)` → void
  - [ ] Wraps async LLM calls to avoid log spam on failures
  - [ ] Log exceptions without crashing mod

### Localization Fallback
- [ ] In both patches, add fallback logic:
  - [ ] If `Localization.instance.Localize()` returns null/empty
  - [ ] Fall back to Character.name or internal name
  - [ ] Log warning for debugging

### Error Handling
- [ ] In LLMService:
  - [ ] Handle HttpRequestException (connection failure)
  - [ ] Handle JsonException (malformed response)
  - [ ] Handle TimeoutException (LM Studio unresponsive)
  - [ ] Graceful degradation: log error, don't crash mod
- [ ] In patches:
  - [ ] Null-check for targets and characters
  - [ ] Null-check for Character.m_name before localization
  - [ ] Catch exceptions in async callbacks

---

## Phase 7: Configuration & Testing

### BepInEx Configuration UI
- [ ] Create config entries in `ModConfig.cs`:
  - [ ] Bind to BepInEx ConfigFile
  - [ ] Set descriptions and acceptable values
  - [ ] Auto-generate config file on first load

### Testing Checklist
- [ ] **Unit Tests (optional but recommended):**
  - [ ] PromptBuilder generates valid prompts
  - [ ] LLMService parses API response correctly
  - [ ] CooldownManager enforces cooldowns
  - [ ] OutputManager formats messages correctly

- [ ] **Integration Tests (manual in-game):**
  - [ ] [ ] Load mod in Valheim server only (NO mod on client)
  - [ ] [ ] Connect with **VANILLA client** (no mods) to server
  - [ ] [ ] Spawn mob, get hit → mob taunts appear
  - [ ] [ ] Vanilla client sees yellow speech bubble (Shout mode)
  - [ ] [ ] Vanilla client sees chat message (Chat mode)
  - [ ] [ ] Aggro mob → mob taunts appear to vanilla client
  - [ ] [ ] Global cooldown prevents spam
  - [ ] [ ] Per-mob cooldown works
  - [ ] [ ] Toggle OutputMode (Shout/Chat) works
  - [ ] [ ] Disable mod via config → no taunts
  - [ ] [ ] LM Studio offline → graceful failure, no crash
  - [ ] **CRITICAL:** Test with completely unmodded Valheim client to confirm it works

---

## Phase 8: Polish & Optimization

### Performance Optimization
- [ ] Review HttpClient usage (ensure no socket leaks)
- [ ] Profile async task creation
- [ ] Minimize Harmony patch overhead
- [ ] Cache localized mob names if possible

### Logging & Debugging
- [ ] Add debug logging for:
  - [ ] Patch invocations
  - [ ] Cooldown checks
  - [ ] LLM API requests/responses
  - [ ] Broadcast messages
- [ ] Make logging configurable (toggle in config)

### Documentation
- [ ] [ ] Create README.md:
  - [ ] Installation instructions
  - [ ] Configuration guide
  - [ ] LM Studio setup (model recommendations)
  - [ ] Known issues/limitations
- [ ] [ ] Add inline code comments for complex logic
- [ ] [ ] Document Harmony patches with method signatures

### Cleanup & Finalization
- [ ] Remove debug code/logging
- [ ] Update version number (1.0.0 → etc.)
- [ ] Create release notes
- [ ] Test on clean Valheim install

---

## Phase 9: Deployment & Distribution

### Build & Package
- [ ] Build Release configuration
- [ ] Verify DLL loads in BepInEx
- [ ] Create distribution package:
  - [ ] DLL file
  - [ ] README
  - [ ] Sample config file
  - [ ] License (if applicable)

### Distribution
- [ ] Upload to Nexus Mods / GitHub Releases
- [ ] Add mod listing to Valheim mod communities
- [ ] Share with potential testers for feedback

---

## Dependencies & Requirements

| Dependency | Version | Purpose | Linux Note |
|---|---|---|---|
| BepInEx | 5.x (Unix) | Plugin framework | Use Unix build, requires Mono |
| Harmony | 2.x | Method patching | Cross-platform |
| Valheim Assemblies | Latest | Game API | Extract from server install |
| Mono | 6.x+ | Runtime | Required for BepInEx on Linux |
| .NET SDK | 6.0+ | Build tools | dotnet CLI for building |
| LM Studio | Latest | Local LLM inference | Install on Linux Mint host |

**Recommended LLM Models:**
- TinyLlama-1.1B (fastest, ~1-2 sec)
- Phi-3-mini (balanced, ~2-3 sec)
- Neural-chat-7B (best quality, ~5-10 sec)

---

## Linux-Specific Setup

### Development Environment (Linux Mint)
- [ ] Install Mono: `sudo apt install mono-complete`
- [ ] Install .NET SDK 6.0+: `sudo apt install dotnet-sdk-6.0`
- [ ] Verify versions:
  - [ ] `mono --version`
  - [ ] `dotnet --version`
- [ ] Clone/create project on Linux Mint

### BepInEx for Linux (Valheim Server)
- [ ] Download BepInEx **Unix build** (NOT Windows) from [BepInEx releases](https://github.com/BepInEx/BepInEx/releases)
- [ ] Extract to Valheim server root: `~/.local/share/Steam/steamapps/common/Valheim` or PufferPanel data directory
- [ ] Verify structure:
  - [ ] `BepInEx/core/` (BepInEx libraries)
  - [ ] `BepInEx/plugins/` (mod DLLs go here)
  - [ ] `BepInEx/config/` (mod configs)
  - [ ] `doorstop_config.ini` (BepInEx entry point)
  - [ ] `run_bepinex.sh` (launch script)

### Building on Linux
- [ ] Update `.csproj` to use `dotnet` CLI instead of Visual Studio:
  - [ ] Target: `net472` (Mono-compatible) or `net6.0` (if using .NET Core)
  - [ ] Remove Visual Studio-specific properties
  - [ ] Build command: `dotnet build -c Release`
  - [ ] Output: DLL in `bin/Release/net472/`
- [ ] Copy built DLL to: `BepInEx/plugins/RagebateMobs/RagebateMobs.dll`

### PufferPanel Integration
- [ ] Locate PufferPanel server data directory (typically in `/var/lib/pufferpanel/servers/[server-id]/`)
- [ ] BepInEx plugin path: `/var/lib/pufferpanel/servers/[server-id]/BepInEx/plugins/`
- [ ] Mod config path: `/var/lib/pufferpanel/servers/[server-id]/BepInEx/config/`
- [ ] Add startup script modification (if needed) to use `run_bepinex.sh` instead of vanilla executable
- [ ] Ensure file permissions: mods should be readable by PufferPanel service user

### LM Studio on Linux Mint (Host Machine)
- [ ] Install LM Studio for Linux from [lmstudio.ai](https://lmstudio.ai)
- [ ] Run LM Studio server: `lms server start` (default: `http://localhost:1234`)
- [ ] Keep API URL in mod config pointing to host machine IP/localhost
- [ ] Verify connectivity from Valheim server to LM Studio:
  - [ ] From server: `curl -X GET http://[host-ip]:1234/v1/models`
  - [ ] Should return available models

### File Paths (Linux vs Windows)
- [ ] Use forward slashes `/` in all file paths (not backslashes)
- [ ] Example config path: `/home/user/.config/BepInEx/config.ini`
- [ ] HttpClient URLs: Always use `http://localhost:1234` or `http://[lan-ip]:1234`
- [ ] No drive letters (C:\, D:\) — use absolute paths from root `/`

---

## Notes & Considerations

### Server-Side-Only Constraints (CRITICAL)
- **NO client-side UI code:** Do not reference UnityEngine.UI, Canvas, Text, Image, etc.
- **NO custom network protocol:** Use only vanilla Valheim's Character.Say() and Chat.instance
- **NO client-required assets:** All logic must work with vanilla client executables
- **Server check:** Always verify `if (!ZNet.instance.IsServer())` before executing mod logic
- **Vanilla clients WILL connect:** Test with unmodded Valheim to confirm visibility
- **What vanilla clients will see:**
  - Yellow speech bubbles (Character.Say) over mob heads
  - Chat messages in the log (Chat.instance.SendMessage)
  - That's it — no custom UI, effects, or particle systems
- **What vanilla clients will NOT need:**
  - BepInEx (mod only runs on server)
  - This mod DLL
  - LM Studio (server-side only)
  - Any configuration

### Other Considerations
- **Async design:** LLM calls are async/fire-and-forget to avoid blocking server
- **Cooldown strategy:** Global + per-mob cooldowns prevent spam without killing immersion
- **Localization critical:** Must use `Localization.instance.Localize()` before sending to LLM
- **Error resilience:** Mod should gracefully degrade if LM Studio is offline or slow

---

## Completed Tasks
✅ = Done | ⏳ = In Progress | ❌ = Blocked

| Task | Status |
|---|---|
| Project setup | ❌ |
| LLMService | ❌ |
| Plugin.cs | ❌ |
| Patches | ❌ |
| Broadcasting | ❌ |
| Testing | ❌ |
| Documentation | ❌ |
| Release | ❌ |
