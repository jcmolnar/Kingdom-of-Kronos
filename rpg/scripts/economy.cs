function ShowCoinsDisplay(%clientId, %token)
{
	dbecho($dbechoMode, "ShowCoinsDisplay(" @ %clientId @ ")");
	
	// Tribes 1 has no cancel(). Use a generation token so stale scheduled
	// callbacks are ignored.
	if(%token == "" || %token == -1)
	{
		if(%clientId.coinDisplayToken == "" || %clientId.coinDisplayToken == -1)
			%clientId.coinDisplayToken = 0;
		
		%clientId.coinDisplayToken++;
		%token = %clientId.coinDisplayToken;
	}
	else if(%token != %clientId.coinDisplayToken)
	{
		return;
	}

	// Only display and schedule if player is still at a shop
	if(%clientId.currentShop != "" || %clientId.currentBank != "" || %clientId.currentSmith != "")
	{
		// Display current coins at bottom of screen
		%coins = fetchData(%clientId, "COINS");
		%msg = "<f1>Coins: <f2>" @ Number::Beautify(%coins, -3);
		bottomprint(%clientId, %msg, -1);

		// Schedule next update in 1 second (same token keeps this chain valid)
		schedule("ShowCoinsDisplay(" @ %clientId @ ", " @ %token @ ");", 1);
	}
	else
	{
		// Player left the shop, clear the display
		bottomprint(%clientId, "", -1);
	}
}

function StopCoinsDisplay(%clientId)
{
	dbecho($dbechoMode, "StopCoinsDisplay(" @ %clientId @ ")");
	
	// Invalidate any pending ShowCoinsDisplay schedule callback.
	if(%clientId.coinDisplayToken == "" || %clientId.coinDisplayToken == -1)
		%clientId.coinDisplayToken = 0;
	%clientId.coinDisplayToken++;
	%clientId.coinDisplaySchedule = "";

	// Clear the bottom print
	bottomprint(%clientId, "", -1);
}

function getBuyCost(%clientId, %item)
{
	dbecho($dbechoMode, "getBuyCost(" @ %clientId @ ", " @ %clientId.currentShop @ ", " @ %item @ ")");

	%aiName = fetchData(%clientId.currentShop, "BotInfoAiName");
	// Fallback to Client::getName if BotInfoAiName is invalid
	if(%aiName == "" || %aiName == -1 || %aiName == "0")
		%aiName = Client::getName(%clientId.currentShop);

	%baseCost = GetItemCost(%item);
	if(%aiName != "" && %aiName != -1 && %aiName != "0" && $NewItemBuyCost[%aiName, %item] != "")
		%cost = $NewItemBuyCost[%aiName, %item];
	else
		%cost = %baseCost;

	// Calculate maximum possible discounts for minimum floor
	%maxHagglingPercent = 0.5;  // 50% max from 1000 haggling skill
	%maxTournyRank = 10;  // Assuming max TournyRank is 10
	%maxTournyRankPercent = 0.03 * %maxTournyRank;  // 30% max
	// Calculate minimum cost after applying maximum discounts multiplicatively
	%minCost = round(%baseCost * (1.0 - %maxHagglingPercent) * (1.0 - %maxTournyRankPercent));
	
	// Apply haggling reduction multiplicatively (first)
	%hagglingPercent = round($PlayerSkill[%clientId, $SkillHaggling] / 50) / 100;
	%hagglingPercent = Cap(%hagglingPercent, 0.0, 0.5);
	%cost = round(%cost * (1.0 - %hagglingPercent));
	
	// Apply TournyRank reduction multiplicatively (second, on already-reduced cost)
	%tournyRankPercent = 0.03 * fetchData(%clientId, "TournyRank");
	%cost = round(%cost * (1.0 - %tournyRankPercent));
	
	// Ensure cost never goes below the minimum (result after maximum discounts)
	if(%cost < %minCost)
		%cost = %minCost;

	return %cost;
}
function getSellCost(%clientId, %item)
{
	dbecho($dbechoMode, "getSellCost(" @ %clientId @ ", " @ %clientId.currentShop @ ", " @ %item @ ")");

	%aiName = fetchData(%clientId.currentShop, "BotInfoAiName");
	// Fallback to Client::getName if BotInfoAiName is invalid
	if(%aiName == "" || %aiName == -1 || %aiName == "0")
		%aiName = Client::getName(%clientId.currentShop);

	%p = GetItemCost(%item);
	if(%aiName != "" && %aiName != -1 && %aiName != "0" && $NewItemSellCost[%aiName, %item] != "")
		%cost = $NewItemSellCost[%aiName, %item];
	else
		%cost = round(%p * ($resalePercentage/100));

	// Apply haggling bonus multiplicatively (first)
	%hagglingPercent = round($PlayerSkill[%clientId, $SkillHaggling] / 14) / 100;
	%hagglingPercent = Cap(%hagglingPercent, 0.0, 0.5);
	%cost = round(%cost * (1.0 + %hagglingPercent));
	
	// Apply TournyRank bonus multiplicatively (second, on already-increased cost)
	%tournyRankPercent = 0.03 * fetchData(%clientId, "TournyRank");
	%cost = round(%cost * (1.0 + %tournyRankPercent));

	return %cost;
}
function GetItemCost(%item)
{
	dbecho($dbechoMode, "GetItemCost(" @ %item @ ")");

	if($HardcodedItemCost[%item] != "")
		return $HardcodedItemCost[%item];

	return $ItemCost[%item];		
}

function BuySell(%player, %item, %delta, %buyORsell)
{
	dbecho($dbechoMode, "BuySell(" @ %player @ ", " @ %item @ ", " @ %delta @ ", " @ %buyORsell @ ")");

	// IF - Cost positive selling    IF - Cost Negative buying 

	%clientId = Player::getClient(%player);
	%station = %player.Station;
	%stationName = GameBase::getDataName(%station); 

	// Calculate cost for both admins and regular players
	if(%buyORsell == BUY)
	{
		%cost = getBuyCost(%clientId, %item) * %delta * -1;
	}
	else if(%buyORsell == SELL)
	{
		%cost = getSellCost(%clientId, %item) * %delta;
	}

	if(%clientId.adminLevel < 5)
	{
		//Add entry to merchant counter.
		//Admin purchases do not count towards the economy.
		%aiName = fetchData(%clientId.currentShop, "BotInfoAiName");
		// Fallback to Client::getName if BotInfoAiName is invalid
		if(%aiName == "" || %aiName == -1 || %aiName == "0")
			%aiName = Client::getName(%clientId.currentShop);
		if(%buyORsell == BUY)
		{
			$MerchantCounterB[%aiName, %item] += %delta;
		}
		else if(%buyORsell == SELL)
		{
			$MerchantCounterS[%aiName, %item] += %delta;
		}
	}

	UseSkill(%clientId, $SkillHaggling, True, True);
	storeData(%clientId, "COINS", %cost, "inc");

	%txt = "<f1><jc>COINS: " @ Number::Beautify(fetchData(%clientId, "COINS"), -3);
	Client::setInventoryText(%clientId, %txt);
}

function buyItem(%clientId, %item)
{
	dbecho($dbechoMode, "buyItem(" @ %clientId @ ", " @ %item @ ")");

	if(IsDead(%clientId))
		return;

	// Check for robe talent requirements (must unlock talent before purchasing)
	if(%item == "JudgementRobe" && !Ascension::HasTalent(%clientId, "JudgementRobeTalent"))
	{
		Client::sendMessage(%clientId, $MsgRed, "You must unlock the Judgement Robe talent first! Visit an Ascension trainer.");
		return;
	}
	if(%item == "StormRobe" && !Ascension::HasTalent(%clientId, "StormRobeTalent"))
	{
		Client::sendMessage(%clientId, $MsgRed, "You must unlock the Storm Robe talent first! Visit an Ascension trainer.");
		return;
	}
	if(%item == "VoidRobe" && !Ascension::HasTalent(%clientId, "VoidRobeTalent"))
	{
		Client::sendMessage(%clientId, $MsgRed, "You must unlock the Void Robe talent first! Visit an Ascension trainer.");
		return;
	}

	%player = Client::getOwnedObject(%clientId);

	if(Client::isItemShoppingOn(%clientId, %item))
	{
		if(%clientId.currentBank != "")
		{
			//============================================================
			//  Player is at a bank, removing from his/her bank storage
			//============================================================
			if(%clientId.bulkNum != "")
				%n = %clientId.bulkNum;
			else
				%n = 1;
			%cnt = GetStuffStringCount(fetchData(%clientId, "BankStorage"), %item);
			if(%cnt >= %n)
			{
				Player::incItemCount(%clientId, %item, %n);
				storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %item, -%n));
	
				SetupBank(%clientId, %clientId.currentBank);	//refresh

				RefreshAll(%clientId);
			}
			else
				Client::sendMessage(%clientId, $MsgRed, "You only have " @ %cnt @ " of this item.~wC_BuySell.wav");
		}
		else if(%clientId.currentShop != "")
		{
			//=========================================
			//  Player is at a regular shop
			//=========================================

			%cost = getBuyCost(%clientId, %item);
			if($LastClickItemB[%clientId, %item] != %item)
			{
				Client::sendMessage(%clientId, $MsgWhite, "The " @ %item.description @ " will cost you " @ Number::Beautify(%cost, -3) @ " coins.");
				%msg = WhatIs(%item);
				KronosExamineInfo(%clientId, %msg, floor(String::len(%msg) / 20));

				$LastClickItemB[%clientId, %item] = %item;
				schedule("$LastClickItemB[" @ %clientId @ ", " @ %item @ "] = \"\";", 5);
				%clientId.bulkNum = 1;

				return 0;
			}
			else
			{
				if(checkResources(%player,%item,%cost,%clientId.bulkNum) && !IsDead(%clientId))
				{
					// Route belt items to Belt system, others to player inventory
					if(isBeltItem(%item))
						Belt::GiveThisStuff(%clientId, %item, %clientId.bulkNum);
					else
						Player::incItemCount(%clientId, %item, %clientId.bulkNum);
					
					BuySell(%player, %item, %clientId.bulkNum, BUY);
		
					RefreshAll(%clientId);
					%clientId.bulkNum = 1;

					// Show persistent coin display
					ShowCoinsDisplay(%clientId);

					return 1;
				}
			}
		}
		// TEMPORARILY DISABLED - steal code block (pickpocket/mug)
		//else if(%clientId.currentInvSteal != "")
		//{
		//	//=========================================
		//	//  Player is trying to pickpocket/mug
		//	//=========================================
		//	%cl = %clientId.currentInvSteal;
		//
		//	%itemweight = GetAccessoryVar(%item, $Weight);
		//
		//	if(Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(%cl)) > 2)
		//	{
		//		remotePlayMode(%clientId);
		//		Client::sendMessage(%clientId, $MsgWhite, "Your target has wandered off...");
		//	}
		//	else if(%clientId.TryingToSteal)
		//	{
		//		Client::sendMessage(%clientId, $MsgRed, "You are already trying to steal an item...");
		//	}
		//	else
		//	{
		//		if(%clientId.stealType == 1)
		//		{
		//			%max = 10;
		//			%w = "pickpocket";
		//			%canStealEquipped = False;
		//			%weightToTimeFactor = 0.6;
		//		}
		//		else if(%clientId.stealType == 2)
		//		{
		//			%max = 99999;
		//			%w = "mug";
		//			%canStealEquipped = True;
		//			%weightToTimeFactor = 0.55;
		//		}
		//
		//		%eflag = "";
		//		%fitem = %item;
		//		if(%item.className == Equipped)
		//		{
		//			%fitem = String::getSubStr(%item, 0, String::len(%item)-1);
		//			if(%canStealEquipped)
		//				%eflag = True;
		//		}
		//		else
		//			%eflag = True;
		//
		//		if(%eflag)
		//		{
		//			if(%itemweight <= %max)
		//			{
		//				if(!$StealProtectedItem[%item])
		//				{
		//					%icnt = Player::getItemCount(%cl, %item);
		//					if(%icnt)
		//					{
		//						Client::sendMessage(%clientId, $MsgBeige, "Attempting to " @ %w @ "...");
		//
		//						%a = %itemweight * %weightToTimeFactor;
		//						%b = (1000 - $PlayerSkill[%clientId, $SkillStealing]) / 50;
		//						%c = Cap(%b, 0, "inf");
		//						%d = %c * %weightToTimeFactor;
		//
		//						%time = %a + %d;
		//
		//						%clientId.TryingToSteal = True;
		//						schedule("DoSteal(" @ %clientId @ ", " @ %cl @ ", " @ %icnt @ ", " @ %item @ ", " @ %fitem @ ", \"" @ %w @ "\");", %time);
		//					}
		//				}
		//				else
		//					Client::sendMessage(%clientId, $MsgWhite, "This item is magically protected from your thieving hands...");
		//			}
		//			else
		//				Client::sendMessage(%clientId, $MsgWhite, "This item is too heavy to " @ %w @ "...");
		//		}
		//		else
		//			Client::sendMessage(%clientId, $MsgWhite, "You can't " @ %w @ " an equipped item...");
		//	}
		//}
		else if(%clientId.currentSmith != "")
		{
			//=================================================
			//  Player is at a blacksmith unselecting an item
			//=================================================
			BlackSmithClick(%clientId, %item, -1);
		}
		else
		{
			storeData(%clientId, "TempPack", SetStuffString(fetchData(%clientId, "TempPack"), %item, -1));
			SetupCreatePack(%clientId);
		}
 	}

	return 0;
}
// TEMPORARILY DISABLED - DoSteal function
//function DoSteal(%clientId, %cl, %itemcount, %item, %fitem, %w)
//{
//	dbecho($dbechoMode, "DoSteal(" @ %clientId @ ", " @ %cl @ ", " @ %itemcount @ ", " @ %item @ ", " @ %fitem @ ", " @ %w @ ")");
//
//	%victimName = Client::getName(%cl);
//	%stealerName = Client::getName(%clientId);
//
//	%icnt = Player::getItemCount(%cl, %item);
//
//	%clientId.TryingToSteal = "";
//
//	//weights
//	%itemweight = GetAccessoryVar(%item, $Weight);
//	%wweight = 10;
//	if(Player::getMountedItem(%clientId, $WeaponSlot) == %item || %item.className == Equipped)
//		%handweight = 5.0;
//	else
//		%handweight = 1.0;
//	%FailWeight = (%itemweight * %wweight) * %handweight;
//
//	if(Vector::getDistance(GameBase::getPosition(%clientId), GameBase::getPosition(%cl)) > 2)
//		Client::sendMessage(%clientId, $MsgWhite, "Your target has wandered off...");
//	else
//	{
//		%r1 = GetRoll("1d" @ $PlayerSkill[%clientId, $SkillStealing]);
//		%r2 = GetRoll("1d" @ ($PlayerSkill[%cl, $SkillStealing] + $PlayerSkill[%cl, $SkillDodging]));
//		%b = %r1 - %r2;
//		%a = %b - %FailWeight;
//		if(%a > 0 && %itemcount == %icnt)
//		{
//			Client::sendMessage(%clientId, $MsgBeige, "You successfully stole a " @ %fitem.description @ " from " @ %victimName @ "!");
//			Player::decItemCount(%cl, %item, 1);
//			Player::incItemCount(%clientId, %fitem, 1);
//			PerhapsPlayStealSound(%clientId, %clientId.stealType);
//		
//			SetupInvSteal(%clientId, %cl);
//					
//			RefreshAll(%clientId);
//			RefreshAll(%cl);
//				
//			UseSkill(%clientId, $SkillStealing, True, True);
//			PostSteal(%clientId, True, %clientId.stealType, %cl);
//
//			return True;
//		}
//		else
//		{
//			Client::sendMessage(%clientId, $MsgRed, "You failed to " @ %w @ " " @ %victimName @ "!");
//			Client::sendMessage(%cl, $MsgRed, %stealerName @ " just failed to " @ %w @ " you!");
//	
//			UseSkill(%clientId, $SkillStealing, False, True);
//			PostSteal(%clientId, False, %clientId.stealType, %cl);
//		}
//	}
//
//	//force kick from Inv Screen
//	remotePlayMode(%clientId);
//
//	return False;
//}

// TEMPORARILY DISABLED - HasTheftFlag function (NOTE: Also used in playerdamage.cs - will comment that usage too)
//function HasTheftFlag(%clientId)
//{
//	%b = AddBonusStatePoints(%clientId, "Theft");
//
//	if(%b >= 1)
//		return True;
//	else
//		return False;
//}

function remoteBuyItem(%clientId, %type)
{
	dbecho($dbechoMode, "remoteBuyItem(" @ %clientId @ ", " @ %type @ ")");

	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastWaitActionTime > $waitActionDelay)
	{
		%clientId.lastWaitActionTime = %time;

		%item = getItemData(%type);
		buyItem(%clientId, %item);
	}
}

function sellItem(%clientId, %item)
{
	dbecho($dbechoMode, "sellItem(" @ %clientId @ ", " @ %item @ ")");

	if(IsDead(%clientId))
		return;

	// Belt-weapon sync: unequip belt state if this transfers the shell of
	// the equipped belt weapon (phantom mounts are count 0 and can't sell)
	BeltWeapon::GuardShellTransfer(%clientId, %item);

	%player = Client::getOwnedObject(%clientId);

	if(%clientId.currentShop != "" || %clientId.currentBank != "" || %clientId.currentSmith != "")
	{
		if(%clientId.currentBank != "")
		{
			//============================================================
			//  Player is at a bank, adding to his/her bank storage
			//============================================================
			if(CountObjInList(fetchData(%clientId, "BankStorage")) / 2 < 50)
			{
				if(%clientId.bulkNum != "")
					%n = %clientId.bulkNum;
				else
					%n = 1;
				%cnt = Player::getItemCount(%clientId, %item);
				if(%cnt >= %n)
				{
					if(%item.className != Equipped)
					{
						Player::decItemCount(%clientId, %item, %n);
						storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %item, %n));
		
						SetupBank(%clientId, %clientId.currentBank);	//refresh
		
						RefreshAll(%clientId);
					}
					else
						Client::sendMessage(%clientId, $MsgRed, "Unequip this item before storing it.~wC_BuySell.wav");
				}
				else
					Client::sendMessage(%clientId, $MsgRed, "You only have " @ %cnt @ " of this item.~wC_BuySell.wav");
	
				return 1;
			}
			else
				Client::sendMessage(%clientId, $MsgRed, "You can only store 50 different types of items.~wC_BuySell.wav");
		}
		else if(%clientId.currentShop != "")
		{
			%nitem = getCroppedItem(%item);

			//=========================================
			//  Player is at a regular shop
			//=========================================
			if($LastClickItemS[%clientId, %item] != %item)
			{
				%cost = getSellCost(%clientId, %item);
				Client::sendMessage(%clientId, $MsgWhite, "This merchant will give you " @ Number::Beautify(%cost, -3) @ " coins for the " @ %nitem.description @ ".");
				%msg = WhatIs(%item);
				KronosExamineInfo(%clientId, %msg, floor(String::len(%msg) / 20));

				// BUGFIX: token was stored under the CROPPED name (%nitem) but the
				// confirm check above reads the RAW name (%item) - for Equipped-class
				// items the keys never matched, so the second click re-showed the
				// price forever instead of reaching the "cannot sell equipped" branch
				$LastClickItemS[%clientId, %item] = %item;
				schedule("$LastClickItemS[" @ %clientId @ ", " @ %item @ "] = \"\";", 5);
				%clientId.bulkNum = 1;

				return 0;
			}
			else
			{
				// Check belt items first, then player inventory
				if(isBeltItem(%item))
					%itemCnt = Belt::HasThisStuff(%clientId, %item);
				else
					%itemCnt = Player::getItemCount(%clientId, %item);
					
				if(%item.className == Equipped)
				{
					Client::sendMessage(%clientId, $MsgRed, "You cannot sell an equipped item.~wC_BuySell.wav");
				}
				else if(%itemCnt && !IsDead(%clientId))
				{
					if(%clientId.bulkNum > %itemCnt)
						%clientId.bulkNum = %itemCnt;

					if($LoreItem[%item])
						Client::sendMessage(%clientId, $MsgRed, "(You have sold a lore item)");
	
					// Get count from appropriate system
					if(isBeltItem(%item))
						%count = Belt::HasThisStuff(%clientId, %item);
					else
						%count = Player::getItemCount(%clientId, %item);
						
					%numsell = %clientId.bulkNum;
	
					BuySell(%player, %item, %clientId.bulkNum, SELL);
					
					// CRITICAL: Auto-unequip if selling equipped items
					if(isBeltItem(%item))
					{
						// Check item category to determine equip type
						%category = $BeltItem[%item, "Type"];
						if(%category == "Accessories")
						{
							// Unequip as many instances as we're selling
							for(%unequipCount = 0; %unequipCount < %numsell; %unequipCount++)
							{
								if(Belt::IsAccessoryEquipped(%clientId, %item))
									Belt::UnequipAccessory(%clientId, %item);
							}
						}
						else if(%category == "Armor" && fetchData(%clientId, "EquippedBeltArmor") == %item)
						{
							Belt::UnequipArmor(%clientId, %item);
						}
					}
					
					// Remove from appropriate system
					if(isBeltItem(%item))
						Belt::TakeThisStuff(%clientId, %item, %numsell);
					else
						Player::setItemCount(%player, %item, (%count-%numsell));
					Client::SendMessage(%clientId, $MsgWhite, "~wbuysellsound.wav");
	
					RefreshAll(%clientId);
					%clientId.bulkNum = 1;

					// Show persistent coin display
					ShowCoinsDisplay(%clientId);

					return 1;
				}
			}
		}
		else if(%clientId.currentSmith != "")
		{
			//=========================================
			//  Player is at a blacksmith
			//=========================================
			BlackSmithClick(%clientId, %item, 1);
		}
	}
	else
	{
		if(%item.className != Equipped && !$LoreItem[%item])
		{
			storeData(%clientId, "TempPack", SetStuffString(fetchData(%clientId, "TempPack"), %item, 1));
			SetupCreatePack(%clientId);
			%msg = WhatIs(%item);
			KronosExamineInfo(%clientId, %msg, floor(String::len(%msg) / 20));
		}
		else
		{
			Client::sendMessage(%clientId, $MsgRed, "You can't select this item.~wC_BuySell.wav");
			%msg = WhatIs(%item);
			KronosExamineInfo(%clientId, %msg, floor(String::len(%msg) / 20));
		}
	}

	return 0;
}
function remoteSellItem(%clientId, %type)
{
	dbecho($dbechoMode, "remoteSellItem(" @ %clientId @ ", " @ %type @ ")");

	%time = getIntegerTime(true) >> 5;
	if(%time - %clientId.lastWaitActionTime > $waitActionDelay)
	{
		%clientId.lastWaitActionTime = %time;

		%item = getItemData(%type);
		// Phase D: on a vanilla client, DOUBLE-CLICKING an inventory row sends
		// "sellItem" (stock GUI.CS InventoryList::onDoubleClick). A VSlot row is a
		// display proxy for a belt weapon, not a sellable ItemData - selling the
		// placeholder would hand out coins for nothing. Block it and point the
		// player at the real path. No-op with the feature off.
		if(VSlot::IsSlotItem(%item))
		{
			Client::sendMessage(%clientId, $MsgWhite, "That's a backpack weapon - select it and press Use to equip it, or sell it at a merchant.");
			return;
		}
		sellItem(%clientId, %item);
	}
}


function checkResources(%player, %item, %cost, %delta, %noMessage)
{
	dbecho($dbechoMode, "checkResources(" @ %player @ ", " @ %item @ ", " @ %cost @ ", " @ %delta @ ", " @ %noMessage @ ")");

	%clientId = Player::getClient(%player);

	if(%cost * %delta > fetchData(%clientId, "COINS") && %clientId.adminLevel < 4)
	{
		if(%noMessage == "")
			Client::sendMessage(%clientId, $MsgRed, "You cannot afford the " @ %item.description @ ".~wC_BuySell.wav");
		return 0;
	}
	return 1;
}

function CompleteSmith(%clientId, %cost, %sc, %tempsmith, %multiplier)
{
	dbecho($dbechoMode, "CompleteSmith(" @ %clientId @ ", " @ %cost @ ", " @ %sc @ ", " @ %tempsmith @ ", " @ %multiplier @ ")");

	%clientId.IsSmithing = "";

	// Player disconnected during the 5.5s smith delay - abort (never operate on
	// a possibly-recycled clientId's bank)
	if(Client::getName(%clientId) == "" || Client::getName(%clientId) == -1)
		return;

	if(fetchData(%clientId, "COINS") < %cost)
		return;

	// DUPE FIX: #smith deposits the ingredients into BankStorage and this
	// function withdraws them 5.5s later. Verify they are STILL in the bank -
	// otherwise a player could withdraw them at a banker during the delay and
	// keep the ingredients while still receiving the smithed result.
	for(%i = 0; (%w = GetWord(%tempsmith, %i)) != -1; %i+=2)
	{
		%need = GetWord(%tempsmith, %i+1) * %multiplier;
		if(GetStuffStringCount(fetchData(%clientId, "BankStorage"), %w) < %need)
		{
			AI::sayLater(%clientId, %clientId.currentSmith, "Hey - where did the materials go? No deal.", True);
			return;
		}
	}

	storeData(%clientId, "COINS", %cost, "dec");
	playSound(SoundMoney1, GameBase::getPosition(%clientId));
	GiveThisStuff(%clientId, $SmithComboResult[%sc], True, %multiplier);

	for(%i = 0; (%w = GetWord(%tempsmith, %i)) != -1; %i+=2)
	{
		%w2 = GetWord(%tempsmith, %i+1) * %multiplier;
		storeData(%clientId, "BankStorage", SetStuffString(fetchData(%clientId, "BankStorage"), %w, -%w2));
	}

	AI::sayLater(%clientId, %clientId.currentSmith, "Here you go.", True);
}

function BlackSmithClick(%clientId, %item, %delta)
{
	dbecho($dbechoMode, "BlackSmithClick(" @ %clientId @ ", " @ %item @ ", " @ %delta @ ")");

	if(%clientId.IsSmithing)
	{
		Client::sendMessage(%clientId, $MsgRed, "The blacksmith is busy...");
	}
	else
	{
		if(%item.className != Equipped)
		{
			storeData(%clientId, "TempSmith", SetStuffString(fetchData(%clientId, "TempSmith"), %item, %delta));
			SetupBlacksmith(%clientId, %clientId.currentSmith);

			%tempsmith = LTrim(fetchData(%clientId, "TempSmith"));
			if((%sc = GetSmithCombo(%tempsmith)) != 0)
			{
				%cost = GetSmithComboCost(%clientId, %sc);

				Client::sendMessage(%clientId, $MsgWhite, "It will cost you " @ Number::Beautify(%cost, -3) @ " coins to smith these items.~wcanSmith.wav");
				Client::sendMessage(%clientId, $MsgBeige, "(type #smith to accept the cost and start smithing)");

				return 0;
			}
			else
			{
				%cnt = GetStuffStringCount(fetchData(%clientId, "TempSmith"), %item);
				Client::sendMessage(%clientId, $MsgRed, "Invalid combination. (" @ %cnt @ " " @ %item.description @ ")");
			}
		}
	}
}

function GetSmithCombo(%tempsmith)
{
	dbecho($dbechoMode, "GetSmithCombo(" @ %tempsmith @ ")");

	for(%i = 1; $SmithCombo[%i] != ""; %i++)
	{
		if(IsStuffStringEquiv(%tempsmith, $SmithCombo[%i], True))
			return %i;
	}
	return 0;
}

function GetSmithComboCost(%clientId, %sc)
{
	dbecho($dbechoMode, "GetSmithComboCost(" @ %clientId @ ", " @ %sc @ ")");

	%c1 = GetStuffStringCost(%clientId, $SmithCombo[%sc]);
	%c2 = GetStuffStringCost(%clientId, $SmithComboResult[%sc]);

	return Cap( round(%c2 - (%c1 * 1.35)), 1, "inf");
}

function GetStuffStringCost(%clientId, %itemlist)
{
	dbecho($dbechoMode, "GetStuffStringCost(" @ %clientId @ ", " @ %itemlist @ ")");

	%cost = 0;
	// BUGFIX: was %i++ - the loop re-visited every COUNT word as if it were an
	// item name (harmless only because numeric "items" cost 0, but wrong)
	for(%i = 0; (%w = GetWord(%itemlist, %i)) != -1; %i += 2)
	{
		%w2 = GetWord(%itemlist, %i+1);
		%c = getBuyCost(%clientId, %w) * %w2;
		%cost += %c;
	}

	return %cost;
}
