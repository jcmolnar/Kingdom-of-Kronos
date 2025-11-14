//By: Deus_ex_Machina
//
//	Key Binds VER 2.4


//	If you ran DeusRPGPack 2.23+ and want to keep your old keys saved
//	Change $DeusRPGPref = "new";
//	to
//	$DeusRPGPref = "old";
//
//	And if you want to keep your old keybind for QuickCast (You will still have your old Spells saved)
//	Change $DeusQuickCastPref = "new";
//	to
//	$DeusQuickCastPref = "old";
//
//
$DeusRPGPref = "new";
//
$DeusQuickCastPref = "new";
//
//
//
//
//
//	Now go ahead and skip to -How to bind keys-
//
//
////---------------------
function DeusKeybind::Check() { if($DeusRPGPref == "new") { echo("DeusRPGPack: Setting user's new custom keybinds");
/////--------------------
//
//
//		-How to Bind keys-
//
//
//		**NOTE**
//	You can't use F5 or F10 for key binding(Tribes over writes those)
//	After you Bind keys Make sure you re-start Tribes if your in Tribes, if you don't know how to make the new keys work in-game.
//
//
//
//	And DO NOT change the # in between the [ ]
//	like you can change
//	$DeusRPGPackPrefs::DeusRPG::KeyBind[1] = "1";
//	to
//	$DeusRPGPackPrefs::DeusRPG::KeyBind[1] = "5";
//	or
//	$DeusRPGPackPrefs::DeusRPG::KeyBind[1] = "control 5";
//	DO NOT change the num between the [ ]
//
//	If you want to remove a keybind make the new bind **.
//  Ex:
//	$DeusRPGPackPrefs::DeusRPG::KeyBind[1] = "**";
//
//
//
//
//
//		-DeusHUDs Key Binds-
//
$DeusRPGPackPrefs::DeusRPG::KeyBind[1] = "1";  //DeusHP MP Hud toggle
$DeusRPGPackPrefs::DeusRPG::KeyBind[2] = "2";  //DeusRPG Hud toggle(Main HUD)
$DeusRPGPackPrefs::DeusRPG::KeyBind[3] = "3";  //DeusGroup Hud toggle
$DeusRPGPackPrefs::DeusRPG::KeyBind[4] = "4";  //DeusItem Hud toggle (Shows all your potions on you)
$DeusRPGPackPrefs::DeusRPG::KeyBind[5] = "5";  //DeusMiner Hud toggle
$DeusRPGPackPrefs::DeusRPG::KeyBind[6] = "0";  //DeusSkill Hud toggle
$DeusRPGPackPrefs::DeusRPG::KeyBind[7] = "6";  //DeusAmmo Hud toggle
$DeusRPGPackPrefs::DeusRPG::KeyBind[8] = "9";  //Deus# Hud toggle (only really good for admins)
//
//
//
//		-BestWeapon Key Binds-
//
$DeusRPGPackPrefs::DeusRPG::KeyBind[9] = "control F1";   //Pick Best Bludgeoning
$DeusRPGPackPrefs::DeusRPG::KeyBind[10] = "control F2";   //Pick Best Piercing
$DeusRPGPackPrefs::DeusRPG::KeyBind[11] = "control F3";  //Pick Best Slashing
$DeusRPGPackPrefs::DeusRPG::KeyBind[12] = "control F4";  //Pick Best Archery
//
//
//
//		-AutoRun & AutoFire Key Toggle-
//
$DeusRPGPackPrefs::DeusRPG::KeyBind[13] = "x";  //Toggle AutoRun
//
$DeusRPGPackPrefs::DeusRPG::KeyBind[14] = "f";  //Toggle AutoFire
//
//
//
//		-New Chat Menu-
//
$DeusRPGPackPrefs::DeusRPG::KeyBind[15] = "control v";  //Display Main Menu (Don't remove this bind, this is the main menu with all the training stuff, only change the bind don't remove it)
//
//	Skip down to  -Quick Cast-
//
//
//		-Newbie Guide-
$DeusRPGPackPrefs::DeusRPG::KeyBind[16] = "control down";	//Page Up
$DeusRPGPackPrefs::DeusRPG::KeyBind[17] = "control up";	//Page Down
//
//
//
//
//
//	-Skip down to -Quick Cast-
//
//
////----------------------------------------------
} else if($DeusRPGPref != "new") { echo("DeusRPGPack: Setting user's old custom keybinds"); } } DeusKeybind::Check();function DeusQuickCast::Check() { if($DeusQuickCastPref == "new") { echo("DeusRPGPack: Setting user's new custom QuickCast keybinds");
/////---------------------------------------------
//
//
//		-Quick Cast-
//
//	To add or remove keys, change the NumBinds to what you want
//	and then add/remove the lines you need. Follow the example of
//	the below settings.
//	Here you can change whats in the [ ]
//
$DeusRPGPackPrefs::QuickCast::NumBinds = 7;
//
$DeusRPGPackPrefs::QuickCast::KeyBind[1] = "control F5";   //Quick Cast Spell 1
$DeusRPGPackPrefs::QuickCast::KeyBind[2] = "control F6";   //Quick Cast Spell 2
$DeusRPGPackPrefs::QuickCast::KeyBind[3] = "control F7";   //Quick Cast Spell 3
$DeusRPGPackPrefs::QuickCast::KeyBind[4] = "control F8";   //Quick Cast Spell 4
$DeusRPGPackPrefs::QuickCast::KeyBind[5] = "control F9";   //Quick Cast Spell 5
$DeusRPGPackPrefs::QuickCast::KeyBind[6] = "control F11";   //Quick Cast Spell 6
$DeusRPGPackPrefs::QuickCast::KeyBind[7] = "control F12";   //Quick Cast Spell 7
//
//You can add as many $QuickCast as you want! (well almost)
//
//
//
//
//
//
//
// -End of user options-
//
//
//
//
//
//
//
//
//
//------------------------------
//Don't change whats below!!
//
//
//---------------------------------------------
} else if($DeusQuickCastPref != "new") { echo("DeusRPGPack: Setting user's old custom QuickCast keybinds"); } }
//---------------------------------------------
DeusQuickCast::Check();
$DeusKeyBindsver = "ver2.4";
$DeusRPGPackPrefs::DeusRPG::NumBinds = "17";
EditActionMap("playMap.sae");
for(%i = 1; %i <= $DeusRPGPackPrefs::DeusRPG::NumBinds; %i++) {
	%func = "DeusRPGPack::func"@%i@"();";
	%extra = getWord($DeusRPGPackPrefs::DeusRPG::KeyBind[%i], 0);
	if(%extra != -1) {
		%key = getWord($DeusRPGPackPrefs::DeusRPG::KeyBind[%i], 1);
		if($DeusRPGPackPrefs::DeusRPG::KeyBind[%i] == "**")
			bindCommand(keyboard0, make, %extra, %key, TO, "DeusRPGPack::Nofunc();");
		else
			bindCommand(keyboard0, make, %extra, %key, TO, %func);
	}
	else {
		if(%extra != -1) {
			%key = getWord($DeusRPGPackPrefs::DeusRPG::KeyBind[%i], 0);
			if($DeusRPGPackPrefs::DeusRPG::KeyBind[%i] == "**")
				bindCommand(keyboard0, make, %key, TO, "DeusRPGPack::Nofunc();");
			else
				bindCommand(keyboard0, make, %key, TO, %func);
		}
	}
	//echo("BIND: ["@%i@"] "@%extra@" - "@%key@" - "@%func@"");
}
function DeusRPGPack::Nofunc() { echo("DeusRPGPack: This key has no function! Keybind was set to **"); }
//DeusRPGPack::Conveter();
function DeusRPGPack::SaveSettings() {
	export("$DeusRPGPackPrefs::*", "config\\DeusRPGPackPrefs.cs");
	export("$DeusPW*", "config\\DeusPWPrefs.cs");
}
if($DeusRPGPackPrefs::PrefsVersion == "") $DeusRPGPackPrefs::PrefsVersion = "0.09";
$DeusRPGPackPrefs::PrefsVersion += "0.01";
DeusRPGPack::SaveSettings();
Event::Attach(eventExit, DeusRPGPack::SaveSettings); //If you change keybinds in-game it'll save on quit.
$DeusRPG::ScriptCheck++;