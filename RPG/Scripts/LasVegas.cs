			else if(clipTrailingNumbers(%aiName) == "gambler")
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
				if($GambleBot != "" && %trueClientId != getWord($GambleBot,0))
				{
					AI::sayLater(%TrueClientId,%closestId,"I'm terribly sorry, but right now another customer is having a shot at my game. Could you wait a few more minutes, please?",true);
					$state[%closestId,%TrueClientId] = "";
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
							AI::sayLater(%TrueClientId,%closestId,"The Rules: You wager either COINS or EXP. You then choose one of the bags I lay out. If you win, you win your wager, and if you lose, you lose it! There is a 15 second time limit. [OKAY]",true);
							$state[%closestId,%TrueClientId] = 2;
						}
					}
					else if($state[%closestId,%TrueClientId] == 2)
					{
						if(String::findSubStr(%message,%trigger[3]) != -1)
						{
							AI::sayLater(%TrueClientId,%closestId,"Ok! Would you like to bet COINS or EXP?",true);
							$state[%closestId,%TrueClientId] = 3;
						}
					}
					else if($state[%closestId,%TrueClientId] == 3)
					{
						if(String::findSubStr(%message,%trigger[4]) != -1)
						{
							if(fetchData(%trueClientId,"COINS") > 0)
							{
								AI::sayLater(%TrueClientId,%closestId,"Coins, eh? Well then, how much would you like to bet? Make it a sum worthy of someone such as yourself!",true);
								$state[%closestId,%TrueClientId] = 4;
							}
							else
							{
								AI::sayLater(%TrueClientId,%closestId,"Hey, what're you trying to pull? You don't have any money... come back when you've got a walletfull, good sir!",true);
								$state[%closestId,%TrueClientId] = "";
							}
						}
						if(String::findSubStr(%message,%trigger[5]) != -1)
						{
							if(fetchData(%trueClientId,"EXP") > 0)
							{
								AI::sayLater(%TrueClientId,%closestId,"Experience, eh? Well then, how much would you like to wager? Make it a sum worthy of someone such as yourself!",true);
								$state[%closestId,%TrueClientId] = 6;
							}
							else
							{
								AI::sayLater(%TrueClientId,%closestId,"Hahahahaha! You're a good joker, kid! Come back when you've learned a few life lessons!",true);
								$state[%closestId,%TrueClientId] = "";
							}
						}
					}
					else if($state[%closestId,%TrueClientId] == 4)
					{
						%amt = %cropped;
						%amt = floor(%amt);
						if(%amt > 0 && fetchData(%trueClientId,"COINS") >= %amt)
						{
							if(%amt < 500)
								AI::sayLater(%TrueClientId,%closestId,"Hmm, disappointing... but a wager nonetheless! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 4000)
								AI::sayLater(%TrueClientId,%closestId,"A tad below average, but it's a wager! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 10000)
								AI::sayLater(%TrueClientId,%closestId,"Very good, very good, that's quite a wager! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 40000)
								AI::sayLater(%TrueClientId,%closestId,"Wow, you're quite the risk-taker! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 100000)
								AI::sayLater(%TrueClientId,%closestId,"I'm impressed, my friend! What a wager you've made! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else
								AI::sayLater(%TrueClientId,%closestId,"Good sir, you have made my day with this unbelievable sum! I commend you heartily for a wager worthy of a king! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							$GambleAmount[%trueClientId] = %amt;
							$state[%closestId,%TrueClientId] = 5;
						}
						else
						{
							AI::sayLater(%TrueClientId,%closestId,"Now, let's be a little serious, my friend. I'm sure you're just itching to win a prize!",true);
							$state[%closestId,%TrueClientId] = "";
						}
					}
					else if($state[%closestId,%TrueClientId] == 5)
					{
						if(String::findSubStr(%message,%trigger[6]) != -1)
						{
							AI::sayLater(%TrueClientId,%closestId,"Alright! Choose a bag, either one! If you win, super! If you lose, I'm sorry, just speak to me if you wish to try again.",true);
							$state[%closestId,%TrueClientId] = "";
							%rnd = 1+(floor(getRandom()*4));
							%rnd2 = 1+(floor(getRandom()*4));
							if(%rnd2 == %rnd)
							{
								if(%rnd == 1) %rnd2 = 3;
								if(%rnd == 2) %rnd2 = 4;
								if(%rnd == 3) %rnd2 = 1;
								if(%rnd == 4) %rnd2 = 2;
							}
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", COINS "@$GambleAmount[%trueClientId]*1;
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%rnd]);
							$GamblePackList = %lootbag;

							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", COINS -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%rnd2]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							$GambleAmount[%trueClientId] = "";
							$GambleBot = %trueClientId@" "@%closestId;
						}
						if(String::findSubStr(%message,%trigger[7]) != -1)
						{
							AI::sayLater(%TrueClientId,%closestId,"Alright! Choose a bag, any of them will do! If you win, excellent! If you lose, I'm sorry, just speak to me if you wish to try again.",true);
							$state[%closestId,%TrueClientId] = "";
							%rnd = 1+(floor(getRandom()*4));
							if(%rnd == 1)
							{
								%r2 = 2;
								%r3 = 3;
								%r4 = 4;
							}
							if(%rnd == 2)
							{
								%r2 = 1;
								%r3 = 3;
								%r4 = 4;
							}
							if(%rnd == 3)
							{
								%r2 = 1;
								%r3 = 2;
								%r4 = 4;
							}
							if(%rnd == 4)
							{
								%r2 = 1;
								%r3 = 2;
								%r4 = 3;
							}
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", COINS "@$GambleAmount[%trueClientId]*2;
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%rnd]);
							$GamblePackList = %lootbag;

							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", COINS -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%r2]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", COINS -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%r3]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", COINS -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%r4]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							$GambleAmount[%trueClientId] = "";
							$GambleBot = %trueClientId@" "@%closestId;
						}
					}
					else if($state[%closestId,%TrueClientId] == 6)
					{
						%amt = %cropped;
						%amt = floor(%amt);
						if(%amt > 0 && fetchData(%trueClientId,"EXP") >= %amt)
						{
							if(%amt < 500)
								AI::sayLater(%TrueClientId,%closestId,"Hmm, disappointing... but a wager nonetheless! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 4000)
								AI::sayLater(%TrueClientId,%closestId,"A tad below average, but it's a wager! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 10000)
								AI::sayLater(%TrueClientId,%closestId,"Very good, very good, that's quite a wager! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 40000)
								AI::sayLater(%TrueClientId,%closestId,"Wow, you're quite the risk-taker! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else if(%amt < 100000)
								AI::sayLater(%TrueClientId,%closestId,"I'm impressed, my friend! What a wager you've made! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							else
								AI::sayLater(%TrueClientId,%closestId,"Good sir, you have made my day with this unbelievable sum! I commend you heartily for a wager worthy of a king! Well then, one last question, and begin the contest we shall! Do you want TWO or FOUR packs?",true);
							$GambleAmount[%trueClientId] = %amt;
							$state[%closestId,%TrueClientId] = 7;
						}
						else
						{
							AI::sayLater(%TrueClientId,%closestId,"Nowwww, let's be a little serious, my friend. I'm sure you're just itching to win a prize!",true);
							$state[%closestId,%TrueClientId] = "";
						}
					}
					else if($state[%closestId,%TrueClientId] == 7)
					{
						if(String::findSubStr(%message,%trigger[6]) != -1)
						{
							AI::sayLater(%TrueClientId,%closestId,"Alright! Choose a bag, either one! If you win, super! If you lose, I'm sorry, just speak to me if you wish to try again.",true);
							$state[%closestId,%TrueClientId] = "";
							%rnd = 1+(floor(getRandom()*4));
							%rnd2 = 1+(floor(getRandom()*4));
							if(%rnd2 == %rnd)
							{
								if(%rnd == 1) %rnd2 = 3;
								if(%rnd == 2) %rnd2 = 4;
								if(%rnd == 3) %rnd2 = 1;
								if(%rnd == 4) %rnd2 = 2;
							}
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", EXP "@$GambleAmount[%trueClientId]*1;
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%rnd]);
							$GamblePackList = %lootbag;

							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", EXP -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%rnd2]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							$GambleAmount[%trueClientId] = "";
							$GambleBot = %trueClientId@" "@%closestId;
						}
						if(String::findSubStr(%message,%trigger[7]) != -1)
						{
							AI::sayLater(%TrueClientId,%closestId,"Alright! Choose a bag, any of them will do! If you win, excellent! If you lose, I'm sorry, just speak to me if you wish to try again.",true);
							$state[%closestId,%TrueClientId] = "";
							%rnd = 1+(floor(getRandom()*4));
							if(%rnd == 1)
							{
								%r2 = 2;
								%r3 = 3;
								%r4 = 4;
							}
							if(%rnd == 2)
							{
								%r2 = 1;
								%r3 = 3;
								%r4 = 4;
							}
							if(%rnd == 3)
							{
								%r2 = 1;
								%r3 = 2;
								%r4 = 4;
							}
							if(%rnd == 4)
							{
								%r2 = 1;
								%r3 = 2;
								%r4 = 3;
							}
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", EXP "@$GambleAmount[%trueClientId]*2;
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%rnd]);
							$GamblePackList = %lootbag;

							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", EXP -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%r2]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", EXP -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%r3]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							%lootbag = newObject("","Item","Lootbag",1,false);
							schedule("deleteObject("@%lootbag@");",15,%lootbag);
							$loot[%lootbag] = "* "@Client::getName(%trueClientId)@", EXP -"@$GambleAmount[%trueClientId];
							addToSet("MissionCleanup",%lootbag);
							GameBase::setMapName(%lootbag,"GamblePack");
							GameBase::setPosition(%lootbag,%pos[%r4]);
							$GamblePackList = $GamblePackList@" "@%lootbag;
							$GambleAmount[%trueClientId] = "";
							$GambleBot = %trueClientId@" "@%closestId;
						}
					}
				}
			}