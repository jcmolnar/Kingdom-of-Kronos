//By : Deus_ex_Machina
//
//	RPGHUDs ver 2.561

function DeusRPGHud::UpdateDeusITEMhud(%hud) {
	if(getItemCount("Blue Potion") > "0")
		HUD::AddTextLine(DeusITEMhud, "<f0>Blue Potion:<f2> " @ getItemCount("Blue Potion"));
	else
		HUD::AddTextLine(DeusITEMhud, "<f0>No Blue Potions");

	if(getItemCount("Crystal Blue Potion") > "0")
		HUD::AddTextLine(DeusITEMhud, "<f0>Crystal Blue Potion:<f2> " @ getItemCount("Crystal Blue Potion"));
	else
		HUD::AddTextLine(DeusITEMhud, "<f0>No Crystal Blue Potions");

	if(getItemCount("Energy Vial") > "0")
		HUD::AddTextLine(DeusITEMhud, "<f0>Energy Vial:<f2> " @ getItemCount("Energy Vial"));
	else
		HUD::AddTextLine(DeusITEMhud, "<f0>No Energy Vials");

	if(getItemCount("Crystal Energy Vial") > "0")
		HUD::AddTextLine(DeusITEMhud, "<f0>Crystal Energy Vial:<f2> " @ getItemCount("Crystal Energy Vial"));
	else
		HUD::AddTextLine(DeusITEMhud, "<f0>No Crystal Energy Vial");

	return $DeusRPGHud::CBdelay;
}

HUD::New(DeusITEMhud, DeusRPGHud::UpdateDeusITEMhud, $Hud::prefs::last::DeusITEMhud);

$DeusRPGHUD::ScriptCheck++;