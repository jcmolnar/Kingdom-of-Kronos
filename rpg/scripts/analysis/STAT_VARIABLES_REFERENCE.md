# Stat Variables Reference - Player and Enemy Bot Stats

This document lists all variables that affect player and enemy bot stats, based on analysis of `rpgstats.cs` and related scripts.

## Core Stat Calculations

### MaxHP (Maximum Health Points)
**Formula** (rpgstats.cs lines 181-194):
```
%a = $MinHP[RACE] + ($PlayerSkill[clientId, $SkillEndurance] * 0.6)
%b = AddPoints(clientId, 4)  // Equipment bonuses (HP from accessories)
%c = floor(RemortStep * ($PlayerSkill[clientId, $SkillEndurance] / 8))
%d = LVL
%e = AddBonusStatePoints(clientId, "MaxHP")  // Temporary bonus states

baseHP = floor(%a + %b + %c + %d + %e)

if(Stance == "Glass Cannon")
    MaxHP = floor(baseHP * 0.5)  // 50% reduction
else
    MaxHP = baseHP
```

**Variables that affect MaxHP:**
- `RACE` - Determines base `$MinHP[RACE]` value
- `$PlayerSkill[clientId, $SkillEndurance]` - Endurance skill level
  - Adds `Endurance * 0.6` directly to HP
  - Adds `RemortStep * (Endurance / 8)` via remort bonus
- `RemortStep` - Remort level (affects HP via Endurance skill)
- `LVL` - Character level (directly adds to HP)
- `Stance` - If "Glass Cannon", reduces HP by 50%
- **Equipment** - Via `AddPoints(clientId, 4)` checks accessories for HP bonuses
- **Bonus States** - Via `AddBonusStatePoints(clientId, "MaxHP")` for temporary bonuses

### HP (Current Health Points)
**Formula** (rpgstats.cs lines 196-204):
```
%armor = Player::getArmor(clientId)
%c = armor.maxDamage - GameBase::getDamageLevel(playerObject)
%a = %c * MaxHP
HP = round(%a / armor.maxDamage)
```

**Variables that affect HP:**
- `MaxHP` - Maximum HP (calculated above)
- `Armor` - Armor type affects maxDamage, which affects current HP calculation

### MaxMANA (Maximum Mana Points)
**Formula** (rpgstats.cs lines 206-215):
```
%a = 8 + round($PlayerSkill[clientId, $SkillEnergy] * (1/3) + (RemortStep * 2))
%b = AddPoints(clientId, 5)  // Equipment bonuses (MANA from accessories)
%c = AddBonusStatePoints(clientId, "MaxMANA")  // Temporary bonus states

if(Stance == "Glass Cannon")
    MaxMANA = round((%a + %b) * 0.5)  // 50% reduction
else
    MaxMANA = %a + %b + %c
```

**Variables that affect MaxMANA:**
- `$PlayerSkill[clientId, $SkillEnergy]` - Energy skill level
  - Adds `Energy * (1/3)` directly to MANA
- `RemortStep` - Remort level (adds `RemortStep * 2` to MANA)
- `Stance` - If "Glass Cannon", reduces MANA by 50%
- **Equipment** - Via `AddPoints(clientId, 5)` checks accessories for MANA bonuses
- **Bonus States** - Via `AddBonusStatePoints(clientId, "MaxMANA")` for temporary bonuses

### MANA (Current Mana Points)
**Formula** (rpgstats.cs lines 217-224):
```
%armor = Player::getArmor(clientId)
%a = GameBase::getEnergy(playerObject) * MaxMANA
MANA = round(%a / armor.maxEnergy)
```

**Variables that affect MANA:**
- `MaxMANA` - Maximum MANA (calculated above)
- `Armor` - Armor type affects maxEnergy, which affects current MANA calculation

### DEF (Defense)
**Formula** (rpgstats.cs lines 142-150):
```
%a = AddPoints(clientId, 7)  // Equipment bonuses (DEF from accessories)
%b = AddBonusStatePoints(clientId, "DEF")  // Temporary bonus states
%c = %a + %b + (RemortStep * 2)
%d = (OverweightStep * 7.0) / 100  // Overweight penalty (7% per step)
%e = Cap(%c - (%c * %d), 0, "inf")

DEF = floor(%e)
```

**Variables that affect DEF:**
- `RemortStep` - Remort level (adds `RemortStep * 2` to DEF)
- `OverweightStep` - Overweight penalty (reduces DEF by 7% per step)
- **Equipment** - Via `AddPoints(clientId, 7)` checks accessories for DEF bonuses
- **Bonus States** - Via `AddBonusStatePoints(clientId, "DEF")` for temporary bonuses

### MDEF (Magic Defense)
**Formula** (rpgstats.cs lines 152-160):
```
%a = AddPoints(clientId, 3)  // Equipment bonuses (MDEF from accessories)
%b = AddBonusStatePoints(clientId, "MDEF")  // Temporary bonus states
%c = %a + %b + (RemortStep * 2)
%d = (OverweightStep * 7.0) / 100  // Overweight penalty (7% per step)
%e = Cap(%c - (%c * %d), 0, "inf")

MDEF = floor(%e)
```

**Variables that affect MDEF:**
- `RemortStep` - Remort level (adds `RemortStep * 2` to MDEF)
- `OverweightStep` - Overweight penalty (reduces MDEF by 7% per step)
- **Equipment** - Via `AddPoints(clientId, 3)` checks accessories for MDEF bonuses
- **Bonus States** - Via `AddBonusStatePoints(clientId, "MDEF")` for temporary bonuses

### ATK (Attack)
**Formula** (rpgstats.cs lines 162-179):
```
%weapon = Player::getMountedItem(clientId, $WeaponSlot)

if(weapon != -1)
    %a = AddBonusStatePoints(clientId, "ATK")  // Temporary bonus states
    
    if(weapon is Ranged)
        %weapon = LoadedProjectile  // Use loaded projectile for ranged weapons
    
    %b = GetRoll(GetWord(GetAccessoryVar(weapon, $SpecialVar), 1))  // Weapon damage
    %c = RemortStep
    
    ATK = %a + %b + %c
else
    ATK = 0  // No weapon = no attack
```

**Variables that affect ATK:**
- `RemortStep` - Remort level (directly adds to ATK)
- **Weapon** - Weapon's `$SpecialVar` damage value (via `GetAccessoryVar`)
- **LoadedProjectile** - For ranged weapons, uses projectile damage instead
- **Bonus States** - Via `AddBonusStatePoints(clientId, "ATK")` for temporary bonuses

### DMG (Damage)
**Note:** DMG is stored directly via `storeData()` and is not calculated from other stats. It's typically set from equipment strings or bot spawn configs.

**Variables that affect DMG:**
- `DMG` - Directly stored value (from equipment strings or bot configs)

### MaxWeight (Maximum Weight Capacity)
**Formula** (rpgstats.cs lines 226-232):
```
%a = 50 + $PlayerSkill[clientId, $SkillWeightCapacity] + (RemortStep * 4)
%c = AddBonusStatePoints(clientId, "MaxWeight")  // Temporary bonus states

MaxWeight = FixDecimals(%a + %c)
```

**Variables that affect MaxWeight:**
- `$PlayerSkill[clientId, $SkillWeightCapacity]` - Weight Capacity skill level
- `RemortStep` - Remort level (adds `RemortStep * 4` to MaxWeight)
- **Bonus States** - Via `AddBonusStatePoints(clientId, "MaxWeight")` for temporary bonuses

### Weight (Current Weight)
**Formula** (rpgstats.cs line 236):
```
Weight = GetWeight(clientId)  // Calculated from all items in inventory
```

**Variables that affect Weight:**
- All items in inventory (weapons, armor, accessories, belt items, etc.)
- Equipment weight values

### OverweightStep
**Formula:** Calculated from current Weight vs MaxWeight
- If Weight > MaxWeight, OverweightStep increases
- Each step reduces DEF and MDEF by 7%

**Variables that affect OverweightStep:**
- `Weight` - Current weight (from inventory)
- `MaxWeight` - Maximum weight capacity

## Core Variables

### Base Character Variables
- `LVL` - Character level (calculated from EXP)
- `EXP` - Experience points
- `RemortStep` - Remort level (affects HP, MANA, DEF, MDEF, ATK, MaxWeight)
- `RACE` - Character race (affects base HP via `$MinHP[RACE]`)
- `CLASS` - Character class
- `GROUP` - Character group (Priest, Rogue, Warrior, Wizard)
- `LCK` - Luck stat (affects drop rates, hit chances, etc.)

### Skills
- `$PlayerSkill[clientId, $SkillEndurance]` - Endurance skill (affects MaxHP)
- `$PlayerSkill[clientId, $SkillEnergy]` - Energy skill (affects MaxMANA)
- `$PlayerSkill[clientId, $SkillWeightCapacity]` - Weight Capacity skill (affects MaxWeight)
- All other skills (Slashing, Piercing, Bludgeoning, Dodging, etc.) - affect combat effectiveness

### Stances
- `Stance` - Current stance
  - `"Glass Cannon"` - Reduces MaxHP and MaxMANA by 50%
  - `"Offensive"` - Increases damage, decreases defense
  - `"Defensive"` - Increases defense, decreases damage
  - `"MageBane"` - Anti-mage stance
  - `"BladeBane"` - Anti-blade stance
  - `"Normal"` - No modifications

### Equipment Variables
Equipment affects stats via `AddPoints()` function, which checks accessories for `$SpecialVar` values:
- `AddPoints(clientId, 4)` - HP bonuses from equipment
- `AddPoints(clientId, 5)` - MANA bonuses from equipment
- `AddPoints(clientId, 7)` - DEF bonuses from equipment
- `AddPoints(clientId, 3)` - MDEF bonuses from equipment

**Equipment types:**
- **Weapons** - Affect ATK (via `$SpecialVar` damage value)
- **Armor** - Affects HP/MANA calculation (via maxDamage/maxEnergy)
- **Accessories** - Can provide HP, MANA, DEF, MDEF bonuses via `$SpecialVar`
- **Shields** - Can provide DEF bonuses

### Bonus States
Temporary bonuses that affect stats via `AddBonusStatePoints()`:
- `$BonusState[clientId, index]` - Bonus state type (e.g., "MaxHP", "MaxMANA", "DEF", "MDEF", "ATK")
- `$BonusStateCnt[clientId, index]` - Number of ticks remaining
- Affects: MaxHP, MaxMANA, DEF, MDEF, ATK, MaxWeight

### Other Variables
- `OverweightStep` - Overweight penalty (reduces DEF and MDEF by 7% per step)
- `TournyRank` - Tournament rank (affects EXP gain)
- `RankPoints` - Rank points (affects EXP gain multiplier)
- `MyHouse` - House membership (required for EXP gain at LVL 60+)

## Differences: Players vs Enemy Bots

### Players
- All stats are calculated dynamically from variables above
- Can have stances (Glass Cannon, Offensive, Defensive, etc.)
- Can be overweight (affects DEF/MDEF)
- Can have bonus states
- Equipment affects stats via `AddPoints()` and `AddBonusStatePoints()`

### Enemy Bots
- **Same stat calculations** as players (use same formulas)
- **Stances are cleared** - Enemy bots should not have stances (cleared in `RefreshAll()`)
- Can be overweight (affects DEF/MDEF)
- Can have bonus states (if applied)
- Equipment affects stats via `AddPoints()` and `AddBonusStatePoints()`
- Stats are typically set from spawn configs (equipment strings) and then calculated

## Summary Table

| Stat | Core Variables | Skills | Equipment | Remort | Stance | Overweight | Bonus States |
|------|---------------|--------|-----------|--------|--------|------------|--------------|
| **MaxHP** | RACE, LVL | Endurance | HP bonuses | Yes | Glass Cannon (-50%) | No | Yes |
| **HP** | MaxHP, Armor | - | - | - | - | - | - |
| **MaxMANA** | - | Energy | MANA bonuses | Yes | Glass Cannon (-50%) | No | Yes |
| **MANA** | MaxMANA, Armor | - | - | - | - | - | - |
| **DEF** | - | - | DEF bonuses | Yes | - | Yes (-7%/step) | Yes |
| **MDEF** | - | - | MDEF bonuses | Yes | - | Yes (-7%/step) | Yes |
| **ATK** | - | - | Weapon damage | Yes | - | No | Yes |
| **DMG** | Direct value | - | - | - | - | - | - |
| **MaxWeight** | - | Weight Capacity | - | Yes | - | - | Yes |
| **Weight** | All items | - | - | - | - | - | - |

## Notes

1. **STR, DEX, INT, VIT** are NOT used in stat calculations for regular bots or players. They're only scaled for Seal Battle bots but don't affect MaxHP/MaxMANA.

2. **Equipment bonuses** are checked via `AddPoints()` which searches all accessories for `$SpecialVar` values matching specific character codes (4=HP, 5=MANA, 7=DEF, 3=MDEF).

3. **Bonus States** are temporary bonuses that can be applied via spells, items, or other effects. They expire after a certain number of ticks.

4. **Overweight penalty** only affects DEF and MDEF, not other stats.

5. **Stances** only affect MaxHP and MaxMANA (Glass Cannon reduces both by 50%). Other stances may affect combat mechanics but not base stats.

6. **RemortStep** affects multiple stats:
   - HP: `RemortStep * (Endurance / 8)`
   - MANA: `RemortStep * 2`
   - DEF: `RemortStep * 2`
   - MDEF: `RemortStep * 2`
   - ATK: `RemortStep` (direct addition)
   - MaxWeight: `RemortStep * 4`


