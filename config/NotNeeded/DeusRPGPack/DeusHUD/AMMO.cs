
function DeusRPGHud::UpdateDeusAmmohud(%hud) {

	$DeusHUD::AmmoCheckSize = 0;
	$DeusHUD::AmmoCheckSizeLen = 1;
	for(%i = 1; %i <= $DeusRPGHud::TotalAmmo; %i++) {
		%itemcount = getItemCount($DeusRPGHud::Ammo[%i, Arrow]);
		if(%itemcount > 0) {

			%name = $DeusRPGHud::Ammo[%i, Desc];
			%txt2 = "<f0>"@%name@":<f2> "@%itemcount@"\n";

			%len = strlen(%txt2);
			if(%len > $DeusHUD::AmmoCheckSizeLen)
				$DeusHUD::AmmoCheckSizeLen = %len;
			$DeusHUD::AmmoCheckSize++;

			%txt = %txt @ %txt2;
		}
	}
	HUD::AddTextLine(DeusAmmohud, %txt);

	$DeusHUD::newHeight = round(($DeusHUD::AmmoCheckSize * 13.3)+2);
	$DeusHUD::newWidth = round(($DeusHUD::AmmoCheckSizeLen * 5));

	if($DeusHUD::newHeight != $DeusHUD::tmpHeight || $DeusHUD::newWidth != $DeusHUD::tmpWidth) {
		$DeusHUD::tmpHeight = $DeusHUD::newHeight;
		$DeusHUD::tmpWidth = $DeusHUD::newWidth;
		HUD::Move(DeusAmmohud, $DeusHUD::getAmmoLeft@" "@$DeusHUD::getAmmoTop@" "@$DeusHUD::newWidth@" "@$DeusHUD::newHeight);
	}
	return $DeusRPGHud::CBdelay;
}

HUD::New(DeusAmmohud, DeusRPGHud::UpdateDeusAmmohud, $Hud::prefs::last::DeusAmmohud);

if($Hud::prefs::last::DeusAmmohud != "") {
	$DeusHUD::getAmmoLeft = GetWord($Hud::prefs::last::DeusAmmohud, 0);
	$DeusHUD::getAmmoTop = GetWord($Hud::prefs::last::DeusAmmohud, 1);
}
else {
	$DeusHUD::getAmmoLeft = $DeusRPG::AmmoLeft;
	$DeusHUD::getAmmoTop = $DeusRPG::AmmoTop;
}

$DeusRPGHUD::ScriptCheck++;