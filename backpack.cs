          //-----------------------------------
          //
          // ----------------------------------
         // Tribes RPG Backpack!!!
        //
       // Created by Carling
      // 07/02/04
     // ---------------------------------------
    // Adds a Backpack option to tab menu
    // designed to reduce number of itemdatas
    // in server.
    //
    // This script requires many hookup's into
    // other files and will not run correctly
    // without them.
    //
    // It has been written for Salmons MoS
    // and may not be compatible with other
    // mods.
    //
    // It has been recoded to be compatible
    // TvT.
    //
    // --------------------------------------
   // I take no responsibility for any damage
  // this script may do to your server or
 // computer, although it shouldnt do any..
// ------------------------------------------
//
// ------------------- //
// Menu Functions      //
// ------------------- //

function MenuViewBackpack(%clientId, %page)
{
	Client::buildMenu(%clientId, "Backpack:", "ViewBackpack", true);
//  Quests, Rares, Keys, Scrolls, Unique, SmithBook
	//if(Backpack::GetNS(%clientid,"GemItems") > 0)		
Client::addMenuItem(%clientId, %cnt++ @ "Gems", "GemItems");
	//if(Backpack::GetNS(%clientid,"RareItems") > 0)	
Client::addMenuItem(%clientId, %cnt++ @ "Rares", "RareItems");
	//if(Backpack::GetNS(%clientid,"KeysItems") > 0)		
Client::addMenuItem(%clientId, %cnt++ @ "Keys", "KeysItems");
	//if(Backpack::GetNS(%clientid,"ScrollsItems") > 0)		
Client::addMenuItem(%clientId, %cnt++ @ "Scrolls", "ScrollsItems");
	//if(Backpack::GetNS(%clientid,"UniqueItems") > 0)		
Client::addMenuItem(%clientId, %cnt++ @ "Unique Items", "UniqueItems");
	Client::addMenuItem(%clientId, %cnt++ @ "Smith Book", "smith");
	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuViewBackpack(%clientId, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);

	if($count[%o] > 0)
	{
		MenuBackpackGear(%clientid, %o, 1);
		return;
	}

	if(%o != "done")
		MenuViewBackpack(%clientId, %p);

	return;
}

function MenuBackpackGear(%clientid, %type, %page)
{
	if(%type == "QuestItems") 		%disp = "Quests";
	if(%type == "KeysItems") 		%disp = "Keys";
	if(%type == "RareItems")		%disp = "Rares";
	if(%type == "ScrollsItems") 		%disp = "Scrolls";
	if(%type == "UniqueItems") 		%disp = "Unique Items";

	Client::buildMenu(%clientId, %disp@":", "BackpackGear", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $count[%type];
	%nf = Backpack::GetNS(%clientid,%type);
	%ns = GetWord(%nf,0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf,%x);
		%amnt = Backpack::HasThisStuff(%clientid,%item);
		Client::addMenuItem(%clientId, %cnt++ @%amnt@" "@ $Backpackitem[%item, "Name"], %item @ " " @ %page @" "@%type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
	}

	return;
}

function processMenuBackpackGear(%clientid, %opt)
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

		MenuBackpackDrop(%clientid, %o, %t);
		return;
	}

	if(%o != "done")
		MenuBackpackGear(%clientid, %t, %p);

	return;
}

function MenuBackpackDrop(%clientid, %item, %type)
{
	%name = $Backpackitem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Backpack::HasThisStuff(%clientid,%item);

	if(%amnt > %cmnt)
		%amnt = %cmnt;

	Client::buildMenu(%clientId, %name@" ("@%cmnt@")", "BackpackDrop", true);

	Client::addMenuItem(%clientId, %cnt++ @ "Drop "@%amnt, %type@" drop "@%item@" "@%amnt);
	Client::addMenuItem(%clientId, %cnt++ @ "Examine", %type@" examine "@%item);
	Client::addMenuItem(%clientId, "xDone", "done");
	return;
}

function processMenuBackpackDrop(%clientId, %opt)
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
		MenuBackpackDrop(%clientid, %item, %type);
	}
	else if(%option == "smith")
	{
		%k = String::getSubStr(%item, 0, 1);
		%f = String::getSubStr(%item, 1, String::Len(%item) - 5);
		remoteSay(%clientId, 0, "#smith "@%k@" "@%f);
	}
	else if(%option == "drop")
	{
		Backpack::DropItem(%clientid,%item,%amnt,%type);
	}
	else if(%option == "examine")
	{
		%msg = WhatIs(%item);
		bottomprint(%clientId, %msg, floor(String::len(%msg) / 20));
	}
	return;
}

function MenuSellBackpack(%clientId)
{
	Client::buildMenu(%clientId, "Backpack sell:", "SellBackpack", true);

	if(Backpack::GetNS(%clientid,"QuestItems") > 0)	Client::addMenuItem(%clientId, %cnt++ @ "Quests", "QuestItems");
	if(Backpack::GetNS(%clientid,"RareItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Rares", "RareItems");
	if(Backpack::GetNS(%clientid,"KeysItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Keys", "KeysItems");
	if(Backpack::GetNS(%clientid,"ScrollsItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Scrolls", "ScrollsItems");
	if(Backpack::GetNS(%clientid,"UniqueItems") > 0)	Client::addMenuItem(%clientId, %cnt++ @ "Unique Items", "UniqueItems");
	Client::addMenuItem(%clientId, "xCancel", "done");
	return;
}

function processMenuSellBackpack(%clientId, %opt)
{
	if($count[%opt] > 0)
	{
		MenuSellBackpackItem(%clientid, %opt, 1);
		return;
	}

	if(%opt != "done")
		MenuSellBackpack(%clientId);

	return;
}

function MenuSellBackpackItem(%clientid, %type, %page)
{
	if(%type == "QuestItems") %disp = "Quests";
	if(%type == "KeysItems") %disp = "Keys";
	if(%type == "ScrollsItems") %disp = "Scrolls";
	if(%type == "RareItems") %disp = "Rares";
	if(%type == "UniqueItems") %disp = "Unique Items";

	Client::buildMenu(%clientId, %disp@" sell:", "SellBackpackItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $count[%type];
	%nf = Backpack::GetNS(%clientid,%type);
	%ns = GetWord(%nf,0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf,%x);
		%amnt = Backpack::HasThisStuff(%clientid,%item);
		Client::addMenuItem(%clientId, %cnt++ @%amnt@" "@ $Backpackitem[%item, "Name"], %item @ " " @ %page @" "@%type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
	}

	return;
}

function processMenuSellBackpackItem(%clientid, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuSellBackpackItemFinal(%clientid, %o, %t);
		return;
	}

	if(%o != "done")
		MenuSellBackpackItem(%clientid, %t, %p);

	return;
}

function MenuSellBackpackItemFinal(%clientid, %item, %type)
{
	%name = $Backpackitem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Backpack::HasThisStuff(%clientid,%item);

	if(%amnt > %cmnt)
		%amnt = %cmnt;

	Client::buildMenu(%clientId, %name@" ("@%cmnt@")", "SellBackpackItemFinal", true);

	Client::addMenuItem(%clientId, %cnt++ @ "Sell "@%amnt, %type@" sell "@%item@" "@%amnt);
	Client::addMenuItem(%clientId, "xCancel", "done");
	return;
}

function processMenuSellBackpackItemFinal(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	if(%clientId.bulkNum != %amnt)
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuSellBackpackItemFinal(%clientid, %item, %type);
	}
	else if(%option == "sell")
	{
		%cmnt = Backpack::HasThisStuff(%clientid,%item);
		if(%cmnt >= %amnt)
		{
			%cost = Backpack::GetSellCost(%clientid,%item) * %amnt;
			UseSkill(%clientId, $SkillHaggling, True, True);
			storeData(%clientId, "COINS", %cost, "inc");
			Backpack::TakeThisStuff(%clientid,%item,%amnt);
			Client::SendMessage(%clientId, $MsgWhite, "You received "@%cost@" coins.~wbuysellsound.wav");
			RefreshAll(%clientId);
			%clientId.bulkNum = 1;
		}
	}
	return;
}

//-----------------------------------------------------------------
function MenuStoreBackpack(%clientId)
{
	Client::buildMenu(%clientId, "Backpack store:", "StoreBackpack", true);

	if(Backpack::GetNS(%clientid,"QuestItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Quests", "QuestItems");
	if(Backpack::GetNS(%clientid,"RareItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Rares", "RareItems");
	if(Backpack::GetNS(%clientid,"KeysItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Keys", "KeysItems");
	if(Backpack::GetNS(%clientid,"ScrollsItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Scrolls", "ScrollsItems");
	if(Backpack::GetNS(%clientid,"UniqueItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Unique Items", "UniqueItems");

	Client::addMenuItem(%clientid, "wWithdraw","withdraw");
	Client::addMenuItem(%clientId, "xCancel", "done");
	return;
}

function processMenuStoreBackpack(%clientId, %opt)
{
	if($count[%opt] > 0)
	{
		MenuStoreBackpackItem(%clientid, %opt, 1);
		return;
	}

	if(%opt == "withdraw")
	{
		MenuWithdrawBackpack(%clientId);
		return;
	}

	if(%opt != "done")
		MenuStoreBackpack(%clientId);

	return;
}

function MenuWithdrawBackpack(%clientId)
{
	Client::buildMenu(%clientId, "Backpack withdraw:", "WithdrawBackpack", true);

	if(Backpack::BankGetNS(%clientid,"QuestItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Quests", "QuestItems");
	if(Backpack::BankGetNS(%clientid,"RareItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Rares", "RareItems");
	if(Backpack::BankGetNS(%clientid,"KeysItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Keys", "KeysItems");
	if(Backpack::BankGetNS(%clientid,"ScrollsItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Scrolls", "ScrollsItems");
	if(Backpack::BankGetNS(%clientid,"UniqueItems") > 0)		Client::addMenuItem(%clientId, %cnt++ @ "Unique Items", "UniqueItems");

	Client::addMenuItem(%clientid, "sStore","store");
	Client::addMenuItem(%clientId, "xCancel", "done");
	return;
}

function processMenuWithdrawBackpack(%clientId, %opt)
{
	if($count[%opt] > 0)
	{
		MenuWithdrawBackpackItem(%clientid, %opt, 1);
		return;
	}

	if(%opt == "store")
	{
		MenuStoreBackpack(%clientId);
		return;
	}

	if(%opt != "done")
		MenuWithdrawBackpack(%clientId);

	return;
}

function MenuStoreBackpackItem(%clientid, %type, %page)
{
	if(%type == "QuestItems") %disp = "Quests";
	if(%type == "KeysItems") %disp = "Keys";
	if(%type == "ScrollsItems") %disp = "Scrolls";
	if(%type == "RareItems") %disp = "Rares";
	if(%type == "UniqueItems") %disp = "Unique Items";

	Client::buildMenu(%clientId, %disp@" store:", "StoreBackpackItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $count[%type];
	%nf = Backpack::GetNS(%clientid,%type);
	%ns = GetWord(%nf,0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf,%x);
		%amnt = Backpack::HasThisStuff(%clientid,%item);
		Client::addMenuItem(%clientId, %cnt++ @%amnt@" "@ $Backpackitem[%item, "Name"], %item @ " " @ %page @" "@%type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
	}

	return;
}

function processMenuStoreBackpackItem(%clientid, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuStoreBackpackItemFinal(%clientid, %o, %t);
		return;
	}

	if(%o != "done")
		MenuStoreBackpackItem(%clientid, %t, %p);

	return;
}

function MenuStoreBackpackItemFinal(%clientid, %item, %type)
{
	%name = $Backpackitem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Backpack::HasThisStuff(%clientid,%item);

	if(%amnt > %cmnt)
		%amnt = %cmnt;

	Client::buildMenu(%clientId, %name@" ("@%cmnt@")", "StoreBackpackItemFinal", true);

	Client::addMenuItem(%clientId, %cnt++ @ "Store "@%amnt, %type@" store "@%item@" "@%amnt);
	Client::addMenuItem(%clientId, "xCancel", "done");
	return;
}

function processMenuStoreBackpackItemFinal(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	if(%clientId.bulkNum != %amnt)
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuStoreBackpackItemFinal(%clientid, %item, %type);
	}
	else if(%option == "store")
	{
		%cmnt = Backpack::HasThisStuff(%clientid,%item);
		if(%cmnt >= %amnt)
		{
			Client::SendMessage(%clientid,0,"You store "@%amnt@" "@%item@".");
			Backpack::TakeThisStuff(%clientid,%item,%amnt);
			Backpack::BankGiveThisSTuff(%clientid,%item,%amnt);
		}
	}
	return;
}

//---------------------------

function MenuWithdrawBackpackItem(%clientid, %type, %page)
{
	if(%type == "QuestItems") %disp = "Quests";
	if(%type == "KeysItems") %disp = "Keys";
	if(%type == "ScrollsItems") %disp = "Scrolls";
	if(%type == "RareItems") %disp = "Rares";
	if(%type == "UniqueItems") %disp = "Unique Items";


	Client::buildMenu(%clientId, %disp@" withdraw:", "WithdrawBackpackItem", true);
	%clientId.bulkNum = "";

	%l = 6;
	%nx = $count[%type];
	%nf = Backpack::BankGetNS(%clientid,%type);
	%ns = GetWord(%nf,0);
	%np = floor(%ns / %l);
	%lb = (%page * %l) - (%l-1);
	%ub = %lb + (%l-1);
	if(%ub > %ns)
		%ub = %ns;

	%x = %lb - 1;
	for(%i = %lb; %i <= %ub; %i++)
	{
		%x++;
		%item = getword(%nf,%x);
		%amnt = Backpack::BankHasThisStuff(%clientid,%item);
		Client::addMenuItem(%clientId, %cnt++ @%amnt@" "@ $Backpackitem[%item, "Name"], %item @ " " @ %page @" "@%type);
	}

	if(%page == 1)
	{
		if(%ns > 6) Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else if(%page == %np+1)
	{
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
		Client::addMenuItem(%clientId, "xDone", "done");
	}
	else
	{
		Client::addMenuItem(%clientId, "nNext >>", "page " @ %page+1 @" "@%type);
		Client::addMenuItem(%clientId, "p<< Prev", "page " @ %page-1 @" "@%type);
	}

	return;
}

function processMenuWithdrawBackpackItem(%clientid, %opt)
{
	%o = GetWord(%opt, 0);
	%p = GetWord(%opt, 1);
	%t = GetWord(%opt, 2);

	if(%o != "page" && %o != "done")
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuWithdrawBackpackItemFinal(%clientid, %o, %t);
		return;
	}

	if(%o != "done")
		MenuWithdrawBackpackItem(%clientid, %t, %p);

	return;
}

function MenuWithdrawBackpackItemFinal(%clientid, %item, %type)
{
	%name = $Backpackitem[%item, "Name"];
	%amnt = %clientId.bulkNum;
	%cmnt = Backpack::BankHasThisStuff(%clientid,%item);

	if(%amnt > %cmnt)
		%amnt = %cmnt;

	Client::buildMenu(%clientId, %name@" ("@%cmnt@")", "WithdrawBackpackItemFinal", true);

	Client::addMenuItem(%clientId, %cnt++ @ "Withdraw "@%amnt, %type@" withdraw "@%item@" "@%amnt);
	Client::addMenuItem(%clientId, "xCancel", "done");
	return;
}

function processMenuWithdrawBackpackItemFinal(%clientId, %opt)
{
	%type = GetWord(%opt, 0);
	%option = GetWord(%opt, 1);
	%item = GetWord(%opt, 2);
	%amnt = GetWord(%opt, 3);

	if(%clientId.bulkNum != %amnt)
	{
		if(%clientId.bulkNum < 1)	%clientId.bulkNum = 1;
		if(%clientId.bulkNum > 500)	%clientId.bulkNum = 500;
		MenuWithdrawBackpackItemFinal(%clientid, %item, %type);
	}
	else if(%option == "withdraw")
	{
		%cmnt = Backpack::BankHasThisStuff(%clientid,%item);
		if(%cmnt >= %amnt)
		{
			Client::SendMessage(%clientid,0,"You withdraw "@%amnt@" "@%item@".");
			Backpack::BankTakeThisSTuff(%clientid,%item,%amnt);
			Backpack::GiveThisStuff(%clientid,%item,%amnt);
		}
	}
	return;
}

// ------------------- //
// Non Menu Functions  //
// ------------------- //

$count["QuestItems"] = 0;
$count["RareItems"] = 0;
$count["KeysItems"] = 0;
$count["ScrollsItems"] = 0;
$count["UniqueItems"] = 0;

function Backpack::GetSellCost(%clientid,%item)
{
	%p = $HardcodedItemCost[%item];
	%cost = round(%p * ($resalePercentage/100));

	%p = round($PlayerSkill[%clientId, $SkillHaggling] / 11) / 100;
	%x = round(%cost * Cap(%p, 0.0, 1.0) );
	%cost += %x;

	return %cost;
}

function Backpack::Mug(%clientid,%id)
{
	Client::SendMessage(%clientid,1,"#mugBackpack");
}

function Backpack::Sell(%clientid,%npc)
{
	AI::sayLater(%clientid, %npc, "What would you like to sell?", True);
	MenuSellBackpack(%clientId);
}

function Backpack::Store(%clientid,%npc)
{
	AI::sayLater(%clientid, %npc, "This is the equipment you have stored from your Backpack.", True);
	MenuStoreBackpack(%clientid,"Store");
}

function Backpack::GetWeight(%clientid)
{
	%list[1] = "QuestItems";
	%list[2] = "RareItems";
	%list[3] = "KeysItems";
	%list[4] = "ScrollsItems";
	%list[4] = "UniqueItems";

	for(%s=1;%s<=4;%s++)
	{
		%type = %list[%s];
		for(%i=0;%i<=$count[%type];%i++)
		{
			%item = $Backpackitem[%i, "Num", %type];
			%amnt = Backpack::HasThisStuff(%clientid,%item);
			%weig = $AccessoryVar[%item, $Weight];
			%total += %amnt * %weig;
		}
	}
	return %total;
}

function Backpack::DropItem(%clientid,%item,%amnt,%type)
{
	%chk = Backpack::HasThisStuff(%clientid,%item);
	if(%chk >= %amnt)
	{
		Backpack::TakeThisStuff(%clientid,%item,%amnt);
		TossLootbag(%clientId, %item@" "@%amnt, 8, "*", 0, 1);
	}
}

function Backpack::GetNS(%clientid,%type)
{
	%bn = 0;
	%num = $count[%type];
	for(%i;%i<=%num;%i++)
	{
		%item = $Backpackitem[%i, "Num", %type];
		%amnt = Backpack::HasThisStuff(%clientid,%item);
		if(%amnt > 0) {
			%list = %list @" "@%item;
			%bn++;
		}
	}

	return %bn@%list;
}

function BackpackItem::Add(%name,%item,%type,%weight,%cost)
{
	%num = $count[%type]++;
	$Backpackitem[%num, "Num", %type] = %item;
	$Backpackitem[%item, "Item"] = %item;
	$Backpackitem[%item, "Name"] = %name;
	$Backpackitem[%item, "Type"] = %type;
	$AccessoryVar[%item, $Weight] = %weight;
	$HardcodedItemCost[%item] = %cost;
}

function Backpack::HasThisStuff(%clientid,%item)
{
	%item = $Backpackitem[%item, "Item"];
	%type = $Backpackitem[%item, "Type"];
	%list = fetchdata(%clientid,%type);
	%amnt = Backpack::ItemCount(%item,%list);
	return %amnt;
}

function Backpack::GiveThisStuff(%clientid, %item, %amnt, %echo)
{
	if(%amnt > 0)
	{
		%item = $Backpackitem[%item, "Item"];
		%type = $Backpackitem[%item, "Type"];
		%list = fetchdata(%clientid,%type);
		%count = Backpack::ItemCount(%item,%list);

		if(%echo) Client::sendMessage(%clientId, 0, "You received " @ %amnt @ " " @ $Backpackitem[%item, "Name"] @".");

		if(%count > 0)
		{
			%list = Backpack::RemoveFromList(%list, %item@" "@%count);
			%amnt = %amnt + %count;
		}
		%list = Backpack::AddToList(%list, %item@" "@%amnt);
		Storedata(%clientid,%type,%list);
	}
}

function Backpack::TakeThisStuff(%clientid,%item,%amnt)
{
	if(%amnt > 0)
	{
		%item = $Backpackitem[%item, "Item"];
		%type = $Backpackitem[%item, "Type"];
		%list = fetchdata(%clientid,%type);
		%count = Backpack::ItemCount(%item,%list);
		%amnt = %count - %amnt;

		%list = Backpack::RemoveFromList(%list, %item@" "@%count);
		if(%amnt > 0)	%list = Backpack::AddToList(%list, %item@" "@%amnt);

		Storedata(%clientid,%type,%list);
	}
}

function isBackpackItem(%item)
{
	%flag = false;
	if((String::ICompare($Backpackitem[%item, "Item"], %item) == 0))
		%flag = True;

	return %flag;
}

function Backpack::ItemCount(%item,%list)
{
	%count = 0;

	for(%i=0;(%w = getword(%list,%i)) != -1;%i++)
	{
		if(%w == %item)
		{
			%count = getword(%list,%i++);
			break;
		}
	}
	return %count;
}

function Backpack::AddToList(%list, %item)
{
	%list = %list @ %item @ " ";
	return %list;
}

function Backpack::RemoveFromList(%list, %item)
{
	%a = " " @ %list;
	%a = String::replace(%a, " " @ %item @ " ", " ");
	%list = String::NEWgetSubStr(%a, 1, 99999);
	return %list;
}

function Backpack::IsInList(%list, %item)
{
	%a = " " @ %list;
	if(String::findSubStr(%a, " " @ %item @ " ") != -1)
		return True;
	else
		return False;
}

function Backpack::BankStorageConversion(%clientid)
{
	%bank =  fetchData(%clientId, "BankStorage");
	for(%i=0;getword(%bank,%i)!=-1;%i++)
	{
		%a = getword(%bank,%i);
		%b = getword(%bank,%i++);

		if(IsBackpackItem(%a))
			Backpack::BankGiveThisStuff(%clientid, %a, %b);
		else
			%banklist = %banklist@%a@" "@%b@" ";
	}
	storeData(%clientId, "BankStorage", " "@%banklist);
}

function Backpack::BankGiveThisStuff(%clientid, %item, %amnt)
{
	if(%amnt > 0)
	{
		%item = $Backpackitem[%item, "Item"];
		%type = $Backpackitem[%item, "Type"];
		%list = fetchdata(%clientid,"Bank"@%type);
		%count = Backpack::ItemCount(%item,%list);

		if(%count > 0)
		{
			%list = Backpack::RemoveFromList(%list, %item@" "@%count);
			%amnt = %amnt + %count;
		}
		%list = Backpack::AddToList(%list, %item@" "@%amnt);
		Storedata(%clientid,"Bank"@%type,%list);
	}
}

function Backpack::BankTakeThisStuff(%clientid,%item,%amnt)
{
	if(%amnt > 0)
	{
		%item = $Backpackitem[%item, "Item"];
		%type = $Backpackitem[%item, "Type"];
		%list = fetchdata(%clientid,"Bank"@%type);
		%count = Backpack::ItemCount(%item,%list);
		%amnt = %count - %amnt;

		%list = Backpack::RemoveFromList(%list, %item@" "@%count);
		if(%amnt > 0)	%list = Backpack::AddToList(%list, %item@" "@%amnt);

		Storedata(%clientid,"Bank"@%type,%list);
	}
}

function Backpack::BankHasThisStuff(%clientid,%item)
{
	%item = $Backpackitem[%item, "Item"];
	%type = $Backpackitem[%item, "Type"];
	%list = fetchdata(%clientid,"Bank"@%type);
	%amnt = Backpack::ItemCount(%item,%list);
	return %amnt;
}

function Backpack::BankGetNS(%clientid,%type)
{
	%bn = 0;
	%num = $count[%type];
	for(%i;%i<=%num;%i++)
	{
		%item = $Backpackitem[%i, "Num", %type];
		%amnt = Backpack::BankHasThisStuff(%clientid,%item);
		if(%amnt > 0) {
			%list = %list @" "@%item;
			%bn++;
		}
	}

	return %bn@%list;
}

function Backpack::GetDeathItems(%clientid)
{
	%tmploot = "";
	if(fetchData(%clientId, "LCK") <= 0)
	{
		%tmploot = %tmploot @ fetchdata(%clientid,"RareItems");
		%tmploot = %tmploot @ fetchdata(%clientid,"QuestItems");
		%tmploot = %tmploot @ fetchdata(%clientid,"KeysItems");
		%tmploot = %tmploot @ fetchdata(%clientid,"UniqueItems");

		for(%i=0;getword(%tmploot,%i)!=-1;%i++)
		{
			%a = getword(%tmploot,%i);
			%b = getword(%tmploot,%i++);

			if($StealProtectedItem[%a])
			{
				%ba = Backpack::RemoveFromList(%tmploot, %a@" "@%b);
				%tmploot = %ba;
			}
			else
				Backpack::TakeThisStuff(%clientid, %a, %b);
		}
	}
	return %tmploot;
}

// ----------------------------- //
// Define the Backpack items         //
// ----------------------------- //
// %type =			 //
// KeysItems, QuestItems, 	 //
// RareItems, UniqueItems,	 //
// FoodItems                     //
// ----------------------------- //

//BackpackItem::Add(%name,%item,%type,%weight,%cost);

//Bread
//BackpackItem::Add("Bread","bread","FoodItems",0.01,50);


//Quests//
//BackpackItem::Add("Gold","Gold","QuestItems",1.3,10);
BackpackItem::Add("RubyNecklace","RubyNecklace","QuestItems",3,10);
BackpackItem::Add("RubyNecklace","RubyNecklace","uniqueItemsItems",3,10);

//Unique Items//
//BackpackItem::Add("DeathKnight Insignia","DeathKnightInsignia","uniqueItems",1,0);
//BackpackItem::Add("420 Pin","420Pin","uniqueItems",1,0);
//BackpackItem::Add("Guardians Badge","GuardiansBadge","uniqueItems",1,0);
//BackpackItem::Add("Guppy Pass","GuppyPass","uniqueItems",1,250000); ///1 is for weight, 250000 is the cost


//Rare Items//
//BackpackItem::Add("Tab","Tab","rareItems",1,0);
//BackpackItem::Add("Sun Stone","sunstone","rareItems",2.8,10000000);
//BackpackItem::Add("Black Statue","BlackStatue","rareItems",2.3,180);
//BackpackItem::Add("Skeleton Bone","skeletonbone","rareItems",15,5860);
//BackpackItem::Add("Dragon Scale","dragonscale","rareItems",1,245310);
//BackpackItem::Add("Yeti Pelt","yetispelt","rareItems",5,180);
//BackpackItem::Add("Shiny Rock","ShinyRock","rareItems",2,50);
//BackpackItem::Add("Trancephyte","trancephyte","rareItems",2,120000);