# Viking Ragebait

A Valheim BepInEx mod that makes mobs trash-talk players using a local LLM (LM Studio). When a Greydwarf rolls up on you and you whiff a swing, the Greydwarf personally tells you to delete the game. In real-time. Generated fresh by an AI running on the server host's machine.

```
[in-world bubble above the mob's head]

  Skeleton
  what the fuck was that, did you cum on the keyboard and miss?
```

## Features

- **AI-generated mob speech** via a local LM Studio server — no API keys, no remote calls, no telemetry. The model runs on the server host's machine.
- **Hybrid client/server architecture** — modded clients detect events, the server makes the LLM call, all clients render the bubble. Only the server host needs LM Studio running.
- **Soft join dependency** — vanilla (unmodded) clients can still join your server. They can play normally; they just won't see the bubbles or trigger roasts. Crossplay is unaffected.
- **In-world speech bubbles, not chat spam** — uses `Chat.SetNpcText` (the same API Ravens and Traders use). Bubbles appear above the mob, are cull-distance limited (30m default), have a 5-second TTL, and never enter the chat log.
- **Per-mob cooldown** — same skeleton can't spam roasts. Configurable.
- **Trigger types** — fires when a mob first targets a player (`spotted_player`) or when a player takes damage above a threshold (`took_damage`).
- **Fire-and-forget LLM calls** — never blocks the server tick; if LM Studio is offline or slow, the request just times out silently.

## How it works

```
┌────────────────────────┐
│ Modded client          │
│  - hits skeleton       │
│  - patch detects event │
└──────────┬─────────────┘
           │ RagebateMobs_RequestRoast (custom routed RPC)
           │ payload: ZDOID, mobName, playerName, triggerType
           ▼
┌────────────────────────┐
│ Server                 │
│  - per-mob cooldown    │
│  - calls LM Studio at  │
│    localhost:1234/v1   │
└──────────┬─────────────┘
           │ RagebateMobs_RoastBroadcast (custom routed RPC, target Everybody)
           │ payload: ZDOID, mobName, insult
           ▼
┌────────────────────────┐
│ All modded clients     │
│  - find mob via ZDOID  │
│  - Chat.SetNpcText()   │
│    bubble above mob    │
└────────────────────────┘
```

Why hybrid and not server-only? Because Valheim's networking model executes `MonsterAI.DoAttack` and `Character.ApplyDamage` on the *owner* of the entity (typically a connected client, not the dedicated server). A pure server-only patch never sees these methods called for client-owned mobs and players. The hybrid approach detects events where they actually fire (clients) and centralizes the LLM call where the host wants it (server).

## Requirements

- **Valheim** `l-0.221.12` or compatible (network version 36)
- **BepInEx** 5.4.x (Unix or Windows)
- **LM Studio** running on the server host's machine, with a model loaded
  - Recommended: any uncensored / abliterated 7-8B instruct model (e.g. `meta-llama-3.1-8b-instruct-abliterated`). Smaller models work too — Gemma-3 1B or Ministral-3 3B are fast and acceptable.
  - The vanilla, RLHF-tuned models will refuse to roast and you'll get bland output or empty responses.

## Install

### Server (required)

1. Install BepInEx on your dedicated server (Unix build for Linux, normal build for Windows).
2. Drop the mod files into `BepInEx/plugins/RagebateMobs/`:
   - `RagebateMobs.dll`
   - `Newtonsoft.Json.dll`
3. Start LM Studio on the same machine, load a model, start the local server (defaults to `http://localhost:1234`).
4. Start the dedicated server. On first launch BepInEx generates `BepInEx/config/com.valheim.ragebatemobs.cfg` — edit if you want non-default behavior, then restart.

### Client (optional but recommended)

The mod is designed so vanilla clients can join. **However, only modded clients trigger roasts and only modded clients render the bubble.** If you want roasts to actually fire when *you* take a hit, install on your client too:

1. Install BepInEx on the client.
2. Drop the same `RagebateMobs.dll` and `Newtonsoft.Json.dll` into the client's `BepInEx/plugins/RagebateMobs/`.

The client doesn't need LM Studio, doesn't need configuration, and doesn't talk to the LLM directly. It just sends a small RPC to the server when an event fires.

### LM Studio

1. Install [LM Studio](https://lmstudio.ai) on the host machine.
2. Download a model. The mod default expects the model name `meta-llama-3.1-8b-instruct-abliterated`; change `LLMModel` in the config if you use a different one.
3. Start the local server: `lms server start` or via the LM Studio GUI ("Start Server"). Defaults to `http://localhost:1234`.
4. Verify it's reachable from the Valheim server host:
   ```
   curl http://localhost:1234/v1/models
   ```

## Configuration

Config file: `BepInEx/config/com.valheim.ragebatemobs.cfg`

```ini
[General]
Enabled = true            # master kill switch
OutputMode = Shout        # currently only Shout (in-world bubble) is wired

[API]
LLMModel = meta-llama-3.1-8b-instruct-abliterated
LMStudioApiUrl = http://localhost:1234/v1   # MUST include /v1

[Cooldowns]
PerMobCooldownSeconds = 10   # same mob waits N seconds before roasting again
MaxSimultaneousInsults = 5   # reserved (per-frame cap)

[Triggers]
MinDamageThreshold = 5       # took_damage trigger only fires for hits >= this much

[Debug]
DebugLogging = false
```

## Soft dependency / Crossplay

The mod registers two custom routed RPCs (`RagebateMobs_RequestRoast`, `RagebateMobs_RoastBroadcast`). Custom routed RPC names are *not* part of Valheim's version handshake — vanilla clients can still join, including over crossplay (Steam ↔ PlayFab), and the server doesn't reject them. They simply don't have the RPC handlers, so they don't send requests and don't render bubbles.

If you want every player on a public server to get the experience, share the mod with them. If you don't care, just install on the host and your own client. Both setups are supported.

## Building from source

Requires .NET SDK 6.0+ (for the `dotnet` CLI). Built target framework is `net472`.

```
dotnet build RagebateMobs.csproj -c Release
```

Output ends up at `bin/Release/net472/net472/RagebateMobs.dll`. The csproj has `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` so `Newtonsoft.Json.dll` is dropped alongside it.

The Valheim assemblies (`assembly_valheim.dll`, `assembly_utils.dll`, `Splatform.dll`, `0Harmony.dll`, `BepInEx.dll`, `UnityEngine.*`) are referenced from `lib/BepInEx/core/` — these are not redistributed in the repo. Copy them from your own Valheim/BepInEx installation before building. See `SETUP.md`.

### Project layout

```
src/
├── Plugin.cs                          # BepInEx entry point, wiring
├── Configuration/
│   └── ModConfig.cs                   # config bindings
├── Managers/
│   ├── CooldownManager.cs             # ZDOID-keyed per-mob cooldown
│   └── TaskManager.cs                 # SafeFireAndForgetAsync wrapper
├── Network/
│   ├── RoastRpc.cs                    # custom routed RPCs (request + broadcast)
│   └── MainThreadDispatcher.cs        # marshals HTTP-completion thread → Unity main thread
├── Patches/
│   ├── MonsterAITargetingPatch.cs     # postfix on MonsterAI.DoAttack
│   ├── CharacterDamagePatch.cs        # postfix on Character.ApplyDamage
│   └── GameStartPatch.cs              # ZNet.Awake postfix → registers routed RPCs
└── Services/
    ├── LLMService.cs                  # async HttpClient → LM Studio /v1/chat/completions
    └── PromptBuilder.cs               # constraint + few-shot prompt
```

## Testing without a game

`test_pipeline.sh` runs the full prompt pipeline (LM Studio reachable → model loaded → prompt built → insult generated → output sanity-checked) without launching the game:

```
./test_pipeline.sh
```

Reads the trigger threshold and API URL from `TEST_SERVER/BepInEx/config/com.valheim.ragebatemobs.cfg`. If this fails, the in-game test will fail too — fix it first.

## Troubleshooting

**Mod loads but nothing happens when I get hit.**
- Confirm both the server *and* your client have the mod installed. Trigger detection is client-side.
- Watch `BepInEx/LogOutput.log` on both ends. On the client you should see `[Ragebait] ApplyDamage postfix fired (call #1)` etc. — these fire unconditionally for the first three calls. If you don't see them, your client isn't loading the mod.

**Server says `Could not load file or assembly 'Newtonsoft.Json'`.**
- Drop `Newtonsoft.Json.dll` into `BepInEx/plugins/RagebateMobs/` next to the mod DLL.

**Server says `Empty insult from LLM`.**
- LM Studio returned an empty/blank completion. Usually means the model refused (vanilla RLHF model) or hit a stop token immediately. Try an abliterated/uncensored model.

**Server says `Failed to connect to LM Studio`.**
- LM Studio isn't running, or the API URL in config is wrong, or it's missing the `/v1` suffix. Default should be `http://localhost:1234/v1`.

**Roasts are too long / wall of text.**
- Reduce `max_tokens` in `src/Services/LLMService.cs` and/or tighten the prompt in `src/Services/PromptBuilder.cs`. Default ships at `max_tokens = 40` with a hard "ONE sentence, max 15 words" rule.

**`HarmonyX` warning about a method not being found.**
- A Valheim update may have renamed or moved a method. Check the patch attributes in `src/Patches/`. The patches are isolated, so one failed patch won't take down the others.

## License

This mod is provided as-is. Use at your own risk. The roasts are intentionally offensive — that's the point. Don't deploy on a public server unless your players are the kind of people who enjoy being told their aim is microscopic.

## Credits

- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX).
- LLM inference via [LM Studio](https://lmstudio.ai).
- JSON via [Newtonsoft.Json](https://www.newtonsoft.com/json).
- Valheim by Iron Gate Studio.
