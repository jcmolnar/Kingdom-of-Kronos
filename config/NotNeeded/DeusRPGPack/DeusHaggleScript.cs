//By: Deus_ex_Machina
//
//	HaggleScript ver 1.51

$DeusHagglever = "ver1.51";
$Haggle::Check = "0";
$DeusRPG::HaggleDesc[1] = "Gems";
$DeusRPG::HaggleDesc[2] = "Arrows";
$DeusRPG::HaggleDesc[3] = "Rusty/Cracked weapons";
$DeusRPG::HaggleDesc[4] = "Gems, arrows, and rusty/cracked weapons";

function Haggle::Stop($Haggle::Check) {
	Client::centerPrint("<jc><f0>Haggling <f1>stopped", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);
}
function Haggle::Misc($Haggle::Check) {

	Client::centerPrint("<jc><f0>Haggling "@$DeusRPG::HaggleDesc[$Haggle::Check]@" <f1>started", 1);
	Schedule("Client::centerPrint(\"\", 1);", 4);

	Haggle::Start();
}

function Haggle::Start() {
	if($Haggle::Check == 1) {
		if(getItemCount("Small rock")) sell("Small rock");
		else if(getItemCount("Quartz")) sell("Quartz");
		else if(getItemCount("Granite")) sell("Granite");
		else if(getItemCount("Opal")) sell("Opal");
		else if(getItemCount("Jade")) sell("Jade");
		else if(getItemCount("Turquoise")) sell("Turquoise");
		else if(getItemCount("Ruby")) sell("Ruby");
		else if(getItemCount("Topaz")) sell("Topaz");
		else if(getItemCount("Sapphire")) sell("Sapphire");
		else if(getItemCount("Gold")) sell("Gold");
		else if(getItemCount("Emerald")) sell("Emerald");
		else if(getItemCount("Diamond")) sell("Diamond");
		else  Client::centerPrint("<jc><f0>Haggling Gems <f1>done", 1);

		Schedule("Client::centerPrint(\"\", 1);", 2);
		Schedule::Add("Haggle::Start();", 1.0);
	}
	if($Haggle::Check == 2) {
		if(getItemCount("Small Rock")) sell("Small Rock");
		else if(getItemCount("Basic Arrow")) sell("Basic Arrow");
		else if(getItemCount("Bladed Arrow")) sell("Bladed Arrow");
		else if(getItemCount("Light Quarrel")) sell("Light Quarrel");
		else if(getItemCount("Sheaf Arrow")) sell("Sheaf Arrow");
		else if(getItemCount("Heavy Quarrel")) sell("Heavy Quarrel");
		else Client::centerPrint("<jc><f0>Haggling Arrows <f1>done", 1);

		Schedule("Client::centerPrint(\"\", 1);", 2);
		Schedule::Add("Haggle::Start();", 1.0);
	}
	if($Haggle::Check == 3) {
		if(getItemCount("Rusty Knife")) sell("Rusty Knife");
		else if(getItemCount("Rusty Pick Axe")) sell("Rusty Pick Axe");
		else if(getItemCount("Cracked Spiked Club")) sell("Cracked Spiked Club");
		else if(getItemCount("Rusty Broad Sword")) sell("Rusty Broad Sword");
		else if(getItemCount("Rusty War Axe")) sell("Rusty War Axe");
		else if(getItemCount("Rusty Short Sword")) sell("Rusty Short Sword");
		else if(getItemCount("Cracked Short Bow")) sell("Cracked Short Bow");
		else if(getItemCount("Rusty Long Sword")) sell("Rusty Long Sword");
		else if(getItemCount("Cracked Light Crossbow")) sell("Cracked Light Crossbow");
		else Client::centerPrint("<jc><f0>Haggling Rusty weapons <f1>done", 1);

		Schedule("Client::centerPrint(\"\", 1);", 2);
		Schedule::Add("Haggle::Start();", 1.0);
	}
	if($Haggle::Check == 4) {
		if(getItemCount("Small rock")) sell("Small rock");
		else if(getItemCount("Quartz")) sell("Quartz");
		else if(getItemCount("Granite")) sell("Granite");
		else if(getItemCount("Opal")) sell("Opal");
		else if(getItemCount("Jade")) sell("Jade");
		else if(getItemCount("Turquoise")) sell("Turquoise");
		else if(getItemCount("Ruby")) sell("Ruby");
		else if(getItemCount("Topaz")) sell("Topaz");
		else if(getItemCount("Sapphire")) sell("Sapphire");
		else if(getItemCount("Gold")) sell("Gold");
		else if(getItemCount("Emerald")) sell("Emerald");
		else if(getItemCount("Diamond")) sell("Diamond");
		else if(getItemCount("Small Rock")) sell("Small Rock");
		else if(getItemCount("Basic Arrow")) sell("Basic Arrow");
		else if(getItemCount("Bladed Arrow")) sell("Bladed Arrow");
		else if(getItemCount("Light Quarrel")) sell("Light Quarrel");
		else if(getItemCount("Sheaf Arrow")) sell("Sheaf Arrow");
		else if(getItemCount("Heavy Quarrel")) sell("Heavy Quarrel");
		else if(getItemCount("Rusty Knife")) sell("Rusty Knife");
		else if(getItemCount("Rusty Pick Axe")) sell("Rusty Pick Axe");
		else if(getItemCount("Cracked Spiked Club")) sell("Cracked Spiked Club");
		else if(getItemCount("Rusty Broad Sword")) sell("Rusty Broad Sword");
		else if(getItemCount("Rusty War Axe")) sell("Rusty War Axe");
		else if(getItemCount("Rusty Short Sword")) sell("Rusty Short Sword");
		else if(getItemCount("Cracked Short Bow")) sell("Cracked Short Bow");
		else if(getItemCount("Rusty Long Sword")) sell("Rusty Long Sword");
		else if(getItemCount("Cracked Light Crossbow")) sell("Cracked Light Crossbow");
		else Client::centerPrint("<jc><f0>Haggling all Gems, arrows, and rustys <f1>done", 1);

		Schedule("Client::centerPrint(\"\", 1);", 2);
		Schedule("Haggle::Start();", 1.0);
	}
	else if($Haggle::Check == 6) {
		%coins = DeusRPG::FetchData("COINS");
		%cost = DeusRPG::FetchData("getbuycost BluePotion");
		if(%coins >= %cost) {//$rpgdata["getbuycost Blue Potion"]
			buy("Blue Potion");
		}
		else {
			Client::centerPrint("<jc><f0>You need "@%cost - %coins@" coins to buy a Blue Potion!", 1);
			Schedule("Client::centerPrint(\"\", 1);", 4);
		}
		Schedule("Haggle::Start();", $PotionDelay);
	}
	else if($Haggle::Check == 7) {
		%coins = DeusRPG::FetchData("COINS");
		%cost = DeusRPG::FetchData("getbuycost CrystalBluePotion");
		if(%coins >= %cost) { //$rpgdata["getbuycost Crystal Blue Potion"]
			buy("Crystal Blue Potion");
		}
		else {
			Client::centerPrint("<jc><f0>You need "@%cost - %coins@" coins to buy a Crystal Blue Potion!", 1);
			Schedule("Client::centerPrint(\"\", 1);", 4);
		}
		Schedule("Haggle::Start();", $PotionDelay);
	}
	else if($Haggle::Check == 8) {
		%coins = DeusRPG::FetchData("COINS");
		%cost = DeusRPG::FetchData("getbuycost EnergyVial");
		if(%coins >= %cost) {//$rpgdata["getbuycost Energy Vial"]
			buy("Energy Vial");
		}
		else {
			Client::centerPrint("<jc><f0>You need "@%cost - %coins@" coins to buy a Energy Vial!", 1);
			Schedule("Client::centerPrint(\"\", 1);", 4);
		}
		Schedule("Haggle::Start();", $PotionDelay);
	}
	else if($Haggle::Check == 9) {
		%coins = DeusRPG::FetchData("COINS");
		%cost = DeusRPG::FetchData("getbuycost CrystalEnergyVial");
		if(%coins >= %cost) {//$rpgdata["getbuycost Crystal Energy Vial"]
			buy("Crystal Energy Vial");
		}
		else {
			Client::centerPrint("<jc><f0>You need "@%cost - %coins@" coins to buy a Crystal Energy Vial!", 1);
			Schedule("Client::centerPrint(\"\", 1);", 4);
		}
		Schedule("Haggle::Start();", $PotionDelay);
	}
}

$DeusRPG::ScriptCheck++;