# Viking Ragebait: The Toxic Mob Mod - Development Plan

**Project:** Server-side BepInEx mod integrating local LLM (LM Studio) for mob trash-talk
**Target Framework:** .NET Framework 4.7.2 (Valheim/BepInEx compatible)
**LLM Integration:** LM Studio API (http://localhost:1234/v1/chat/completions)

---

## Phase 1: Project Setup & Infrastructure ✅ COMPLETE

### Folder Structure & Dependencies
- [x] Create project root directory structure:
  - [x] `/RagebateMobs/` (root)
  - [x] `/RagebateMobs/src/` (C# source files)
  - [x] `/RagebateMobs/DOCS/` (documentation)
  - [x] `/RagebateMobs/bin/` (build output)
  - [x] `/RagebateMobs/obj/` (build artifacts)
  - [x] `/RagebateMobs/.gitignore` (exclude bin, obj, .vs)

### Core Project Files
- [x] **RagebateMobs.csproj** - Configure project file with:
  - [x] Target Framework: `net472` (Mono-compatible) for Linux/PufferPanel
  - [x] Add local BepInEx assembly references
  - [x] Configure output directory: `bin/Release/net472/`
  - [x] **Linux note:** Use forward slashes in all paths
  - [x] Minimal .csproj structure configured

---

## Phase 2: Core LLM Integration ✅ COMPLETE

### LLMService.cs - API Communication Layer
- [x] Create `src/Services/LLMService.cs` with:
  - [x] HttpClient static instance (10s timeout)
  - [x] Async method: `GenerateInsultAsync(string prompt)`
  - [x] Proper JSON serialization for LM Studio API
  - [x] Model: "gemma-3", temperature: 0.95, max_tokens: 50
  - [x] JSON response parsing with error handling
  - [x] Graceful timeout/connection failure handling
  - [x] Logging for debugging

### LLM Prompt Builder
- [x] Create `src/Services/PromptBuilder.cs` with:
  - [x] Method: `BuildInsultPrompt(string localizedMobName, string triggerType, string playerName)` 
  - [x] Context-aware prompts: "spotted_player" vs "took_damage"
  - [x] **AUTHENTIC trash-talk** - no sanitized cringe
  - [x] Real gaming insults: retard, dogwater, skill issue, L+ratio, washed, cope, seethe, etc.
  - [x] Encourages genuine meanness and pettiness
  - [x] Constraint: <20 words, 1-2 sentences max

---

## Phase 3: Configuration & Cooldown Management ✅ COMPLETE

### Plugin Configuration System
- [x] Create `src/Configuration/ModConfig.cs` with:
  - [x] Config property: `OutputMode` (enum: Shout, Chat)
  - [x] Config property: `GlobalCooldownSeconds` (default: 5)
  - [x] Config property: `PerMobCooldownSeconds` (default: 60)
  - [x] Config property: `LMStudioApiUrl` (default: "http://localhost:1234")
  - [x] Config property: `Enabled` (toggle for entire mod)
  - [x] Config property: `MinDamageThreshold` (only trigger on significant damage)
  - [x] Auto-save to BepInEx ConfigFile

### Cooldown Manager
- [x] Create `src/Managers/CooldownManager.cs` with:
  - [x] Per-mob cooldown: 15 seconds (same mob can't spam)
  - [x] Per-mob cooldown dict (keyed by InstanceID)
  - [x] NO global cooldown (allows multiple mobs to talk simultaneously)
  - [x] Method: `CanMobSpeak(Character mob)` → bool
  - [x] Method: `RecordMobSpeak(Character mob)` → void

---

## Phase 4: Core Plugin & Harmony Patches ✅ COMPLETE

### Plugin.cs - Main Entry Point
- [x] Create `src/Plugin.cs` with:
  - [x] Class: `RagebateMobsPlugin : BaseUnityPlugin`
  - [x] BepInEx plugin attributes (GUID, Name, Version)
  - [x] Static properties for all services
  - [x] Awake(): Initialize ModConfig, LLMService, CooldownManager, OutputManager, TaskManager
  - [x] ApplyPatches(): Apply both Harmony patches
  - [x] OnDestroy(): Cleanup

### Harmony Patch: MonsterAI.UpdateTargeting
- [x] Create `src/Patches/MonsterAITargetingPatch.cs` with:
  - [x] Postfix patch on `MonsterAI.UpdateTargeting()`
  - [x] Detects initial mob aggro (target acquisition)
  - [x] HashSet tracks mobs already aggro'd (one trigger per aggro)
  - [x] Cooldown checks before requesting insult
  - [x] Localizes mob name via `Localization.instance.Localize()`
  - [x] Async call to `LLMService.GenerateInsultAsync()` (fire-and-forget)
  - [x] Broadcasts via `OutputManager.BroadcastInsult()`

### Harmony Patch: Character.ApplyDamage
- [x] Create `src/Patches/CharacterDamagePatch.cs` with:
  - [x] Postfix patch on `Character.ApplyDamage()`
  - [x] Filters: only mobs (not players), must be attacked by player
  - [x] Respects MinDamageThreshold config
  - [x] Cooldown checks before requesting insult
  - [x] Passes "took_damage" context to LLM
  - [x] Async call with fire-and-forget pattern
  - [x] Broadcasts response via OutputManager

---

## Phase 5: Message Broadcasting & Output ✅ COMPLETE

### Broadcast/Output Manager (SERVER-SIDE ONLY)
- [x] Create `src/Managers/OutputManager.cs` with:
  - [x] Method: `BroadcastInsult(Character mob, string insult)` → void
  - [x] **CRITICAL:** Use ONLY vanilla Valheim features that work on unmodded clients
  - [x] Logic branch based on `ModConfig.OutputMode`:
    - [x] **Shout Mode (Preferred for vanilla clients):**
      - [x] Use `Character.Say(insult)` on the mob character
      - [x] Displays yellow speech bubble over mob's head (vanilla feature)
      - [x] Visible to all connected players automatically
      - [x] No custom RPC needed — vanilla feature
    - [x] **Chat Mode:**
      - [x] Use `Chat.instance.SendMessage(insult)` 
      - [x] Broadcasts to all players' chat logs
      - [x] Format: `[MOB NAME]: insult text`
      - [x] Vanilla feature, no client mod required
  - [x] **Server-side validation:**
    - [x] Only run on `if (!ZNet.instance.IsServer()) return;`
    - [x] Never instantiate client-side UI
    - [x] Never reference UnityEngine.UI or client-only components
  - [x] **Per-frame message limiting:**
    - [x] Frame counter to prevent 50 messages per instant
    - [x] MaxSimultaneousInsults config (default: 5)

### Vanilla Feature Broadcasting (NO Custom RPC Needed)
- [x] **NO custom ZRoutedRpc for vanilla clients**
- [x] Use vanilla `Character.Say()` and `Chat.instance.SendMessage()` only
- [x] These are built-in Valheim features that work on unmodded clients
- [x] No custom network protocols or RPC handlers
- [x] Implemented with proper server-side checks

---

## Phase 6: Integration & Error Handling ✅ COMPLETE

### Async Task Management
- [x] Create `src/Managers/TaskManager.cs` with:
  - [x] Method: `SafeFireAndForgetAsync(Func<Task> asyncFunc)` → void
  - [x] Wraps async LLM calls to avoid log spam on failures
  - [x] Catches exceptions and logs without crashing mod
  - [x] Handles cancelled tasks gracefully

### Localization Fallback
- [x] In both patches, add fallback logic:
  - [x] If `Localization.instance.Localize()` returns null/empty
  - [x] Fall back to Character.m_name or internal name
  - [x] Implemented in both MonsterAI and Character patches

### Error Handling
- [x] In LLMService:
  - [x] Handle HttpRequestException (connection failure)
  - [x] Handle JsonException (malformed response)
  - [x] Handle TaskCanceledException (timeout)
  - [x] Graceful degradation: returns null, logs warning
- [x] In patches:
  - [x] Null-check for mob/target/attacker characters
  - [x] Null-check for Character.m_name before localization
  - [x] Fire-and-forget pattern prevents blocking
- [x] In OutputManager:
  - [x] Null-checks before broadcasting
  - [x] Server-side validation before any output

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
- **Gemma-3 1B (DEFAULT - recommended for v1.0)** (~1-2 sec, excellent instruction-following, perfect for <15 word constraints)
- Ministral-3 3B (slightly better quality, ~2-3 sec, if Gemma feels repetitive)
- Nemotron-3 Nano 4B (higher quality, ~3-4 sec, if you want more creativity)

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
- [ ] Download **Gemma-3 1B** model in LM Studio (recommended for v1.0)
  - [ ] Search: "Gemma-3 1B" in LM Studio model browser
  - [ ] Download and load into context
- [ ] Run LM Studio server: `lms server start` (default: `http://localhost:1234`)
- [ ] Verify Gemma-3 1B is loaded: `curl -X GET http://localhost:1234/v1/models`
- [ ] Keep API URL in mod config pointing to host machine IP/localhost
- [ ] Verify connectivity from Valheim server to LM Studio:
  - [ ] From server: `curl -X GET http://[host-ip]:1234/v1/models`
  - [ ] Should return "gemma-3" in model list

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
- **Cooldown strategy:** Per-mob cooldown (15s) only - NO global cooldown. MaxSimultaneousInsults (5 per frame) prevents message spam
- **Localization critical:** Must use `Localization.instance.Localize()` before sending to LLM
- **Error resilience:** Mod should gracefully degrade if LM Studio is offline or slow

---

## Completed Tasks
✅ = Done | ⏳ = In Progress | ❌ = Not Started

| Task | Status |
|---|---|
| Phase 1: Project Setup & Infrastructure | ✅ COMPLETE |
| Phase 2: Core LLM Integration | ✅ COMPLETE |
| Phase 3: Configuration & Cooldown Management | ✅ COMPLETE |
| Phase 4: Core Plugin & Harmony Patches | ✅ COMPLETE |
| Phase 5: Message Broadcasting & Output | ✅ COMPLETE |
| Phase 6: Integration & Error Handling | ✅ COMPLETE |
| Phase 7: Configuration & Testing | ❌ Pending |
| Phase 8: Polish & Optimization | ❌ Pending |
| Phase 9: Deployment & Distribution | ❌ Pending |

**Core Implementation: 66% Complete** (6/9 phases)
