# Team and Armor Analysis

## Current Team Definitions

| Team | Name | Skin Model |
|------|------|------------|
| 0 | Citizen | rpgbase |
| 1 | Enemy | robewhite |
| 2 | Ogres | rpgorc |
| 3 | Pigmen | rpggnoll |
| 4 | Undead | undead |
| 5 | Demons | fedmonster |
| 6 | Minotaur | min |
| 7 | Aliens | fedmonster |
| 8 | Seals | fedmonster |

## Race to Armor Mapping

| Race | Armor Type | Team Assignment |
|------|------------|-----------------|
| Traveller | TravellerArmor | Team 1 (Enemy) |
| Ogre | OgreArmor | Team 2 (Ogres) |
| Pigman | PigmanArmor | Team 3 (Pigmen) |
| Orc | OrcArmor | Team 2 (Ogres) |
| Undead | UndeadArmor | Team 4 (Undead) |
| Zombie | ZombieArmor | Team 4 (Undead) |
| Demon | DemonArmor | Team 5 (Demons) |
| God | GodArmor | Team 5 (Demons) |
| Angel | AngelArmor | Team 1 (Enemy) |
| **Admin** | **AngelArmor** | **NOT DEFINED** ⚠️ |
| Alien | AlienArmor | Team 7 (Aliens) |
| Seals | SealsArmor | Team 8 (Seals) |
| Minotaur | MinotaurArmor | Team 6 (Minotaur) |

## Issues Identified

### 1. Admin Race Missing Team Assignment ⚠️
- **Problem**: `$RaceToArmorType[Admin] = "AngelArmor"` is defined, but `$TeamForRace[Admin]` is NOT defined
- **Impact**: Admin bots (Liquifier, Obliterator, Abolisher) will fall back to pattern matching instead of race-based team assignment
- **Current Behavior**: Pattern matching sets Admin bots to team 1 (Enemy) because "Admin" prefix doesn't match "Demon" or "God"
- **Fix Needed**: Add `$TeamForRace[Admin] = 1;` (since Admin uses AngelArmor and should be Team 1 like Angels)

### 2. Multiple Teams Using Same Skin Model
- **Teams 5, 7, 8 all use "fedmonster" skin**: Demons, Aliens, and Seals
- **This is NOT a problem** - Teams are separate game entities, skin is just visual
- **Teams are correctly assigned** based on race → armor → team mapping
- **No conflicts** - Each race has unique armor type, so team assignment is unambiguous

### 3. Angel and Admin Both Use AngelArmor
- **Angel race** → AngelArmor → Team 1 ✅
- **Admin race** → AngelArmor → (no team defined) ⚠️
- **This is fine** as long as Admin gets proper team assignment
- **Both should be Team 1** since they use the same armor

### 4. Demon and God Both Use Team 5
- **Demon race** → DemonArmor → Team 5 ✅
- **God race** → GodArmor → Team 5 ✅
- **This is intentional** - Both are on the same team (Demons)
- **Different armors** (DemonArmor vs GodArmor) but same team - this is fine

## Armor Type to Race Mapping

| Armor Type | Race | Team |
|------------|------|------|
| TravellerArmor | Traveller | 1 |
| OgreArmor | Ogre | 2 |
| PigmanArmor | Pigman | 3 |
| OrcArmor | Orc | 2 |
| UndeadArmor | Undead | 4 |
| ZombieArmor | Zombie | 4 |
| DemonArmor | Demon | 5 |
| GodArmor | God | 5 |
| **AngelArmor** | **Angel** | **1** |
| **AngelArmor** | **Admin** | **NOT DEFINED** ⚠️ |
| AlienArmor | Alien | 7 |
| SealsArmor | Seals | 8 |
| MinotaurArmor | Minotaur | 6 |

## Bot Name Prefixes and Their Races

| Prefix | Race | Armor | Team |
|--------|------|-------|------|
| Traveller* | Traveller | TravellerArmor | 1 |
| Ogre* | Ogre | OgreArmor | 2 |
| Pigman* | Pigman | PigmanArmor | 3 |
| Orc* | Orc | OrcArmor | 2 |
| Undead* | Undead | UndeadArmor | 4 |
| Zombie* | Zombie | ZombieArmor | 4 |
| Demon* | Demon | DemonArmor | 5 |
| God* | God | GodArmor | 5 |
| Angel* | Angel | AngelArmor | 1 |
| **Admin*** | **Admin** | **AngelArmor** | **1 (via pattern)** |
| Alien* | Alien | AlienArmor | 7 |
| Seal* | Seals | SealsArmor | 8 |
| Minotaur* | Minotaur | MinotaurArmor | 6 |

## Recommendations

1. **Add `$TeamForRace[Admin] = 1;`** to EnemyArmors.cs
   - This ensures Admin bots use race-based team assignment instead of pattern matching
   - Consistent with Angel bots (both use AngelArmor, both should be Team 1)

2. **Verify pattern matching fallback** handles all cases correctly
   - Currently "Admin" and "Angel" are handled in pattern matching
   - But race-based assignment is more reliable

3. **No changes needed for shared skins**
   - Multiple teams can share the same skin model
   - Teams are separate game entities, skin is just visual representation


