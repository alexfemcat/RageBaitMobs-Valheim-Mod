# Viking Ragebait: The Toxic Mob Mod - Development Plan

**Project:** Hybrid (client-detect / server-LLM) BepInEx mod for Valheim using a local LLM (LM Studio)
**Target Framework:** .NET Framework 4.7.2 (Valheim/BepInEx compatible)
**LLM Integration:** LM Studio API (http://localhost:1234/v1/chat/completions)

---

## Architecture: Hybrid Client/Server (revised)

The original "server-only" plan didn't survive contact with Valheim's networking model. In Valheim, both `MonsterAI.DoAttack` and `Character.ApplyDamage` execute on the **owner** of the entity:
- A spawned mob's owner is the client that spawned it (or owns its zone).
- A player character is owned by the player's own client.

So a dedicated server never sees these methods called for entities owned by connected clients. A pure server-only patch can't reliably trigger on "mob attacks player".

### How the hybrid works

```
[Modded client]                        [Server (your host)]            [Vanilla / modded clients]
DoAttack / ApplyDamage postfix
        │
        │  RagebateMobs_RequestRoast (custom routed RPC)
        ▼                              receives RPC
                                       cooldown / dedup
                                       calls LM Studio (localhost)
                                                │
                                                ▼
                                       ChatMessage RPC (vanilla) ───────▶ everyone sees yellow
                                       broadcast to Everybody             speech bubble at mob
```

- **Modded client** detects events locally, sends a tiny RPC to the server.
- **Server** is the only thing that talks to LM Studio. LM Studio stays on the host machine.
- **All clients (vanilla too)** render the speech bubble because the broadcast uses Valheim's vanilla `ChatMessage` routed RPC.

### Soft join dependency

- The mod registers a custom routed RPC name (`RagebateMobs_RequestRoast`). Custom routed RPC registration is **not** part of Valheim's network handshake — vanilla clients can still join, including crossplay.
- Vanilla clients simply don't send the request RPC, so their hits don't trigger roasts. They can still see other players' roasts (vanilla `ChatMessage` handler).
- Modded clients send the request; the server roasts them.
- Crossplay (`-crossplay`) is unaffected.

---

## Phase 1: Project Setup & Infrastructure ✅ COMPLETE
- [x] `RagebateMobs.csproj` (net472, Mono-compatible)
- [x] Local BepInEx assembly references (`BepInEx`, `0Harmony`, `assembly_valheim`, `assembly_utils`, `Splatform`, etc.)
- [x] Build output: `bin/Release/net472/`

## Phase 2: LLM Integration ✅ COMPLETE
- [x] `src/Services/LLMService.cs` — async HttpClient call to LM Studio chat/completions
- [x] `src/Services/PromptBuilder.cs` — context-aware prompt for `spotted_player` / `took_damage`

## Phase 3: Configuration ✅ COMPLETE
- [x] `src/Configuration/ModConfig.cs`
- [x] `[General]` Enabled
- [x] `[API]` LLMModel, LMStudioApiUrl (default `http://localhost:1234/v1`)
- [x] `[Cooldowns]` PerMobCooldownSeconds (default 5), MaxSimultaneousInsults (default 5)
- [x] `[Triggers]` MinDamageThreshold (default 5)
- [x] `[Debug]` DebugLogging

## Phase 4: Patches (client-side trigger) ✅ COMPLETE
- [x] `src/Patches/MonsterAITargetingPatch.cs` — postfix on `MonsterAI.DoAttack`. Fires on the mob's owning peer (typically a client). Sends `RagebateMobs_RequestRoast` RPC.
- [x] `src/Patches/CharacterDamagePatch.cs` — postfix on `Character.ApplyDamage`. Fires on the damaged character's owner (typically the player's own client). Sends `RagebateMobs_RequestRoast` RPC.
- [x] `src/Patches/GameStartPatch.cs` — postfix on `ZRoutedRpc.Awake`. Registers the custom RPC handler on whichever side initialized.

## Phase 5: Networking (custom RPC + vanilla broadcast) ✅ COMPLETE
- [x] `src/Network/RoastRpc.cs`
  - `SendRequest(ZDOID, mobName, playerName, triggerType)` — client sends to server (auto-targets via 2-arg `InvokeRoutedRPC`).
  - `OnRequest(long sender, ZPackage)` — server-side handler: cooldown gate → LLM call → main-thread broadcast.
  - `Broadcast(ZDOID, mobName, insult)` — server invokes vanilla `ChatMessage` routed RPC on `Everybody`, with `Talker.Type.Shout` and a `UserInfo` whose Name is the mob name.
- [x] `src/Network/MainThreadDispatcher.cs` — small `MonoBehaviour` queue so HTTP-completion thread can post Unity-API calls back to the main thread.

## Phase 6: Cooldown & Async ✅ COMPLETE
- [x] `src/Managers/CooldownManager.cs` — keyed on `ZDOID` (server-side; client/server use the same key).
- [x] `src/Managers/TaskManager.cs` — fire-and-forget async wrapper with exception logging.

## Phase 7: Testing
- [x] **Pipeline test (no game):** `./test_pipeline.sh` confirms LM Studio reachable + model loaded + prompt → insult round-trip works.
- [ ] **In-game test (hybrid):**
  - [ ] Mod deployed on TEST_SERVER → server logs `Loaded ... API: http://localhost:1234/v1`.
  - [ ] Mod deployed on the testing client (`Steam Valheim/BepInEx/plugins/RagebateMobs/`).
  - [ ] Client joins TEST_SERVER (`127.0.0.1:2457`, password `696969`).
  - [ ] On hit → server log shows `Mob -> Player (took_damage) requested by peer N, generating roast` followed by `Mob: <insult>`.
  - [ ] Speech bubble appears in-game above the mob (yellow Shout).
  - [ ] Vanilla client (no mod) can still join, can still play. Confirms soft-dep.
  - [ ] Cooldown: same mob doesn't roast more than once per `PerMobCooldownSeconds`.
- [ ] LM Studio offline → server logs `Failed to connect`/timeout, doesn't crash.

## Phase 8: Polish
- [ ] Cleanup `_callCount <= 3` first-fire diagnostic logging once trigger flow is verified.
- [x] Add a server-side max-in-flight LLM semaphore (so 10 simultaneous hits don't fan out 10 concurrent LM Studio calls).
- [x] Fix CooldownManager memory leak — prune stale _lastTalk entries after 200 entries or 5 minutes idle.
- [ ] Cache localized mob names client-side.
- [ ] Document hybrid architecture and required client install in README.

## Phase 9: Distribution
- [ ] Tag a 1.0 release.
- [ ] Package: `RagebateMobs.dll` + sample config + README.
- [ ] Server install: drop into `BepInEx/plugins/RagebateMobs/` on the dedicated server. LM Studio on the same host (or reachable host).
- [ ] Client install (optional, but required to *trigger* roasts): drop the same DLL into the client's `BepInEx/plugins/RagebateMobs/`. No client config needed — client only sends RPCs.
- [ ] Soft join: vanilla clients work without the mod.

---

## File Layout

```
RagebateMobs/
├── RagebateMobs.csproj
├── src/
│   ├── Plugin.cs                 # BepInEx entry, Harmony PatchAll, dispatcher init
│   ├── Configuration/
│   │   └── ModConfig.cs
│   ├── Managers/
│   │   ├── CooldownManager.cs    # ZDOID-keyed
│   │   └── TaskManager.cs        # SafeFireAndForgetAsync
│   ├── Network/
│   │   ├── RoastRpc.cs           # custom RPC + vanilla broadcast
│   │   └── MainThreadDispatcher.cs
│   ├── Patches/
│   │   ├── MonsterAITargetingPatch.cs   # MonsterAI.DoAttack
│   │   ├── CharacterDamagePatch.cs       # Character.ApplyDamage
│   │   └── GameStartPatch.cs             # ZRoutedRpc.Awake (RPC registration hook)
│   └── Services/
│       ├── LLMService.cs
│       └── PromptBuilder.cs
└── TEST_SERVER/                  # self-contained dedicated server for dev testing
```

---

## Dependencies

| Dependency | Version | Purpose |
|---|---|---|
| BepInEx | 5.x (Unix) | Plugin framework |
| Harmony (0Harmony) | 2.x | Method patching |
| Valheim assemblies | matches server | `assembly_valheim`, `assembly_utils`, `Splatform`, etc. |
| Mono | 6.x+ | Runtime (Linux) |
| .NET SDK | 6.0+ | Build (`dotnet build -c Release`) |
| LM Studio | latest | Local LLM (`localhost:1234`) |

Recommended models: Gemma-3 1B, Ministral-3 3B, or Llama 3.1 8B (abliterated for unfiltered roasts).

---

## Linux Setup (Development)

- Mono: `sudo apt install mono-complete`
- .NET SDK: `sudo apt install dotnet-sdk-6.0`
- Build: `dotnet build RagebateMobs.csproj -c Release`
- Output DLL: `bin/Release/net472/net472/RagebateMobs.dll`
- Deploy server: `BepInEx/plugins/RagebateMobs/RagebateMobs.dll` on the dedicated server
- Deploy client: same DLL into the client's BepInEx plugins folder (only required for clients who want to *trigger* roasts; vanilla clients can still join)

---

## Notes on Soft Dependency / Crossplay

- Custom routed RPCs do not affect Valheim's version handshake.
- Vanilla `ChatMessage` RPC is what the server uses to deliver the speech bubble — every client (PC, console, crossplay) handles it natively.
- Server never rejects unmodded clients because the mod doesn't touch `ZNet.RPC_PeerInfo`, version strings, or world hash.
- Conclusion: the mod is fully optional for clients, and crossplay continues to work.

---

## Status

| Phase | Status |
|---|---|
| 1 — Setup | ✅ |
| 2 — LLM | ✅ |
| 3 — Config | ✅ |
| 4 — Patches | ✅ |
| 5 — Networking (hybrid RPC) | ✅ |
| 6 — Cooldown & Async | ✅ |
| 7 — Testing | ⏳ in progress |
| 8 — Polish | ❌ |
| 9 — Distribution | ❌ |
