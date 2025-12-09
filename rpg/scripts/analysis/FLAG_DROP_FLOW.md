# Flag Drop Flow - House Artifact Theft

## Complete Flow When Player Steals and Drops House Artifact

### 1. **Flag is at House Base** (Initial State)
- Flag is positioned at house flagstand
- `%flag.holdingTeam = "HouseKronos"` (or HouseArbal, HouseCurama, HouseYuliple)
- `%flag.flagStand = [flagstand object]`
- `$FlagCommand[HouseKronos] = 1` (house has 1 artifact)

### 2. **Player Picks Up Flag** (`Flag::onCollision` - line 744)
- Player walks into flag at house base
- **Line 856-869**: 
  - Gets current `holdingTeam` (e.g., "HouseKronos")
  - Decrements house score: `$teamScore[%hteam] -= %flag.scoreValue`
  - **NEW**: Stores house name in `previousHoldingTeam` before clearing
  - Clears `holdingTeam`: `%flag.holdingTeam = -1`
  - Decrements house artifact count: `$FlagCommand[%flagTeam] -= 1`
  - Sets `%flag.carrier = %object` (player carrying flag)
  - Sets `%object.carryFlag = %flag` (player's carry flag reference)
  - Hides flag visually: `Item::hide(%this, true)`

### 3. **Player Drops Flag** (`Flag::onDrop` - line 641)
**Can happen from:**
- Transport spell
- Death
- Manual drop
- Any other drop method

**What happens:**
- **Line 644**: Gets flag from `%player.carryFlag`
- **Line 649-665**: 
  - Checks if map flag (team -1)
  - Gets house name from `previousHoldingTeam` (stored when picked up)
  - Formats house name (removes "House" prefix: "HouseKronos" → "Kronos")
  - **Broadcasts global message**: 
    ```
    "[Playername] has attempted to steal the Artifact from [housename] but dropped it - The Artifact will reset to its original position in 1 minute!"
    ```
- **Line 673-677**: 
  - Throws flag away from player
  - Makes flag visible: `Item::hide(%flag, false)`
  - Removes flag from player inventory
  - Clears carrier: `%flag.carrier = -1`
  - Clears player's carry flag: `%player.carryFlag = ""`
- **Line 680-687**: 
  - Schedules `Flag::checkReturn()` after **60 seconds** (1 minute) for map flags
  - Sets `%flag.dropFade = 1` (starts fade out animation)

### 4. **Flag Reset Timer** (`Flag::checkReturn` - line 694)
**After 60 seconds:**
- **Line 699-703**: Fade out animation (2.5 seconds)
- **Line 707-713**: 
  - Checks if still map flag (team -1)
  - **Resets flag to original position**: `GameBase::setPosition(%flag, %flag.originalPosition)`
  - Stops velocity: `Item::setVelocity(%flag, "0 0 0")`
  - Clears flagstand reference: `%flag.flagStand = ""`
  - Broadcasts: `"[FlagName] was returned to its initial position."`
- **Line 736-739**: 
  - Sets `%flag.atHome = true`
  - Fades flag back in
  - Updates objectives

## Reset Coordinates

**Flags reset to:** `%flag.originalPosition`

This position is set when the flag is first initialized in `ObjectiveMission::initCheck()` (line 247):
```cs
%object.originalPosition = GameBase::getPosition(%object);
```

This captures the flag's position from the **mission file** when it's first loaded. So flags always reset to their original spawn position from the mission.

## Key Properties

- **`holdingTeam`**: Which house currently holds the flag (set when placed on flagstand, cleared when picked up)
- **`previousHoldingTeam`**: NEW - Stores the house name when flag is picked up, so we can show it in drop message
- **`originalPosition`**: The flag's default position from the mission file (set on initialization)
- **`carrier`**: Player object carrying the flag (-1 when not carried)
- **`flagStand`**: The flagstand object the flag is on (if any)

## Example Flow

1. **Flag at HouseKronos base** → `holdingTeam = "HouseKronos"`
2. **Player "Bob" picks it up** → `previousHoldingTeam = "HouseKronos"`, `holdingTeam = -1`, `carrier = Bob`
3. **Bob transports away and drops flag** → 
   - Broadcast: `"Bob has attempted to steal the Artifact from Kronos but dropped it - The Artifact will reset to its original position in 1 minute!"`
   - Timer starts: 60 seconds
4. **After 60 seconds** → Flag resets to `originalPosition` (mission file position)

