// By: Deus_ex_Machina
//
//	BattleHUD 1.12

Include("presto\\TeamTrak.cs");
Include("presto\\Event.cs");
Include("presto\\HUD.cs");

function BattleHUD::UpdateBattleHUD() {
	if($BattleHUD::SaveTicker++ >= 10) {
		BattleHUD::Save();
		$BattleHUD::SaveTicker = 0;
	}
	if(!$BattleHUD::toggle)
		return;
	%txt1 = "<f0>Bots <f1>"@$BattleHUD::stats::Bots[$BattleHUD::stats::PlayerName];
	%txt2 = "<f0>PKs <f1>"@$BattleHUD::stats::PKs[$BattleHUD::stats::PlayerName];

	%txt3 = "\n<f0>Deaths <f1>"@$BattleHUD::stats::Killed[$BattleHUD::stats::PlayerName];
	%txt4 = "<f0>Suicides <f1>"@$BattleHUD::stats::KillSelf[$BattleHUD::stats::PlayerName];

	%txt5 = "\n<jl><f0>Spells casted <f1>"@$BattleHUD::stats::Spells[$BattleHUD::stats::PlayerName];


	if($BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName] <= 9)
		%sec = "0"@$BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName];
	else
		%sec = $BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName];
	if($BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName] <= 9)
		%min = "0"@$BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName];
	else
		%min = $BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName];

	%hour = $BattleHUD::stats::Time::H[$BattleHUD::stats::PlayerName];

	%time = "<f0>Time played <f1>"@%hour@":"@%min@":"@%sec;

	if($BattleHUD::ActionText != "" && $BattleHUD::ShowActionText == true) {
		%pos = $Hud::prefs::last::BattleHUD3;
		//%w = getWord(%pos, 1);
		//%pos1 = String::GetSubStr(%w, 0, 4);
		//%pos2 = round(String::GetSubStr(%w, 4, 5));
		//%newpos = %pos1@%pos2;
//echo("NEWPOS: "@%newpos);
		//HUD::Move(BattleHUD3, getWord(%pos, 0)@" "@%newpos@" "@getWord(%pos, 2)@" "@getWord(%pos, 3)+14);
		%newpos = 16 + 14;
		HUD::Move(BattleHUD3, getWord(%pos, 0)@" "@getWord(%pos, 1)@" "@getWord(%pos, 2)@" "@%newpos);
		%time = %time@"\n"@$BattleHUD::ActionText;
	}

	HUD::AddTextLine(BattleHUD1, %txt1@%txt3);
	HUD::AddTextLine(BattleHUD2, %txt2@%txt4);
	HUD::AddTextLine(BattleHUD3, %time);

}

$BattleHUD::toggle = false;//Do not change this!
function BattleHUD::toggleHUD() { HUD::ToggleDisplay(BattleHUD1); HUD::ToggleDisplay(BattleHUD2); HUD::ToggleDisplay(BattleHUD3); if(!$BattleHUD::toggle) $BattleHUD::toggle = true; else $BattleHUD::toggle = false;}

function BattleHUD::CheckPos() {
	if($Hud::prefs::last::BattleHUD1 == "")
		$Hud::prefs::last::BattleHUD1 = "62%-49 11%-7 80 30";
	if($Hud::prefs::last::BattleHUD2 == "")
		$Hud::prefs::last::BattleHUD2 = "62%+28 11%-7 80 30";
	if($Hud::prefs::last::BattleHUD3 == "")
		$Hud::prefs::last::BattleHUD3 = "62%-1 11%+22 157 16";
}

BattleHUD::CheckPos();

HUD::New(BattleHUD1, BattleHUD::UpdateBattleHUD, $Hud::prefs::last::BattleHUD1);
HUD::New(BattleHUD2, BattleHUD::UpdateBattleHUD, $Hud::prefs::last::BattleHUD2);
HUD::New(BattleHUD3, BattleHUD::UpdateBattleHUD, $Hud::prefs::last::BattleHUD3);

//80 30
//HUD::Move(BattleHUD1, Left Top Width Height);

function DebugBattle() {
	$Hud::prefs::last::BattleHUD1 = "62%-49 11%-7 80 30";
	$Hud::prefs::last::BattleHUD2 = "62%+28 11%-7 80 30";
	$Hud::prefs::last::BattleHUD3 = "62%-1 11%+22 157 16";
	HUD::New(BattleHUD1, BattleHUD::UpdateBattleHUD, $Hud::prefs::last::BattleHUD1);
	HUD::New(BattleHUD2, BattleHUD::UpdateBattleHUD, $Hud::prefs::last::BattleHUD2);
	HUD::New(BattleHUD3, BattleHUD::UpdateBattleHUD, $Hud::prefs::last::BattleHUD3);
}

function BattleHUD::ReSetActionHUDPos() {
	if($BattleHUD::ActionDelay != 0) {
		%pos = $Hud::prefs::last::BattleHUD3;
		HUD::Move(BattleHUD3, getWord(%pos, 0)@" "@getWord(%pos, 1)@" "@getWord(%pos, 2)@" 16");
		$BattleHUD::ActionText = "";
	}
}

function BuildClientList(%Client) {

	$BattleHUD::NameCounter = 0;
	for(%Client = 2833; %Client > 2048; %Client--) {
		if(Client::getName(%Client) != "")
			$BattleHUD::TrackClientData[$BattleHUD::NameCounter++] = Client::getName(%Client)@" "@%Client;
	}
}

function BattleHUD::OnMessage(%client, %msg) {
//echo("CLIENT MSG: \'"@%client@"\' \'"@%msg@"\'");
	if(%client)
		$lastClientMessage = %client;
	else {
	if(String::findSubStr(%msg, getWord(%msg, 0)@" has died") == 0) {
			%PKed = False;
			%name = GetWord(%msg, 0);	//echo("NAME "@%name);
			for(%i = 1; %i <= $BattleHUD::NameCounter; %i++) {
				if(GetWord($BattleHUD::TrackClientData[%i], 0) == %name) {
					if( Client::GetTeam( GetWord($BattleHUD::TrackClientData[%i], 1) ) == Client::GetTeam(getManagerId()) ) {
						%PKed = True;
						break;
					}
				}
			}
			if(%PKed) {
				$BattleHUD::stats::PKs[$BattleHUD::stats::PlayerName]++;
				$BattleHUD::ActionText = "PK'ed "@%name;
			}
			else {
				$BattleHUD::stats::Bots[$BattleHUD::stats::PlayerName]++;
				$BattleHUD::ActionText = "Killed bot "@%name;
			}
			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			if($BattleHUD::MuteKills)
				return mute;
			else
				return;
		}
		else if(String::findSubStr(%msg, "You killed yourself!") == 0) {
			$BattleHUD::stats::KillSelf[$BattleHUD::stats::PlayerName]++;
			$BattleHUD::ActionText = "You killed yourself!";

			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			if($BattleHUD::MuteSuicides)
				return mute;
			else
				return;
		}
		else if(String::findSubStr(%msg, "You were killed") == 0) {
			$BattleHUD::stats::Killed[$BattleHUD::stats::PlayerName]++;
			%name = GetWord(%msg, 4);
			if(%name != "-1")
				$BattleHUD::ActionText = GetWord(%msg, 4)@" killed you!";
			else
				$BattleHUD::ActionText = "You were killed!";

			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			if($BattleHUD::MuteDeaths)
				return mute;
			else
				return;
		}
		else if(getWords(%msg, 0, 3) == "You were "@getWord(%msg, 2)@" by") {
			//You were damaged by blah for 20 points of damage!
			%dmg = round(GetWord(%msg, 6));
			$BattleHUD::stats::DamageTaken[$BattleHUD::stats::PlayerName] += %dmg;
			$BattleHUD::ActionText = %dmg@" dmg taken!";

			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			if($BattleHUD::MuteDamageTaken)
				return mute;
			else
				return;
		}
		else if(getWords(%msg, 0, 3) == "You "@getWord(%msg, 1)@" "@getWord(%msg, 2)@" for") {
			//You damaged blah for 20 points of damage!
			%dmg = round(GetWord(%msg, 4));
			$BattleHUD::stats::DamageDone[$BattleHUD::stats::PlayerName] += %dmg;
			$BattleHUD::ActionText = %dmg@" dmg done!";

			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			if($BattleHUD::MuteDamageDone)
				return mute;
			else
				return;
		}
		else if(String::findSubStr(%msg, "Casting ") == 0) {
			$BattleHUD::stats::Spells[$BattleHUD::stats::PlayerName]++;
			$BattleHUD::ActionText = "Casting "@GetWord(%msg, 1);

			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			if($BattleHUD::MuteCastingSpells)
				return mute;
			else
				return;
		}
		else if(String::findSubStr(%msg, "You say, \"!mytime") == 0) {
			%tmpmsg = RemoveThisFromList("\"", %msg);
			%time = getTimePlayed();
			%send = GetWord(%tmpmsg, 3);
			%player = GetWord(%tmpmsg, 4);
			%color = GetWord(%tmpmsg, 5);
			if(%send == "#zone" || %send == "#global" || %send == "#anon" || %send == "#tell" || %send == "#p" || %send == "#party" || %send == "#group" || %send == "#shout" || %send == "#whisper") {
				if(%send == "#anon" || %send == "#tell") {
					if(%send == "#anon" && %player != "-1") {
						if(%color == "-1")
							%color = 1;
						%send = %send@" "@%player@" "@%color@" "@$BattleHUD::stats::PlayerName@":";
					}
					else if(%send == "#tell" && %player != "-1")
						%send = %send@" "@%player@",";
					else
						echo("Syntax error in input.");
				}
				Schedule("say(0, \""@%send@" Total: "@%time@"\");", 1.0);
			}
			else {
				%send = "";
				Schedule("say(0, \"#say Total: "@%time@"\");", 1.0);
			}

			$BattleHUD::ActionText = "Total: "@getWord(%time, 0);

			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			return;
		}
		else if(String::findSubStr(%msg, "You say, \"!mystats") == 0) {
			%tmpmsg = RemoveThisFromList("\"", %msg);
			%stats = getMyStats();
			%send = GetWord(%tmpmsg, 3);//echo(%send);
			%player = GetWord(%tmpmsg, 4);	//echo(%player);
			%color = GetWord(%tmpmsg, 5);	//echo(%color);
			if(%send == "#zone" || %send == "#global" || %send == "#anon" || %send == "#tell" || %send == "#p" || %send == "#party" || %send == "#group" || %send == "#shout" || %send == "#whisper") {
				if(%send == "#anon" || %send == "#tell") {
					if(%send == "#anon" && %player != "-1") {
						if(%color == "-1")
							%color = 1;
						%send = %send@" "@%player@" "@%color@" "@$BattleHUD::stats::PlayerName@":";
					}
					else if(%send == "#tell" && %player != "-1")
						%send = %send@" "@%player@",";
					else
						echo("Syntax error in input.");
				}
				Schedule("say(0, \""@%send@" BattleHUD Stats: "@%stats@"\");", 1.0);
			}
			else {
				%send = "";
				Schedule("say(0, \"#say BattleHUD Stats: "@%stats@"\");", 1.0);
			}
			$BattleHUD::ActionText = "Show Stats "@%send;
			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			return;
		}
		else if(String::findSubStr(%msg, "You say, \"!mydmgstats") == 0) {
			%tmpmsg = RemoveThisFromList("\"", %msg);
			%stats = getMyDMGStats();
			%send = GetWord(%tmpmsg, 3);
			%player = GetWord(%tmpmsg, 4);
			%color = GetWord(%tmpmsg, 5);
			if(%send == "#zone" || %send == "#global" || %send == "#anon" || %send == "#tell" || %send == "#p" || %send == "#party" || %send == "#group" || %send == "#shout" || %send == "#whisper") {
				if(%send == "#anon" || %send == "#tell") {
					if(%send == "#anon" && %player != "-1") {
						if(%color == "-1")
							%color = 1;
						%send = %send@" "@%player@" "@%color@" "@$BattleHUD::stats::PlayerName@":";
					}
					else if(%send == "#tell" && %player != "-1")
						%send = %send@" "@%player@",";
					else
						echo("Syntax error in input.");
				}
				Schedule("say(0, \""@%send@" BattleHUD DMGStats: "@%stats@"\");", 1.0);
			}
			else {
				%send = "";
				Schedule("say(0, \"#say BattleHUD DMGStats: "@%stats@"\");", 1.0);
			}
			$BattleHUD::ActionText = "DMGStats "@%send;
			Schedule::Cancel(BattleHUDActionText);
			Schedule::Add("BattleHUD::ReSetActionHUDPos();", $BattleHUD::ActionDelay, BattleHUDActionText);
			return;
		}
	}
	return true;
}

function BattleHUD::Time() {

	Schedule::Add("BattleHUD::Time();", 1, BattleHUD::Timer);

	if($BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName]++ >= 60) {
		$BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName]++;
	}
	if($BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName] >= 60) {
		$BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Time::H[$BattleHUD::stats::PlayerName]++;
	}

	if($BattleHUD::stats::TotalTimeS++ >= 60) {
		$BattleHUD::stats::TotalTimeS = 0;
		$BattleHUD::stats::TotalTimeM++;
	}
	if($BattleHUD::stats::TotalTimeM >= 60) {
		$BattleHUD::stats::TotalTimeM = 0;
		$BattleHUD::stats::TotalTimeH++;
	}

	HUD::Update(BattleHUD1);
	HUD::Update(BattleHUD2);
	HUD::Update(BattleHUD3);
}

function getTimePlayed() {

	if($BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName] <= 9)
		%sec = "0"@$BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName];
	else
		%sec = $BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName];
	if($BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName] <= 9)
		%min = "0"@$BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName];
	else
		%min = $BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName];

	%hour = $BattleHUD::stats::Time::H[$BattleHUD::stats::PlayerName];

	%time = %hour@":"@%min@":"@%sec;

	if($BattleHUD::stats::TotalTimeS <= 9)
		%sec = "0"@$BattleHUD::stats::TotalTimeS;
	else
		%sec = $BattleHUD::stats::TotalTimeS;
	if($BattleHUD::stats::TotalTimeM <= 9)
		%min = "0"@$BattleHUD::stats::TotalTimeM;
	else
		%min = $BattleHUD::stats::TotalTimeM;
	%hour = $BattleHUD::stats::TotalTimeH;

	return %hour@":"@%min@":"@%sec@"  MyTime: "@%time;

}
function BattleHUD::Save() {
	File::delete("config\\BattleHUDPref.cs");
	export("$BattleHUD::stats::*", "config\\BattleHUDPref.cs");
}

function getMyStats() {

	%name = $BattleHUD::stats::PlayerName;

	%stats = "Bots "@$BattleHUD::stats::Bots[%name]
	@" PKs "@$BattleHUD::stats::PKs[%name]
	@" Deaths "@$BattleHUD::stats::Killed[%name]
	@" Suicides "@$BattleHUD::stats::KillSelf[%name]
	@" Spells casted "@$BattleHUD::stats::Spells[%name];

	if($BattleHUD::stats::Time::S[%name] <= 9)
		%sec = "0"@$BattleHUD::stats::Time::S[%name];
	else
		%sec = $BattleHUD::stats::Time::S[%name];
	if($BattleHUD::stats::Time::M[%name] <= 9)
		%min = "0"@$BattleHUD::stats::Time::M[%name];
	else
		%min = $BattleHUD::stats::Time::M[%name];

	%hour = $BattleHUD::stats::Time::H[%name];

	return %stats@" Time "@%hour@":"@%min@":"@%sec;
}

function getMyDMGStats() {

	%Done = $BattleHUD::stats::DamageDone[$BattleHUD::stats::PlayerName];
	%Taken = $BattleHUD::stats::DamageTaken[$BattleHUD::stats::PlayerName];

	return "DMGDone: "@%Done@" DMGTaken: "@%Taken;
}

function BattleHUD::Init() {

	Event::Attach(eventQuit, BattleHUD::ReSetActionHUDPos); //Just in case you quit while ActionText is on.
	Event::Attach(eventExit, HUD::DeleteAll);
	Event::Attach(eventExit, BattleHUD::Save);
	Event::Attach(eventConnected, BattleHUD::Reset);

	Event::Attach(eventClientJoin, BuildClientList);
	Event::Attach(eventClientDrop, BuildClientList);
	Event::Attach(eventClientChangeTeam, BuildClientList);
	Event::Attach(eventClientTeamAdd, BuildClientList);
//
	Event::Attach(eventClientMessage, BattleHUD::OnMessage);
//
	function BattleHUD::FetchData(%BattleHUD::DataName) {
		rpgfetchdata(%BattleHUD::DataName);
		return $rpgdata[%BattleHUD::DataName];
	}
}
function BattleHUD::CheckRecords() {
	$BattleHUD::stats::PlayerName = Client::getname(getManagerId());
	if($BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName] == "" && $BattleHUD::stats::S[$BattleHUD::stats::PlayerName] == "") {
		echo("BattleHUD: New player detected! Setting default settings for "@$BattleHUD::stats::PlayerName@".");
		$BattleHUD::stats::Bots[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::PKs[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Killed[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::KillSelf[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Spells[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Time::S[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Time::M[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::Time::H[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::DamageTaken[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::DamageDone[$BattleHUD::stats::PlayerName] = 0;
		$BattleHUD::stats::HasPlayed = true;
		BattleHUD::Save();
	}
	else
		echo("BattleHUD: Loading records for "@$BattleHUD::stats::PlayerName@".");
}
function BattleHUD::Reset() {
	BattleHUD::Save();
	Schedule::Cancel(BattleHUD::Timer);
	//deleteVariables("$BattleHUD::stats::*");
	$BattleHUD::UpdateName = "";
	deleteVariables("$BattleHUD::HUD::*");
	//HUD::ToggleDisplay(PlayerHUD);
	$BattleHUD::stats::PlayerName = Client::getname(getManagerId());
	$BattleHUD::TeamCheck = 0;
	Schedule("BattleHUD::JoiningCheck();", 5);
}
function BattleHUD::JoiningCheck() {
	if($BattleHUD::TeamCheck == 0) {
		$BattleHUD::HUD::class = BattleHUD::FetchData("CLASS");
		if($BattleHUD::HUD::class == "")
			Schedule("BattleHUD::JoiningCheck();", 10);
		else if($BattleHUD::HUD::class != "") {
			Schedule::Cancel(Stats);
			$BattleHUD::stats::PlayerName = Client::getname(getManagerId());
			$BattleHUD::UpdateName = Client::getname(getManagerId());
			exec("BattleHUDPref.cs");
			BattleHUD::CheckRecords();
			BattleHUD::Time();
			$BattleHUD::HUD::lvl = BattleHUD::FetchData("LVL");
			$BattleHUD::HUD::RemortStep = BattleHUD::FetchData("RemortStep");
			$BattleHUD::HUD::class = BattleHUD::FetchData("CLASS");
			$BattleHUD::HUD::bounty = BattleHUD::FetchData("bounty");
			if($BattleHUD::HUD::bounty == "0" || $BattleHUD::HUD::bounty == "")
				$BattleHUD::HUD::bounty = 0;
			$BattleHUD::HUD::RankPoints = BattleHUD::FetchData("RankPoints");
			$BattleHUD::HUD::MyHouse = BattleHUD::FetchData("MyHouse");
			echo("BattleHUD: version "@$BattleHUDver);
			if($BattleHUD::RemortStep == 0) {
				if($BattleHUD::MyHouse == "")
					echo("Stats: Level "@$BattleHUD::HUD::lvl@" Class "@$BattleHUD::HUD::class@" with a bounty of "@$BattleHUD::HUD::bounty);
				else
					echo("Stats: Level "@$BattleHUD::HUD::lvl@" Class "@$BattleHUD::HUD::class@" Rank "@$BattleHUD::HUD::RankPoints@" in house "@$BattleHUD::HUD::MyHouse@" with a bounty of "@$BattleHUD::HUD::bounty);
			}
			else if($BattleHUD::RemortStep >= 1) {
				if($BattleHUD::MyHouse == "")
					echo("Stats: Level "@$BattleHUD::HUD::lvl@" Remort level "@$BattleHUD::HUD::RemortStep@" Class "@$BattleHUD::HUD::class@" with a bounty of "@$BattleHUD::HUD::bounty);
				else
					echo("Stats: Level "@$BattleHUD::HUD::lvl@" Remort level "@$BattleHUD::HUD::RemortStep@" Class "@$BattleHUD::HUD::class@" Rank "@$BattleHUD::HUD::RankPoints@" in house "@$BattleHUD::HUD::MyHouse@" with a bounty of "@$BattleHUD::HUD::bounty);
			}
			$BattleHUD::TeamCheck = 1;
		}
	}
}
BattleHUD::Init();
exec("BattleHUDPref.cs");