//=============================================================================
// LasVegas.cs - Las Vegas Gambling Minigames
// This file provides bot dialogue handling for Las Vegas gambling NPCs
//=============================================================================

// Handler function called from comchat.cs
// Returns true if this file handled the bot type, false otherwise
function LasVegas_HandleBotDialogue(%botType, %TrueClientId, %closestId, %aiName, %message, %cropped, %initTalk)
{
	//=========================================================================
	// GAMBLER (Honest Jim) - Bag Pick Game
	//=========================================================================
	if(%botType == "gambler")
	{
		%trigger[2] = "game";
		%trigger[3] = "okay";
		%trigger[4] = "coins";
		%trigger[5] = "exp";
		%trigger[6] = "two";
		%trigger[7] = "four";
		%pos[1] = "-4887 865.135 701.498";
		%pos[2] = "-4885 865.135 701.498";
		%pos[3] = "-4883 865.135 701.498";
		%pos[4] = "-4881 865.135 701.498";
		
		// Check if another player is using the game
		if($GambleBot != "" && %TrueClientId != getWord($GambleBot,0))
		{
			AI::sayLater(%TrueClientId,%closestId,"I'm terribly sorry, but right now another customer is having a shot at my game. Could you wait a few more minutes, please?",true);
			$state[%closestId,%TrueClientId] = "";
			return true;
		}
		// 30-second cooldown between games
		%lastGamble = $LastGambleTime[%TrueClientId];
		%now = getSimTime();
		if(%lastGamble != "" && (%now - %lastGamble) < 30)
		{
			%remaining = 30 - floor(%now - %lastGamble);
			AI::sayLater(%TrueClientId,%closestId,"Hold your horses! Wait " @ %remaining @ " more seconds.",true);
			$state[%closestId,%TrueClientId] = "";
			return true;
		}
		
		if(%initTalk || $state[%closestId,%TrueClientId] != "")
		{
			if($state[%closestId,%TrueClientId] == "")
			{
				if(%initTalk)
				{
					AI::sayLater(%TrueClientId,%closestId,"Hello! Care to have a go at a little GAME?",true);
					$state[%closestId,%TrueClientId] = 1;
				}
			}
			else if($state[%closestId,%TrueClientId] == 1)
			{
				if(String::findSubStr(%message,%trigger[2]) != -1)
				{
					AI::sayLater(%TrueClientId,%closestId,"The Rules: You wager COINS. Choose one of the bags I lay out. Win = profit, Lose = lose your bet! 10 second time limit. [OKAY]",true);
					$state[%closestId,%TrueClientId] = 2;
				}
			}
			else if($state[%closestId,%TrueClientId] == 2)
			{
				if(String::findSubStr(%message,%trigger[3]) != -1)
				{
					AI::sayLater(%TrueClientId,%closestId,"How many COINS do you bet?",true);
					$state[%closestId,%TrueClientId] = 4;
				}
			}
			else if($state[%closestId,%TrueClientId] == 4)
			{
				%amt = floor(%cropped);
				if(%amt > 0 && fetchData(%TrueClientId,"COINS") >= %amt)
				{
					if(%amt < 500)
						AI::sayLater(%TrueClientId,%closestId,"A modest wager! TWO or FOUR packs?",true);
					else if(%amt < 10000)
						AI::sayLater(%TrueClientId,%closestId,"Nice wager! TWO or FOUR packs?",true);
					else
						AI::sayLater(%TrueClientId,%closestId,"Impressive wager! TWO or FOUR packs?",true);
					$GambleAmount[%TrueClientId] = %amt;
					$state[%closestId,%TrueClientId] = 5;
				}
				else
				{
					AI::sayLater(%TrueClientId,%closestId,"You can't afford that bet!",true);
					$state[%closestId,%TrueClientId] = "";
				}
			}
			else if($state[%closestId,%TrueClientId] == 5)
			{
				if(String::findSubStr(%message,%trigger[6]) != -1)
				{
					// Deduct bet upfront
					storeData(%TrueClientId, "COINS", -$GambleAmount[%TrueClientId], "inc");
					AI::sayLater(%TrueClientId,%closestId,"Choose a bag! You have 10 seconds! No refunds! Hurry up.",true);
					$state[%closestId,%TrueClientId] = "";
					$LastGambleTime[%TrueClientId] = getSimTime();
					%rnd = 1 + floor(getRandom()*4);
					%rnd2 = 1 + floor(getRandom()*4);
					if(%rnd2 == %rnd) { if(%rnd <= 2) %rnd2 = %rnd + 2; else %rnd2 = %rnd - 2; }
					
					// Win bag: 1.4x total (30% house edge)
					%lootbag = newObject("","Item","Lootbag",1,false);
					schedule("deleteObject("@%lootbag@");",10,%lootbag);
					$loot[%lootbag] = "* "@Client::getName(%TrueClientId)@", COINS "@floor($GambleAmount[%TrueClientId]*1.4);
					addToSet("MissionCleanup",%lootbag);
					GameBase::setMapName(%lootbag,"GamblePack");
					GameBase::setPosition(%lootbag,%pos[%rnd]);
					$GamblePackList = %lootbag;
					
					// Lose bag: 0 coins (bet already lost)
					%lootbag = newObject("","Item","Lootbag",1,false);
					schedule("deleteObject("@%lootbag@");",10,%lootbag);
					$loot[%lootbag] = "* "@Client::getName(%TrueClientId)@", COINS 0";
					addToSet("MissionCleanup",%lootbag);
					GameBase::setMapName(%lootbag,"GamblePack");
					GameBase::setPosition(%lootbag,%pos[%rnd2]);
					$GamblePackList = $GamblePackList@" "@%lootbag;
					$GambleAmount[%TrueClientId] = "";
					$GambleBot = %TrueClientId@" "@%closestId;
				}
				if(String::findSubStr(%message,%trigger[7]) != -1)
				{
					// Deduct bet upfront
					storeData(%TrueClientId, "COINS", -$GambleAmount[%TrueClientId], "inc");
					AI::sayLater(%TrueClientId,%closestId,"Four packs! Choose wisely - 10 seconds! No refunds! Hurry up.",true);
					$state[%closestId,%TrueClientId] = "";
					$LastGambleTime[%TrueClientId] = getSimTime();
					%rnd = 1 + floor(getRandom()*4);
					if(%rnd == 1) { %r2 = 2; %r3 = 3; %r4 = 4; }
					if(%rnd == 2) { %r2 = 1; %r3 = 3; %r4 = 4; }
					if(%rnd == 3) { %r2 = 1; %r3 = 2; %r4 = 4; }
					if(%rnd == 4) { %r2 = 1; %r3 = 2; %r4 = 3; }
					
					// Win bag: 2.0x total (50% house edge)
					%lootbag = newObject("","Item","Lootbag",1,false);
					schedule("deleteObject("@%lootbag@");",10,%lootbag);
					$loot[%lootbag] = "* "@Client::getName(%TrueClientId)@", COINS "@floor($GambleAmount[%TrueClientId]*2.0);
					addToSet("MissionCleanup",%lootbag);
					GameBase::setMapName(%lootbag,"GamblePack");
					GameBase::setPosition(%lootbag,%pos[%rnd]);
					$GamblePackList = %lootbag;
					
					// Lose bags: 0 coins (bet already lost)
					%lootbag = newObject("","Item","Lootbag",1,false);
					schedule("deleteObject("@%lootbag@");",10,%lootbag);
					$loot[%lootbag] = "* "@Client::getName(%TrueClientId)@", COINS 0";
					addToSet("MissionCleanup",%lootbag);
					GameBase::setMapName(%lootbag,"GamblePack");
					GameBase::setPosition(%lootbag,%pos[%r2]);
					$GamblePackList = $GamblePackList@" "@%lootbag;
					
					%lootbag = newObject("","Item","Lootbag",1,false);
					schedule("deleteObject("@%lootbag@");",10,%lootbag);
					$loot[%lootbag] = "* "@Client::getName(%TrueClientId)@", COINS 0";
					addToSet("MissionCleanup",%lootbag);
					GameBase::setMapName(%lootbag,"GamblePack");
					GameBase::setPosition(%lootbag,%pos[%r3]);
					$GamblePackList = $GamblePackList@" "@%lootbag;
					
					%lootbag = newObject("","Item","Lootbag",1,false);
					schedule("deleteObject("@%lootbag@");",10,%lootbag);
					$loot[%lootbag] = "* "@Client::getName(%TrueClientId)@", COINS 0";
					addToSet("MissionCleanup",%lootbag);
					GameBase::setMapName(%lootbag,"GamblePack");
					GameBase::setPosition(%lootbag,%pos[%r4]);
					$GamblePackList = $GamblePackList@" "@%lootbag;
					$GambleAmount[%TrueClientId] = "";
					$GambleBot = %TrueClientId@" "@%closestId;
				}
			}
		}
		return true;
	}
	
	//=========================================================================
	// CARDDEALER - Blackjack, High-Low, Dice Duel, Streak
	//=========================================================================
	else if(%botType == "carddealer")
	{
		if(%initTalk || $state[%closestId,%TrueClientId] != "")
		{
			if($state[%closestId,%TrueClientId] == "")
			{
				if(%initTalk)
				{
					AI::sayLater(%TrueClientId,%closestId,"Welcome! I offer BLACKJACK, HIGHLOW, DICE duels, or STREAK challenges. What'll it be?",true);
					$state[%closestId,%TrueClientId] = 1;
				}
			}
			else if($state[%closestId,%TrueClientId] == 1)
			{
				$CardDealerGame[%TrueClientId] = "";
				if(String::findSubStr(%message,"blackjack") != -1) $CardDealerGame[%TrueClientId] = "blackjack";
				else if(String::findSubStr(%message,"highlow") != -1 || String::findSubStr(%message,"high") != -1) $CardDealerGame[%TrueClientId] = "highlow";
				else if(String::findSubStr(%message,"dice") != -1) $CardDealerGame[%TrueClientId] = "dice";
				else if(String::findSubStr(%message,"streak") != -1) $CardDealerGame[%TrueClientId] = "streak";
				if($CardDealerGame[%TrueClientId] != "")
				{
					%bal = fetchData(%TrueClientId, "COINS");
					AI::sayLater(%TrueClientId,%closestId,"You have " @ Number::Beautify(%bal, -3) @ " coins. How much do you bet?",true);
					$state[%closestId,%TrueClientId] = "bet";
				}
			}
			else if($state[%closestId,%TrueClientId] == "bet")
			{
				%amt = floor(%cropped);
				if(%amt > 0 && fetchData(%TrueClientId, "COINS") >= %amt)
				{
					$GambleBet[%TrueClientId] = %amt;
					%game = $CardDealerGame[%TrueClientId];
					if(%game == "blackjack")
					{
						storeData(%TrueClientId, "COINS", -%amt, "inc");
						%pCard1 = CardDealer_DrawCard();
						%pCard2 = CardDealer_DrawCard();
						%dCard1 = CardDealer_DrawCard();
						$BJPlayerCards[%TrueClientId] = %pCard1 @ " " @ %pCard2;
						$BJDealerUp[%TrueClientId] = %dCard1;
						%pTotal = CardDealer_GetHandValue($BJPlayerCards[%TrueClientId]);
						if(%pTotal == 21)
						{
							%winnings = floor(%amt * 2.5);
							storeData(%TrueClientId, "COINS", %winnings, "inc");
							%newBal = fetchData(%TrueClientId, "COINS");
							AI::sayLater(%TrueClientId,%closestId,"BLACKJACK! You win " @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
							$state[%closestId,%TrueClientId] = "";
							SaveCharacter(%TrueClientId);
							return true;
						}
						AI::sayLater(%TrueClientId,%closestId,"Your cards: " @ %pCard1 @ ", " @ %pCard2 @ " (" @ %pTotal @ "). I show: " @ %dCard1 @ ". HIT or STAND?",true);
						$state[%closestId,%TrueClientId] = "bj_play";
					}
					else if(%game == "highlow")
					{
						storeData(%TrueClientId, "COINS", -%amt, "inc");
						%rnd = getRandom();
						$HighLowNumber[%TrueClientId] = floor(%rnd * 9) + 2; // Range 2-10, not 1-10 (so there's always room for higher/lower)
						AI::sayLater(%TrueClientId,%closestId,"Number is: " @ $HighLowNumber[%TrueClientId] @ "! Will next be HIGHER or LOWER? (1-11 range)",true);
						$state[%closestId,%TrueClientId] = "hl_play";
					}
					else if(%game == "dice")
					{
						AI::sayLater(%TrueClientId,%closestId,"Ready! Say ROLL!",true);
						$state[%closestId,%TrueClientId] = "dice_play";
					}
					else if(%game == "streak")
					{
						storeData(%TrueClientId, "COINS", -%amt, "inc");
						// First round also has risk - 45% chance to win
						if(getRandom() < 0.45)
						{
							$StreakCurrent[%TrueClientId] = %amt * 2;
							$StreakCount[%TrueClientId] = 1;
							AI::sayLater(%TrueClientId,%closestId,"Win #1! At " @ Number::Beautify($StreakCurrent[%TrueClientId], -3) @ " coins! CONTINUE or CASHOUT?",true);
							$state[%closestId,%TrueClientId] = "streak_play";
						}
						else
						{
							%newBal = fetchData(%TrueClientId, "COINS");
							AI::sayLater(%TrueClientId,%closestId,"BUSTED on round 1! House Wins! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
							$state[%closestId,%TrueClientId] = "";
							SaveCharacter(%TrueClientId);
						}
					}
				}
				else
					AI::sayLater(%TrueClientId,%closestId,"Can't afford that!",true);
			}
			else if($state[%closestId,%TrueClientId] == "bj_play")
			{
				%amt = $GambleBet[%TrueClientId];
				if(String::findSubStr(%message,"hit") != -1)
				{
					%newCard = CardDealer_DrawCard();
					$BJPlayerCards[%TrueClientId] = $BJPlayerCards[%TrueClientId] @ " " @ %newCard;
					%pTotal = CardDealer_GetHandValue($BJPlayerCards[%TrueClientId]);
					if(%pTotal > 21)
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"Drew " @ %newCard @ ". BUST! House Wins! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
						$state[%closestId,%TrueClientId] = "";
						SaveCharacter(%TrueClientId);
					}
					else
						AI::sayLater(%TrueClientId,%closestId,"Drew " @ %newCard @ ". Total: " @ %pTotal @ ". HIT or STAND?",true);
				}
				else if(String::findSubStr(%message,"stand") != -1)
					CardDealer_DealerPlay(%TrueClientId, %closestId);
			}
			else if($state[%closestId,%TrueClientId] == "hl_play")
			{
				%higher = (String::findSubStr(%message,"higher") != -1);
				%lower = (String::findSubStr(%message,"lower") != -1);
				if(%higher || %lower)
				{
					%first = $HighLowNumber[%TrueClientId];
					%second = 1 + floor(getRandom() * 10);
					%amt = $GambleBet[%TrueClientId];
					%win = (%higher && %second > %first) || (%lower && %second < %first);
					if(%win)
					{
						%winnings = floor(%amt * 1.8);
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"It was " @ %second @ "! WIN! +" @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"It was " @ %second @ "! Wrong! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
			}
			else if($state[%closestId,%TrueClientId] == "dice_play")
			{
				if(String::findSubStr(%message,"roll") != -1)
				{
					%amt = $GambleBet[%TrueClientId];
					storeData(%TrueClientId, "COINS", -%amt, "inc");
					%player = GetRoll("2d6");
					%house = GetRoll("2d6");
					Client::sendMessage(%TrueClientId, $MsgBeige, "You: " @ %player @ " | House: " @ %house);
					if(%player > %house)
					{
						%winnings = floor(%amt * 1.8);
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"WIN! +" @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"LOSE! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
			}
			else if($state[%closestId,%TrueClientId] == "streak_play")
			{
				%amt = $GambleBet[%TrueClientId];
				if(String::findSubStr(%message,"cash") != -1)
				{
					%winnings = $StreakCurrent[%TrueClientId];
					storeData(%TrueClientId, "COINS", %winnings, "inc");
					%newBal = fetchData(%TrueClientId, "COINS");
					AI::sayLater(%TrueClientId,%closestId,"Cashed out! You win " @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
				else if(String::findSubStr(%message,"cont") != -1)
				{
					if(getRandom() < 0.45)
					{
						$StreakCount[%TrueClientId]++;
						$StreakCurrent[%TrueClientId] = $StreakCurrent[%TrueClientId] * 2;
						AI::sayLater(%TrueClientId,%closestId,"Win #" @ $StreakCount[%TrueClientId] @ "! At " @ Number::Beautify($StreakCurrent[%TrueClientId], -3) @ "! CONTINUE or CASHOUT?",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"BUSTED! House Wins! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
						$state[%closestId,%TrueClientId] = "";
						SaveCharacter(%TrueClientId);
					}
				}
			}
		}
		return true;
	}
	
	//=========================================================================
	// WHEELMASTER - Wheel of Fortune, Roulette, Slots
	//=========================================================================
	else if(%botType == "wheelmaster")
	{
		if(%initTalk || $state[%closestId,%TrueClientId] != "")
		{
			if($state[%closestId,%TrueClientId] == "")
			{
				if(%initTalk)
				{
					AI::sayLater(%TrueClientId,%closestId,"Spin to win! Try WHEEL, ROULETTE, or SLOTS?",true);
					$state[%closestId,%TrueClientId] = 1;
				}
			}
			else if($state[%closestId,%TrueClientId] == 1)
			{
				$WheelGame[%TrueClientId] = "";
				if(String::findSubStr(%message,"wheel") != -1) $WheelGame[%TrueClientId] = "wheel";
				else if(String::findSubStr(%message,"roulette") != -1) $WheelGame[%TrueClientId] = "roulette";
				else if(String::findSubStr(%message,"slot") != -1) $WheelGame[%TrueClientId] = "slots";
				if($WheelGame[%TrueClientId] != "")
				{
					%bal = fetchData(%TrueClientId, "COINS");
					AI::sayLater(%TrueClientId,%closestId,"You have " @ Number::Beautify(%bal, -3) @ " coins. How much?",true);
					$state[%closestId,%TrueClientId] = "bet";
				}
			}
			else if($state[%closestId,%TrueClientId] == "bet")
			{
				%amt = floor(%cropped);
				if(%amt > 0 && fetchData(%TrueClientId, "COINS") >= %amt)
				{
					$GambleBet[%TrueClientId] = %amt;
					%game = $WheelGame[%TrueClientId];
					if(%game == "wheel")
					{
						AI::sayLater(%TrueClientId,%closestId,"Prizes: LOSE(50%), 1.5x(25%), 2x(15%), 5x(5%), JACKPOT 20x(5%). Say SPIN!",true);
						$state[%closestId,%TrueClientId] = "wheel_play";
					}
					else if(%game == "roulette")
					{
						AI::sayLater(%TrueClientId,%closestId,"Bet RED, BLACK, HIGH, LOW, EVEN, ODD (2x) or NUMBER 0-36 (35x)?",true);
						$state[%closestId,%TrueClientId] = "roulette_play";
					}
					else if(%game == "slots")
					{
						AI::sayLater(%TrueClientId,%closestId,"Match 3 to win! Say SPIN!",true);
						$state[%closestId,%TrueClientId] = "slots_play";
					}
				}
			}
			else if($state[%closestId,%TrueClientId] == "wheel_play")
			{
				if(String::findSubStr(%message,"spin") != -1)
				{
					%amt = $GambleBet[%TrueClientId];
					storeData(%TrueClientId, "COINS", -%amt, "inc");
					Client::sendMessage(%TrueClientId, $MsgYellow, "*** Wheel Master Wendy spins the wheel! ***");
					%roll = floor(getRandom() * 20);
					if(%roll < 10) { %prize = "LOSE"; %mult = 0; }
					else if(%roll < 15) { %prize = "1.5x"; %mult = 1.5; }
					else if(%roll < 18) { %prize = "2x"; %mult = 2; }
					else if(%roll == 18) { %prize = "5x"; %mult = 5; }
					else { %prize = "JACKPOT 20x"; %mult = 20; }
					Client::sendMessage(%TrueClientId, $MsgBeige, "The wheel slows down...");
					if(%mult > 0)
					{
						%winnings = floor(%amt * %mult);
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"The wheel lands on... " @ %prize @ "! You win " @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"The wheel lands on... LOSE! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
			}
			else if($state[%closestId,%TrueClientId] == "roulette_play")
			{
				%amt = $GambleBet[%TrueClientId];
				%betType = "";
				if(String::findSubStr(%message,"red") != -1) %betType = "red";
				else if(String::findSubStr(%message,"black") != -1) %betType = "black";
				else if(String::findSubStr(%message,"high") != -1) %betType = "high";
				else if(String::findSubStr(%message,"low") != -1) %betType = "low";
				else if(String::findSubStr(%message,"even") != -1) %betType = "even";
				else if(String::findSubStr(%message,"odd") != -1) %betType = "odd";
				else
				{
					%num = floor(%cropped);
					if(%num >= 0 && %num <= 36) { %betType = "number"; $RouletteNum[%TrueClientId] = %num; }
				}
				if(%betType != "")
				{
					storeData(%TrueClientId, "COINS", -%amt, "inc");
					%result = 1 + floor(getRandom() * 36);
					if(getRandom() < 0.027) %result = 0; // ~1/37 chance for green 0
					
					// Red numbers on a standard roulette wheel
					%redNums = " 1 3 5 7 9 12 14 16 18 19 21 23 25 27 30 32 34 36 ";
					%isRed = (String::findSubStr(%redNums, " " @ %result @ " ") != -1);
					
					%win = false;
					%payout = 1.9;
					if(%betType == "red" && %isRed) %win = true;
					if(%betType == "black" && !%isRed && %result != 0) %win = true;
					if(%betType == "high" && %result >= 19) %win = true;
					if(%betType == "low" && %result >= 1 && %result <= 18) %win = true;
					if(%betType == "even" && %result != 0 && (%result % 2 == 0)) %win = true;
					if(%betType == "odd" && (%result % 2 == 1)) %win = true;
					if(%betType == "number" && %result == $RouletteNum[%TrueClientId]) { %win = true; %payout = 35; }
					
					if(%isRed) %color = "RED"; else %color = "BLACK";
					if(%result == 0) %color = "GREEN";
					
					Client::sendMessage(%TrueClientId, $MsgBeige, "*** The ball lands on " @ %result @ " " @ %color @ "! ***");
					
					if(%win)
					{
						%winnings = floor(%amt * %payout);
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"WIN! +" @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"LOSE! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
			}
			else if($state[%closestId,%TrueClientId] == "slots_play")
		{
			if(String::findSubStr(%message,"spin") != -1)
			{
				%amt = $GambleBet[%TrueClientId];
				storeData(%TrueClientId, "COINS", -%amt, "inc");
				%syms = "Crown Crown Diamond Diamond Diamond Sword Sword Sword Sword Shield Shield Shield Shield Shield Skull Skull Skull Skull Skull Skull";
				
				// Generate 3 rows of 3 symbols each (9 total)
				%r1s1 = getWord(%syms, floor(getRandom() * 20));
				%r1s2 = getWord(%syms, floor(getRandom() * 20));
				%r1s3 = getWord(%syms, floor(getRandom() * 20));
				%r2s1 = getWord(%syms, floor(getRandom() * 20));
				%r2s2 = getWord(%syms, floor(getRandom() * 20));
				%r2s3 = getWord(%syms, floor(getRandom() * 20));
				%r3s1 = getWord(%syms, floor(getRandom() * 20));
				%r3s2 = getWord(%syms, floor(getRandom() * 20));
				%r3s3 = getWord(%syms, floor(getRandom() * 20));
				
				// Display 3 rows
			Client::sendMessage(%TrueClientId, $MsgYellow, "*** SLOTS - 5 PAY LINES ***");
			Client::sendMessage(%TrueClientId, $MsgBeige, "[" @ %r1s1 @ "] [" @ %r1s2 @ "] [" @ %r1s3 @ "]");
			Client::sendMessage(%TrueClientId, $MsgGreen, "[" @ %r2s1 @ "] [" @ %r2s2 @ "] [" @ %r2s3 @ "]");
			Client::sendMessage(%TrueClientId, $MsgBeige, "[" @ %r3s1 @ "] [" @ %r3s2 @ "] [" @ %r3s3 @ "]");
			
			// Check all 5 pay lines: 3 rows + 2 diagonals
			%winnings = 0;
			%winSymbol = "";
			%lineCount = 0;
			
			// Row 1 (top)
			if(%r1s1 == %r1s2 && %r1s2 == %r1s3)
			{
				if(%r1s1 == "Crown") %mult = 50;
				else if(%r1s1 == "Diamond") %mult = 20;
				else if(%r1s1 == "Sword") %mult = 10;
				else if(%r1s1 == "Shield") %mult = 5;
				else %mult = 3;
				%winnings = %winnings + (%amt * %mult);
				%winSymbol = %r1s1;
				%lineCount = %lineCount + 1;
			}
			// Row 2 (middle)
			if(%r2s1 == %r2s2 && %r2s2 == %r2s3)
			{
				if(%r2s1 == "Crown") %mult = 50;
				else if(%r2s1 == "Diamond") %mult = 20;
				else if(%r2s1 == "Sword") %mult = 10;
				else if(%r2s1 == "Shield") %mult = 5;
				else %mult = 3;
				%winnings = %winnings + (%amt * %mult);
				%winSymbol = %r2s1;
				%lineCount = %lineCount + 1;
			}
			// Row 3 (bottom)
			if(%r3s1 == %r3s2 && %r3s2 == %r3s3)
			{
				if(%r3s1 == "Crown") %mult = 50;
				else if(%r3s1 == "Diamond") %mult = 20;
				else if(%r3s1 == "Sword") %mult = 10;
				else if(%r3s1 == "Shield") %mult = 5;
				else %mult = 3;
				%winnings = %winnings + (%amt * %mult);
				%winSymbol = %r3s1;
				%lineCount = %lineCount + 1;
			}
			// Diagonal top-left to bottom-right
			if(%r1s1 == %r2s2 && %r2s2 == %r3s3)
			{
				if(%r1s1 == "Crown") %mult = 50;
				else if(%r1s1 == "Diamond") %mult = 20;
				else if(%r1s1 == "Sword") %mult = 10;
				else if(%r1s1 == "Shield") %mult = 5;
				else %mult = 3;
				%winnings = %winnings + (%amt * %mult);
				%winSymbol = %r1s1;
				%lineCount = %lineCount + 1;
			}
			// Diagonal bottom-left to top-right
			if(%r3s1 == %r2s2 && %r2s2 == %r1s3)
			{
				if(%r3s1 == "Crown") %mult = 50;
				else if(%r3s1 == "Diamond") %mult = 20;
				else if(%r3s1 == "Sword") %mult = 10;
				else if(%r3s1 == "Shield") %mult = 5;
				else %mult = 3;
				%winnings = %winnings + (%amt * %mult);
				%winSymbol = %r3s1;
				%lineCount = %lineCount + 1;
			}
			
			if(%winnings > 0)
			{
				storeData(%TrueClientId, "COINS", %winnings, "inc");
				%newBal = fetchData(%TrueClientId, "COINS");
				AI::sayLater(%TrueClientId,%closestId,"JACKPOT! " @ %lineCount @ " line(s)! You win " @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
			}
			else
			{
				%newBal = fetchData(%TrueClientId, "COINS");
				AI::sayLater(%TrueClientId,%closestId,"No matches! House Wins! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
			}
			$state[%closestId,%TrueClientId] = "";
			SaveCharacter(%TrueClientId);
			}
		}
		}
		return true;
	}
	
	//=========================================================================
	// QUICKGAMES - Coin Flip, RPS, Number Guess, Memory
	//=========================================================================
	else if(%botType == "quickgames")
	{
		if(%initTalk || $state[%closestId,%TrueClientId] != "")
		{
			if($state[%closestId,%TrueClientId] == "")
			{
				if(%initTalk)
				{
					AI::sayLater(%TrueClientId,%closestId,"Quick games! COIN flip, RPS, or NUMBER guess?",true);
					$state[%closestId,%TrueClientId] = 1;
				}
			}
			else if($state[%closestId,%TrueClientId] == 1)
			{
				$QuickGame[%TrueClientId] = "";
				if(String::findSubStr(%message,"coin") != -1) $QuickGame[%TrueClientId] = "coin";
				else if(String::findSubStr(%message,"rps") != -1 || String::findSubStr(%message,"rock") != -1) $QuickGame[%TrueClientId] = "rps";
				else if(String::findSubStr(%message,"number") != -1) $QuickGame[%TrueClientId] = "number";
				// Memory game disabled - chat messages persist making it trivial
				// else if(String::findSubStr(%message,"memory") != -1) $QuickGame[%TrueClientId] = "memory";
				if($QuickGame[%TrueClientId] != "")
				{
					%bal = fetchData(%TrueClientId, "COINS");
					AI::sayLater(%TrueClientId,%closestId,"You have " @ Number::Beautify(%bal, -3) @ " coins. How much?",true);
					$state[%closestId,%TrueClientId] = "bet";
				}
			}
			else if($state[%closestId,%TrueClientId] == "bet")
			{
				%amt = floor(%cropped);
				if(%amt > 0 && fetchData(%TrueClientId, "COINS") >= %amt)
				{
					$GambleBet[%TrueClientId] = %amt;
					%game = $QuickGame[%TrueClientId];
					if(%game == "coin")
					{
						AI::sayLater(%TrueClientId,%closestId,"HEADS or TAILS?",true);
						$state[%closestId,%TrueClientId] = "coin_play";
					}
					else if(%game == "rps")
					{
						AI::sayLater(%TrueClientId,%closestId,"ROCK, PAPER, or SCISSORS?",true);
						$state[%closestId,%TrueClientId] = "rps_play";
					}
					else if(%game == "number")
					{
						storeData(%TrueClientId, "COINS", -%amt, "inc");
						$NumGuessTarget[%TrueClientId] = 1 + floor(getRandom() * 100);
						$NumGuessAttempts[%TrueClientId] = 5;
						AI::sayLater(%TrueClientId,%closestId,"I picked 1-100. 5 guesses. Win = 3x!",true);
						$state[%closestId,%TrueClientId] = "num_play";
					}
					// Memory game disabled - chat messages persist making it trivial
				// else if(%game == "memory")
				// {
				// 	storeData(%TrueClientId, "COINS", -%amt, "inc");
				// 	%seq = "";
				// 	for(%i = 0; %i < 4; %i++) %seq = %seq @ (1 + floor(getRandom() * 9));
				// 	$MemorySeq[%TrueClientId] = %seq;
				// 	AI::sayLater(%TrueClientId,%closestId,"MEMORIZE: " @ %seq,true);
				// 	Client::sendMessage(%TrueClientId, $MsgGreen, "REMEMBER: " @ %seq);
				// 	schedule("$state[" @ %closestId @ "," @ %TrueClientId @ "] = \"mem_play\"; AI::sayLater(" @ %TrueClientId @ "," @ %closestId @ ",\"What was it?\",true);", 5);
				// }
				}
			}
			else if($state[%closestId,%TrueClientId] == "coin_play")
			{
				%heads = (String::findSubStr(%message,"heads") != -1);
				%tails = (String::findSubStr(%message,"tails") != -1);
				if(%heads || %tails)
				{
					%amt = $GambleBet[%TrueClientId];
					storeData(%TrueClientId, "COINS", -%amt, "inc");
					%isHeads = (getRandom() < 0.5);
					%win = (%heads && %isHeads) || (%tails && !%isHeads);
					if(%isHeads) %result = "HEADS"; else %result = "TAILS";
					if(%win)
					{
						%winnings = floor(%amt * 1.9);
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,%result @ "! WIN! +" @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,%result @ "! LOSE! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
			}
			else if($state[%closestId,%TrueClientId] == "rps_play")
			{
				%rock = (String::findSubStr(%message,"rock") != -1);
				%paper = (String::findSubStr(%message,"paper") != -1);
				%scissors = (String::findSubStr(%message,"scissors") != -1);
				if(%rock || %paper || %scissors)
				{
					%amt = $GambleBet[%TrueClientId];
					storeData(%TrueClientId, "COINS", -%amt, "inc");
					if(%rock) %pChoice = "ROCK";
					else if(%paper) %pChoice = "PAPER";
					else %pChoice = "SCISSORS";
					%hRoll = floor(getRandom() * 3);
					if(%hRoll == 0) %hChoice = "ROCK";
					else if(%hRoll == 1) %hChoice = "PAPER";
					else %hChoice = "SCISSORS";
					%win = (%pChoice == "ROCK" && %hChoice == "SCISSORS") || (%pChoice == "PAPER" && %hChoice == "ROCK") || (%pChoice == "SCISSORS" && %hChoice == "PAPER");
					%tie = (%pChoice == %hChoice);
					if(%win)
					{
						%winnings = floor(%amt * 1.8);
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,%pChoice @ " beats " @ %hChoice @ "! +" @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					else if(%tie)
					{
						storeData(%TrueClientId, "COINS", %amt, "inc");
						AI::sayLater(%TrueClientId,%closestId,"Tie! Bet returned.",true);
					}
					else
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,%hChoice @ " beats " @ %pChoice @ "! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
					}
					$state[%closestId,%TrueClientId] = "";
					SaveCharacter(%TrueClientId);
				}
			}
			else if($state[%closestId,%TrueClientId] == "num_play")
			{
				%guess = floor(%cropped);
				if(%guess >= 1 && %guess <= 100)
				{
					%target = $NumGuessTarget[%TrueClientId];
					$NumGuessAttempts[%TrueClientId]--;
					%left = $NumGuessAttempts[%TrueClientId];
					%amt = $GambleBet[%TrueClientId];
					if(%guess == %target)
					{
						%winnings = %amt * 3;
						storeData(%TrueClientId, "COINS", %winnings, "inc");
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"CORRECT! You win " @ Number::Beautify(%winnings - %amt, -3) @ " coins! (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
						$state[%closestId,%TrueClientId] = "";
						SaveCharacter(%TrueClientId);
					}
					else if(%left <= 0)
					{
						%newBal = fetchData(%TrueClientId, "COINS");
						AI::sayLater(%TrueClientId,%closestId,"Out of guesses! It was " @ %target @ ". House Wins! Lost " @ Number::Beautify(%amt, -3) @ " coins. (Balance: " @ Number::Beautify(%newBal, -3) @ ")",true);
						$state[%closestId,%TrueClientId] = "";
						SaveCharacter(%TrueClientId);
					}
					else
					{
						if(%guess < %target) %hint = "HIGHER"; else %hint = "LOWER";
						AI::sayLater(%TrueClientId,%closestId,%guess @ " - " @ %hint @ "! " @ %left @ " left.",true);
					}
				}
			}
			// Memory game disabled - chat messages persist making it trivial
		// else if($state[%closestId,%TrueClientId] == "mem_play")
		// {
		// 	%answer = String::Replace(%cropped, " ", "");
		// 	%correct = $MemorySeq[%TrueClientId];
		// 	%amt = $GambleBet[%TrueClientId];
		// 	if(%answer == %correct)
		// 	{
		// 		%winnings = %amt * 3;
		// 		storeData(%TrueClientId, "COINS", %winnings, "inc");
		// 		AI::sayLater(%TrueClientId,%closestId,"CORRECT! +" @ Number::Beautify(%winnings - %amt, -3) @ " coins!",true);
		// 	}
		// 	else
		// 		AI::sayLater(%TrueClientId,%closestId,"Wrong! It was " @ %correct @ ".",true);
		// 	$state[%closestId,%TrueClientId] = "";
		// 	SaveCharacter(%TrueClientId);
		// }
		}
		return true;
	}
	
	// Bot type not handled by this file
	return false;
}

//=============================================================================
// Card Game Helper Functions
//=============================================================================
function CardDealer_DrawCard()
{
	%n = 1 + floor(getRandom() * 13);
	if(%n == 1) return "A";
	if(%n == 11) return "J";
	if(%n == 12) return "Q";
	if(%n == 13) return "K";
	return %n;
}

function CardDealer_GetCardValue(%c)
{
	if(%c == "A") return 11;
	if(%c == "J" || %c == "Q" || %c == "K") return 10;
	return floor(%c);
}

function CardDealer_GetHandValue(%cards)
{
	%total = 0;
	%aces = 0;
	for(%i = 0; (%c = getWord(%cards, %i)) != -1; %i++)
	{
		if(%c == "A") { %aces++; %total += 11; }
		else { %total += CardDealer_GetCardValue(%c); }
	}
	while(%total > 21 && %aces > 0) { %total -= 10; %aces--; }
	return %total;
}

function CardDealer_DealerPlay(%clientId, %botId)
{
	%dCards = $BJDealerUp[%clientId] @ " " @ CardDealer_DrawCard();
	%dTotal = CardDealer_GetHandValue(%dCards);
	while(%dTotal < 17)
	{
		%dCards = %dCards @ " " @ CardDealer_DrawCard();
		%dTotal = CardDealer_GetHandValue(%dCards);
	}
	%pTotal = CardDealer_GetHandValue($BJPlayerCards[%clientId]);
	%amt = $GambleBet[%clientId];
	if(%dTotal > 21)
	{
		%winnings = %amt * 2;
		storeData(%clientId, "COINS", %winnings, "inc");
		AI::sayLater(%clientId, %botId, "Dealer BUSTS! You win " @ Number::Beautify(%amt, -3) @ " coins!", true);
	}
	else if(%pTotal > %dTotal)
	{
		%winnings = %amt * 2;
		storeData(%clientId, "COINS", %winnings, "inc");
		AI::sayLater(%clientId, %botId, "Your " @ %pTotal @ " beats my " @ %dTotal @ "!", true);
	}
	else if(%pTotal < %dTotal)
		AI::sayLater(%clientId, %botId, "My " @ %dTotal @ " beats your " @ %pTotal @ ". Lost " @ Number::Beautify(%amt, -3) @ " coins.", true);
	else
	{
		storeData(%clientId, "COINS", %amt, "inc");
		AI::sayLater(%clientId, %botId, "Push! Bet returned.", true);
	}
	$state[%botId, %clientId] = "";
	SaveCharacter(%clientId);
}