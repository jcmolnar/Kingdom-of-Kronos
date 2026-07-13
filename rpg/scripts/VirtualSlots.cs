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

$VSlot::Count = 8;			// K reserved ItemData slots (fund from Phase B savings)

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

	echo("[VSLOT] " @ $VSlot::Count @ " virtual inventory slots ready (indices " @ $VSlot::Index[0] @ ".." @ $VSlot::Index[$VSlot::Count - 1] @ "); DLL ok: " @ %probe);
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
function VSlot::SetRow(%idx, %displayName, %price)
{
	%db = getItemData(%idx);			// datablock name, e.g. "VSlot3"
	if(%db == "" || %db == -1)
		return;
	%db.description = %displayName;		// the row label the stock inventory renders
	%db.price       = %price;
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
			Player::setItemCount(%clientId, %idx, 1);	// make the row appear
		}
		else
		{
			$VSlot::Map[%clientId, %idx] = "";
			Player::setItemCount(%clientId, %idx, 0);	// hide the row
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
	// Toggle, mirroring Belt::UseItem's Weapons branch.
	if(fetchData(%clientId, "EquippedBeltWeapon") == %beltItem)
		BeltWeapon::Unequip(%clientId, false);
	else
		BeltWeapon::Equip(%clientId, %beltItem);
	// Equip/Unequip don't touch the belt LIST, so no re-Sync needed here.
}
