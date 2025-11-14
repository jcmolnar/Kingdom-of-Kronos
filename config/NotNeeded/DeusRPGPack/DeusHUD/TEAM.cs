//By : Deus_ex_Machina
//
//	RPGHUDs ver 2.562

$DeusRPGHud::tmpgroup = "12345";
function DeusRPGHud::UpdateDeusTEAMhud(%hud) {

	DeusRPGHud::GetGroupRpgData();

	if($DeusRPGHud::group != $DeusRPGHud::tmpgroup) {//This way if you didn't add anyone else you won't have to call RemoveThisFromList everytime
		$DeusRPGHud::tmpgroup = $DeusRPGHud::group;
		$DeusRPGHud::grp = RemoveThisFromList(",", $DeusRPGHud::group);
	}
	HUD::AddTextLine(DeusTEAMhud, "<f0>"@$DeusRPGHud::tmpname@"'s Group List: ");

	HUD::AddTextLine(DeusTEAMhud, "<f1>"@$DeusRPGHud::grp);//120

	return $DeusRPGHud::CB5delay;
}

HUD::New(DeusTEAMhud, DeusRPGHud::UpdateDeusTEAMhud, $Hud::prefs::last::DeusTEAMhud);

$DeusRPGHUD::ScriptCheck++;