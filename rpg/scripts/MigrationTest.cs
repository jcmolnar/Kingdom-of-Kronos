//----------------------------------------------------------------------------
// MigrationTest.cs - DEV-ONLY harness for the armor/accessory Void/belt
// conversion. NOT exec'd by Server.cs - load it by hand in the server console:
//     exec(MigrationTest);
//
// Workflow (validates the dev -> live conversion end-to-end on one character):
//   BEFORE the conversion deploy:
//     1. exec(MigrationTest);
//     2. MigTest::GiveAll(2049);          // 1 of every Accessory-class item
//     3. (in game) equip one armor, deposit a couple items in the bank
//     4. MigTest::Snapshot(2049, "pre");  // persists to config\MigrationSnap.cs
//   Deploy the conversion, restart, log the same character in, THEN:
//     5. exec(MigrationTest);  MigTest::Load();
//     6. MigTest::Snapshot(2049, "post");
//     7. MigTest::Compare(2049, "pre", "post");   // per-item PASS/MOVED/MISSING
//
// Snapshot captures BOTH representations of every item (engine itemTypeList
// counts AND belt category lists + the armor funkvars), so items that change
// systems (regular -> belt) are followed across, and the known "Armor" funkvar
// key collision (worn engine armor vs belt carried-armor list) is visible.
//----------------------------------------------------------------------------

$MigTest::SnapFile = "config\\MigrationSnap.cs";

// The funkvar keys worth capturing around the conversion.
$MigTest::VarKeys = "Armor EquippedBeltArmor Weapons Accessories Consumables QuestItems KeyItems Other AllBelt BeltStorage StoredArmor StoredAccessories StoredConsumables StoredQuestItems StoredKeyItems StoredOther spawnStuff";

// Belt carried-list categories to search when an item leaves the engine counts.
$MigTest::BeltCats = "Weapons Armor Accessories Consumables QuestItems KeyItems Other";

function MigTest::GiveAll(%clientId)
{
	%player = Client::getOwnedObject(%clientId);
	if(%player == -1 || %player == "" || !isObject(%player))
	{
		echo("[MIGTEST] GiveAll: no player object for clientId " @ %clientId @ " (must be spawned in-game).");
		return;
	}

	%given = 0;
	%skippedCap = 0;
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%item = getItemData(%i);
		if(%item == "" || %item == -1)
			continue;
		// Base armors + accessories are className "Accessory" (worn variants are
		// "Equipped" and are deliberately excluded - equip one manually instead).
		if(%item.className != "Accessory")
			continue;
		// T1Vista (retail 1.40) hosting: itemTypeList caps at 200; count writes
		// above that are OOB (see phase-d notes). Skip + report rather than corrupt.
		if(%i >= 200)
		{
			echo("[MIGTEST] GiveAll: SKIPPED idx " @ %i @ " '" @ %item @ "' (>= 200-cap on this binary).");
			%skippedCap++;
			continue;
		}
		// Route through the real give path so pre/post conversion both work
		// (post-conversion, belt-registered names route to the belt automatically).
		GiveThisStuff(%clientId, %item @ " 1", False);
		%given++;
	}
	echo("[MIGTEST] GiveAll: gave 1 each of " @ %given @ " Accessory-class items to " @ Client::getName(%clientId) @ (%skippedCap ? " (" @ %skippedCap @ " skipped at cap)" : "") @ ".");
	echo("[MIGTEST] Now equip one armor and bank a couple of items, then run MigTest::Snapshot(" @ %clientId @ ", \"pre\");");
}

function MigTest::Snapshot(%clientId, %label)
{
	%name = Client::getName(%clientId);
	if(%name == "" || %name == -1)
	{
		echo("[MIGTEST] Snapshot: no client " @ %clientId);
		return;
	}

	// 1) Engine per-item counts (by NAME - stable across reindex/conversion).
	%items = 0;
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%item = getItemData(%i);
		if(%item == "" || %item == -1)
			continue;
		%cnt = Player::getItemCount(%clientId, %i);
		if(%cnt > 0)
		{
			$MigSnapshot[%label, "item", %item] = %cnt;
			%items++;
		}
	}

	// 2) Funkvars (both armor systems + all belt/storage lists).
	for(%w = 0; (%key = GetWord($MigTest::VarKeys, %w)) != -1; %w++)
		$MigSnapshot[%label, "var", %key] = fetchData(%clientId, %key);

	$MigSnapshot[%label, "meta", "name"] = %name;
	$MigSnapshot[%label, "meta", "items"] = %items;

	// Persist so the snapshot survives the conversion deploy + restart.
	export("MigSnapshot*", $MigTest::SnapFile, False);
	echo("[MIGTEST] Snapshot '" @ %label @ "' for " @ %name @ ": " @ %items @ " carried item types + " @ "funkvars saved to " @ $MigTest::SnapFile);
}

function MigTest::Load()
{
	exec($MigTest::SnapFile);
	echo("[MIGTEST] Snapshots reloaded from " @ $MigTest::SnapFile);
}

// Count of %item inside a belt list string ("name count name count ").
function MigTest::BeltCount(%list, %item)
{
	if(%list == "" || %list == "0" || %list == -1)
		return 0;
	for(%i = 0; GetWord(%list, %i) != -1; %i += 2)
		if(GetWord(%list, %i) == %item)
			return GetWord(%list, %i+1);
	return 0;
}

// Total of %item across every belt carried category + BeltStorage in snapshot %label.
function MigTest::SnapBeltTotal(%label, %item)
{
	%total = 0;
	for(%w = 0; (%cat = GetWord($MigTest::BeltCats, %w)) != -1; %w++)
		%total += MigTest::BeltCount($MigSnapshot[%label, "var", %cat], %item);
	%total += MigTest::BeltCount($MigSnapshot[%label, "var", "BeltStorage"], %item);
	%total += MigTest::BeltCount($MigSnapshot[%label, "var", "StoredArmor"], %item);
	%total += MigTest::BeltCount($MigSnapshot[%label, "var", "StoredAccessories"], %item);
	return %total;
}

function MigTest::Compare(%clientId, %labelA, %labelB)
{
	echo("[MIGTEST] ===== Compare '" @ %labelA @ "' -> '" @ %labelB @ "' for " @ $MigSnapshot[%labelA, "meta", "name"] @ " =====");

	%pass = 0; %moved = 0; %fail = 0;
	%max = getNumItems();
	for(%i = 0; %i < %max; %i++)
	{
		%item = getItemData(%i);
		if(%item == "" || %item == -1)
			continue;
		%a = $MigSnapshot[%labelA, "item", %item];
		if(%a == "") %a = 0;
		if(%a <= 0)
			continue;

		// Where is it now? Engine count, or belt lists (item may be datablock-less
		// post-conversion, but the pre-snapshot's name list drives this loop).
		%b = $MigSnapshot[%labelB, "item", %item];
		if(%b == "") %b = 0;
		%bBelt = MigTest::SnapBeltTotal(%labelB, %item);

		// The worn variant may have been consumed by an equip-state migration:
		// XxxYyy0 disappearing is OK if the base name is now EquippedBeltArmor.
		%isWorn = 0;
		if(%item.className == "Equipped")
			%isWorn = 1;

		if(%b == %a)
			%pass++;
		else if(%bBelt >= %a)
		{
			%moved++;
			echo("[MIGTEST] MOVED to belt: " @ %item @ " x" @ %a);
		}
		else if(%isWorn)
		{
			%base = String::getSubStr(%item, 0, String::len(%item)-1);	// strip the 0
			if($MigSnapshot[%labelB, "var", "EquippedBeltArmor"] == %base || MigTest::SnapBeltTotal(%labelB, %base) > 0)
			{
				%moved++;
				echo("[MIGTEST] MOVED (worn->belt): " @ %item @ " -> " @ %base);
			}
			else
			{
				%fail++;
				echo("[MIGTEST] *** MISSING (worn): " @ %item @ " had " @ %a @ ", now engine=" @ %b @ " belt=" @ %bBelt);
			}
		}
		else
		{
			%fail++;
			echo("[MIGTEST] *** MISSING: " @ %item @ " had " @ %a @ ", now engine=" @ %b @ " belt=" @ %bBelt);
		}
	}

	// Funkvar diffs (worn armor, equipped belt armor, storage) - informational.
	for(%w = 0; (%key = GetWord($MigTest::VarKeys, %w)) != -1; %w++)
	{
		%va = $MigSnapshot[%labelA, "var", %key];
		%vb = $MigSnapshot[%labelB, "var", %key];
		if(%va != %vb)
			echo("[MIGTEST] var " @ %key @ ": '" @ %va @ "' -> '" @ %vb @ "'");
	}

	echo("[MIGTEST] ===== RESULT: " @ %pass @ " unchanged, " @ %moved @ " migrated to belt, " @ %fail @ " MISSING =====");
	if(%fail == 0)
		echo("[MIGTEST] PASS - no items lost.");
	else
		echo("[MIGTEST] *** FAIL - investigate the MISSING lines above before going live. ***");
}
