$AccessoryVar[WindPaws, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[WindPaws, $SpecialVar] = "8 4";
$AccessoryVar[WindPaws, $Weight] = 1;
$AccessoryVar[WindPaws, $MiscInfo] = "<f2>WindPaws Combine the speed of CheataursPaws and the Ability to fly from WindWalkers!";
$SkillRestriction[WindPaws] = $MinLevel @ " 60 ";
$ItemCost[WindPaws]= 1000000;

ItemData WindPaws
{
	description = "Wind Paws";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData WindPaws0
{
	description = "WindPaws";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};

$AccessoryVar[AdminBoots, $AccessoryType] = $BootsAccessoryType;
$AccessoryVar[AdminBoots, $SpecialVar] = "8 4.4";  // 10% faster than WindPaws (4 * 1.1 = 4.4)
$AccessoryVar[AdminBoots, $Weight] = 1;
$AccessoryVar[AdminBoots, $MiscInfo] = "<f2>Admin Boots - Fly faster than Wind Paws with no mana cost!";
$SkillRestriction[AdminBoots] = $MinLevel @ " 1 ";
$ItemCost[AdminBoots]= 1000000000;

ItemData AdminBoots
{
	description = "Admin Boots";
	className = "Accessory";
	shapeFile = "discammo";

	heading = "eMiscellany";
	price = 0;
};
ItemData AdminBoots0
{
	description = "AdminBoots";
	className = "Equipped";
	shapeFile = "discammo";

	heading = "aArmor";
};