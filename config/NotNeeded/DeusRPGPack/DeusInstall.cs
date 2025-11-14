// By: Deus_ex_Machina
//
//	Install 1.54

//
//
//
//
//
// Check if this user is running -mod RPG
for(%i = 1; $cargv[%i] != ""; %i++) {
	if($cargv[%i] == "-mod") {
		%mod = $cargv[%i + 1];
		if(%mod == RPG) {
			$Check::Mod = %mod;
			break;
		}
	}
}
if($Check::Mod == RPG)
	echo(">> Mod "@$Check::Mod@" found!");
else {
	echo("DeusRPG: WARNING!! YOU ARE NOT RUNNING -mod RPG !! You may get errors if you don't run Tribes.exe -mod RPG");
	$SpamUser = true; // >=p
}

function NoRPGMod() {
	echo("DeusRPG: WARNING!! YOU ARE NOT RUNNING -mod RPG !! You may get errors if you don't run Tribes.exe -mod RPG");
}

if($Server::HostPublicGame == true)
	echo("DesuRPG: Warning! If host public game is on you may get fetchdata(); errors if you're not hosting!");


//====================================
// JeremyIron's functions
// round(); FixDecimals(); Cap(); String::replace(); String::NEWgetSubStr();
//
function round(%n) {
	if(%n < 0)
	{
		%t = -1;
		%n = -%n;
	}
	else if(%n >= 0)
		%t = 1;

	%f = floor(%n);
	%a = %n - %f;

	if(%a < 0.5)
		%b = 0;
	else if(%a >= 0.5)
		%b = 1;

	return (%f + %b) * %t;
}
function FixDecimals(%c) {
	%d = round(%c * 10);
	%m = (%d / 10) * 1.000001;
	return %m;
}
function Cap(%n, %lb, %ub) {
	if(%lb != "inf") {
		if(%n < %lb)
			%n = %lb;
	}
	if(%ub != "inf") {
		if(%n > %ub)
			%n = %ub;
	}
	return %n;
}
function String::replace(%string, %search, %replace)
{
	%loc = String::findSubStr(%string, %search);

	if(%loc != -1)
	{
		%ls = String::len(%search);

		%part1 = String::NEWgetSubStr(%string, 0, %loc);
		%part2 = String::NEWgetSubStr(%string, %loc + %ls, 99999);

		%string = %part1 @ %replace @ %part2;
	}

	return %string;
}
function String::NEWgetSubStr(%s, %x, %y) {

	%len = %y;
	%chunks = floor(%len / 255) + 1;

	%q = %len;
	%nx = %x;
	%final = "";

	for(%i = 1; %i <= %chunks; %i++)
	{
		%q = %q - 255;
		if(%q <= 0)
			%chunkLen = %q+255;
		else
			%chunkLen = 255;

		%final = %final @ String::getSubStr(%s, %nx, %chunkLen);
		%nx = %nx + %chunkLen;
	}

	return %final;
}
//
//====================================

//Start Install
//
//
Include("DeusRPGPackPrefs.cs");
Include("QuickCastPrefs.cs");
Include("DeusRPGPackKeyBindPrefs.cs");
Include("DeusRPGPrefs.cs");
Include("DeusPWPrefs.cs");
Include("DeusRPGPack\\UserOptions.cs");
	echo(">> DeusInstall Loading...");
	$error = "";
	$DeusRPG::ScriptCheck = 0;
	exec("DeusRPGPack\\DeusKeyBinds.cs");
	exec("DeusRPGPack\\DeusConversion.cs");
	Include("DeusRPGPack\\DeusSmithCombos.cs");
	Include("DeusRPGPack\\DeuschatBind.cs");
	Include("DeusRPGPack\\DeusAutodrop.cs");
	Include("DeusRPGPack\\DeusBestWeapon.cs");
	Include("DeusRPGPack\\DeusHaggleScript.cs");
	Include("DeusRPGPack\\DeusHUD\\DeusRPGhud.cs");
	Include("DeusRPGPack\\DeusSkillTraining.cs");
	Include("DeusRPGPack\\DeusNewbieGuide.cs");
	Include("DeusRPGPack\\DeusPW.cs");
	Include("DeusRPGPack\\DeusQuickCast.cs");
	Include("DeusRPGPack\\DeusAuto.cs");
	echo(">> DeusRPGPref ver"@FixDecimals($DeusRPGPackPrefs::PrefsVersion));
	DeusRPGPack::SaveSettings();
	if($DeusRPG::ScriptCheck == 12)
		echo(">> DeusInstall done! No errors found!");
	else {
		$error = true;
		echo(">> DeusInstall done! But with errors!");
	}
	if(!$DeusRPG::ScriptCheckUser)
		echo(">> Error in UserOptions.cs!");
	if($SpamUser == true)
		echo("DeusRPG: WARNING!! YOU ARE NOT RUNNING -mod RPG !! You may get errors if you don't run Tribes.exe -mod RPG");

//===
function DeusRPG::FetchData(%dataname) {
	rpgfetchdata(%dataname);
	return $rpgdata[%dataname];
}
function DeusRPG::FetchDataTemp(%dataname) {
	rpgfetchdata(%dataname);
	return $rpgdata[%dataname];
}

function Between(%l, %h) {

	if(%l == "" || %h == "" || %l > %h) {
		echo("Between(LowNum, HighNum);");
		return;
	}
	%perc = floor(getRandom() * 100);

	%lh = %h - %l;

	%num = Cap(round((%lh * (%perc/100)) + %l), %l, %h);

	return %num;
}

function strlen(%string)  {
	for(%len = 0; String::getSubStr(%string, %len, 1) != ""; %len++) {}
	return %len;
}

function getWords(%list, %start, %end) {

	for(%i = %start; %i <= %end; %i++)
		%words = %words @ GetWord(%list, %i)@" ";

	return strtrim(%words);
}

function String::left(%string, %len) {
    if(%len >= String::len(%string))
        return %string;

    %left = String::getSubStr(%string, 0, %len);
    return %left;
}
function String::right(%string, %len) {
    if(%len >= String::len(%string))
        return %string;

    %idx = String::len(%string) - %len;
    %right = String::getSubStr(%string, %idx, %len);
    return %right;
}
function String::starts(%string, %search) {
    %idx = String::len(%search);
    if(%idx > String::len(%string))
        return false;

    if(String::left(%string, %idx) == %search)
        return true;
    else
        return false;
}
function String::ends(%string, %search) {
    %idx = String::len(%search);
    if(%idx > String::len(%string))
        return false;

    if(String::right(%string, %idx) == %search)
        return true;
    else
        return false;
}
function strtrim(%string) {

    while(String::starts(%string, " ")) {
        %string = String::right(%string, String::len(%string) - 1);
    }
    while(String::ends(%string, " ")) {
        %string = String::left(%string, String::len(%string) - 1);
    }
    return %string;
}

function RemoveThisFromList(%char, %list, %replace) { //This func can slow you down a bit on big lists

	if(%char == "") {
		echo("RemoveThisFromList(char, list, replace);");
		return;
	}

	if(%replace == "")
		%replace = " ";
	while(String::findSubStr(%list, %char) != -1) {
		%list = String::replace(%list, %char, %replace);
	}
	return %list;
}

//DoStuff(Options, SettingNewPW, true, Schedule, false, 30);
//			  1				2 			3		4			5	6
function DoStuff(%a1,%a2,%a3,%a4,%a5,%a6,%a7) {

	if(%a1 == "" || %a2 == "") {
		echo("DoStuff(arg1,...arg7);");
		return;
	}
	if(%a1 == Options) {
		Schedule("$DeusRPG::Options["@%a2@"] = "@%a3@";", 1);
		if(%a4 != "") {
			%string = ""@%a4@"(\"$DeusRPG::Options["@%a2@"]\");";
			if(%a5 != "") {
				%string = ""@%a4@"(\"$DeusRPG::Options["@%a2@"] = "@%a5@";\");";
				if(%a6 != "") {
					%string = ""@%a4@"(\"$DeusRPG::Options["@%a2@"] = "@%a5@";\", "@%a6@");";
				}
			}
		}
		if(%string != "") {
			Eval(%string);
			if(%a7 != "")
				Eval(%a7);
		return;
		}
	}
	%string = ""@%a1@"();";
	if(%a2 != "") {
		%string = ""@%a1@"("@%a2@");";
		if(%a3 != "") {
			%string = ""@%a1@"("@%a2@", "@%a3@");";
			if(%a4 != "") {
				%string = ""@%a1@"("@%a2@", "@%a3@", "@%a4@");";
			}
		}
	}
	if(%string != "")
		Eval(%string);
}


//===
//Force Install
function FSpam() {
	echo(">> Forceing DeusInstall...");
	$error = "";
	DeusRPG::FindPresto();
	$DeusRPG::ScriptCheck = 0;
	$DeusRPG::ScriptCheckUser = "";
	exec("DeusRPGPack\\DeusConversion.cs");
	exec("DeusRPGPack\\UserOptions.cs");
	exec("DeusRPGPack\\DeusKeyBinds.cs");
	exec("DeusRPGPack\\DeuschatBind.cs");
	exec("DeusRPGPack\\DeusAutodrop.cs");
	exec("DeusRPGPack\\DeusBestWeapon.cs");
	exec("DeusRPGPack\\DeusHaggleScript.cs");
	exec("DeusRPGPack\\DeusSmithCombos.cs");
	exec("DeusRPGPack\\DeusHUD\\DeusRPGhud.cs");
	exec("DeusRPGPack\\DeusSkillTraining.cs");
	exec("DeusRPGPack\\DeusNewbieGuide.cs");
	exec("DeusRPGPack\\DeusPW.cs");
	exec("DeusRPGPack\\DeusQuickCast.cs");
	exec("DeusRPGPack\\DeusAuto.cs");
	echo(">> DeusRPGPref ver"@FixDecimals($DeusRPGPackPrefs::PrefsVersion));
	DeusRPGPack::SaveSettings();
	if($DeusRPG::ScriptCheck == 12)
		echo(">> Force DeusInstall done! No errors found!");
	else {
		$error = true;
		echo(">> Force DeusInstall done! But with errors!");
	}
	if(!$DeusRPG::ScriptCheckUser)
		echo(">> Error in UserOptions.cs!");

	if($Server::HostPublicGame == true)
		echo("DesuRPG: Warning! If host public game is on you may get fetchdata(); errors if your not hosting!");
	if($SpamUser == true)
		echo("DeusRPG: WARNING!! YOU ARE NOT RUNNING -mod RPG !! You may get errors if you don't run Tribes.exe -mod RPG");
}

Event::Attach(eventExit, HUD::DeleteAll, hud);
//
//End Install
$RPGPackver = "2.274";
$DeusRPGPackPrefs::DeusRPGPackVersion = $RPGPackver;
//$PrestoPrefs::ShowPackStatus = false;
$DeusRPGFoundPresto = false;
function DeusRPG::FindPresto() {
	if($Presto::version >= "0.933") {
		echo(">> Presto found.");
		$DeusRPGFoundPresto = true;
	}
	else {
		echo("DeusRPGPack: NO PRESTO PACK FOUND! MAKE SURE YOU EXEC PRESTO BEFORE THIS SCRIPT IF YOU HAVE PRESTO VERSION 0.933 OR HIGHER");
		$DeusRPGFoundPresto = false;
	}
}
for(%i = 1; $cargv[%i] != ""; %i++) {
	if($cargv[%i] == "-mod") {
		if($cargv[%i + 1] == RMRPG) {
			$PrestoPrefs::ShowPackStatus = false; //=p
			$RMRPG = true;
			break;
		}
	}
}
DeusRPG::FindPresto();
if($SpamUser == true) {
	echo("DeusRPG: WARNING!! YOU ARE NOT RUNNING -mod RPG !! You may get errors if you don't run Tribes.exe -mod RPG");
	Event::Attach(eventConnected, NoRPGMod);
}
$DeusInstallLoaded = true;
Presto::AddScriptBanner(SpamBanner, "\n<f2><jc>DeusRPG Pack\n\n<f1>version <f2>"@$RPGPackver@"\n\n<f0>Done by:\n<f1>Deus_ex_Machina\n<f1>Shots\n\n");