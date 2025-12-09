# Seal Battle Bot Scaling Analysis

## HP/MANA Calculation Formulas (from rpgstats.cs)

### MaxHP Formula (line 183-189)
```
%a = $MinHP[RACE] + ($PlayerSkill[clientId, $SkillEndurance] * 0.6)
%b = AddPoints(clientId, 4)  // Equipment bonuses
%c = floor(RemortStep * ($PlayerSkill[clientId, $SkillEndurance] / 8))
%d = LVL
%e = AddBonusStatePoints(clientId, "MaxHP")  // Accessory bonuses
MaxHP = floor(%a + %b + %c + %d + %e)
```

**Endurance contributes to HP in two ways:**
1. `Endurance * 0.6` - Direct HP bonus
2. `RemortStep * (Endurance / 8)` - Remort-based HP bonus

### MaxMANA Formula (line 208-215)
```
%a = 8 + round($PlayerSkill[clientId, $SkillEnergy] * (1/3) + (RemortStep * 2))
%b = AddPoints(clientId, 5)  // Equipment bonuses
%c = AddBonusStatePoints(clientId, "MaxMANA")  // Accessory bonuses
MaxMANA = %a + %b + %c
```

**Energy contributes to MANA:**
1. `Energy * (1/3)` - Direct MANA bonus

## Current Scaling in SealBattle::SetupBot()

The scaling multiplies the following values by the multiplier:

1. **STR, DEX, INT** (base attributes) - Lines 1187-1199
2. **LVL** (level) - Lines 1218-1222
3. **VIT** (vitality) - Lines 1224-1230
4. **Endurance skill** - Lines 1235-1240
5. **Energy skill** - Lines 1244-1249
6. **Base HP value** - Lines 1251-1256
7. **Base MANA value** - Lines 1258-1263

## Analysis

### ✅ What's Being Scaled Correctly

1. **Endurance skill** - Correctly scaled, affects HP via:
   - `Endurance * 0.6` (scaled)
   - `RemortStep * (Endurance / 8)` (scaled if RemortStep is also scaled)

2. **Energy skill** - Correctly scaled, affects MANA via:
   - `Energy * (1/3)` (scaled)

3. **LVL** - Correctly scaled, directly adds to MaxHP

### ⚠️ Potential Issues

1. **RemortStep not scaled**: 
   - RemortStep affects both HP (`RemortStep * (Endurance / 8)`) and MANA (`RemortStep * 2`)
   - Currently, RemortStep is NOT being scaled in `SealBattle::SetupBot()`
   - **Impact**: Remort-based bonuses won't scale with the multiplier

2. **AddPoints() and AddBonusStatePoints()**:
   - These are equipment/accessory bonuses that are NOT scaled
   - **Impact**: Equipment bonuses won't scale, but this might be intentional (equipment is already scaled via base HP/MANA)

3. **Base HP/MANA scaling**:
   - The code scales the base HP/MANA values from the spawn config
   - But RefreshAll() recalculates from the formulas above
   - **Impact**: The final MaxHP/MaxMANA might not match exactly due to formula recalculation

## Recommendations

1. **Scale RemortStep** (if bots have remort):
   ```torquescript
   %baseRemortStep = fetchData(%aiId, "RemortStep");
   if(%baseRemortStep != "" && %baseRemortStep != -1 && %baseRemortStep != 0)
   {
       %scaledRemortStep = floor(%baseRemortStep * %mult);
       storeData(%aiId, "RemortStep", %scaledRemortStep);
   }
   ```

2. **Verify scaling order**:
   - Current order: STR/DEX/INT → LVL → VIT → Endurance → Energy → Base HP/MANA → RefreshAll()
   - This order is correct - attributes first, then skills, then base values

3. **Check if MinHP[RACE] should be scaled**:
   - MinHP is race-based and might need scaling for very high multipliers
   - Currently not scaled, but might be negligible compared to other factors

## Conclusion

The scaling is mostly correct, but **RemortStep should be scaled** if seal battle bots have remort values. The current approach of scaling underlying values and then calling RefreshAll() is correct, as it ensures all formulas recalculate with scaled inputs.


