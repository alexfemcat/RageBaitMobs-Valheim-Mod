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

## Phase 11: Victory & Death Interactions

### 11.1 - Player Victory Quotes ⬜
- Design: when player KILLS a mob, mob delivers "last words" as it dies
- New trigger: `player_killed_mob` (requires hooking mob death event)
- Prompt context: mob is dying, player just killed them, flavor is "defiant but doomed"
- Mob's personality still applies - a Troll might say "You got lucky, idiot", Draugr might say "This... changes nothing..."
- Different trigger name in bubble? Maybe subtle badge or just normal bubble
- Implementation: patch `Character.MarkAsDeath()` or `DoDamage` to detect mob death from player
- Cooldown: separate cooldown from other triggers, maybe 10s per mob type

### 11.2 - Mob Call & Response ⬜
- Design: when multiple mobs of SAME TYPE are near each other (within 20m), they do back-and-forth roasts
- Mob A triggers roast, mob B follows up 1-2 seconds later with a response
- Call: "[MobName]: roast", Response: "[Same MobName 2]: callback/reference to what MobA said"
- Example: Greydwarf 1: "He's terrible at this game." Greydwarf 2: "Yeah, worse than the last guy we killed."
- Requires tracking nearby same-type mobs when roast triggers
- Rate limit: only do call-response every 30s per mob group
- Only works when mob is actively engaged (targeting player or recently hit player)

---

## Phase 12: Mob Social Features

### 12.1 - Mob Rivalries ⬜
- Design: certain mob types have rivalries and insult each other when nearby
- Rivalry pairs: Greydwarf vs Skeleton, Draugr vs Wraith, Goblin vs Wolf
- When rival mobs are within 30m of each other AND one targets player, they trash-talk each other
- Flavor: caught in the crossfire of monster beef
- Implementation: track rival group pairs, check distance on roast trigger
- Rate limit: once per 45s per rivalry pair per zone
- Example: Skeleton: "At least we're not green and stupid." Greydwarf: "At least we have working arms."

### 12.2 - Mob Hype Man ⬜
- Design: when a mob is LOW HEALTH (below 30%), nearby same-type mobs defend it
- Hype: "[Hurt Mob] is losing! Someone help!" / "Leave [Mob] alone!"
- Mob that just helped gets a "protecting friend" personality boost
- Requires tracking mob health on each damage event
- Rate limit: every 20s per mob group
- Only triggers if player is actively damaging the hurt mob

### 12.3 - Mob Betting ⬜
- Design: when player is fighting a mob, nearby same-type mobs bet on the outcome
- Bet: "[Mob]: 20 bucks he dies in 30 seconds." / "[Mob]: I'm saying he pulls through."
- Outcome reveals after fight ends (player lives or dies)
- Winner mob delivers a gloating or disappointed line
- Track fight start time, nearby bettors, outcome after 30s
- Rate limit: once per 60s per mob group
- Config: enable/disable betting feature

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
│   │   ├── CooldownManager.cs
│   │   ├── TaskManager.cs
│   │   └── KillCountManager.cs     # [NEW] player death tracking
│   ├── Network/
│   │   ├── RoastRpc.cs
│   │   └── MainThreadDispatcher.cs
│   ├── Patches/
│   │   ├── MonsterAITargetingPatch.cs
│   │   ├── CharacterDamagePatch.cs
│   │   ├── PlayerDeathPatch.cs      # [NEW] player death trigger
│   │   └── GameStartPatch.cs
│   └── Services/
│       ├── LLMService.cs
│       └── PromptBuilder.cs
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
| 10 — Victory & Death Interactions | ⬜ pending (11.1, 11.2) |
| 11 — Mob Social Features | ⬜ pending (12.1, 12.2, 12.3) |
| 12 — Polish | ⬜ pending |
| 13 — Distribution | ⬜ pending |

---

## Implementation Order (Recommended)

## Implementation Order

1. **Victory & Death Interactions** (Phase 11) — implement victory quotes and call-response
2. **Mob Social Features** (Phase 12) — implement rivalries, hype man, and betting
3. **Polish & Testing** (Phase 13) — clean up, test thoroughly
4. **Distribution** (Phase 14) — tag release, update docs
