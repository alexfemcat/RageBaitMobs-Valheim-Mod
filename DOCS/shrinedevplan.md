# Shrine of Voices — Development Plan

**Project:** Interactive shrine system where players can talk to ghostly spirits using LLM

---

## Concept

Buildable shrine structures. Each shrine houses a trapped spirit. Press E to summon the spirit. Type normal chat to talk to it. Spirit responds via speech bubble above the shrine. Walk away when done.

**Player-placed via hammer, not world-spawned.** Players choose where to build their shrines. Can place multiple of the same type. Shares naturally with friends when they visit your base.

---

## Visual Design

**Base Shrine** (shared model): Weathered Norse runestone pillar (~1.5m tall). Can reskin existing Valheim runestone asset.

**Per-Spirit Variation**: Colored point light at base + matching particle color rising from stone.

| Spirit | Light Color | Particle Color | Personality |
|--------|-------------|----------------|-------------|
| Hralskuld the Defiant | Orange | Orange/Red embers | Gruff warrior, combat taunts, judgmental |
| Mira the Wanderer | Teal | Teal wisps | Gossip, rumors, world knowledge |
| Ulfgar the Oracle | Purple | Purple mist | Cryptic prophecies, fortune telling |
| Briar of the Burial Mound | Green | Green smoke | Witchy, mischievous, herbalism advice |

**Spirit Appearance**: Simple name text above shrine in spirit's color. No 3D model needed. Text: **"[Spirit Name]"** in bold.

**Active State**: When player is near and spirit is invoked:
- Name text appears above shrine
- Soft glow from base
- Particles slowly rise
- Fades out when player leaves (8m radius)

---

## Interaction Flow

1. Player approaches shrine, presses **E**
2. Spirit name text appears above shrine: **[Ulfgar the Oracle]**
3. Spirit may greet unprompted: "Another fool seeks my wisdom. Speak."
4. Player types in local chat normally
5. Message is intercepted by mod (only when shrine is "active" and player is near)
6. LLM receives: spirit personality + conversation history + player message
7. Spirit responds via speech bubble above shrine
8. Spirit may ask follow-up questions to keep conversation flowing
9. Player can continue chatting or walk away
10. After player is 8m+ away for 5s, spirit deactivates but remembers the player
11. When player returns, spirit recalls previous conversation: "Back again. Couldn't stay away."

**No commands needed**. Just chat. Spirit handles the conversation.

---

## Technical Implementation

### Placement
- [ ] Buildable via hammer — new tab in building menu called "Shrines"
- [ ] 4 shrine variants in tab, one per spirit type
- [ ] Placed like any other Valheim structure — snaps to terrain, has footprint
- [ ] Player decides placement, quantity, and location
- [ ] Shares with friends when they visit your base

### State Management
- [ ] `ShrineState`: { SpiritType, IsActive, ActivatingPlayer, DeactivateTimer }
- [ ] Player within 8m + pressed E = active
- [ ] Player leaves 8m radius + 5s timer = deactivate
- [ ] Only one player can interact per shrine at a time

### Chat Routing
- [ ] Hook `PlayerChat` or equivalent
- [ ] If player is near active shrine, intercept message
- [ ] Send to LLM with shrine/spirit context
- [ ] Return response as bubble above shrine (not as chat message)

### LLM Prompt Structure
```
[System]
You are {SpiritName}, {SpiritPersonality}.
You are bound to an ancient runestone. A mortal approaches to speak with you.
Keep responses SHORT (1-2 sentences). Be in character.
Ask follow-up questions. Don't just answer — ENGAGE.
If asked about the future, give cryptic non-sequitur prophecies.
If asked about lore, make up believable but absurd details.
Sometimes say things unprompted. Keep conversation flowing.
Reference earlier parts of the conversation.
Never break character. Never mention being an AI.

[Conversation History - last 20 messages]
{history}

[Player Message]
{player_message}
```

### Conversation System
- [ ] **Conversation History**: Keep last 20 messages per shrine-session (player + spirit)
- [ ] **Spirit Asks Questions**: Spirit doesn't just answer — it asks things too. "Oh really? Which mob hit you?" "And did you learn nothing from that?"
- [ ] **Session Memory**: While shrine is active for a player, conversation continues naturally. Spirit tracks what you discussed.
- [ ] **Player Recall**: Spirit remembers player across sessions (stored in JSON). Returns after 10 min? Spirit notices: "Thought you'd never come back."
- [ ] **Proactive Interjection**: Sometimes spirit speaks unprompted after player message. "You seem troubled. Anyway, as I was saying..."
- [ ] **JSON Persistence**: `shrines_memory.json` in config folder. Schema: `{ playerId -> shrineInstanceId -> { lastVisit, conversationNotes, flags } }`
- [ ] **Memory Pruning**: Limit stored memories per player-shrine to 10KB. Prune old entries if exceeded.

---

## Spirit Personalities

### Hralskuld the Defiant
- [ ] Implement personality and prompts
- "Gruff old warrior who died in his prime"
- Comments on player combat: "Your footwork is atrocious."
- Gives advice like a drill sergeant
- Sarcastic, never impressed
- Favorite phrase: "In my day, we fought with HONOR."

### Mira the Wanderer
- [ ] Implement personality and prompts
- "Traveling merchant ghost who saw everything"
- Knows gossip about all mob types
- Shares rumors: "The Surtlings have been acting strange near the coast..."
- Friendly, chatty, loves to gossip
- Asks about player's adventures

### Ulfgar the Oracle
- [ ] Implement personality and prompts
- "Hollow-eyed seer who speaks in riddles"
- Prophecies are absurd non-sequiturs that somehow always apply
- "I see... a death. Many deaths. Ah. That's just Tuesday."
- Cryptic, ominous, slightly creepy
- Never gives straight answers

### Briar of the Burial Mound
- [ ] Implement personality and prompts
- "Witch-like spirit of the burial mounds"
- Mischievous, knows herb-lore
- Makes potions sound like gossip: "Oh honey, that Troll? Known him for centuries. Total waste of space."
- Motherly but unsettling
- Gives "curses" as jokes

---

## Config Options

- [ ] `[Shrines] Enabled` — toggle shrine system on/off
- [ ] `[Shrines] ChatRadius` — how close to shrine to activate (default 8m)
- [ ] `[Shrines] DeactivateDelay` — seconds before spirit fades after player leaves (default 5s)
- [ ] `[Shrines] ResponseDelay` — seconds between player message and spirit response (default 1s, prevents spam)

---

## File Layout Additions

```
src/
├── Buildings/
│   └── ShrineBuilder.cs        # hammer building integration, 4 shrine pieces
├── Spirits/
│   ├── SpiritBase.cs           # base class for spirits
│   ├── HralskuldSpirit.cs      # warrior spirit
│   ├── MiraSpirit.cs           # wanderer spirit
│   ├── UlfgarSpirit.cs         # oracle spirit
│   └── BriarSpirit.cs          # witch spirit
├── Services/
│   └── ShrineLLMService.cs     # LLM integration for shrines
└── Patches/
    └── ShrineChatPatch.cs      # intercepts chat near active shrine
```

---

## Implementation Order

### Phase 1: Foundation
- [ ] Hammer buildable — add 4 shrine pieces to hammer building tab
- [ ] Shrine prefab — runestone model with spirit-colored particles/glow
- [ ] E to interact — detect keypress, activate shrine for that player

### Phase 2: Chat System
- [ ] Basic chat routing — intercept chat when shrine active and player nearby
- [ ] LLM integration — send/receive for one spirit type
- [ ] Bubble response — display LLM response above shrine

### Phase 3: Conversation System
- [ ] Conversation history — store last 20 messages per shrine-session
- [ ] JSON persistence — save/load player memories across sessions
- [ ] Spirit asks questions — update prompts to encourage follow-up questions
- [ ] Proactive interjection — spirit occasionally speaks unprompted
- [ ] Memory pruning — prevent unbounded JSON growth

### Phase 4: Spirits
- [ ] Spirit base class
- [ ] Hralskuld the Defiant personality
- [ ] Mira the Wanderer personality
- [ ] Ulfgar the Oracle personality
- [ ] Briar of the Burial Mound personality

### Phase 5: Polish
- [ ] State management — deactivation, proximity, one-player-at-a-time per shrine
- [ ] Visual polish — particle colors, glow effects per spirit
- [ ] Config options
- [ ] Testing with multiple players

---

## Status

| Phase | Status |
|---|---|
| 1 — Foundation | ⬜ pending |
| 2 — Chat System | ⬜ pending |
| 3 — Conversation System | ⬜ pending |
| 4 — Spirits | ⬜ pending |
| 5 — Polish | ⬜ pending |
