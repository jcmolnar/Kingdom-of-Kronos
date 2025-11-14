// By: Deus_ex_Machina
//
//	Autodrop 1.0

$DeusAutodropver = "1.0";

$DeusRPG::DropCheck = "0";
$DeusRPG::DropDesc[1] = "gems";
$DeusRPG::DropDesc[2] = "arrows";
$DeusRPG::DropDesc[3] = "rusty/cracked weapons";
$DeusRPG::DropDesc[4] = "gems, arrows, and rusty/cracked weapons";

function DeusRPG::Autodrop::Stop($DeusRPG::DropCheck) {
	Client::centerPrint("<jc><f0>Autodrop <f1>stopped", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);
}
function DeusRPG::Autodrop::Check($DeusRPG::DropCheck) {

	Client::centerPrint("<jc><f0>Autodrop "@$DeusRPG::DropDesc[$DeusRPG::DropCheck]@" <f1>started", 1);
	Schedule::Add("Client::centerPrint(\"\", 1);", 4);

	DeusRPG::AutoDrop::Start();
}

function DeusRPG::AutoDrop::Start() {
	if($DeusRPG::DropCheck == 1) {
		if(getItemCount("Small rock")) drop("Small rock");
		else if(getItemCount("Quartz")) drop("Quartz");
		else if(getItemCount("Granite")) drop("Granite");
		else if(getItemCount("Opal")) drop("Opal");
		else if(getItemCount("Jade")) drop("Jade");
		else if(getItemCount("Turquoise")) drop("Turquoise");
		else if(getItemCount("Ruby")) drop("Ruby");
		else if(getItemCount("Topaz")) drop("Topaz");
		else if(getItemCount("Sapphire")) drop("Sapphire");
		else if(getItemCount("Gold")) drop("Gold");
		else if(getItemCount("Emerald")) drop("Emerald");
		else if(getItemCount("Diamond")) drop("Diamond");

		Schedule::Add("DeusRPG::AutoDrop::Start();", 1.0);
	}
	else if($DeusRPG::DropCheck == 2) {
		if(getItemCount("Small Rock")) drop("Small Rock");
		else if(getItemCount("Basic Arrow")) drop("Basic Arrow");
		else if(getItemCount("Bladed Arrow")) drop("Bladed Arrow");
		else if(getItemCount("Light Quarrel")) drop("Light Quarrel");
		else if(getItemCount("Sheaf Arrow")) drop("Sheaf Arrow");
		else if(getItemCount("Heavy Quarrel")) drop("Heavy Quarrel");

		Schedule::Add("DeusRPG::AutoDrop::Start();", 1.0);
	}
	else if($DeusRPG::DropCheck == 3) {
		if(getItemCount("Rusty Knife")) drop("Rusty Knife");
		else if(getItemCount("Rusty Pick Axe")) drop("Rusty Pick Axe");
		else if(getItemCount("Cracked Club")) drop("Cracked Club");
		else if(getItemCount("Cracked Spiked Club")) drop("Cracked Spiked Club");
		else if(getItemCount("Rusty Broad Sword")) drop("Rusty Broad Sword");
		else if(getItemCount("Rusty War Axe")) drop("Rusty War Axe");
		else if(getItemCount("Rusty Short Sword")) drop("Rusty Short Sword");
		else if(getItemCount("Cracked Short Bow")) drop("Cracked Short Bow");
		else if(getItemCount("Rusty Long Sword")) drop("Rusty Long Sword");
		else if(getItemCount("Cracked Light Crossbow")) drop("Cracked Light Crossbow");

		Schedule::Add("DeusRPG::AutoDrop::Start();", 1.0);
	}
	else if($DeusRPG::DropCheck == 4) {
		if(getItemCount("Small rock")) drop("Small rock");
		else if(getItemCount("Quartz")) drop("Quartz");
		else if(getItemCount("Granite")) drop("Granite");
		else if(getItemCount("Opal")) drop("Opal");
		else if(getItemCount("Jade")) drop("Jade");
		else if(getItemCount("Turquoise")) drop("Turquoise");
		else if(getItemCount("Ruby")) drop("Ruby");
		else if(getItemCount("Topaz")) drop("Topaz");
		else if(getItemCount("Sapphire")) drop("Sapphire");
		else if(getItemCount("Gold")) drop("Gold");
		else if(getItemCount("Emerald")) drop("Emerald");
		else if(getItemCount("Diamond")) drop("Diamond");
		else if(getItemCount("Small Rock")) drop("Small Rock");
		else if(getItemCount("Basic Arrow")) drop("Basic Arrow");
		else if(getItemCount("Bladed Arrow")) drop("Bladed Arrow");
		else if(getItemCount("Light Quarrel")) drop("Light Quarrel");
		else if(getItemCount("Sheaf Arrow")) drop("Sheaf Arrow");
		else if(getItemCount("Heavy Quarrel")) drop("Heavy Quarrel");
		else if(getItemCount("Rusty Knife")) drop("Rusty Knife");
		else if(getItemCount("Rusty Pick Axe")) drop("Rusty Pick Axe");
		else if(getItemCount("Cracked Club")) drop("Cracked Club");
		else if(getItemCount("Cracked Spiked Club")) drop("Cracked Spiked Club");
		else if(getItemCount("Rusty Broad Sword")) drop("Rusty Broad Sword");
		else if(getItemCount("Rusty War Axe")) drop("Rusty War Axe");
		else if(getItemCount("Rusty Short Sword")) drop("Rusty Short Sword");
		else if(getItemCount("Cracked Short Bow")) drop("Cracked Short Bow");
		else if(getItemCount("Rusty Long Sword")) drop("Rusty Long Sword");
		else if(getItemCount("Cracked Light Crossbow")) drop("Cracked Light Crossbow");

		Schedule::Add("DeusRPG::AutoDrop::Start();", 1.0);
	}
}

$DeusRPG::ScriptCheck++;