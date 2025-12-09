# Town Bot Conversion: Item Objects → Player Objects

## Current State

**Town bots are currently Item objects:**
- Created with: `newObject("", "Item", $BotInfo[%name, RACE] @ "TownBot", 1, false)`
- Location: `Ai.cs` line 1690 in `InitTownBots()`
- Cannot use Player functions: `Player::mountItem()`, `Player::incItemCount()`, etc.
- Cannot visually display mounted items

**Enemy bots are Player objects:**
- Created with: `AI::spawn()` → `createAI()` → returns a clientId
- Can use all Player functions
- Can visually display mounted items

## Issues with Current Item Object Approach

1. **No Visual Item Display**: Items cannot be visually mounted on Item objects
2. **No Player Functions**: Cannot use `Player::mountItem()`, `Player::incItemCount()`, etc.
3. **Limited Functionality**: Cannot leverage existing bot item management systems

## Conversion Requirements

### 1. **Change Object Creation**

**Current (Item object):**
```cs
%townbot = newObject("", "Item", $BotInfo[%name, RACE] @ "TownBot", 1, false);
```

**Proposed (Player object via AI::spawn):**
```cs
%aiName = "TownBot_" @ %name;  // Unique AI name
%armor = $RaceToArmorType[$BotInfo[%name, RACE]];  // Get armor type for race
%spawnPos = GameBase::getPosition(%marker);
%spawnRot = GameBase::getRotation(%marker);
%townbotId = AI::spawn(%aiName, %armor, %spawnPos, %spawnRot, %name, "male2");
```

### 2. **Key Differences: Item vs Player Objects**

| Aspect | Item Object | Player Object |
|--------|-------------|---------------|
| Creation | `newObject("", "Item", shape, ...)` | `AI::spawn()` → returns clientId |
| Client ID | Object ID itself | `AI::getId(aiName)` → returns clientId |
| Player Object | N/A (is Item) | `Client::getOwnedObject(clientId)` |
| Item Functions | ❌ Cannot use | ✅ Can use |
| Visual Items | ❌ Not possible | ✅ Possible |
| AI Control | ❌ No AI behavior | ✅ Full AI control |
| Movement | ❌ Static | ✅ Can move/patrol |

### 3. **Systems That Need Updates**

#### A. **InitTownBots() Function** (`Ai.cs` line 1670)
- **Change**: Use `AI::spawn()` instead of `newObject("", "Item", ...)`
- **Get clientId**: Use `AI::getId(%aiName)` instead of using object directly
- **Store clientId**: Update `$TownBotList` to store clientIds instead of Item object IDs
- **Post-spawn setup**: May need scheduled function like `SpawnAIPostCreate()` for full initialization

#### B. **RotateTownBot() Function** (`Ai.cs` line 1804)
- **Current**: Takes Item object ID, deletes and recreates Item object
- **Change**: Take clientId, delete AI, respawn at new rotation
- **Note**: Currently uses `%id.name` - need to ensure name is preserved

#### C. **TownBotList Storage** (`Ai.cs` line 1707)
- **Current**: Stores Item object IDs: `$TownBotList = $TownBotList @ %townbot @ " ";`
- **Change**: Store clientIds: `$TownBotList = $TownBotList @ %clientId @ " ";`
- **Impact**: All code that iterates `$TownBotList` needs to use `Client::getOwnedObject()` to get Player object

#### D. **Item Giving System** (`Ai.cs` line 1728 - `InitTownBotItems()`)
- **Current**: Tries to use Item object directly (fails)
- **Change**: Use `Client::getOwnedObject(%clientId)` to get Player object, then use `Player::mountItem()`
- **Benefit**: Will actually work and display items visually!

#### E. **Loot Drop Prevention** (`playerdamage.cs` line 662)
- **Current**: Checks `$TownBotList` for Item object IDs
- **Change**: Check `$TownBotList` for clientIds
- **Note**: `isRPGAI(%clientId)` should work for Player objects

#### F. **Item Events** (`itemevents.cs` line 81)
- **Current**: Checks if object is Item type, returns early
- **Change**: Town bots will be Player objects, so `Player::getClient()` will work
- **Benefit**: Can use standard `Item::giveItem()` flow

### 4. **Potential Issues & Solutions**

#### Issue 1: **AI Behavior Activation**
- **Problem**: `AI::spawn()` creates bots with AI behavior (they may try to move, attack, etc.)
- **Solution**: 
  - Set AI variables to disable behavior: `AI::setVar(%aiName, "pathType", "none")`
  - Or use a "dumb AI" flag: `storeData(%clientId, "dumbAIflag", "true")`
  - Disable targeting: `AI::setVar(%aiName, "spotDist", 0)`

#### Issue 2: **Movement/Patrol**
- **Problem**: Player objects with AI may try to move or patrol
- **Solution**: 
  - Set position and lock it: `GameBase::setPosition()` + disable movement
  - Use `AI::newDirectiveStand(%aiName)` to keep bot stationary
  - Or use existing "dumb AI" system if it exists

#### Issue 3: **Team Assignment**
- **Current**: `GameBase::setTeam(%townbot, $BotInfo[%name, TEAM])`
- **Change**: `GameBase::setTeam(%clientId, $BotInfo[%name, TEAM])` (should work the same)

#### Issue 4: **Name/Identity**
- **Current**: `%townbot.name = %name` and `GameBase::setMapName(%townbot, $BotInfo[%name, NAME])`
- **Change**: `GameBase::setMapName(%clientId, $BotInfo[%name, NAME])` (name should be set via `AI::spawn()`)

#### Issue 5: **Shop System Integration**
- **Check**: How shops identify town bots - may need updates if they check object type
- **Solution**: Use `isRPGAI()` and `$TownBotList` checks (should work with clientIds)

#### Issue 6: **Mission File Structure**
- **Current**: Town bots are defined in mission file as Item objects
- **Change**: May need to update mission file structure, OR keep Item definitions and convert during `InitTownBots()`
- **Recommendation**: Keep mission file as-is, convert during initialization

### 5. **Step-by-Step Conversion Plan**

#### Step 1: **Backup Current System**
- Create backup of `InitTownBots()` and related functions
- Document current Item object behavior

#### Step 2: **Update InitTownBots()**
- Replace `newObject("", "Item", ...)` with `AI::spawn()`
- Get clientId using `AI::getId()`
- Update `$TownBotList` to store clientIds
- Add post-spawn initialization (schedule if needed)

#### Step 3: **Disable AI Behavior**
- Set AI variables to prevent movement/attacking
- Use "dumb AI" flag or disable AI functions
- Ensure bots remain stationary

#### Step 4: **Update RotateTownBot()**
- Change to work with clientIds instead of Item object IDs
- Use `AI::delete()` and respawn instead of `deleteObject()`
- Preserve name and other properties

#### Step 5: **Update InitTownBotItems()**
- Use `Client::getOwnedObject(%clientId)` to get Player object
- Use `Player::mountItem()` on Player object (will actually work!)
- Remove workarounds for Item objects

#### Step 6: **Update All $TownBotList Iterations**
- Find all code that iterates `$TownBotList`
- Update to use `Client::getOwnedObject()` to get Player object when needed
- Update checks in `playerdamage.cs`, `itemevents.cs`, etc.

#### Step 7: **Test Thoroughly**
- Verify town bots spawn correctly
- Verify items are visually displayed
- Verify bots don't move or attack
- Verify shop system still works
- Verify loot drop prevention works
- Verify rotation system works

### 6. **Benefits of Conversion**

✅ **Visual Item Display**: Items will actually appear on town bots  
✅ **Standard Item Functions**: Can use `Player::mountItem()`, `Player::incItemCount()`, etc.  
✅ **Consistency**: Town bots work like enemy bots (both Player objects)  
✅ **Future-Proof**: Easier to add features that require Player objects  
✅ **Auto-Equip System**: Can leverage existing bot auto-equip code  

### 7. **Risks & Considerations**

⚠️ **AI Behavior**: Need to ensure bots don't move or attack  
⚠️ **Performance**: Player objects may have more overhead than Item objects  
⚠️ **Compatibility**: Need to verify all systems work with Player objects  
⚠️ **Testing**: Extensive testing required to ensure nothing breaks  

### 8. **Alternative: Hybrid Approach**

If full conversion is too risky, consider:
- Keep town bots as Item objects
- Create invisible Player objects for item management
- Link Item object to Player object via `storeData()`
- Mount items on Player object, sync visuals to Item object (complex)

**Recommendation**: Full conversion is cleaner and more maintainable.

## Conclusion

Converting town bots from Item objects to Player objects is **feasible but requires careful implementation**. The main challenges are:
1. Disabling AI behavior to keep bots stationary
2. Updating all code that references town bots
3. Ensuring compatibility with existing systems

The benefits (visual item display, standard functions) likely outweigh the risks if implemented carefully.

