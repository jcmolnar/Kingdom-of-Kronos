$Belt::Count["QuestItems"] = 0;
$Belt::Count["KeyItems"] = 0;

$Belt::Categories[1] = "QuestItems";
$Belt::Categories[2] = "KeyItems";

// Stub functions for backward compatibility with old backpack.cs system
function isBackpackItem(%item)
{
	return isBeltItem(%item);
}

function Backpack::GetWeight(%clientId)
{
	return Belt::GetWeight(%clientId);
}

function Backpack::GiveThisStuff(%clientId, %item, %amnt, %echo)
{
	Belt::GiveThisStuff(%clientId, %item, %amnt, %echo);
}

function Backpack::BankStorageConversion(%clientId)
{
	// Legacy function - no longer needed, Belt system doesn't use bank storage conversion
	return;
}

function MenuViewBackpack(%clientId, %page)
{
	MenuViewBelt(%clientId, %page);
}

// Main function called when picking up lootbags
function GiveThisStuff(%clientId, %stuff, %echo)
{
	// Parse the loot string: "item1 count1 item2 count2 ..."
	for(%i = 0; (%item = getWord(%stuff, %i)) != -1; %i += 2)
	{
		%count = getWord(%stuff, %i + 1);
		
		// Check if it's a Belt item
		if(isBeltItem(%item))
		{
			// Give to Belt system
			Belt::GiveThisStuff(%clientId, %item, %count, %echo);
		}
		else
		{
			// Give to regular inventory - item is already the item data name
			%itemData = getItemData(%item);
			if(%itemData != -1 && %itemData != "")
			{
				if(%echo)
					Client::sendMessage(%clientId, 0, "You received " @ %count @ " " @ %itemData.description @ ".");
				Player::incItemCount(%clientId, %itemData, %count);
			}
		}
	}
	
	RefreshAll(%clientId);
}

function MenuViewBelt(%clientId, %page)
{
	Client::buildMenu(%clientId, ".:(Backpack):.", "ViewBelt", true);
	
	for(%i = 1; %i < Belt::GetLastItem(); %i++)
	{
		if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]))
			Client::addMenuItem(%clientId, %cnt++ @ ". " @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i]);
	}

	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuViewBelt(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);

	if($Belt::Count[%o] > 0)
	{
		MenuBeltGear(%clientId, %o, 1);
		return;
	}

	if(%o != "done")
	MenuViewBelt(%clientId, %p);

	return;
}

function MenuBeltGear(%clientId, %type, %page)
{
	%disp = Belt::Display(%type);

	%msg = "<jc><f2>To drop 'bulk' " @ %disp @ ", please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
	
	Client::buildMenu(%clientId, %disp @ ":", "BeltGear", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%clientId, %type);
	%ns = GetWord(%nf, 0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
	}

	return;
}

function processMenuBeltGear(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)
		%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)
		%clientId.bulkNum = 500;

		MenuBeltDrop(%clientId, %o, %t);
		return;
	}

	if(%o != "done")
	MenuBeltGear(%clientId, %t, %p);

	return;
}

function MenuBeltDrop(%clientId, %item, %type)
{
	%name = $BeltItem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Belt::HasThisStuff(%clientId, %item);

	if(%amnt > %cmnt)
	%amnt = %cmnt;

	Client::buildMenu(%clientId, %name @ " (" @ %cmnt @ ")", "BeltDrop", true);
	Client::addMenuItem(%clientId, %cnt++ @ "Drop (" @ %amnt @ ")", %type @ " drop " @ %item @ " " @ %amnt);
	Client::addMenuItem(%clientId, %cnt++ @ "Examine", %type @ " examine " @ %item);
	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuBeltDrop(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	if(%amnt <= 0) %amnt = 1;

	if(%amnt != %clientId.bulkNum)
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuBeltDrop(%clientId, %item, %type);
	}
	else if(%option == "drop")
	{
		Belt::DropItem(%clientId, %item, %amnt, %type);
	}
	else if(%option == "examine")
	{
		Belt::WhatIs(%clientId, %item);
	}
	return;
}

function MenuSellBelt(%clientId)
{
	Client::buildMenu(%clientId, "Pack Sell:", "SellBelt", true);

	for(%i = 1; %i < Belt::GetLastItem(); %i++)
	if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]) > 0)
	Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i]);
	
	Client::addMenuItem(%clientId, "xFinished", "done");
	return;
}

function processMenuSellBelt(%clientId, %opt)
{
	if($Belt::Count[%opt] > 0)
	{
		MenuSellBeltItem(%clientId, %opt, 1);
		return;
	}

	if(%opt != "done")
	MenuSellBelt(%clientId);

	return;
}

function MenuSellBeltItem(%clientId, %type, %page)
{
	%disp = Belt::Display(%type);

	%msg = "<jc><f2>To sell 'bulk' " @ %disp @ ", please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));

	Client::buildMenu(%clientId, %disp @ " Sell:", "SellBeltItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%clientId, %type);
	%ns = GetWord(%nf, 0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
	}

	return;
}

function processMenuSellBeltItem(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuSellBeltItemFinal(%clientId, %o, %t, "sell");
		return;
	}

	if(%o != "done")
	MenuSellBeltItem(%clientId, %t, %p);

	return;
}

function MenuSellBeltItemFinal(%clientId, %item, %type, %mode)
{
	%name = $BeltItem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	if(%amnt > 500) 
	%amnt = 500;
	
	if(%mode == "withdraw")
	%cmnt = Belt::itemCount(%item, fetchData(%clientId, "Stored" @ %type));
	
	if(%mode == "store" || %mode == "sell")
	%cmnt = Belt::itemCount(%item, fetchData(%clientId, %type));

	if(%amnt > %cmnt)
	%amnt = %cmnt;

	Client::buildMenu(%clientId, %name @ " (" @ %cmnt @ ")", "SellBeltItemFinal", true);

	Client::addMenuItem(%clientId, %cnt++ @ %mode @ " " @ %amnt @ " ($" @ (Belt::GetSellCost(%clientId, %item) * %amnt) @ ")", %type @ " " @ %mode @ " " @ %item @ " " @ %amnt);
	Client::addMenuItem(%clientId, "xFinished", "ineedanextrawordsothisisithaha done");
	return;
}

function processMenuSellBeltItemFinal(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);
	if(%type == "ineedanextrawordsothisisithaha")
	Client::CancelMenu(%clientId);
	else if(%option == "sell")
	{
		%cmnt = Belt::HasThisStuff(%clientId, %item);
		if(%cmnt >= %amnt)
		{
			%cost = Belt::GetSellCost(%clientId, %item) * %amnt;
			Client::sendMessage(%clientId, $MsgWhite, "You sold " @ %amnt @ " " @ %item @ " for " @ %cost @ " gil.");
			UseSkill(%clientId, $SkillHaggling, true, true);
			storeData(%clientId, "Gil", %cost, "inc");
			Belt::TakeThisStuff(%clientId, %item, %amnt);
			RefreshAll(%clientId);
			%clientId.bulkNum = 1;
		}
	}
	else if(%option == "store")
	{
		%cmnt = Belt::HasThisStuff(%clientId, %item);
		if(%cmnt >= %amnt)
		{
			if(CountObjInList(fetchData(%clientId, "Stored" @ %type)) / 2 < 25)
			{		
				storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, %amnt));
				Belt::TakeThisStuff(%clientId, %item, %amnt);
				%clientId.bulkNum = 1;
				MenuBeltStoreThisItem(%clientId, %opt, 1, "store");
				RefreshAll(%clientId);				
			}
			else
			Client::sendMessage(%clientId, $MsgRed, "You can only place 25 different items into the " @ Belt::Display(%type) @ " storage.~wC_BuySell.wav");
			
		}
	}
	else if(%option == "withdraw")
	{
		%cmnt = Belt::GetStored(%clientId, %opt);
		if(%cmnt >= %amnt)
		{
			Belt::GiveThisStuff(%clientId, %item, %amnt, 1);
			storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, -%amnt));
			%clientId.bulkNum = 1;
			MenuBeltWithdrawThisItem(%clientId, %opt, 1, "withdraw");
			RefreshAll(%clientId);
		}
	}
	return;
}

function MenuStoreBelt(%clientId, %mode)
{
	Client::buildMenu(%clientId, "Backpack " @ %mode @ ":", "StoreBelt", true);
	if(%mode == "store")
	{
		for(%i = 1; %i < Belt::GetLastItem(); %i++)
		if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]) > 0)
		Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i] @ " " @ %mode);
		
		Client::addMenuItem(%clientId, "nWithdraw Items", "mode store change");
		Client::addMenuItem(%clientId, "xFinished", "done");
	}
	else
	{
		%mode="withdraw";
		for(%i = 1; %i < Belt::GetLastItem(); %i++)
		{
			%fix = getWord(fetchData(%clientId, "Stored" @ $Belt::Categories[%i]), 2);
			if(%fix != -1)
				Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i] @ " " @ %mode);
		}
		
		Client::addMenuItem(%clientId, "nStore Items", "mode withdraw change");
		Client::addMenuItem(%clientId, "xFinished", "done");
	}
	return;
}

function processMenuStoreBelt(%clientId, %opt)
{
	%o = getword(%opt, 0);
	%m = getword(%opt, 1);
	%c = getword(%opt, 2);
	if(%c == "change")
	{
		if(%m == "store")
		MenuStoreBelt(%clientId, "withdraw");
		if(%m == "withdraw")
		MenuStoreBelt(%clientId, "store");
	}

	if(%m == "store" || %m == "withdraw")
	{
		if($Belt::Count[%o] > 0)
		{
			if(%m == "store")
			MenuBeltStoreThisItem(%clientId, %opt, 1, %m);
			if(%m == "withdraw")
			MenuBeltWithdrawThisItem(%clientId, %opt, 1, %m);
		}
		return;

		if(%o != "done")
		MenuStoreBelt(%clientId, %m);
	}

	return;
}
function MenuBeltWithdrawThisItem(%clientId, %type, %page, %mode)
{
	%type = getword(%type, 0);
	%disp = Belt::Display(%type);
	
	%msg = "<jc><f2>To withdraw 'bulk' " @ %disp @ ", please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
	
	Client::buildMenu(%clientId, %disp @ ":", "BeltWithdrawThisItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = fetchdata(%clientId, "Stored" @ %type);
	%ns = (CountObjInList(%nf) / 2) - 1;
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = (%lb - 1) * 2 + 2;
	for(%i = %lb; %i <= %ub; %i++)
	{	
		%item = getword(%nf, %x);
		%amnt = getword(%nf, %x + 1);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ %item, %item @ " " @ %page @ " " @ %type @ " " @ %mode);
		%x+=2;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
	}

	return;
}
function processMenuBeltWithdrawThisItem(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);
	%mode = getword(%opt, 3);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuSellBeltItemFinal(%clientId, %o, %t, %mode);
		return;
	}

	if(%o != "done")
	MenuBeltWithdrawThisItem(%clientId, %t, %p, %mode);

	return;
}
function MenuBeltStoreThisItem(%clientId, %type, %page, %mode)
{
	%type = getword(%type, 0);
	%disp = Belt::Display(%type);
	
	%msg = "<jc><f2>To store 'bulk' " @ %disp @ ", please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 500";
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
	
	Client::buildMenu(%clientId, %disp @ ":", "BeltStoreThisItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%clientId, %type);
	%ns = GetWord(%nf, 0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%clientId, %item);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type @ " " @ %mode);
		%x++;
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type @ " " @ %mode);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type @ " " @ %mode);
	}

	return;
}
function processMenuBeltStoreThisItem(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);
	%mode = getword(%opt, 3);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuSellBeltItemFinal(%clientId, %o, %t, %mode);
		return;
	}

	if(%o != "done")
	MenuBeltStoreThisItem(%clientId, %t, %p, %mode);

	return;
}

// ------------------- //
// Non Menu Functions  //
// ------------------- //

function Belt::StorageConversion(%clientId)
{
	for(%i=0; getword(fetchData(%clientId, "StorageTruce"), %i) != -1; %i+=2)
	{
		%item = getword(fetchData(%clientId, "StorageTruce"), %i);
		if(isBeltItem(%item))
		{
			%itemcount = getword(fetchData(%clientId, "StorageTruce"), %i+1);
			%a = String::replace(fetchData(%clientId, "StorageTruce"), " " @ %item @ " " @ %itemcount, "");
			%list = String::NEWgetSubStr(%a, 0, 99999);
			storeData(%clientId, "StorageTruce", %list);
			Client::sendMessage(%clientId, $MsgBeige, "Now converting " @ %itemcount @ " " @ %item @ "s to your BELT storage.");
			%type = $BeltItem[%item, "Type"];
			echo("BELT ITEM (" @ %item @ ") FOUND IN SOTRAGE ON " @ client::getname(%clientId) @ "! CONVERTING NOW");
			storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, %itemcount));
		}
	}
}

function Belt::ItemsOnThemConversion(%clientId)
{
	for(%i=0; getword(fetchData(%clientId, "SpawnStuff"), %i) != -1; %i+=2)
	{
		%item = getword(fetchData(%clientId, "SpawnStuff"), %i);
		if(isBeltItem(%item))
		{
			%itemcount = getword(fetchData(%clientId, "SpawnStuff"), %i+1);
			%a = String::replace(fetchData(%clientId, "SpawnStuff"), " " @ %item @ " " @ %itemcount, "");
			%list = String::NEWgetSubStr(%a, 0, 99999);
			storeData(%clientId, "SpawnStuff", %list);
			Client::sendMessage(%clientId, $MsgBeige, "Now converting " @ %itemcount @ " " @ %item @ "s to your BELT storage.");
			%type = $BeltItem[%item, "Type"];
			echo("BELT ITEM (" @ %item @ ") FOUND ON " @ client::getname(%clientId) @ "! CONVERTING NOW");
			storeData(%clientId, "Stored" @ %type, SetStuffString(fetchData(%clientId, "Stored" @ %type), %item, %itemcount));
		}
	}
}

//==========================//
//======MUG BELT START======//
//==========================//
function Belt::Mug(%clientId, %id)
{
	%clientId.MugID=%id;
	Belt::MugBeltMain(%clientId, %id);
	client::sendmessage(%clientId, "You are belt mugging");
}
function Belt::MugBeltMain(%clientId, %id)
{
	Client::buildMenu(%clientId, client::getname(%id) @ "'s Belt:", "MugBeltMain", true);
	for(%i = 1; %i < Belt::GetLastItem(); %i++)
	if(!Belt::isEmpty(%clientId, $Belt::Categories[%i]) > 0)
	Client::addMenuItem(%clientId, %cnt++ @ Belt::Display($Belt::Categories[%i]), $Belt::Categories[%i]);	
	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}
function processMenuMugBeltMain(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);

	if($Belt::Count[%o] > 0)
	{
		MenuBelt::MugThisType(%clientId, %o, 1);
		return;
	}
	return;
}
function MenuBelt::MugThisType(%clientId, %type, %page)
{
	%id=%clientId.MugId;
	%disp = Belt::Display(%type);

	Client::buildMenu(%clientId, %disp @ ":", "Belt::MugThisType", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $Belt::Count[%type];
	%nf = Belt::GetNS(%id, %type);
	%ns = GetWord(%nf, 0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
	%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf, %x);
		%amnt = Belt::HasThisStuff(%id, %item);
		Client::addMenuItem(%clientId, %cnt++ @ %amnt @ " " @ $BeltItem[%item, "Name"], %item @ " " @ %page @ " " @ %type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @ " " @ %type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @ " " @ %type);
	}

	return;
}
function processMenuBelt::MugThisType(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o != "page" && %o != "done")
	{
		%clientId.bulkNum = 1;

		MenuBelt::MugChoose(%clientId, %o, %t);
		return;
	}

	return;
}
function MenuBelt::MugChoose(%clientId, %item, %type)
{
	%id=%clientId.MugId;
	%name = $BeltItem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Belt::HasThisStuff(%id, %item);

	if(%amnt > %cmnt)
	%amnt = %cmnt;

	Client::buildMenu(%clientId, %name @ " (" @ %cmnt @ ")", "Belt::MugChoose", true);

	Client::addMenuItem(%clientId, %cnt++ @ "Mug " @ %item, %type @ " MUG " @ %item @ " 1");
	Client::addMenuItem(%clientId, %cnt++ @ "Examine", %type @ " examine " @ %item);
	Client::addMenuItem(%clientId, %cnt++ @ "Back", %type @ " back " @ %item);
	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuBelt::MugChoose(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);

	if(%option == "MUG")
	{
		Client::sendMessage(%clientId, $MsgBeige, "Attempting to Mug " @ client::getName(%clientId.MugId) @ "'s belt...");
		schedule("Belt::MugItem(" @ %clientId @ ", " @ %item @ ", " @ %type @ ");", 5);
	}
	else if(%option == "examine")
	{
		%msg = WhatIs(%item);
		bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
		MenuBelt::MugChoose(%clientId, %item, %type);
	}
	else if(%option == "back")
	{
		MenuBelt::MugThisType(%clientId, %option, 1);
	}
	return;
}
function Belt::MugItem(%clientId, %item, %type)
{
	%cl=%clientId.MugId;
	%list = fetchData(%cl, %type);
	%victimName = Client::getName(%cl);
	%stealerName = Client::getName(%clientId);

	for(%i=0; getword(fetchData(%cl, %type), %i) != -1; %i+=2)
	{
		if(getword(fetchData(%cl, %type), %i) == %item)
		%icnt = getword(fetchData(%cl, %type), %i+1);
	}
	%clientId.TryingToSteal = "";

	//weights
	%itemweight = GetAccessoryVar(%item, $Weight);
	%wweight = 10;
	if(Player::getMountedItem(%clientId, $WeaponSlot) == %item || %item.className == Equipped)
	%handweight = 5.0;
	else
	%handweight = 1.0;
	%FailWeight = (%itemweight * %wweight) * %handweight;

	if(Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(%cl)) > 2)
	Client::sendMessage(%clientId, $MsgWhite, "Your target has wandered off...");
	else
	{
		%r1 = GetRoll("1d" @ $PlayerSkill[%clientId, $SkillStealing]);
		%r2 = GetRoll("1d" @ ($PlayerSkill[%cl, $SkillStealing] + $PlayerSkill[%cl, $SkillDodging]));
		%b = %r1 - %r2;
		%a = %b - %FailWeight;
		if(%a > 0 && %icnt > 0)
		{
			%newcnt = %icnt-1;
			Client::sendMessage(%clientId, $MsgBeige, "You successfully stole a " @ %item @ " from " @ %victimName @ "!");
			Belt::TakeThisStuff(%cl, %item, 1);
			Belt::GiveThisStuff(%clientId, %item, 1);
			PerhapsPlayStealSound(%clientId, %clientId.stealType);

			if(%newcnt > 0)
			MenuBelt::MugChoose(%clientId, %item, %type);

			RefreshAll(%clientId);
			RefreshAll(%cl);

			UseSkill(%clientId, $SkillStealing, true, true);
			PostSteal(%clientId, true, %clientId.stealType);

			return true;
		}
		else
		{
			Client::CancelMenu(%clientId);
			Client::sendMessage(%clientId, $MsgRed, "You failed to mug " @ %victimName @ "'s belt!");
			Client::sendMessage(%cl, $MsgRed, %stealerName @ " just failed to mug your belt!");

			UseSkill(%clientId, $SkillStealing, false, true);
			PostSteal(%clientId, false, %clientId.stealType);
		}
	}

	//force kick from Inv Screen
	remotePlayMode(%clientId);

	return false;
}
//==========================//
//====== MUG BELT END ======//
//==========================//
function Belt::GetWeight(%clientId)
{
	//== HELPS REDUCE LAG WHEN THERE ARE SIMULTANEOUS CALLS ======
	%time = getIntegerTime(true);
	if(%time - %clientId.lastGetBeltWeight <= 1 && fetchData(%clientId, "tmpBeltWeight") != "")
	return fetchData(%clientId, "tmpBeltWeight");
	%clientId.lastGetBeltWeight = %time;
	//============================================================
	
	%weight = 0;
	for(%i = 1; $Belt::Categories[%i] != ""; %i++)
	{
		%string = fetchData(%clientId, $Belt::Categories[%i]);
		for(%j = 0; (%item = GetWord(%string, %j)) != -1; %j+=2)
			%weight += $AccessoryVar[%item, $Weight] * GetWord(%string, %j+1);
	}
	return %weight;
}

function Belt::GetNS(%clientId, %type)
{
	%count = 0;
	%stuff = fetchData(%clientId, %type);
	
	for(%i = 0; (%item = getWord(%stuff, %i)) != -1; %i+=2)
	{
		%amnt = getWord(%stuff, %i + 1);
		if(%amnt > 0)
		{
			%list = %list @ " " @ %item;
			%count++;
		}
	}
	
	return %count @ " " @ %list;
}

function Belt::isEmpty(%clientId, %type)
{
	%stuff = fetchData(%clientId, %type);
	if(getWord(%stuff, 0) != -1 && getWord(%stuff, 1) != -1)
		return false;
	else
		return true;
}

function Belt::GetSellCost(%clientId, %item)
{
	%p = $HardcodedItemCost[%item];
	%cost = round(%p * ($resalePercentage/100));

	%p = round($PlayerSkill[%clientId, $SkillHaggling] / 11) / 100;
	%x = round(%cost * Cap(%p, 0.0, 1.0) );
	%cost += %x;

	return %cost;
}

function Belt::HasThisStuff(%clientId, %item)
{
	%item = $BeltItem[%item, "Item"];
	%type = $BeltItem[%item, "Type"];
	%list = fetchData(%clientId, %type);
	%amnt = Belt::ItemCount(%item, %list);
	return %amnt;
}

function Belt::GiveThisStuff(%clientId, %item, %amnt, %echo)
{
	if(%amnt > 0)
	{
		%item = $BeltItem[%item, "Item"];
		%type = $BeltItem[%item, "Type"];
		%list = fetchData(%clientId, %type);
		%count = Belt::ItemCount(%item, %list);

		if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %amnt @ " " @ $BeltItem[%item, "Name"] @ ".");

		if(%count > 0)
		{
			%list = Belt::RemoveFromList(%list, %item @ " " @ %count);
			%amnt = %amnt + %count;
		}
		%list = Belt::AddToList(%list, %item @ " " @ %amnt);

		storeData(%clientId, %type, %list);
		storeData(%clientId, "AllBelt", fetchData(%clientId, "QuestItems") @ fetchData(%clientId, "KeyItems") @ fetchData(%clientId, "GemItems"));
	}
}

function Belt::TakeThisStuff(%clientId, %item, %amnt)
{
	if(%amnt > 0)
	{
		%item = $BeltItem[%item, "Item"];
		%type = $BeltItem[%item, "Type"];
		%list = fetchData(%clientId, %type);
		%count = Belt::ItemCount(%item, %list);
		%amnt = %count - %amnt;

		%list = Belt::RemoveFromList(%list, %item @ " " @ %count);
		if(%amnt > 0)
		%list = Belt::AddToList(%list, %item @ " " @ %amnt);

		storeData(%clientId, %type, %list);
	}
}

function isBeltItem(%item)
{
	if((String::ICompare($BeltItem[%item, "Item"], %item) == 0))
		return true;
		
	return false;
}

function Belt::ItemCount(%item, %list)
{
	%count = 0;
	for(%i=0;(%w = getword(%list, %i)) != -1;%i+=2)
	{
		if(%w == %item)
		{
			%count = getword(%list, %i++);
			break;
		}
	}
	return %count;
}

function Belt::WhatIs(%clientId, %item)
{
	dbecho($dbechoMode, "Belt::WhatIs(" @ %clientId @ ", " @ %item @ ")");

	if(%item.description == false)
		%desc = $BeltItem[%item, "Name"];
	else
		%desc = %item.description;
		
	%w = GetAccessoryVar(%item, $Weight);
	%c = GetItemCost(%item);
	%t = Belt::Display($BeltItem[%item, "Type"]);
	
	if($AccessoryVar[%item, $MiscInfo] != "")
		%nfo = $AccessoryVar[%item, $MiscInfo];
	else
		%nfo = "There is no further information available.";

	%msg = "";
	%msg = %msg @ "<f1>" @ %desc @ "\n";
	if(%w != "")
	%msg = %msg @ "\nWeight: " @ %w;
	if(%t != "")
	%msg = %msg @ "\nType: " @ %t;	
	if(%c != "")
	%msg = %msg @ "\nPrice: $" @ %c;
	
	
	%msg = %msg @ "\n\n<f0>" @ %nfo;
	bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
}

function Belt::GetLastItem()
{
	for(%i = 1; $Belt::Categories[%i] != ""; %i++) {}
	
	return %i;
}

function Belt::Display(%type)
{
	if(%type == "QuestItems") %disp = "Quest Items";
	else if(%type == "GemItems") %disp = "Minerals";
	else if(%type == "KeyItems") %disp = "Key Items";

	return %disp;
}

function Belt::Sell(%clientId, %npc)
{
	AI::sayLater(%clientId, %npc, "What would you like to sell?", true);
	MenuSellBelt(%clientId);
}

function Belt::Store(%clientId, %npc)
{
	MenuStoreBelt(%clientId, "store");
}

function Belt::DropItem(%clientId, %item, %amnt, %type)
{
	%chk = Belt::HasThisStuff(%clientId, %item);
	if(%chk >= %amnt)
	{
		Belt::TakeThisStuff(%clientId, %item, %amnt);
		TossLootbag(%clientId, %item @ " " @ %amnt, 8, "*", 0, 1);
	}
}

function Belt::AddToList(%list, %item)
{
	%list = %list @ %item @ " ";
	return %list;
}

function Belt::RemoveFromList(%list, %item)
{
	%a = " " @ %list;
	%a = String::replace(%a, " " @ %item @ " ", " ");
	%list = String::NEWgetSubStr(%a, 1, 99999);

	return %list;
}

function Belt::IsInList(%list, %item)
{
	if(String::findSubStr(%list, %item) != -1)
		return true;
	else
		return false;
}

function Belt::GetStored(%clientId, %opt)
{	
	%type = GetWord(%opt, 0);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	for(%i=0; (%item2=getword(fetchData(%clientId, "Stored" @ %type), %i)) != -1; %i+=2)
	{
		if(%item == %item2)
		{
			%amnt=getword(fetchData(%clientId, "Stored" @ %type), %i+1);
			return %amnt;
		}
	}
}

function Belt::GetDeathItems(%clientId)
{
	%tmploot = "";
	if(fetchData(%clientId, "LCK") < 0)
	{
		for(%i = 1; $Belt::Categories[%i] != ""; %i++)
			if($Belt::Storage[$Belt::Categories[%i]])
				%tmploot = %tmploot @ fetchData(%clientId, $Belt::Categories[%i]);

		for(%i=0;getword(%tmploot, %i)!=-1;%i++)
		{
			%a = getword(%tmploot, %i);
			%b = getword(%tmploot, %i++);

			Belt::TakeThisStuff(%clientId, %a, %b);
		}
	}
	
	return %tmploot;
}

function BeltItem::Add(%name, %item, %type, %weight, %cost)
{
	%num = $Belt::Count[%type]++;
	$BeltItem[%num, "Num", %type] = %item;
	$BeltItem[%item, "Item"] = %item;
	$BeltItem[%item, "Name"] = %name;
	$BeltItem[%item, "Type"] = %type;
	$AccessoryVar[%item, $Weight] = %weight;
	$HardcodedItemCost[%item] = %cost;
}

//===================
//  Minerals (Removed - not in this mod)
//===================
// Mining items removed from Kingdom of Kronos

//===================
//  Quest Items
//===================

BeltItem::Add("Black Statue", "BlackStatue", "QuestItems", 1.0, 300);
BeltItem::Add("Enchanted Stone", "EnchantedStone", "QuestItems", 5.0, 2450);
BeltItem::Add("Skeleton Bone", "SkeletonBone", "QuestItems", 2.5, 5860);
BeltItem::Add("Ruby Necklace", "RubyNecklace", "QuestItems", 5.0, 300);
BeltItem::Add("Ogre Tooth", "OgreTooth", "QuestItems", 2.5, 6000);
BeltItem::Add("Ancient Scroll", "AncientScroll", "QuestItems", 5.0, 40000);
BeltItem::Add("Crown", "Crown", "QuestItems", 2.5, 45000);
BeltItem::Add("Minotaur Horn", "MinotaurHorn", "QuestItems", 2.5, 100000);
BeltItem::Add("Alien Spine", "AlienSpine", "QuestItems", 5.0, 150000);
BeltItem::Add("Alien Eye", "AlienEye", "QuestItems", 2.5, 500000);
BeltItem::Add("Demon Breath", "DemonBreath", "QuestItems", 5.0, 700000);
BeltItem::Add("Angel's Tear", "AngelsTear", "QuestItems", 2.5, 900000);

$AccessoryVar[BlackStatue, $MiscInfo] = "A black statue";
$AccessoryVar[EnchantedStone, $MiscInfo] = "An enchanted stone";
$AccessoryVar[SkeletonBone, $MiscInfo] = "A skeleton bone";
$AccessoryVar[RubyNecklace, $MiscInfo] = "A ruby necklace";
$AccessoryVar[OgreTooth, $MiscInfo] = "An ogre tooth";
$AccessoryVar[AncientScroll, $MiscInfo] = "A very old look scroll";
$AccessoryVar[Crown, $MiscInfo] = "A very valuable crown.";
$AccessoryVar[MinotaurHorn, $MiscInfo] = "A horn from a Minotaur's Head";
$AccessoryVar[AlienSpine, $MiscInfo] = "A long spine from an alien";
$AccessoryVar[AlienEye, $MiscInfo] = "A gooey eye from the Alien Queen";
$AccessoryVar[DemonBreath, $MiscInfo] = "The very rare breath of a demon";
$AccessoryVar[AngelsTear, $MiscInfo] = "A tear shed from an angel when it dies.";

//===================
//  Key Items
//===================
BeltItem::Add("Krono Stone", "KronoStone", "KeyItems", 5.0, 80000);
BeltItem::Add("DuelCard", "DuelCard", "KeyItems", 2.0, 100000);

$AccessoryVar[KronoStone, $MiscInfo] = "A mystical stone sizzling with power";
$AccessoryVar[DuelCard, $MiscInfo] = "A special card that allows you to #challenge someone to a duel.";