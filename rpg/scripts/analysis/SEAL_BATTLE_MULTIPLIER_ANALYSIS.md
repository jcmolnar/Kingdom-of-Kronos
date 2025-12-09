# Seal Battle Multiplier Analysis

## Multiplier Formula

```torquescript
function SealBattle::GetEnemyStrengthMultiplier()
{
    %baseMultiplier = 2.0;
    %increment = 1;
    %sealValue = GetTotalSealValue();
    %steps = %sealValue / 20;
    %multiplier = %baseMultiplier + (%steps * %increment);
    return %multiplier;
}
```

## How It Works

**Formula:** `multiplier = 2.0 + (sealValue / 20)`

- **Base Multiplier:** 2.0 (minimum scaling)
- **Increment:** 1.0 per step
- **Steps:** `sealValue / 20` (rounded down)
- **Final Multiplier:** `2.0 + steps`

## Multiplier Examples

| Seal Value | Steps | Multiplier | Scaling |
|------------|-------|------------|---------|
| 0 | 0 | 2.0x | 2x base stats |
| 20 | 1 | 3.0x | 3x base stats |
| 40 | 2 | 4.0x | 4x base stats |
| 60 | 3 | 5.0x | 5x base stats |
| 80 | 4 | 6.0x | 6x base stats |
| 100 | 5 | 7.0x | 7x base stats |
| 200 | 10 | 12.0x | 12x base stats |
| 500 | 25 | 27.0x | 27x base stats |

## What Gets Scaled

### Underlying Values (Scaled Before RefreshAll):
1. **LVL** → `scaledLVL = baseLVL * multiplier`
   - Example: LVL 1000 with 3.0x multiplier = LVL 3000
   - Effect: Directly adds to MaxHP

2. **RemortStep** → `scaledRemortStep = baseRemortStep * multiplier`
   - Example: RemortStep 15 with 3.0x multiplier = RemortStep 45
   - Effect: Affects HP, MANA, DEF, MDEF, ATK, MaxWeight

3. **Endurance Skill** → `scaledEndurance = baseEndurance * multiplier`
   - Example: Endurance 500 with 3.0x multiplier = Endurance 1500
   - Effect: 
     - Adds `Endurance * 0.6` to MaxHP
     - Adds `RemortStep * (Endurance / 8)` to MaxHP

4. **Energy Skill** → `scaledEnergy = baseEnergy * multiplier`
   - Example: Energy 300 with 3.0x multiplier = Energy 900
   - Effect: Adds `Energy * (1/3)` to MaxMANA

5. **WeightCapacity Skill** → `scaledWeightCapacity = baseWeightCapacity * multiplier`
   - Example: WeightCapacity 200 with 3.0x multiplier = WeightCapacity 600
   - Effect: Adds to MaxWeight (MaxWeight = 50 + WeightCapacity + RemortStep*4)

### Directly Scaled Stats (After RefreshAll):
6. **DEF** → `scaledDEF = baseDEF * multiplier`
   - Example: DEF 100 with 3.0x multiplier = DEF 300

7. **MDEF** → `scaledMDEF = baseMDEF * multiplier`
   - Example: MDEF 80 with 3.0x multiplier = MDEF 240

8. **ATK** → `scaledATK = baseATK * multiplier`
   - Example: ATK 150 with 3.0x multiplier = ATK 450

9. **DMG** → `scaledDMG = baseDMG * multiplier`
   - Example: DMG 50 with 3.0x multiplier = DMG 150

### Calculated Stats (From RefreshAll):
- **MaxHP** - Calculated from scaled LVL, RemortStep, Endurance
- **MaxMANA** - Calculated from scaled RemortStep, Energy
- **MaxWeight** - Calculated from scaled WeightCapacity, RemortStep
- **HP** - Calculated from MaxHP and Armor
- **MANA** - Calculated from MaxMANA and Armor

## Example: 3.0x Multiplier (Seal Value 20)

**Base Bot Stats:**
- LVL: 1000
- RemortStep: 15
- Endurance: 500
- Energy: 300
- WeightCapacity: 200
- DEF: 100
- MDEF: 80
- ATK: 150
- DMG: 50

**Scaled Bot Stats (3.0x multiplier):**
- LVL: 3000 (1000 * 3.0)
- RemortStep: 45 (15 * 3.0)
- Endurance: 1500 (500 * 3.0)
- Energy: 900 (300 * 3.0)
- WeightCapacity: 600 (200 * 3.0)
- DEF: 300 (100 * 3.0)
- MDEF: 240 (80 * 3.0)
- ATK: 450 (150 * 3.0)
- DMG: 150 (50 * 3.0)

**Calculated Stats (from RefreshAll):**
- MaxHP: Calculated from:
  - `$MinHP[RACE]` (unchanged)
  - `Endurance * 0.6` = 1500 * 0.6 = 900 (was 300)
  - `RemortStep * (Endurance / 8)` = 45 * (1500 / 8) = 45 * 187.5 = 8437.5 (was 937.5)
  - `LVL` = 3000 (was 1000)
  - Equipment bonuses (unchanged)
  - **Result: MaxHP is significantly higher (approximately 3x)**

- MaxMANA: Calculated from:
  - `8` (base)
  - `Energy * (1/3)` = 900 * (1/3) = 300 (was 100)
  - `RemortStep * 2` = 45 * 2 = 90 (was 30)
  - Equipment bonuses (unchanged)
  - **Result: MaxMANA is significantly higher (approximately 3x)**

- MaxWeight: Calculated from:
  - `50` (base)
  - `WeightCapacity` = 600 (was 200)
  - `RemortStep * 4` = 45 * 4 = 180 (was 60)
  - **Result: MaxWeight = 50 + 600 + 180 = 830 (was 310, approximately 2.7x)**

## Scaling Summary

**Minimum Scaling (Seal Value 0):**
- All stats are **2.0x** base values

**Per 20 Seal Value:**
- Multiplier increases by **1.0x**
- Example: Seal Value 20 = 3.0x, Seal Value 40 = 4.0x, etc.

**What This Means:**
- Seal bots start at **2x** difficulty compared to base spawn stats
- Each 20 seal value adds **+1x** to the multiplier
- At seal value 100, bots are **7x** stronger than base
- At seal value 500, bots are **27x** stronger than base

**Key Points:**
1. **Multiplicative scaling** - All underlying values are multiplied, not added
2. **Exponential growth** - Higher seal values create exponentially stronger bots
3. **Proportional scaling** - All stats scale together, maintaining balance
4. **RefreshAll handles calculations** - MaxHP/MaxMANA/MaxWeight are recalculated from scaled underlying values


