//By: Deus_ex_Machina
//
//	Auto ver 1.0

$Autover = "ver1.0";
$DeusRPG::toggleRun = true;
$DeusRPG::toggleFire = true;

function DeusRPGPack::func13() {
	if($DeusRPG::toggleRun) {
		$DeusRPG::toggleRun = false;
		postAction(2048, IDACTION_MOVEFORWARD, 1.000000);
	}
	else {
		$DeusRPG::toggleRun = true;
		postAction(2048, IDACTION_MOVEFORWARD, -0);
	}
}

function DeusRPGPack::func14() {
	if($DeusRPG::toggleFire) {
		$DeusRPG::toggleFire = false;
		postAction(2048, IDACTION_FIRE1, 1);
	}
	else {
		$DeusRPG::toggleFire = true;
		postAction(2048, IDACTION_BREAK1, 1);
	}
}

function DeusRPG::AutoSaveOnLevelUp(%client, %msg) {
	if(%client)
		$lastClientMessage = %client;
	else {
		if(String::findSubStr(%msg, "Welcome to level ") != -1) {
			say(0, "#savecharacter");
		}
	}
	return true;
}
if($SaveOnLevelUp)
	Event::Attach(eventClientMessage, DeusRPG::AutoSaveOnLevelUp);

$DeusRPG::ScriptCheck++;