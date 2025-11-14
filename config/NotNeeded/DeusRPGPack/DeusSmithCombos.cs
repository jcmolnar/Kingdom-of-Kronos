// By: Deus_ex_Machina
//
//	Install 1.43

$DeusSmithCombosver = "ver1.43";

function Smith::Combos(%Smith) {

	Client::centerPrint("<jc><f1>Need: <f2>"@$SmithCombo[%Smith], 1);
	Schedule("Client::centerPrint(\"\", 1);", 5);
}

$SmithCombo[1] = "RHatchet 1";
$SmithCombo[2] = "RBroadSword 1";
$SmithCombo[3] = "RLongSword 1";
$SmithCombo[4] = "RClub 1";
$SmithCombo[5] = "RSpikedClub 1";
$SmithCombo[6] = "RKnife 1";
$SmithCombo[7] = "RDagger 1";
$SmithCombo[8] = "RShortSword 1";
$SmithCombo[9] = "RPickAxe 1";
$SmithCombo[10] = "RShortBow 1";
$SmithCombo[11] = "RLightCrossbow 1";
$SmithCombo[12] = "RWarAxe 1";
$SmithCombo[13] = "Keldrinite 1 LongSword 1";
$SmithCombo[14] = "ElvenBow 1 CompositeBow 1 Quartz 3";
$SmithCombo[15] = "SmallRock 1 Quartz 1";
$SmithCombo[16] = "Knife 1 Quartz 1";
$SmithCombo[17] = "Dagger 1 Quartz 1 Granite 2";
$SmithCombo[18] = "Dagger 2 Jade 2 Quartz 4";
$SmithCombo[19] = "Club 1 SkeletonBone 1 Granite 3";
$SmithCombo[20] = "SpikedClub 1 SkeletonBone 2 Granite 5";
$SmithCombo[21] = "LightRobe 1 ApprenticeRobe 1 EnchantedStone 5";
$SmithCombo[22] = "Keldrinite 2 FullPlateArmor 1 Gold 5 Emerald 5 Diamond 5 EnchantedStone 5";
$SmithCombo[23] = "DragonScale 5 Diamond 5 Ruby 3";
$SmithCombo[24] = "DragonScale 3 Ruby 2";
$SmithCombo[25] = "AdvisorRobe 1 Topaz 2 EnchantedStone 4";
$SmithCombo[26] = "LongStaff 1 Granite 4 Turquoise 2";

$SmithComboResult[1] = "Hatchet 1";
$SmithComboResult[2] = "BroadSword 1";
$SmithComboResult[3] = "LongSword 1";
$SmithComboResult[4] = "Club 1";
$SmithComboResult[5] = "SpikedClub 1";
$SmithComboResult[6] = "Knife 1";
$SmithComboResult[7] = "Dagger 1";
$SmithComboResult[8] = "ShortSword 1";
$SmithComboResult[9] = "PickAxe 1";
$SmithComboResult[10] = "ShortBow 1";
$SmithComboResult[11] = "LightCrossbow 1";
$SmithComboResult[12] = "WarAxe 1";
$SmithComboResult[13] = "KeldriniteLS 1";
$SmithComboResult[14] = "AeolusWing 1";
$SmithComboResult[15] = "StoneFeather 1";
$SmithComboResult[16] = "MetalFeather 1";
$SmithComboResult[17] = "Talon 1";
$SmithComboResult[18] = "CeraphumsFeather 1";
$SmithComboResult[19] = "BoneClub 1";
$SmithComboResult[20] = "SpikedBoneClub 1";
$SmithComboResult[21] = "FineRobe 1";
$SmithComboResult[22] = "KeldrinArmor 1";
$SmithComboResult[23] = "DragonMail 1";
$SmithComboResult[24] = "DragonShield 1";
$SmithComboResult[25] = "ElvenRobe 1";
$SmithComboResult[26] = "JusticeStaff 1";

$DeusRPG::ScriptCheck++;