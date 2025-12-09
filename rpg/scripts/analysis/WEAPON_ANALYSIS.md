# Weapon Damage vs Attack Speed Analysis

## Key Finding: Attack Speed is NOT Based on Individual Weapon Damage

**Attack speed modifiers are constant per weapon category ($AccessoryType), not variable per weapon.**

---

## Attack Speed Modifier Patterns

### Sword/Slashing Weapons
- **Standard Speed**: `$DelayFactorTable[$SwordAccessoryType]` = **0.9** (faster)
- **Exceptions**:
  - `Hatchet` uses `$AxeAccessoryType` = **1.1** (slower) despite being Slashing skill

**All 15 standard swords** (RustyIronBlade through TerminusEst) use **0.9 speed** regardless of damage (9 to 600).

### Polearm/Piercing Weapons
- **Standard Speed**: `$DelayFactorTable[$PolearmAccessoryType]` = **1.2** (slowest)
- **Exceptions**:
  - `PickAxe` uses `$AxeAccessoryType` = **1.1**
  - `Knife`, `Dagger`, `CastingBlade`, `ButterKnife`, `LongKnife` use `$SwordAccessoryType` = **0.9** (faster)

**All 13 standard polearms** (IronSpear through AecoSeorei) use **1.2 speed** regardless of damage (34 to 600).

### Bludgeon/Mace Weapons
- **Standard Speed**: `$DelayFactorTable[$BludgeonAccessoryType]` = **1.1** (medium)
- **No exceptions** - all 16 weapons use **1.1 speed** regardless of damage (13 to 600).

---

## Speed Modifier Summary

| AccessoryType | Speed Modifier | Interpretation | Weapons Using It |
|--------------|----------------|----------------|------------------|
| `$SwordAccessoryType` | **0.9** | Fastest (10% faster than base) | Most swords, some piercing weapons |
| `$BludgeonAccessoryType` | **1.1** | Medium (10% slower than base) | All maces/bludgeons |
| `$AxeAccessoryType` | **1.1** | Medium (10% slower than base) | Hatchet, PickAxe |
| `$PolearmAccessoryType` | **1.2** | Slowest (20% slower than base) | Most spears/pikes/tridents |

**Note**: Lower delay factor = faster attacks. 0.9 means 90% of base delay (faster), 1.2 means 120% of base delay (slower).

---

## Damage vs Speed Analysis

### There is NO correlation between damage and attack speed

**Examples:**
- `RustyIronBlade` (9 damage) = 0.9 speed
- `TerminusEst` (600 damage) = 0.9 speed
- **Same speed, 66x damage difference**

- `PickAxe` (3 damage) = 1.1 speed  
- `AecoSeorei` (600 damage) = 1.2 speed
- **200x damage difference, but only 9% speed difference (due to category)**

- `CrackedStick` (13 damage) = 1.1 speed
- `MorningStar` (600 damage) = 1.1 speed
- **Same speed, 46x damage difference**

---

## Damage Progression Patterns

### Sword/Slashing Weapons
**Damage progression**: 9 → 16 → 27 → 40 → 55 → 70 → 90 → 115 → 140 → 165 → 195 → 220 → 300 → 450 → 600

**Pattern**: Roughly exponential growth with some inconsistencies:
- Early tiers: +7, +11, +13, +15, +15, +20, +20, +25, +25, +25, +30, +25
- Mid tiers: +80 (jump from 220 to 300)
- High tiers: +150 (jump from 300 to 450), +150 (450 to 600)

### Polearm/Piercing Weapons
**Damage progression** (standard polearms only): 34 → 48 → 65 → 83 → 105 → 130 → 160 → 190 → 220 → 260 → 350 → 520 → 600

**Pattern**: More consistent linear/exponential growth:
- Early tiers: +14, +17, +18, +22, +25, +30, +30, +30, +40
- High tiers: +90 (jump from 260 to 350), +170 (350 to 520), +80 (520 to 600)

### Bludgeon/Mace Weapons
**Damage progression**: 13 → 23 → 36 → 49 → 62 → 77 → 95 → 120 → 150 → 180 → 210 → 240 → 330 → 490 → 600

**Pattern**: Consistent incremental growth:
- Early tiers: +10, +13, +13, +13, +15, +18, +25, +30, +30, +30, +30
- High tiers: +90 (jump from 240 to 330), +160 (330 to 490), +110 (490 to 600)

---

## Special Cases: Weapons Using Different AccessoryTypes

### Piercing Weapons Using Sword Speed (0.9)
These weapons use `$SwordAccessoryType` but have `$SkillPiercing`:
- `Knife` (55 damage) - 0.9 speed
- `Dagger` (23 damage) - 0.9 speed
- `CastingBlade` (18 damage) - 0.9 speed
- `ButterKnife` (5 damage) - 0.9 speed
- `LongKnife` (13 damage) - 0.9 speed

**Reason**: These are small, fast weapons that benefit from sword-like speed despite being piercing.

### Slashing Weapon Using Axe Speed (1.1)
- `Hatchet` (60 damage) - 1.1 speed (uses `$AxeAccessoryType`)

**Reason**: Hatchets are heavier chopping weapons, slower than swords.

### Piercing Weapon Using Axe Speed (1.1)
- `PickAxe` (3 damage) - 1.1 speed (uses `$AxeAccessoryType`)

**Reason**: Pickaxes are mining tools, not combat weapons, so they use axe mechanics.

---

## Conclusion

### Attack Speed is Category-Based, Not Damage-Based

1. **Speed is determined by `$AccessoryType`, not individual weapon damage**
2. **No ratio exists between damage and speed** - a 9 damage weapon can have the same speed as a 600 damage weapon
3. **Damage progression is roughly exponential** but has some inconsistencies, especially at high tiers
4. **Some weapons use different AccessoryTypes** for flavor/balance reasons (e.g., knives use sword speed despite being piercing)

### Design Philosophy

The system appears designed so that:
- **Weapon category determines speed** (swords fast, polearms slow, maces medium)
- **Individual weapon damage scales independently** based on material tier and progression
- **No trade-off between damage and speed** - higher damage doesn't mean slower attacks within the same category

This creates a clear distinction: **Swords are fast, Polearms are slow, Maces are medium** - regardless of how much damage they deal.

---

## Recommendations

If you want to add damage/speed trade-offs, you would need to:
1. Create individual `$DelayFactorTable` entries per weapon (currently only per AccessoryType)
2. Or add a damage-based speed penalty formula
3. Or create sub-categories within each weapon type

Currently, the system is **simple and category-based** - all swords attack at the same speed, all polearms at the same speed, etc.

