//==============================================================================
// VirtualSlots.cs - Phase D: belt weapons in the VANILLA stock inventory screen
//==============================================================================
// Belt weapons (BeltWeapons.cs) already work everywhere the mod's own UI reaches
// (#weapon, the KronosHUD panel, the belt menus). This layer is the OPTIONAL
// "native view": it makes belt weapons appear in the STOCK client's inventory
// screen with click-to-equip, for players who don't run the HUD.
//
// HOW: reserve K real ItemData "VSlots" (VSlot0..VSlot{K-1}) that are otherwise
// inert (never mounted, never ground-dropped). For each connected client we
// rewrite the VSlot rows to name THAT client's belt weapons and push the rewrite
// to them alone via kronos_virtualitems.dll (vslotPushItem = the engine's own
// per-client DataBlockEvent path, decomp-verified). The client caches the row on
// receipt, so two players can see different names in the same VSlot index. A
// click on a VSlot row (remoteUseItem) equips the mapped belt weapon.
//
//------------------------------------------------------------------------------
// GATED OFF: $pref::VSlotsEnabled (default false). With the pref off this file
// registers nothing and every entry point early-returns -> completely inert.
//
// ENABLE ONLY AFTER BOTH:
//   (1) DLL go/no-go passes - a re-pushed ItemData renames live in the stock
//       inventory with no client disconnect (see kronos_virtualitems.txt).
//   (2) boot [SLOT AUDIT] (Server.cs) shows >= $VSlot::Count free ItemData slots
//       (the VSlot0..VSlot{K-1} placeholders below are declared unconditionally).
//
// WHICH FIELD THE STOCK INVENTORY RENDERS - CONFIRMED (TribesSource
// fearGuiInventory.cpp:323, FearGuiInventory::onPreRender):
//     addCell(data->typeString, data->description, itemCount(j), j, true)
// The row LABEL is data->description; the category header is data->typeString
// (the datablock "heading" field). So VSlot::SetRow sets description, and the
// placeholders carry heading "bWeapons" to group under the same header as every
// other mod weapon. (shownName is not a real ItemData field - removed.)
//==============================================================================

//------------------------------------------------------------------------------
// Reserved placeholder ItemData: VSlot0..VSlot7. ALWAYS declared - they cost 8 of
// the 256 ItemData slots (funded by the 9 freed in Phase B), reserved whether or
// not the feature is on. INERT until enabled: never mounted (no imageType), never
// sold (absent from every merchant stock, so FearGuiPurchase never lists them),
// and only ever given count>0 by VSlot::Sync, which no-ops unless
// $pref::VSlotsEnabled. Feature off => no player ever carries one => invisible to
// vanilla and HUD clients alike. Must be declared before preloadServerDataBlocks
// (Server.cs execs VirtualSlots before that call).
//------------------------------------------------------------------------------
ItemData VSlot0 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot1 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot2 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot3 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot4 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot5 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot6 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlot7 { heading = "bWeapons"; description = "Backpack Slot"; showInventory = true; showWeaponBar = false; price = 0; };

//------------------------------------------------------------------------------
// VOID Phase 1b (2026-07-14): ARMOR window (20 rows) - the stock-GUI view of the
// belt "Armor" carried list (armor converted to belt in Phase 1a). heading
// "aArmor" groups them under the same header engine armor used. Same inert-
// until-synced contract as the weapon rows above.
//------------------------------------------------------------------------------
ItemData VSlotArmor0  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor1  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor2  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor3  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor4  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor5  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor6  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor7  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor8  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor9  { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor10 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor11 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor12 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor13 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor14 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor15 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor16 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor17 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor18 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotArmor19 { heading = "aArmor"; description = "Armor Slot"; showInventory = true; showWeaponBar = false; price = 0; };

//------------------------------------------------------------------------------
// VOID Phase 1b: STORAGE window (20 rows) - reserved for the banker deposit/
// withdraw view (wired in the bank feature pass). Declared NOW to lock their
// low indices (< 128, the shop/bank bitfield-safe region). Never synced yet:
// count stays 0 on every player, so they are invisible everywhere.
//------------------------------------------------------------------------------
ItemData VSlotBank0  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank1  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank2  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank3  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank4  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank5  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank6  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank7  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank8  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank9  { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank10 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank11 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank12 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank13 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank14 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank15 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank16 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank17 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank18 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };
ItemData VSlotBank19 { heading = "eMiscellany"; description = "Storage Slot"; showInventory = true; showWeaponBar = false; price = 0; };

$VSlot::Count  = 8;			// WEAPONS window rows (stays 8: with the 40 new rows the
							// count-bearing region tops out at index ~197 of the 200
							// cap - weapon rows grow when the accessory pass frees room)
$VSlot::ACount = 20;		// ARMOR window rows (stock-GUI view of belt "Armor" list)
$VSlot::SCount = 20;		// STORAGE window rows (bank view; declared, not yet wired)

//------------------------------------------------------------------------------
// Resolve the K placeholder ItemData to their engine indices. Called once at
// boot (from Server.cs, after all ItemData scripts have exec'd). Disables the
// whole feature if any placeholder is missing, so a half-set never half-works.
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
// Server-safe "datablock NAME -> ItemData type index". getItemType() (the stock
// console fn) resolves a name via cg.dbm - the CLIENT datablock manager - which is
// NULL on a dedicated / native host (no client game), so calling it server-side
// crashed at boot: 0xC0000005 in getItemDescriptionType (FearPlugin.cpp:1425).
// getNumItems()/getItemData() read the SERVER dbm (wg->dbm, bounds-guarded - the
// same path the boot [SLOT AUDIT] uses), so they are safe here. getItemData also
// returns the NAME, which is what we want (getItemType matched on .description, and
// all placeholders share "Backpack Slot", so it could never tell them apart).
//------------------------------------------------------------------------------
function VSlot::NameToType(%name)
{
	%count = getNumItems();
	for(%i = 0; %i < %count; %i++)
		if(getItemData(%i) == %name)		// == strcmps non-numeric operands; $= is a syntax error on this engine
			return %i;
	return -1;
}

function VSlot::Init()
{
	if(!$pref::VSlotsEnabled)
		return;

	for(%i = 0; %i < $VSlot::Count; %i++)
	{
		%idx = VSlot::NameToType("VSlot" @ %i);	// server-safe; getItemType() derefs the CLIENT dbm -> boot crash on a dedicated host
		if(%idx == -1)
		{
			echo("[VSLOT] VSlot" @ %i @ " ItemData not registered - disabling Virtual Slots");
			$pref::VSlotsEnabled = false;
			return;
		}
		$VSlot::Index[%i] = %idx;
		$VSlot::IsSlot[%idx] = true;		// reverse map for the click dispatch
		$VSlot::NameToIdx["VSlot" @ %i] = %idx;	// name->idx for the click dispatch (runtime getItemType would crash too)
	}

	// VOID Phase 1b: ARMOR window (VSlotArmor0..{ACount-1})
	for(%i = 0; %i < $VSlot::ACount; %i++)
	{
		%idx = VSlot::NameToType("VSlotArmor" @ %i);
		if(%idx == -1)
		{
			echo("[VSLOT] VSlotArmor" @ %i @ " ItemData not registered - disabling Virtual Slots");
			$pref::VSlotsEnabled = false;
			return;
		}
		$VSlot::AIndex[%i] = %idx;
		$VSlot::IsSlot[%idx] = true;
		$VSlot::NameToIdx["VSlotArmor" @ %i] = %idx;
	}

	// VOID Phase 1b: STORAGE window (VSlotBank0..{SCount-1}) - indices locked now,
	// rows stay count-0 until the bank feature wires them.
	for(%i = 0; %i < $VSlot::SCount; %i++)
	{
		%idx = VSlot::NameToType("VSlotBank" @ %i);
		if(%idx == -1)
		{
			echo("[VSLOT] VSlotBank" @ %i @ " ItemData not registered - disabling Virtual Slots");
			$pref::VSlotsEnabled = false;
			return;
		}
		$VSlot::BIndex[%i] = %idx;
		$VSlot::IsSlot[%idx] = true;
		$VSlot::NameToIdx["VSlotBank" @ %i] = %idx;
	}

	// VSlots are declared FIRST (Server.cs execs VirtualSlots before every other ItemData
	// script), so they land at LOW indices - well under the 200 per-player item-count cap
	// (retail Tribes 1.40 MaxItemTypes). Player::setItemCount writes them in-bounds on every
	// binary, so no per-binary gate is needed. (Above the cap, setItemCount has NO bounds
	// check and OOB-corrupts memory - which is exactly what blocked the old 233..240 spot.)

	// The per-client push is a DLL command (kronos_virtualitems.dll). vslotDbm()
	// reports the resolved DataBlockManager; a healthy load contains "dbm=".
	%probe = vslotDbm();
	if(String::findSubStr(%probe, "dbm=") == -1)
	{
		echo("[VSLOT] kronos_virtualitems.dll not loaded (vslotDbm -> '" @ %probe @ "') - disabling Virtual Slots");
		$pref::VSlotsEnabled = false;
		return;
	}

	echo("[VSLOT] windows ready: weapons " @ $VSlot::Count @ " (" @ $VSlot::Index[0] @ ".." @ $VSlot::Index[$VSlot::Count - 1] @ "), armor " @ $VSlot::ACount @ " (" @ $VSlot::AIndex[0] @ ".." @ $VSlot::AIndex[$VSlot::ACount - 1] @ "), storage " @ $VSlot::SCount @ " (" @ $VSlot::BIndex[0] @ ".." @ $VSlot::BIndex[$VSlot::SCount - 1] @ "); DLL ok: " @ %probe);
}

//------------------------------------------------------------------------------
// Rewrite one shared VSlot ItemData's display fields to name a belt weapon.
// Serialized per client by Sync (below): set fields -> push to that one client
// -> the client caches the row, so a later rewrite for another client can't
// retroactively change what the first client already received.
//
// description is THE field the stock inventory renders (fearGuiInventory.cpp:323,
// confirmed) - so that is all we set. price is cosmetic here (VSlots are never in
// a shop; the stock inventory list doesn't show price, only the purchase screen
// does, which never lists VSlots) but is kept in sync for tidiness.
//------------------------------------------------------------------------------
function VSlot::SetRow(%idx, %displayName, %price, %heading)
{
	%db = getItemData(%idx);			// datablock name, e.g. "VSlot3"
	if(%db == "" || %db == -1)
		return;
	%db.description = %displayName;		// the row label the stock inventory renders
	%db.price       = %price;
	// VOID BANK 2026-07-14: the bank/shop screens GROUP by the heading field -
	// a fixed heading dumped every stored item under Miscellany. Optional: only
	// set when the caller passes one (weapon/armor windows keep their declared
	// headings).
	if(%heading != "")
		%db.heading = %heading;
}

// belt category -> stock-GUI section heading (matches the engine items' headings)
function VSlot::CategoryHeading(%cat)
{
	if(%cat == "Armor")
		return "aArmor";
	if(%cat == "Weapons")
		return "bWeapons";
	return "eMiscellany";
}

//------------------------------------------------------------------------------
// Map this client's belt weapons onto their VSlots and push the renamed rows.
// Call on: login/spawn, any belt-weapon list change (buy/sell/give/drop/equip),
// and mission load (datablocks are re-sent to every client on mission change,
// so the rows must be re-pushed). Belt weapons past K are still reachable via
// #weapon / the HUD.
//------------------------------------------------------------------------------
function VSlot::Sync(%clientId)
{
	if(!$pref::VSlotsEnabled)
		return;
	if(%clientId == "" || %clientId == -1)
		return;
	// VSlots are the VANILLA-client native view. HUD clients already see belt
	// weapons in the KronosHUD belt panel (KronosShop_PushInv lists them as "b"
	// rows), so also pushing VSlot "d" rows would double-show every weapon. Bots
	// have no client connection - a push to their managerId is dropped anyway.
	if(%clientId.hasKronosHUD)
		return;
	if(isRPGAI(%clientId))
		return;
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "")
		return;

	// "Weapons" belt list, GetNS format: "count item1 item2 ..."
	%nf = Belt::GetNS(%clientId, "Weapons");
	%n  = GetWord(%nf, 0);

	for(%i = 0; %i < $VSlot::Count; %i++)
	{
		%idx  = $VSlot::Index[%i];	// numeric engine index (DLL push + setItemCount; the NAME
									// path misses sg.dbm's name->index map, playerInventory.cpp:513)
		%item = "";
		if(%i < %n)
			%item = GetWord(%nf, %i + 1);		// words 1..n
		if($BeltItem[%item, "Item"] != "")
			%item = $BeltItem[%item, "Item"];	// display-name alias -> registered name

		if(%item != "" && %item != -1 && $BeltWeapon[%item, "Shell"] != "")
		{
			$VSlot::Map[%clientId, %idx] = %item;	// remember for the click dispatch
			%name = $BeltItem[%item, "Name"];
			if(%name == "")
				%name = %item;
			VSlot::SetRow(%idx, %name, $ItemCost[%item]);
			vslotPushItem(%clientId, %idx);				// DLL: per-client row rewrite (by index)
			// VOID 1b: row count mirrors the real belt count (stacks show as x2 etc)
			%cnt = Belt::ItemCount(%item, fetchData(%clientId, "Weapons"));
			if(%cnt < 1) %cnt = 1;
			Player::setItemCount(%clientId, %idx, %cnt);	// make the row appear
		}
		else
		{
			$VSlot::Map[%clientId, %idx] = "";
			Player::setItemCount(%clientId, %idx, 0);	// hide the row
		}
	}

	// VOID Phase 1b: ARMOR window - same contract, driven by the belt "Armor"
	// carried list. The equipped piece is tagged "(worn)" in its row label.
	%nf = Belt::GetNS(%clientId, "Armor");
	%n  = GetWord(%nf, 0);
	%worn = fetchData(%clientId, "EquippedBeltArmor");

	for(%i = 0; %i < $VSlot::ACount; %i++)
	{
		%idx  = $VSlot::AIndex[%i];
		%item = "";
		if(%i < %n)
			%item = GetWord(%nf, %i + 1);
		if($BeltItem[%item, "Item"] != "")
			%item = $BeltItem[%item, "Item"];

		if(%item != "" && %item != -1 && $BeltItem[%item, "Type"] == "Armor")
		{
			$VSlot::Map[%clientId, %idx] = %item;
			%name = $BeltItem[%item, "Name"];
			if(%name == "")
				%name = %item;
			if(%item == %worn)
				%name = %name @ " (worn)";
			VSlot::SetRow(%idx, %name, $ItemCost[%item]);
			vslotPushItem(%clientId, %idx);
			// VOID 1b: row count mirrors the real belt count (stacks show as x2 etc)
			%cnt = Belt::ItemCount(%item, fetchData(%clientId, "Armor"));
			if(%cnt < 1) %cnt = 1;
			Player::setItemCount(%clientId, %idx, %cnt);
		}
		else
		{
			$VSlot::Map[%clientId, %idx] = "";
			Player::setItemCount(%clientId, %idx, 0);
		}
	}
}

//------------------------------------------------------------------------------
// VOID BANK: map the client's belt STORAGE (BeltStorage) onto the VSlotBank
// rows for the banker screen. Called from SetupBank (shopping.cs) - the rows
// appear only via the SHOPPING/BUYING bitfields (never given inventory counts,
// so they can't show in the inventory screen). "Buying" a row withdraws
// (economy.cs dispatch -> Belt::BankWithdraw). Row label carries the stored
// count since the buy screen has no count column.
//------------------------------------------------------------------------------
function VSlot::SyncBank(%clientId)
{
	if(!$pref::VSlotsEnabled)
		return;
	if(%clientId == "" || %clientId == -1 || isRPGAI(%clientId))
		return;
	// HUD clients manage belt storage through the KronosHUD bank panel - same
	// rationale as VSlot::Sync (avoid double-showing every stored item).
	if(%clientId.hasKronosHUD)
		return;

	%bs = fetchData(%clientId, "BeltStorage");
	if(%bs == "0" || %bs == " ")
		%bs = "";

	for(%i = 0; %i < $VSlot::SCount; %i++)
	{
		%idx  = $VSlot::BIndex[%i];
		%item = GetWord(%bs, %i * 2);
		%cnt  = GetWord(%bs, %i * 2 + 1);

		if(%item != "" && %item != -1 && $BeltItem[%item, "Item"] == %item && (%cnt * 1) > 0)
		{
			$VSlot::Map[%clientId, %idx] = %item;
			%name = $BeltItem[%item, "Name"];
			if(%name == "")
				%name = %item;
			if(%cnt > 1)
				%name = "(" @ %cnt @ ") " @ %name;	// count FIRST - long names get cut off at the right edge
			// heading follows the stored item's category so armor lands under
			// Armor, weapons under Weapons (was fixed eMiscellany = everything
			// dumped under Miscellany at the banker)
			VSlot::SetRow(%idx, %name, 0, VSlot::CategoryHeading($BeltItem[%item, "Type"]));	// price 0: withdrawing is free
			vslotPushItem(%clientId, %idx);
			// NUMERIC index, not the name: the engine name->index map misses the
			// VSlot placeholders (playerInventory.cpp:513 - the same trap as
			// setItemCount), so setItemShopping("VSlotBankN") silently no-ops.
			Client::setItemShopping(%clientId, %idx);
			Client::setItemBuying(%clientId, %idx);
		}
		else
		{
			$VSlot::Map[%clientId, %idx] = "";
			// no shopping bit set -> row absent from the bank screen
		}
	}
}

//------------------------------------------------------------------------------
// Re-sync every connected, in-game client (mission load / manual refresh).
//------------------------------------------------------------------------------
function VSlot::SyncAll()
{
	if(!$pref::VSlotsEnabled)
		return;
	// mod-native connected-client walk (see Admin.cs vote tally)
	for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
		VSlot::Sync(%cl);
}

//------------------------------------------------------------------------------
// Clear a client's VSlot mapping on logout (counts go with the player object).
//------------------------------------------------------------------------------
function VSlot::Clear(%clientId)
{
	if(%clientId == "" || %clientId == -1)
		return;
	for(%i = 0; %i < $VSlot::Count; %i++)
		$VSlot::Map[%clientId, $VSlot::Index[%i]] = "";
	for(%i = 0; %i < $VSlot::ACount; %i++)
		$VSlot::Map[%clientId, $VSlot::AIndex[%i]] = "";
	for(%i = 0; %i < $VSlot::SCount; %i++)
		$VSlot::Map[%clientId, $VSlot::BIndex[%i]] = "";
}

//------------------------------------------------------------------------------
// Click dispatch: remoteUseItem (remote.cs) calls this when the used ItemData is
// a VSlot. Equips (toggles) the belt weapon mapped to that slot for this client.
// %item is the VSlot datablock NAME (getItemData(%type) result).
//------------------------------------------------------------------------------
function VSlot::IsSlotItem(%item)
{
	if(!$pref::VSlotsEnabled)
		return false;
	%idx = $VSlot::NameToIdx[%item];	// server-safe; getItemType() would deref the CLIENT dbm (null on a dedicated host)
	return $VSlot::IsSlot[%idx];
}

//------------------------------------------------------------------------------
// Resolve the belt weapon currently mapped to a VSlot row for this client. %item is
// the VSlot datablock NAME (getItemData result). Returns "" if the slot is empty or
// %item isn't a VSlot. Used by the stock-GUI action dispatch (use/drop/sell/bank) to
// act on the real belt weapon behind the placeholder row.
//------------------------------------------------------------------------------
function VSlot::MappedItem(%clientId, %item)
{
	%idx = $VSlot::NameToIdx[%item];
	if(%idx == "")
		return "";
	return $VSlot::Map[%clientId, %idx];
}

function VSlot::OnUseClick(%clientId, %item)
{
	%beltItem = VSlot::MappedItem(%clientId, %item);
	if(%beltItem == "" || %beltItem == -1)
	{
		Client::sendMessage(%clientId, $MsgWhite, "That backpack slot is empty.");
		return;
	}

	// VOID Phase 1b: dispatch by belt category. Armor rows toggle equip via the
	// belt armor path (Phase 1a; SkillCanUse-gated in Belt::EquipArmor). Equip/
	// UnequipArmor re-sync the rows themselves, so the "(worn)" tag follows.
	if($BeltItem[%beltItem, "Type"] == "Armor")
	{
		if(fetchData(%clientId, "EquippedBeltArmor") == %beltItem)
			Belt::UnequipArmor(%clientId, %beltItem);
		else
			Belt::EquipArmor(%clientId, %beltItem);
		return;
	}

	// Weapons: toggle, mirroring Belt::UseItem's Weapons branch.
	if(fetchData(%clientId, "EquippedBeltWeapon") == %beltItem)
		BeltWeapon::Unequip(%clientId, false);
	else
		BeltWeapon::Equip(%clientId, %beltItem);
	// Equip/Unequip don't touch the belt LIST, so no re-Sync needed here.
}
