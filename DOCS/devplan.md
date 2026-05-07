# Viking Ragebait: Development Plan

**Project:** Hybrid (client-detect / server-LLM) BepInEx mod for Valheim using a local LLM (LM Studio)
**Target Framework:** .NET Framework 4.7.2 (Valheim/BepInEx compatible)
**LLM Integration:** LM Studio API (http://localhost:1234/v1/chat/completions)

---

## Architecture Overview

```
[Modded client]                        [Server (your host)]            [All clients]
DoAttack / ApplyDamage postfix
        │
        │  RagebateMobs_RequestRoast (custom routed RPC)
        ▼                              receives RPC
                                       cooldown / dedup
                                       calls LM Studio (localhost)
                                                │
                                                ▼
                                       RagebateMobs_RoastBroadcast ──────▶ Chat.SetNpcText bubble
```

- **Modded client** detects events locally, sends RPC to server.
- **Server** is the only thing that talks to LM Studio.
- **All modded clients** render speech bubbles via `Chat.SetNpcText`.
- Vanilla clients can still join (soft dependency).

---

## Phase 1: Project Setup & Infrastructure ✅ COMPLETE

- [x] `RagebateMobs.csproj` (net472, Mono-compatible)
- [x] Local BepInEx assembly references
- [x] Build output: `bin/Release/net472/`

## Phase 2: LLM Integration ✅ COMPLETE

- [x] `src/Services/LLMService.cs` — async HttpClient → LM Studio
- [x] `src/Services/PromptBuilder.cs` — context-aware prompt

## Phase 3: Configuration ✅ COMPLETE

- [x] `[General]` Enabled
- [x] `[API]` LLMModel, LMStudioApiUrl
- [x] `[Cooldowns]` PerMobCooldownSeconds
- [x] `[Triggers]` MinDamageThreshold

## Phase 4: Core Triggers ✅ COMPLETE

- [x] `spotted_player` — mob first targets player
- [x] `took_damage` — player takes damage from mob

## Phase 5: Mob Personalities ✅ COMPLETE

- [x] Per-mob personality dictionary in `PromptBuilder.cs`
- [x] 12 personalities implemented: Troll, Greydwarf, Skeleton, Draugr, Goblin, Wolf, Wraith, Surtling, Bat, Leech, Blob, Harpy

---

## Phase 6: Expanded Triggers

### 6.1 - Player Death Roast ✅ COMPLETE
- [x] New trigger: `player_died`
- [x] New patch: `PlayerDeathPatch.cs` - hooks `Character.ApplyDamage` and checks if player died
- [x] Prompt context: player died to this mob, extra shameful
- [x] Mob name shown in bubble header

### 6.2 - Parry Fail Detection ❌ SKIPPED
- Not worth the complexity of Valheim API research
- Would require hooking into low-level parry timing system

### 6.3 - Whiff Detection (Swing Miss) ❌ SKIPPED
- Complex to track attack state vs damage dealt timing
- Can revisit later if desired

### 6.4 - All Valheim Mobs Personality Coverage ✅ COMPLETE
- [x] Added 24+ mob personalities to `PromptBuilder.cs`
- [x] Full mob list covered: Boar, Neck, Greyling, Greydwarf, GreydwarfBrute, GreydwarfShaman, Skeleton, Draugr, DraugrElite, Goblin, Wolf, Wraith, Surtling, Bat, Leech, Blob, Harpy, Ghost, Fenring, Serpent, Hatchling, Imp, Rootwalker, Velodon, Deer, Eikthyr, gdKing
- [x] For mobs without specific personality, falls back to closest existing

---

## Phase 7: Kill Count Shame System ✅ COMPLETE

### 7.1 - Per-Player Kill Tracking ✅
- [x] Server-side `KillCountManager.cs` with JSON persistence
- [x] Tracks mob → player → death count
- [x] Persists to `ragebatemobs_kills.json` in config folder
- [x] Loads on server start

### 7.2 - Shame Integration into Prompts ✅
- [x] When building prompt, look up kill count for this player+mob
- [x] Append shame context: "This idiot has died to you X times already."
- [x] If X > 10: "You've killed this loser X times. When will they ever learn?"
- [x] If X > 50: "This absolute piece of garbage has died to you X times. They should quit."

### 7.3 - Admin Commands for Stats ❌ SKIPPED
- Admin commands require Valheim chat/console API research - deferred

---

## Phase 8: Admin Commands ❌ SKIPPED

- Requires hooking into Valheim's chat/console command system
- Can manually edit `ragebatemobs_kills.json` to reset stats
- Can restart server to apply config changes

---

## Phase 10: Insult Intensity Slider ✅ COMPLETE

### 10.1 - Intensity Levels ✅
- [x] Define 4 levels: `Mild`, `Normal`, `Spicy`, `XXXtreme`
- [x] Config: `InsultIntensity = "Normal"` (string enum in ModConfig.cs)
- [x] Each level modifies prompt constraints and content rules

### 10.2 - Intensity Prompt Modifiers ✅
- [x] `Mild`: "Be cheeky and playful. No swearing. Keep it light and teasing."
- [x] `Normal`: "Trash talk them hard. Use some swearing. Be genuinely mean."
- [x] `Spicy`: "Be extremely mean. Heavy swearing allowed. Genuinely offensive and hostile."
- [x] `XXXtreme`: "NO RESTRICTIONS. Maximum offense. No mercy. Treat them like worthless garbage."

### 10.3 - XXXtreme Sentence Rule ✅
- [x] XXXtreme mode allows 2 sentences instead of 1
- [x] Removed strict word limit for XXXtreme

### 10.4 - Client-Side Intensity ✅
- [x] Intensity affects server-side prompt building only
- [x] Clients just render bubbles, no intensity knowledge needed

---

## Phase 11: Victory & Death Interactions ✅ COMPLETE

### 11.1 - Player Victory Quotes ✅ COMPLETE
- [x] New trigger `player_killed_mob` fires when a player lands the killing blow on a mob
- [x] New patch `MobDeathPatch.cs` postfixes `Character.ApplyDamage`: detects mob HP <= 0 with player attacker
- [x] Per-mob cooldown bypassed for the death trigger (mob's only chance to speak before despawn)
- [x] Prompt context: "You are DYING... last defiant words... stay in character"
- [x] Personality still applies — Troll, Draugr etc keep their voice while dying

### 11.2 - Mob Call & Response ✅ COMPLETE
- [x] Client-side scan in `NearbyMobScanner` finds nearby same-type buddy within 20m
- [x] Patches (`MonsterAITargetingPatch`, `CharacterDamagePatch`) include candidate ZDOID/name in RPC packet
- [x] Server `RoastRpc.OnRequest` schedules a delayed (~1.5s) follow-up from candidate
- [x] `PromptBuilder.BuildCallResponsePrompt` references the original insult so the response builds on it
- [x] Group-cooldown of 30s per mob type (prevents spam chains)
- [x] Skipped on `player_died` / `player_killed_mob` to avoid post-mortem chatter

---

## Phase 12: Mob Social Features ✅ COMPLETE

### 12.1 - Mob Rivalries ✅ COMPLETE
- [x] Rivalry pairs defined in `ScanHelpers.Rivals`: Greydwarf↔Skeleton, Draugr↔Wraith/Ghost, Goblin↔Wolf/Fenring
- [x] Client scans for rival species within 30m and includes in RPC packet (rivalMobId/Name/Type)
- [x] Server `ScheduleRivalry` delays ~2s, calls `PromptBuilder.BuildRivalryPrompt` to mock the rival species
- [x] Player gets caught in crossfire — prompt instructs mob to attack rival species, not player
- [x] Symmetric rivalry-pair group cooldown of 45s (`RivalryKey` normalizes side order)

### 12.2 - Mob Hype Man ✅ COMPLETE
- [x] New patch `MobLowHealthPatch.cs` postfixes `Character.ApplyDamage`, fires when mob HP/MaxHP < 30%
- [x] Scans for same-type buddy within 25m; the buddy is the broadcaster (the hyper)
- [x] Hurt mob's name passed via candidate slot so prompt can name the friend being defended
- [x] `PromptBuilder.BuildHypeManPrompt` instructs hyper to defend friend and threaten the player
- [x] Group cooldown of 20s per mob type prevents hype spam

### 12.3 - Mob Betting ✅ COMPLETE
- [x] `BettingManager.cs` tracks open bets keyed by fighter mob ZDOID with 60s stale-prune
- [x] Bets opened on `spotted_player` when same-type buddy nearby, 35% chance, with 60s group cooldown
- [x] Bettor randomly assigned PLAYER-WIN or MOB-WIN side via `BuildBetPrompt`
- [x] Bet resolves on `player_died` (mob won) or `player_killed_mob` (player won)
- [x] Resolution fires gloat / whine line via `BuildBetOutcomePrompt`
- [x] Bettor mob is the same nearby same-type buddy used for call-response (no extra packet bloat)

---

## Phase 13: Polish & Testing ⬜

### 13.1 - Cleanup ⬜
- [ ] Remove `_callCount <= 3` diagnostic logging once trigger flow is verified
- [ ] Remove debug `Logger.LogInfo` statements that aren't helpful

### 13.2 - Performance ⬜
- [ ] Verify LLM semaphore prevents thundering herd on LM Studio
- [ ] Monitor memory: CooldownManager prune stale entries
- [ ] Ensure kill count dictionary doesn't grow unbounded

### 13.3 - Full In-Game Testing ⬜
- [ ] All triggers fire correctly
- [ ] All 12+ mob personalities respond appropriately
- [ ] Player death roasts fire
- [ ] Victory quotes fire when player kills mob
- [ ] Call & response chains work with nearby mobs
- [ ] Mob rivalries trigger between enemy types
- [ ] Hype man triggers when mob is low health
- [ ] Betting system works and delivers outcomes
- [ ] Parry fail roasts fire (if implemented)
- [ ] Whiff roasts fire (if implemented)
- [ ] Admin commands work
- [ ] Intensity slider changes behavior
- [ ] Kill counts accumulate and appear in roasts

---

## Phase 14: Distribution ⬜

- [ ] Tag 2.0 release
- [ ] Update README with new features
- [ ] Package: `RagebateMobs.dll` + `Newtonsoft.Json.dll` + config sample + README
- [ ] Update deploy paths in docs

---

## File Layout (Updated)

```
RagebateMobs/
├── RagebateMobs.csproj
├── src/
│   ├── Plugin.cs
│   ├── Configuration/
│   │   └── ModConfig.cs
│   ├── Managers/
│   │   ├── CooldownManager.cs       # mob + group cooldowns
│   │   ├── TaskManager.cs
│   │   ├── KillCountManager.cs      # player death tracking
│   │   └── BettingManager.cs        # [Phase 12.3] open/resolve bets
│   ├── Network/
│   │   ├── RoastRpc.cs              # extended packet (candidate + rival)
│   │   └── MainThreadDispatcher.cs
│   ├── Patches/
│   │   ├── MonsterAITargetingPatch.cs # spotted_player + buddy/rival scan
│   │   ├── CharacterDamagePatch.cs    # took_damage + buddy/rival scan
│   │   ├── PlayerDeathPatch.cs        # player_died trigger
│   │   ├── MobDeathPatch.cs           # [Phase 11.1] player_killed_mob
│   │   ├── MobLowHealthPatch.cs       # [Phase 12.2] hype_man
│   │   └── GameStartPatch.cs
│   └── Services/
│       ├── LLMService.cs
│       ├── PromptBuilder.cs           # added call_response/rivalry/hype/bet builders
│       ├── NearbyMobScanner.cs        # [Phase 11.2] same-type and rival-type scans
│       └── ScanHelpers.cs             # rivalry pair definitions + radii
└── TEST_SERVER/
```

---

## Status

| Phase | Status |
|---|---|
| 1 — Setup | ✅ |
| 2 — LLM | ✅ |
| 3 — Config | ✅ |
| 4 — Core Triggers | ✅ |
| 5 — Mob Personalities | ✅ |
| 6 — Expanded Triggers | ⏳ partial (6.1 ✅ 6.4 ✅, 6.2/6.3 ❌) |
| 7 — Kill Count Shame | ✅ |
| 8 — Admin Commands | ❌ skipped |
| 9 — Intensity Slider | ✅ |
| 11 — Victory & Death Interactions | ✅ |
| 12 — Mob Social Features | ✅ |
| 13 — Polish | ⬜ pending |
| 14 — Distribution | ⬜ pending |

---

## Implementation Order (Recommended)

## Implementation Order

1. ✅ **Victory & Death Interactions** (Phase 11) — implement victory quotes and call-response
2. ✅ **Mob Social Features** (Phase 12) — implement rivalries, hype man, and betting
3. **Polish & Testing** (Phase 13) — clean up, test thoroughly
4. **Distribution** (Phase 14) — tag release, update docs
