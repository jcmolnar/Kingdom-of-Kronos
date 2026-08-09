// Banking.cs
// Modern Banker Menu System with Dynamic Client::buildMenu interface

function SetupBankDefault(%clientId, %bankerId)
{
	dbecho($dbechoMode, "SetupBankDefault(" @ %clientId @ ", " @ %bankerId @ ")");
	
	%clientId.currentBankBot = %bankerId;
	
	// Show 'bulk' hint for any sub-menus that might use it
	%msg = "<jc><f2>To Deposit/Withdraw 'bulk' Coins or Items, please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 1,000,000,000";
	bottomprint(%clientId, %msg, 10);
	
	%header = "Banker Menu (M:$" @ numFormat(fetchData(%clientId, "COINS")) @ " B:$" @ Bank::Format(%clientId) @ ")";
	Client::buildMenu(%clientId, %header, "Banker", true);
	
	Client::addMenuItem(%clientId, "1Deposit Coins", "deposit");
	Client::addMenuItem(%clientId, "2Withdraw Coins", "withdraw");
	Client::addMenuItem(%clientId, "3Access Storage", "storage");
	Client::addMenuItem(%clientId, "4Access Backpack/Belt", "backpack");
	Client::addMenuItem(%clientId, "qExit", "exit");
}

function processMenuBanker(%clientId, %opt)
{
	dbecho($dbechoMode, "processMenuBanker(" @ %clientId @ ", " @ %opt @ ")");
	
	%bankerId = %clientId.currentBankBot;
	
	if(%opt == "deposit")
	{
		Banker::CoinMenu(%clientId, %bankerId, "deposit");
	}
	else if(%opt == "withdraw")
	{
		Banker::CoinMenu(%clientId, %bankerId, "withdraw");
	}
	else if(%opt == "storage")
	{
		SetupBank(%clientId, %bankerId);
	}
	else if(%opt == "backpack")
	{
		Belt::Store(%clientId, %bankerId);
	}
	else if(%opt == "exit")
	{
		Client::cancelMenu(%clientId);
	}
}

function Banker::CoinMenu(%clientId, %bankerId, %mode)
{
	dbecho($dbechoMode, "Banker::CoinMenu(" @ %clientId @ ", " @ %bankerId @ ", " @ %mode @ ")");
	
	%clientId.currentBankMode = %mode;
	
	%disp = "take out";
	if(%mode == "deposit")
		%disp = "hold";
		
	%msg = "<jc><f2>To " @ %mode @ " 'bulk' Coins, please enter your desired 'bulk' number now!\n\n'Bulk' numbers must be greater than 0 and less than 1,000,000,000";
	bottomprint(%clientId, %msg, 10);
	
	%header = "How much do you want to " @ %disp @ "?";
	Client::buildMenu(%clientId, %header, "BankerCoins", true);
	
	Client::addMenuItem(%clientId, "1   100", "100");
	Client::addMenuItem(%clientId, "2  1,000", "1000");
	Client::addMenuItem(%clientId, "3  10,000", "10000");
	Client::addMenuItem(%clientId, "4 100,000", "100000");
	Client::addMenuItem(%clientId, "5    ALL", "all");
	Client::addMenuItem(%clientId, "5    ALL", "all");
	Client::addMenuItem(%clientId, "c CUSTOM (Type #)", "custom");
	Client::addMenuItem(%clientId, "x BACK", "back");
}

function processMenuBankerCoins(%clientId, %opt)
{
	dbecho($dbechoMode, "processMenuBankerCoins(" @ %clientId @ ", " @ %opt @ ")");
	
	%bankerId = %clientId.currentBankBot;
	%mode = %clientId.currentBankMode;
	
	if(%opt == "back")
	{
		SetupBankDefault(%clientId, %bankerId);
		return;
	}
	
	if(%opt == "custom")
	{
		// Check if player has typed a bulk number
		%val = %clientId.bulkNum;
		if(%val != "" && floor(%val) > 0)
		{
			// Use the typed value
			%amount = floor(%val);
		}
		else
		{
			// No value typed yet
			Client::sendMessage(%clientId, $MsgBeige, "Please type the amount in CHAT first, then click Custom.");
			// Re-show menu so they can try again
			Banker::CoinMenu(%clientId, %bankerId, %mode);
			return;
		}
	}
	else if(%opt == "all")
	{
		if(%mode == "deposit")
			%amount = fetchData(%clientId, "COINS");
		else
			%amount = "all";
	}
	else
	{
		%amount = %opt;
	}
	
	if(%amount == "all")
		%c = "all";
	else
		%c = SafeFloor(%amount);
	if(%c != "all" && %c <= 0)
	{
		Client::sendMessage(%clientId, $MsgRed, "Invalid amount.");
	}
	else
	{
		if(%mode == "deposit")
		{
			%moved = Bank::DepositFromCoins(%clientId, %c);
			if(%moved > 0)
			{
				RefreshAll(%clientId);
				AI::sayLater(%clientId, %bankerId, "You have given me " @ Number::Beautify(%moved, -3) @ " coins. You are now carrying " @ Number::Beautify(fetchData(%clientId, "COINS"), -3) @ " coins.", True);
				playSound(SoundMoney1, GameBase::getPosition(%bankerId));
			}
			else
				AI::sayLater(%clientId, %bankerId, "You don't have that many coins.", True);
		}
		else // withdraw
		{
			%moved = Bank::WithdrawToCoins(%clientId, %c);
			if(%moved > 0)
			{
				RefreshAll(%clientId);
				AI::sayLater(%clientId, %bankerId, "I have given you " @ Number::Beautify(%moved, -3) @ " coins. I now have " @ Bank::Format(%clientId) @ " of yours.", True);
				playSound(SoundMoney1, GameBase::getPosition(%clientId));
			}
			else
				AI::sayLater(%clientId, %bankerId, "You don't have that many coins in the bank.", True);
		}
	}
	
	// Stay in the menu for more transactions
	Banker::CoinMenu(%clientId, %bankerId, %mode);
}
