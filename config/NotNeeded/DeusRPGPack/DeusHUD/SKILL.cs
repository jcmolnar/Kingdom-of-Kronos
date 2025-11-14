//By : Deus_ex_Machina
//
//	RPGHUDs ver 2.561

function DeusRPGHud::UpdateDeusSKILLhud(%hud) {

	DeusRPGHud::GetSkillRpgData();

	for(%i = 1; %i <= $DeusRPGHud::SkillCount; %i++)
	{
		if(%i != 16) // temporary fix until the fix skills in TRPG
		{
			if($DeusRPGHud::Skill[%i, Amount] > 700)
				HUD::AddTextLine(DeusSKILLhud, "<f0>" @ $DeusRPGHud::Skill[%i, Desc] @ ": <f1>" @ $DeusRPGHud::Skill[%i, Amount]);
			else
				HUD::AddTextLine(DeusSKILLhud, "<f0>" @ $DeusRPGHud::Skill[%i, Desc] @ ": <f2>" @ $DeusRPGHud::Skill[%i, Amount]);
		}
	}

	return ($DeusRPGHud::CB5delay+5);
}

function DeusRPGHud::UpdateDeusNhud(%hud) {
	for(%i = 1; %i <= 22; %i++)
		if(%i != 16)
			HUD::AddTextLine(DeusNhud, "<f2>"@%i@"");
}


HUD::New(DeusSKILLhud, DeusRPGHud::UpdateDeusSKILLhud, $Hud::prefs::last::DeusSKILLhud);
HUD::New(DeusNhud, DeusRPGHud::UpdateDeusNhud, $Hud::prefs::last::DeusNhud);

$DeusRPGHUD::ScriptCheck++;