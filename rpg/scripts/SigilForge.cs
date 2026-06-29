//====================================================================
// SigilForge.cs - "The Architect" SIGKILL Sigil vendor
//
//   Two services, both consuming SigkillSigil belt items:
//     (1) Hand sigils in for COINS   (the classic 5 -> 90M quest deal)
//     (2) Trade sigils for an endgame WEAPON (pure item-for-item barter)
//
//   WHY THE WEAPON PATH EXISTS: Tribes console math is 32-bit signed
//   int, so any coin price above 2,147,483,647 wraps negative (a 25B
//   weapon "costs" -769,803,776 -> buying PAYS the player). The weapon
//   path never computes a coin value at all -- it just counts sigils
//   and hands over the item -- so it sidesteps that ceiling entirely.
//
//   Hooked from comchat.cs via bot type "sigilforge" (mission SimGroup
//   "sigilforge1" -> NAME "The Architect"). Menu plumbing mirrors the
//   Ascension talent shop (Client::buildMenu / addMenuItem ->
//   processMenu<Key>).
//====================================================================

// ---- Coin hand-in deal (menu option 1) ----
// Keep CoinReward <= ~2.1B, and remember the player's TOTAL coin
// balance is also a 32-bit int -- it can still overflow if a player
// hoards past 2.1B. This is the pre-existing economy behavior; the
// weapon path is the overflow-proof route.
$SigilForge::CoinSigils = 5;          // sigils consumed per hand-in
$SigilForge::CoinReward = 90000000;   // coins granted
$SigilForge::ExpReward  = 5000;       // bonus exp (0 to disable)

// ---- Weapon barter deals (menu option 2) ----
// Internal item names must match the strings used elsewhere (e.g.
// $ItemCost[EchoFang]); they pass straight into GiveThisStuff.
$SigilForge::WeaponList = "EchoFang SoulReaver SkyRender";
$SigilForge[EchoFang,   Name] = "Echo Fang";    $SigilForge[EchoFang,   Cost] = 300;
$SigilForge[SoulReaver, Name] = "Soul Reaver";  $SigilForge[SoulReaver, Cost] = 300;
$SigilForge[SkyRender,  Name] = "Sky Render";   $SigilForge[SkyRender,  Cost] = 300;


// Current number of SIGKILL Sigils the player is carrying.
function SigilForge::Count(%clientId)
{
	%have = Belt::HasThisStuff(%clientId, "SigkillSigil");
	if(%have == "" || %have == -1)
		%have = 0;
	return %have;
}


// ---- Top-level menu: pick Coins or Weapons ----
function SetupSigilForge(%clientId, %botId)
{
	// Bots cannot use the vendor
	if(Player::isAiControlled(%clientId) || isRPGAI(%clientId))
		return;

	%clientId.currentSigilForge = %botId;
	%have = SigilForge::Count(%clientId);

	Client::buildMenu(%clientId, "The Architect  (Your Sigils: " @ %have @ ")", "SigilMain", true);
	Client::addMenuItem(%clientId, "Hand in " @ $SigilForge::CoinSigils @ " Sigils  ->  "
	                    @ $SigilForge::CoinReward @ " Coins", "coins");
	Client::addMenuItem(%clientId, "Forge a Weapon  (300 Sigils each)", "weapons");
	Client::addMenuItem(%clientId, "Never mind", "cancel");
}


// ---- processMenuSigilMain : coins-vs-weapons branch ----
function processMenuSigilMain(%clientId, %code)
{
	dbecho($dbechoMode, "processMenuSigilMain(" @ %clientId @ ", " @ %code @ ")");

	if(%code == "coins")
	{
		%need = $SigilForge::CoinSigils;
		%have = SigilForge::Count(%clientId);
		%clientId.sigilDealType = "coins";

		Client::buildMenu(%clientId, "Hand in " @ %need @ " Sigils for " @ $SigilForge::CoinReward @ " Coins?", "SigilConfirm", true);
		if(%have >= %need)
			Client::addMenuItem(%clientId, "YES - hand in " @ %need @ " Sigils", "confirm");
		else
			Client::sendMessage(%clientId, $MsgRed, "You only have " @ %have @ " Sigils (need " @ %need @ ").");
		Client::addMenuItem(%clientId, "NO - cancel", "cancel");
		return;
	}
	else if(%code == "weapons")
	{
		%have = SigilForge::Count(%clientId);
		Client::buildMenu(%clientId, "Forge which weapon?  (Sigils: " @ %have @ ")", "SigilWeapon", true);

		%n = GetWordCount($SigilForge::WeaponList);
		for(%i = 0; %i < %n; %i++)
		{
			%wpn  = GetWord($SigilForge::WeaponList, %i);
			%slot = %i + 1;
			$SigilForgeSlot[%clientId, %slot] = %wpn;
			Client::addMenuItem(%clientId, %slot @ ". " @ $SigilForge[%wpn, Name]
			                    @ "  (" @ $SigilForge[%wpn, Cost] @ " Sigils)", %slot);
		}
		Client::addMenuItem(%clientId, "<< Back", "cancel");
		return;
	}

	// cancel / anything else
	%clientId.currentSigilForge = "";
}


// ---- processMenuSigilWeapon : a weapon slot was picked -> confirm ----
function processMenuSigilWeapon(%clientId, %code)
{
	dbecho($dbechoMode, "processMenuSigilWeapon(" @ %clientId @ ", " @ %code @ ")");

	%wpn = $SigilForgeSlot[%clientId, %code];
	if(%wpn == "" || %wpn == -1)
	{
		%clientId.currentSigilForge = "";
		return;
	}

	%clientId.selectedSigilWeapon = %wpn;
	%clientId.sigilDealType = "weapon";
	%cost = $SigilForge[%wpn, Cost];
	%have = SigilForge::Count(%clientId);

	Client::sendMessage(%clientId, $MsgYellow, "=== " @ $SigilForge[%wpn, Name] @ " ===");
	Client::sendMessage(%clientId, $MsgWhite,  "Cost: " @ %cost @ " SIGKILL Sigils   (you have " @ %have @ ")");

	Client::buildMenu(%clientId, "Forge " @ $SigilForge[%wpn, Name] @ "?", "SigilConfirm", true);
	if(%have >= %cost)
		Client::addMenuItem(%clientId, "YES - trade " @ %cost @ " Sigils", "confirm");
	else
		Client::sendMessage(%clientId, $MsgRed, "Not enough Sigils (need " @ %cost @ ").");
	Client::addMenuItem(%clientId, "NO - cancel", "cancel");
}


// ---- processMenuSigilConfirm : final YES/NO for either deal ----
function processMenuSigilConfirm(%clientId, %code)
{
	dbecho($dbechoMode, "processMenuSigilConfirm(" @ %clientId @ ", " @ %code @ ")");

	if(%code != "confirm")
	{
		Client::sendMessage(%clientId, $MsgWhite, "The forge falls silent.");
		%clientId.selectedSigilWeapon = "";
		%clientId.sigilDealType = "";
		%clientId.currentSigilForge = "";
		return;
	}

	if(%clientId.sigilDealType == "coins")
	{
		%need = $SigilForge::CoinSigils;
		if(SigilForge::Count(%clientId) >= %need)
		{
			TakeThisStuff(%clientId, "SigkillSigil " @ %need, true);
			GiveThisStuff(%clientId, "COINS " @ $SigilForge::CoinReward @ " EXP " @ $SigilForge::ExpReward, true);
			Client::sendMessage(%clientId, $MsgGreen, "You hand over " @ %need @ " Sigils for " @ $SigilForge::CoinReward @ " Coins.");
		}
		else
			Client::sendMessage(%clientId, $MsgRed, "You no longer have enough Sigils.");
	}
	else if(%clientId.sigilDealType == "weapon")
	{
		%wpn  = %clientId.selectedSigilWeapon;
		%cost = $SigilForge[%wpn, Cost];
		if(%wpn != "" && %wpn != -1 && SigilForge::Count(%clientId) >= %cost)
		{
			TakeThisStuff(%clientId, "SigkillSigil " @ %cost, true);
			GiveThisStuff(%clientId, %wpn @ " 1", true);
			Client::sendMessage(%clientId, $MsgGreen, "The forge consumes " @ %cost @ " Sigils.  " @ $SigilForge[%wpn, Name] @ " is yours!");
			playSound(SoundSpawn2, GameBase::getPosition(Client::getOwnedObject(%clientId)));
		}
		else
			Client::sendMessage(%clientId, $MsgRed, "You no longer have enough Sigils.");
	}

	%clientId.selectedSigilWeapon = "";
	%clientId.sigilDealType = "";
	%clientId.currentSigilForge = "";
}

echo("[SIGILFORGE] SIGKILL Sigil vendor loaded - " @ GetWordCount($SigilForge::WeaponList) @ " weapons");
