// ---------- //
// User Options //
// ---------- //


$DeusRPG::QuitOnOverWeight = true;	//If over weight it'll stop automine or weight training.
//
$MTrack::Toggle = 1;		//tracking while meditating toggle, on by default.(By Xin_ )
//
$CItem = "Blue Potion";		//Custom Item under the Steal menu.
//
$DeusPW = False;		//Set to True to Enable DeusPW (Read the read me for more info.)
//
$PrestoPref::TeamHud = "control t";	//Change to false if you don't want this on!
//
$Admin::FilterMessages = false;		//Change to true to have it on when you start up Tribes. (for admins)
//
$SaveOnLevelUp = True;		//When you level up it'll do a #savecharacter for you.
//
$DeusRPG::SkipLines = 5;  //About how many lines to skip when press up or down on the NewbieGuide
//
$NewbieGuideHeight = 400;	//NewbieGuide HUD size (shouldn't need to change this unless your screen res is really small...)
$NewbieGuideWidth = 700;
//
//
//
//	-DeusAmmoHUD-
//
//	Add/Remove arrows to show in the hud here
//
//This shows the name on the HUD
//Change it to whatever you want!
//Like you can change Small Rock to SR or anything... or remove it :P
$DeusRPGHud::Ammo[1, Desc] = "Small Rock";
$DeusRPGHud::Ammo[2, Desc] = "Basic Arrow";
$DeusRPGHud::Ammo[3, Desc] = "Sheaf Arrow";
$DeusRPGHud::Ammo[4, Desc] = "Bladed Arrow";
$DeusRPGHud::Ammo[5, Desc] = "Light Quarrel";
$DeusRPGHud::Ammo[6, Desc] = "Heavy Quarrel";
$DeusRPGHud::Ammo[7, Desc] = "Short Quarrel";
$DeusRPGHud::Ammo[8, Desc] = "Stone Feather";
$DeusRPGHud::Ammo[9, Desc] = "Metal Feather";
$DeusRPGHud::Ammo[10, Desc] = "Talon";
$DeusRPGHud::Ammo[11, Desc] = "Ceraphum's Feather";
//
//
//These here need to be the name that you see in your inv!
$DeusRPGHud::Ammo[1, Arrow] = "Small Rock";
$DeusRPGHud::Ammo[2, Arrow] = "Basic Arrow";
$DeusRPGHud::Ammo[3, Arrow] = "Sheaf Arrow";
$DeusRPGHud::Ammo[4, Arrow] = "Bladed Arrow";
$DeusRPGHud::Ammo[5, Arrow] = "Light Quarrel";
$DeusRPGHud::Ammo[6, Arrow] = "Heavy Quarrel";
$DeusRPGHud::Ammo[7, Arrow] = "Short Quarrel";
$DeusRPGHud::Ammo[8, Arrow] = "Stone Feather";
$DeusRPGHud::Ammo[9, Arrow] = "Metal Feather";
$DeusRPGHud::Ammo[10, Arrow] = "Talon";
$DeusRPGHud::Ammo[11, Arrow] = "Ceraphum's Feather";
//
//	Note that [1, Desc] = WhatEverName  [1, Arrow] = TheRealName
//	Make sure that if you add new arrows or remove arrows that the numbers for the arrows are the same
//	Like
//	$DeusRPGHud::Ammo[3, Desc] = "Sheaf Arrow";
//	$DeusRPGHud::Ammo[3, Arrow] = "Sheaf Arrow";
//
$DeusRPGHud::TotalAmmo = "11";
//
//


//
// BattleHUD options
//

$BattleHUD::ShowActionText = true;	//Displays a message on the HUD when you kill someone, hit soneone, get hit, get killed, kill your self, or cast a spell.

$BattleHUD::ActionDelay = "3";	//How long the actiontext will stay displayed on the HUD. (Set to 0 to keep the message there until a new action happens.)

//	Note, if you have the hud toggled on
// These messages show on the HUD if $BattleHUD::ShowActionText = true; . (Well, part of the message)
$BattleHUD::MuteKills = False;
//
$BattleHUD::MuteSuicides = False;
//
$BattleHUD::MuteDeaths = False;
//
$BattleHUD::MuteDamageTaken = False;
//
$BattleHUD::MuteDamageDone = False;
//
$BattleHUD::MuteCastingSpells = False;



// Read DeusKeyBinds.cs and BattleHUD\BattleHUDKeyBinds.cs to change your keysbinds.




// -------------------------- //
//-------- DO NOT EDIT ------//
$DeusRPG::ScriptCheckUser = true;//