# Kingdom of Kronos - Codebase Context Prompt

Use this prompt at the start of a new chat session along with `AllScripts.txt` to provide context:

---

**I'm working on "Kingdom of Kronos", a TorqueScript-based RPG game. The main codebase is in `AllScripts.txt`, which is a concatenated file containing 144 individual script files.**

## Recent Major Fixes (2025)

### 1. Bot Spawning System Overhaul
- **Fixed race conditions** in bot spawning by switching from display names to internal names
- **Implemented spawn transaction system** with `ReserveSpawnSlot()` and `RollbackSpawnSlot()` to prevent counter leaks
- **Fixed guardtype extraction** in `createAI()` - replaced buggy `clipTrailingNumbers()` with reliable trailing digit removal
- **Enhanced bot identification** in `SpawnAIGetClientId()` to correctly identify Colloseum/Seal Battle bots

### 2. Colloseum & Seal Battle Improvements
- **Refactored to use internal names** (`AI::helper()` return values) instead of display names
- **Added spawn cooldowns** to prevent premature death checks
- **Implemented smart bot behavior flags** (`botAttackMode = 2`, `AImaxRangeOverride = 120`)
- **Created dedicated Colloseum bot types** (33 new bots: `NewbieRoundOne`, `AdventurerRoundTwo`, etc.)
- **Fixed ATK scaling order** in Seal Battle to ensure weapon damage is scaled before ATK calculation

### 3. Key Technical Concepts

**Internal Name vs. Display Name:**
- **Internal Name**: Unique identifier returned by `AI::helper()` (e.g., "RoundTwo497"), available immediately
- **Display Name**: Human-readable name (e.g., "SealFighter2"), subject to replication lag (1-3 seconds)

**Name Replication Lag:**
- Torque engine delay between bot creation and display name availability via `Client::getName()`
- Solutions: Use internal names for immediate lookups, implement cooldowns, trust nameless fresh bots

**Spawn Transaction System:**
- `ReserveSpawnSlot(%spawnPointId)` - Atomically reserves a spawn slot
- `CommitSpawnSlot(%spawnPointId)` - Commits the reservation after successful spawn
- `RollbackSpawnSlot(%spawnPointId)` - Rolls back on failure to prevent counter leaks

**Bot Lookup Priority:**
1. `AI::getId(%internalName)` - Most reliable for newly spawned bots
2. `AI::getClientIdFromName(%internalName)` - Custom lookup using internal name
3. `NEWgetClientByName(%displayName)` - Legacy display name lookup (fallback)

## File Structure

**Key Files in AllScripts.txt:**
- `Ai.cs` - Core AI functions, bot spawning, client ID lookup
- `remortseal.cs` - Seal Battle event system
- `rpgarena.cs` - Colloseum event system
- `EnemyArmors.cs` - Bot race definitions, equipment loadouts
- `classes.cs` - World ranks, Colloseum bot arrays
- `rpgfunk.cs` - Utility functions, reserved words validation
- `playerdamage.cs` - Damage calculation, bot loot dropping
- `Admin.cs` - Administrative functions, stance restrictions

## Working with AllScripts.txt

**To extract a specific file:**
- Search for `=== FILE START: filename.cs ===` and `=== FILE END: filename.cs ===`
- The content between these markers is the file content

**To update AllScripts.txt after editing individual files:**
```python
python -c "import re; content = open('AllScripts.txt', 'r', encoding='utf-8', errors='ignore').read(); file_content = open('filename.cs', 'r', encoding='utf-8', errors='ignore').read(); content = re.sub(r'=== FILE START: filename\.cs ===.*?=== FILE END: filename\.cs ===', '=== FILE START: filename.cs ===\n\n' + file_content + '\n\n=== FILE END: filename.cs ===', content, flags=re.DOTALL); open('AllScripts.txt', 'w', encoding='utf-8').write(content); print('AllScripts.txt updated')"
```

## Common Patterns

**Bot Name Format:**
- Internal: `{BotType}{Number}` (e.g., "Obliterator0", "RoundTwo497")
- Display: `{RacePrefix}{BotType}{Number}` (e.g., "AdminObliterator0", "SealFighter2")
- Colloseum bots: `{RankName}{RoundWord}{Number}` (e.g., "NewbieRoundOne0")

**Guardtype Extraction:**
- Use trailing digit removal (backwards iteration) instead of `clipTrailingNumbers()`
- Guardtype is the base bot name without the AI instance number

**Spawn Flow:**
1. `SpawnLoop()` → `ReserveSpawnSlot()` → `AI::helper()` → `SpawnAI()` → `createAI()`
2. `SpawnAIGetClientId()` (scheduled 3.0s after spawn) → Bot registration
3. `CommitSpawnSlot()` on success, `RollbackSpawnSlot()` on failure

## Important Notes

- **Always update AllScripts.txt** after editing individual script files
- **Use internal names** for bot lookups when possible (more reliable)
- **Implement rollbacks** in all failure paths to prevent spawn counter leaks
- **Test spawn cooldowns** are respected before checking bot death
- **Normalize empty strings** from `AI::getClientIdFromName()` to `-1` for consistency

---

**When I ask you to work on this codebase, please:**
1. Read the relevant sections from AllScripts.txt
2. Understand the context of recent fixes
3. Follow the established patterns (internal names, transaction system, etc.)
4. Update AllScripts.txt after making changes
5. Check for lint errors before finalizing


