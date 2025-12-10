//=============================================================================
// Damage Avoidance Calculator
// Calculates how much damage a player avoids from enemy bot attacks
//=============================================================================

function CalculateDamageAvoidance(%def, %mdef, %dodging, %spellResistance, %attackerSkill, %baseDamage, %damageType, %attackerCastingSkill)
{
	// %damageType: "physical" or "spell"
	// %attackerCastingSkill: Only used for spell damage (defaults to 1000 if not provided)
	
	if(%attackerCastingSkill == "" || %attackerCastingSkill == -1)
		%attackerCastingSkill = 1000; // Default skill value for NPCs
	
	echo("========================================");
	echo("DAMAGE AVOIDANCE CALCULATOR");
	echo("========================================");
	echo("Player Stats:");
	echo("  DEF: " @ %def);
	echo("  MDEF: " @ %mdef);
	echo("  Dodging Skill: " @ %dodging);
	echo("  Spell Resistance Skill: " @ %spellResistance);
	echo("");
	echo("Enemy Bot Stats:");
	echo("  Attacker Skill: " @ %attackerSkill);
	if(%damageType == "spell")
		echo("  Casting Skill: " @ %attackerCastingSkill);
	echo("  Base Damage: " @ %baseDamage);
	echo("  Damage Type: " @ %damageType);
	echo("");
	
	if(%damageType == "spell")
	{
		// SPELL DAMAGE CALCULATION
		echo("--- SPELL DAMAGE CALCULATION ---");
		
		// 1. Calculate miss chance
		%defenderValue = (%mdef / 5) + %spellResistance + 5;
		%attackerValue = %attackerSkill + 5;
		%totalValue = %defenderValue + %attackerValue;
		%missChance = (%defenderValue / %totalValue) * 100;
		%hitChance = (%attackerValue / %totalValue) * 100;
		
		echo("Miss Chance Calculation:");
		echo("  Defender Value (x): (MDEF/5) + SpellResistance + 5");
		echo("    = (" @ %mdef @ "/5) + " @ %spellResistance @ " + 5");
		echo("    = " @ (%mdef / 5) @ " + " @ %spellResistance @ " + 5");
		echo("    = " @ %defenderValue);
		echo("  Attacker Value (y): AttackerSkill + 5");
		echo("    = " @ %attackerSkill @ " + 5");
		echo("    = " @ %attackerValue);
		echo("  Total (n): " @ %totalValue);
		echo("  Miss Chance: " @ floor(%missChance * 100) / 100 @ "%");
		echo("  Hit Chance: " @ floor(%hitChance * 100) / 100 @ "%");
		echo("");
		
		// 2. Calculate damage after skill multiplier
		%skillMultipliedDamage = round((%baseDamage * %attackerCastingSkill) / 1000);
		echo("Damage After Skill Multiplier:");
		echo("  Base Damage: " @ %baseDamage);
		echo("  Casting Skill: " @ %attackerCastingSkill);
		echo("  Formula: (BaseDamage * CastingSkill) / 1000");
		echo("  = (" @ %baseDamage @ " * " @ %attackerCastingSkill @ ") / 1000");
		echo("  = " @ %skillMultipliedDamage);
		echo("");
		
		// 3. Calculate MDEF damage reduction (current weak formula)
		%mdefReductionMin = 1;
		%mdefReductionMax = (%mdef / 10) + 1;
		%mdefReductionAvg = ((%mdef / 10) + 1 + 1) / 2; // Average of min and max
		%mdefReductionAvg = floor(%mdefReductionAvg);
		
		echo("MDEF Damage Reduction (Current Formula):");
		echo("  Formula: random(MDEF/10) + 1");
		echo("  Min Reduction: " @ %mdefReductionMin);
		echo("  Max Reduction: " @ floor(%mdefReductionMax));
		echo("  Average Reduction: " @ %mdefReductionAvg);
		echo("");
		
		// 4. Calculate final damage ranges
		%damageAfterReductionMin = Cap(%skillMultipliedDamage - %mdefReductionMax, 0, "inf");
		%damageAfterReductionMax = Cap(%skillMultipliedDamage - %mdefReductionMin, 0, "inf");
		%damageAfterReductionAvg = Cap(%skillMultipliedDamage - %mdefReductionAvg, 0, "inf");
		
		echo("Final Damage (if hit):");
		echo("  Min Damage: " @ floor(%damageAfterReductionMin) @ " (after max MDEF reduction)");
		echo("  Max Damage: " @ floor(%damageAfterReductionMax) @ " (after min MDEF reduction)");
		echo("  Average Damage: " @ floor(%damageAfterReductionAvg) @ " (after avg MDEF reduction)");
		echo("");
		
		// 5. Calculate total damage avoided
		%damageAvoidedFromMiss = %skillMultipliedDamage * (%missChance / 100);
		%damageAvoidedFromReduction = %mdefReductionAvg * (%hitChance / 100);
		%totalDamageAvoided = %damageAvoidedFromMiss + %damageAvoidedFromReduction;
		%totalDamageAvoidedPercent = (%totalDamageAvoided / %skillMultipliedDamage) * 100;
		
		echo("Total Damage Avoided:");
		echo("  Avoided from Miss: " @ floor(%damageAvoidedFromMiss * 100) / 100 @ " (" @ floor(%missChance * 100) / 100 @ "% of " @ %skillMultipliedDamage @ ")");
		echo("  Avoided from MDEF Reduction: " @ floor(%damageAvoidedFromReduction * 100) / 100 @ " (" @ floor(%hitChance * 100) / 100 @ "% of " @ %mdefReductionAvg @ ")");
		echo("  TOTAL AVOIDED: " @ floor(%totalDamageAvoided * 100) / 100 @ " out of " @ %skillMultipliedDamage);
		echo("  TOTAL AVOIDED %: " @ floor(%totalDamageAvoidedPercent * 100) / 100 @ "%");
		echo("");
		
		// 6. Show expected damage taken
		%expectedDamage = %skillMultipliedDamage - %totalDamageAvoided;
		echo("Expected Damage Taken:");
		echo("  Average: " @ floor(%expectedDamage * 100) / 100);
		echo("  Range: " @ floor(%damageAfterReductionMin) @ " - " @ floor(%damageAfterReductionMax));
	}
	else
	{
		// PHYSICAL DAMAGE CALCULATION
		echo("--- PHYSICAL DAMAGE CALCULATION ---");
		
		// 1. Calculate miss chance
		%defenderValue = (%def / 5) + %dodging + 5;
		%attackerValue = %attackerSkill + 5;
		%totalValue = %defenderValue + %attackerValue;
		%missChance = (%defenderValue / %totalValue) * 100;
		%hitChance = (%attackerValue / %totalValue) * 100;
		
		echo("Miss Chance Calculation:");
		echo("  Defender Value (x): (DEF/5) + Dodging + 5");
		echo("    = (" @ %def @ "/5) + " @ %dodging @ " + 5");
		echo("    = " @ (%def / 5) @ " + " @ %dodging @ " + 5");
		echo("    = " @ %defenderValue);
		echo("  Attacker Value (y): AttackerSkill + 5");
		echo("    = " @ %attackerSkill @ " + 5");
		echo("    = " @ %attackerValue);
		echo("  Total (n): " @ %totalValue);
		echo("  Miss Chance: " @ floor(%missChance * 100) / 100 @ "%");
		echo("  Hit Chance: " @ floor(%hitChance * 100) / 100 @ "%");
		echo("");
		
		// 2. Physical damage has NO reduction (only miss chance)
		echo("Physical Damage Reduction:");
		echo("  WARNING: DEF does NOT reduce physical damage taken!");
		echo("  DEF only affects miss chance, not damage reduction.");
		echo("  If hit, full damage is taken: " @ %baseDamage);
		echo("");
		
		// 3. Calculate total damage avoided
		%damageAvoidedFromMiss = %baseDamage * (%missChance / 100);
		%totalDamageAvoided = %damageAvoidedFromMiss;
		%totalDamageAvoidedPercent = (%totalDamageAvoided / %baseDamage) * 100;
		
		echo("Total Damage Avoided:");
		echo("  Avoided from Miss: " @ floor(%damageAvoidedFromMiss * 100) / 100 @ " (" @ floor(%missChance * 100) / 100 @ "% of " @ %baseDamage @ ")");
		echo("  Avoided from DEF Reduction: 0 (DEF does not reduce damage)");
		echo("  TOTAL AVOIDED: " @ floor(%totalDamageAvoided * 100) / 100 @ " out of " @ %baseDamage);
		echo("  TOTAL AVOIDED %: " @ floor(%totalDamageAvoidedPercent * 100) / 100 @ "%");
		echo("");
		
		// 4. Show expected damage taken
		%expectedDamage = %baseDamage - %totalDamageAvoided;
		echo("Expected Damage Taken:");
		echo("  Average: " @ floor(%expectedDamage * 100) / 100);
		echo("  (Full damage if hit, 0 if miss)");
	}
	
	echo("========================================");
	echo("");
	
	return %totalDamageAvoided;
}

// Example usage:
// CalculateDamageAvoidance(4000, 4000, 1000, 1000, 1000, 10000, "physical");
// CalculateDamageAvoidance(4000, 4000, 1000, 1000, 1000, 10000, "spell", 1500);

