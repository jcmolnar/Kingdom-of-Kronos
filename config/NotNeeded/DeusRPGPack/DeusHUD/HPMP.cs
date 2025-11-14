//By : Deus_ex_Machina
//
//	RPGHUDs ver 2.561

function DeusRPGHud::UpdateDeusHPMPhud(%hud) {
	if($FastUpdate == "") {
		DeusRPGHud::GetHPMPRpgData();
	}

	HUD::AddTextLine(DeusHPMPhud, "<f0>HP: <f2>"@$DeusRPGHud::hp@"<f0> / <f2>"@$DeusRPGHud::maxhp);
	HUD::AddTextLine(DeusHPMPhud, "<f0>MP: <f2>"@$DeusRPGHud::mana@"<f0> / <f2>" @ $DeusRPGHud::maxmana);

	return $DeusRPGHud::CBdelay;
}



HUD::New(DeusHPMPhud, DeusRPGHud::UpdateDeusHPMPhud, $Hud::prefs::last::DeusHPMPhud);


function DeusRPGHUD::OnMessage(%client, %msg) { //Good way to update HP with-out doing a fetchdata every sec or less.
	if(%client)
		$lastClientMessage = %client;
	else {
		if(String::findSubStr(%msg, "You were damaged ") != -1) {
			%dmg = round(GetWord(%msg, 6));

			$DeusRPGHud::hp = ($DeusRPGHud::hp-%dmg);
			$FastUpdate = true;
			HUD::Update(DeusRPGhud);
			HUD::Update(DeusHPMPhud);
			Schedule("$FastUpdate = \"\";", 2.1);
		}
	}
}

Event::Attach(eventClientMessage, DeusRPGHUD::OnMessage);

$DeusRPGHUD::ScriptCheck++;