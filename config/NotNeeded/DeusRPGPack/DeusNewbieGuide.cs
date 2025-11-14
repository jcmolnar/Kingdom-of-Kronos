// By: Deus_ex_Machina
//
//	DeusNewbieGuide 1.1

$DeusNewbieGuidesver = "ver1.1";
// Newbie Guide by Toaster
// http://dynamic2.gamespy.com/~rpg/

%i = 0;
$NewbieGuideText[%i++] = "<jc><f1>Tribes RPG Newbie Guide";
$NewbieGuideText[%i++] = "<f0>Welcome to<f1> Tribes RPG!<jl>";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<jl><f0>This guide is designed to answer your questions and get you started playing.";
$NewbieGuideText[%i++] = "This explains the terms, explains character creation, and tells what to do when you first start playing.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f2>Classes:";
$NewbieGuideText[%i++] = "<f0>There are four groups of classes, warrior, priest, rouge, and wizard. Each group has classes within.";
$NewbieGuideText[%i++] = "<f1>Warrior- <f0>Fighter, Ranger, Paladin";
$NewbieGuideText[%i++] = "<f1>Priest- <f0>Cleric, Druid";
$NewbieGuideText[%i++] = "<f1>Rogue- <f0>Thief, Bard";
$NewbieGuideText[%i++] = "<jl><f1>Wizard- <f0>Mage";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Fighter- <f0>Fighters are pure fighters. Their skill goes up quickly in all fighting related skills,";
$NewbieGuideText[%i++] = "such as slashing, bashing, and endurance, but their magic is almost unusable.";
$NewbieGuideText[%i++] = "<f1>Ranger- <f0>Rangers are similar to fighters. They are decent in most combat skills, but their";
$NewbieGuideText[%i++] = "archery is highest.";
$NewbieGuideText[%i++] = "<f1>Paladin- <f0>Paladins are similar to fighters except they have a somewhat faster magic skill,";
$NewbieGuideText[%i++] = "especially defensive.";
$NewbieGuideText[%i++] = "<f1>Cleric- <f0>Clerics are healers. Their defensive casting is the best of all. Clerics go up best in";
$NewbieGuideText[%i++] = "bludgeoning for weapons. They have a little offensive magic as well.";
$NewbieGuideText[%i++] = "<f1>Druid- <f0>Druids are similar to clerics. The main difference is the offensive and defensive magic";
$NewbieGuideText[%i++] = "skills are more balanced.";
$NewbieGuideText[%i++] = "<f1>Thief- <f0>Thieves are stealthy fighters, and excel in stealing, hiding, and backstabbing. They";
$NewbieGuideText[%i++] = "also have excellent dodging and good archery, but have piercing as main weapon skill.";
$NewbieGuideText[%i++] = "<jl><f1>Bard- <f0>Bards can do pretty much anything. Their skills are average all around.";
$NewbieGuideText[%i++] = "<f1>Mage- <f0>Mages are pure offensive spell casters. Defensive magic is slow, and weapon and armor";
$NewbieGuideText[%i++] = "skills are very slow.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f2>The Skill System:";
$NewbieGuideText[%i++] = "<f0>Instead of Strength, Dexterity, and other stats guiding your character's abilities, Tribes RPG";
$NewbieGuideText[%i++] = "uses a skill system. All your abilities are limited by the skills. All classes can invest in all skills, but certain";
$NewbieGuideText[%i++] = "skills go up faster for particular classes. For example, mages have much better Offensive Casting than";
$NewbieGuideText[%i++] = "<f0>fighters, but do not have nearly as strong Slashing and Bludgeoning. Skills can be built up either by using";
$NewbieGuideText[%i++] = "abilities related to that skill or distributing skill points. You get 60 SP when you start the game, and 10 SP";
$NewbieGuideText[%i++] = "every time";
$NewbieGuideText[%i++] = "you level. Skills are capped at: (level*10)+20 You cannot go past that by either practicing skill or";
$NewbieGuideText[%i++] = "distributing SP.";
$NewbieGuideText[%i++] = "<jl>";
$NewbieGuideText[%i++] = "<f1>The skills are:";
$NewbieGuideText[%i++] = "<f1>Slashing: <f0>Used for slashing weapons- more skill means more damage, accuracy, and the ability";
$NewbieGuideText[%i++] = "to use more powerful slashing weapons. Train by attacking with slashing weapons.";
$NewbieGuideText[%i++] = "<f1>Piercing: <f0>Used for piercing weapons- more skill means more damage, accuracy, and the ability";
$NewbieGuideText[%i++] = "to use more powerful piercing weapons. Train by attacking with piercing weapons.";
$NewbieGuideText[%i++] = "<jl><f1>Bludgeoning: <f0>Used for bludgeoning weapons- more skill means more damage, accuracy, and";
$NewbieGuideText[%i++] = "the ability to use more powerful bludgeoning weapons. Train by attacking with bludgeoning weapons.";
$NewbieGuideText[%i++] = "<f1>Dodging: <f0>Gives a chance to dodge a enemy attack. Train by not getting hit by an enemy attack.";
$NewbieGuideText[%i++] = "<f1>Weight Capacity: <f0>Lets you carry more. Train by moving.";
$NewbieGuideText[%i++] = "<f1>Bashing: <f0>A bludgeoning attack. To bash, type <f1>#bash<f0>, then hit an enemy with a bludgeoning";
$NewbieGuideText[%i++] = "weapon to do ex tra damage and knock the enemy back. Requires 15 skill to bash. Train by bashing.";
$NewbieGuideText[%i++] = "<f1>Stealing: <f0>Lets you attempt to rob other players. To steal, type <f1>#steal<f0> when very close to your";
$NewbieGuideText[%i++] = "target. If you succeed, you will get some of the money the target was carrying. If you fail, the target";
$NewbieGuideText[%i++] = "will be alerted of your attempt and you get nothing. Requires 15 skill to steal, 270 to <f1>#pickpocket<f0>,";
$NewbieGuideText[%i++] = "and 620 to <f1>#mug<f0>. Train by stealing more.";
$NewbieGuideText[%i++] = "<f1>Hiding: <f0>Lets you hide in shadows. To hide, type <f1>#hide<f0> in a dark area. More skill lets you hide";
$NewbieGuideText[%i++] = "longer. Train by hiding. Requires 15 skill to hide.";
$NewbieGuideText[%i++] = "<f1>Backstabbing: <f0>Lets you use a piercing attack. To back stab, be hidden, use a piercing weapon,";
$NewbieGuideText[%i++] = "and hit the enemy from behind.";
$NewbieGuideText[%i++] = "<jl><f1>Offensive casting: <f0>Lets you cast offensive spells. Train by attacking with spells.";
$NewbieGuideText[%i++] = "<f1>Defensive casting: <f0>Lets you cast defensive spells. Train by casting defensive spells.";
$NewbieGuideText[%i++] = "<f1>Neutral casting: <f0>Lets you cast neutral spells. Train by casting neutral spells.";
$NewbieGuideText[%i++] = "<f1>Spell Resistance: <f0>Gives a chance to avoid being hit with a spell. Train by getting hit by spells.";
$NewbieGuideText[%i++] = "<f1>Healing: <f0>Regens HP. More healing means faster regen. Train by drinking healing potions.";
$NewbieGuideText[%i++] = "<f1>Archery: <f0>Used for bows- more skill means more damage, accuracy, and the ability to use more";
$NewbieGuideText[%i++] = "powerful bows weapons. Train by attacking with bows.";
$NewbieGuideText[%i++] = "<f1>Endurance: <f0>Lets you wear more armor and increases your HP. Train by being attacked.";
$NewbieGuideText[%i++] = "<jl><f1>Mining: <f0>Higher mining means a better chance of getting valuable gems. Train by mining.";
$NewbieGuideText[%i++] = "<f1>Speech: <f0>Allows use of chat commands. Requires 3 for <f1>#shout<f0>, 5 for <f1>#global<f0>, and 6 for";
$NewbieGuideText[%i++] = "<f1>#group<f0>. Train by chatting.";
$NewbieGuideText[%i++] = "<f1>Sense heading: <f0>Allows use of <f1>#compass<f0>, <f1>#track<f0>, and <f1>#trackpack<f0>. Tracking tells you what";
$NewbieGuideText[%i++] = "direction and how far a person or bot is from you. To track, type <f1>#track playername<f0>. <f1>#trackpack allows";
$NewbieGuideText[%i++] = "<f0>you to keep track of lost packs. Requires 3 for <f1>#compass<f0>, 15 for <f1>#track<f0>, 45 for <f1>#zonelist<f0>, and 85";
$NewbieGuideText[%i++] = "for <f1>#trackpack<f0>. Train by using these abities.";
$NewbieGuideText[%i++] = "<f1>Energy: <f0>Gives you more mana and faster regen. Train by drinking mana potions.";
$NewbieGuideText[%i++] = "<f1>Haggling: <f0>Lets you sell items for more and buy for less at merchant. Train by buying and selling at merchant.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f2>Magic:";
$NewbieGuideText[%i++] = "<f0>To cast a spell, type either <f1>#cast spellname<f0> or <f1>#spell spellname<f0>. (They do the same thing.) The ";
$NewbieGuideText[%i++] = "target (if needed) is the cross hair. Each spell has different casting times and recovery time. ";
$NewbieGuideText[%i++] = "(time before you can cast another spell) For offensive spells, the higher your offensive casting, the";
$NewbieGuideText[%i++] = "more damage you do.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f2>Spells:";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<jl><f1>Offensive:";
$NewbieGuideText[%i++] = "<f1>Thorn- 15 O casting- 1 Mana- <f0>Shoots a thorn. Instant cast, fast recovery.";
$NewbieGuideText[%i++] = "<f1>Fireball- 20 O casting- 3 Mana- <f0>Small fireball. Fast cast, fast recovery.";
$NewbieGuideText[%i++] = "<f1>Firebomb- 35 O casting- 5 Mana- <f0>Larger fireball. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Icespike- 45 O casting- 3 Mana- <f0>Spike of ice. Instant cast, fast recovery.";
$NewbieGuideText[%i++] = "<f1>Icestorm- 85 O casting- 4 Mana- <f0>Large storm of ice. Fast cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Ironfist- 110 O casting- 12 Mana- <f0>Explosive crushing attack. Instant cast, very slow recovery.";
$NewbieGuideText[%i++] = "<f1>Cloud- 145 O casting- 10 Mana- <f0>Explosive cloud. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<jl><f1>Melt- 220 O casting- 15 Mana- <f0>Searing heat explosion. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Powercloud- 340 O casting- 20 Mana- <f0>Trio of explosive clouds. Medium cast, slow recovery.";
$NewbieGuideText[%i++] = "<f1>Hellstorm- 420 O casting- 20 Mana- <f0>Very large blast of flame. Slow cast, slow recovery.";
$NewbieGuideText[%i++] = "<f1>Beam- 520 O casting- 30 Mana- <f0>A beam of light. Instant cast, very slow recovery.";
$NewbieGuideText[%i++] = "<f1>Dimensionrift- 750 O casting- 40 Mana- <f0>A hole to another dimension opens, ripping pieces of anything";
$NewbieGuideText[%i++] = "nearby away. Very slow cast, very slow recovery.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Defensive:";
$NewbieGuideText[%i++] = "<f1>Heal- 10 D casting- 2 Mana- <f0>Heals you. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<jl><f1>Advheal1- 80 D casting- 3 Mana- <f0>Heals you or target in LOS. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Advheal2- 110 D casting- 4 Mana- <f0>Heals you or target in LOS. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Advheal3- 200 D casting- 5 Mana- <f0>Heals you or target in LOS. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Advheal4- 320 D casting- 6 Mana- <f0>Heals you or target in LOS. Medium cast, medium recovery.";
$NewbieGuideText[%i++] = "<f1>Advheal5- 400 D casting- 7 Mana- <f0>Heals you or target in LOS. Medium cast, slow recovery.";
$NewbieGuideText[%i++] = "<f1>Advheal6- 500 D casting- 8 Mana- <f0>Heals you or target in LOS. Medium cast, slow recovery.";
$NewbieGuideText[%i++] = "<f1>Godlyheal- 600 D casting- 15 Mana- <f0>Heals you or target in LOS. Medium cast, slow recovery.";
$NewbieGuideText[%i++] = "<jl><f1>Fullheal- 750 D casting- 2 Mana- <f0>Heals you completely. Medium cast, very slow recovery.";
$NewbieGuideText[%i++] = "<f1>Massheal- 850 D casting, remort level 2- 12 Mana- <f0>Heals you and all players near you. Medium cast,";
$NewbieGuideText[%i++] = "slow recovery.";
$NewbieGuideText[%i++] = "<f1>MassFullheal- 950 D casting, remort level 3- 200 Mana- <f0>Heals you and all players near you completely.";
$NewbieGuideText[%i++] = "Medium cast, insanely slow recovery.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Shield- 20 D casting- 5 Mana- <f0>Creates a protecting magical shield around you. Medium cast,";
$NewbieGuideText[%i++] = "slow recovery.";
$NewbieGuideText[%i++] = "<f1>AdvShield1- 60 D casting- 8 Mana- <f0>Creates a protecting magical shield around you or target in LOS.";
$NewbieGuideText[%i++] = "Medium cast, slow recovery.";
$NewbieGuideText[%i++] = "<f1>AdvShield2- 140 D casting- 15 Mana- <f0>Creates a protecting magical shield around you or target in LOS.";
$NewbieGuideText[%i++] = "Medium cast, slow recovery.";
$NewbieGuideText[%i++] = "<f1>AdvShield3- 290 D casting- 18 Mana- <f0>Creates a protecting magical shield around you or target in LOS.";
$NewbieGuideText[%i++] = "Medium cast, very slow recovery.";
$NewbieGuideText[%i++] = "<f1>AdvShield4- 420 D casting- 22 Mana- <f0>Creates a protecting magical shield around you or target in LOS.";
$NewbieGuideText[%i++] = "Medium cast, very slow recovery.";
$NewbieGuideText[%i++] = "<jl><f1>AdvShield5- 635 D casting- 25 Mana- <f0>Creates a protecting magical shield around you or target in LOS.";
$NewbieGuideText[%i++] = "Medium cast, very slow recovery.";
$NewbieGuideText[%i++] = "<f1>MassShield- 680 D casting- 20 Mana- <f0>Creates a protecting magical shield around you and everyone nearby.";
$NewbieGuideText[%i++] = "Medium cast, very slow recovery.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Neutral:";
$NewbieGuideText[%i++] = "<f1>Teleport- 60 N casting- 8 Mana- <f0>Teleports you near the closest zone. ";
$NewbieGuideText[%i++] = "Syntax is <f1>#cast teleport<f0> (town or dungeon). Takes you to nearest of chosen type. It is important to";
$NewbieGuideText[%i++] = "know that you may not end up very close to the zone you wanted to go to- it is somewhat random and";
$NewbieGuideText[%i++] = "you may be outside the zone.";
$NewbieGuideText[%i++] = "<f1>Transport- 200 N casting- 12 Mana- <f0>Takes you directly to specific zone.";
$NewbieGuideText[%i++] = "Syntax is <f1>#cast transport<f0> (name of zone). You do not have to type the entire name- you just need";
$NewbieGuideText[%i++] = "enough to separate it from all other zones. This spell is much more accurate than Teleport- you always";
$NewbieGuideText[%i++] = "end up inside or right at the zone.";
$NewbieGuideText[%i++] = "<f1>Advtransport- 350 N casting- 16 Mana- <f0>Takes you or person in cross hair to the chosen zone.";
$NewbieGuideText[%i++] = "It is the same as Transport with the exception of being able to transport others. The other person must";
$NewbieGuideText[%i++] = "be in your group list (explained later) to be transported.";
$NewbieGuideText[%i++] = "<f1>MassTransport- 650 N casting, remort level 1- 50 Mana- <f0>Takes you and everyone on grouplist";
$NewbieGuideText[%i++] = "nearby to the chosen zone.";
$NewbieGuideText[%i++] = "<f1>Mimic- 145 N casting, remort level 2- 80 Mana- <f0>An unusual spell that has a chance of";
$NewbieGuideText[%i++] = "transforming you into the enemy in your LOS...";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Starting to play:";
$NewbieGuideText[%i++] = "<jl><f0>Talking to other players is slightly different then in regular Tribes. Using regular";
$NewbieGuideText[%i++] = "chat (non-team) is \"<f1>say<f0>\". Only people near you hear the message. To talk to NPCs, (non-playing";
$NewbieGuideText[%i++] = "characters) use say to say a greeting, such as \"<f1>hello<f0>\", \"<f1>hail<f0>\", \"<f1>hi<f0>\", or similar. To reply to their";
$NewbieGuideText[%i++] = "message, say the keyword. (in all caps or in brackets) Team chat is \"<f1>zone<f0>\". This sends a message to";
$NewbieGuideText[%i++] = "everyone in the same zone as you, so using this in town sends a message to everyone in town with you.";
$NewbieGuideText[%i++] = "Typing <f1>#global<f0> in front of your message (ie \"<f1>#global Hello everyone<f0>\" instead of \"Hello everyone\")";
$NewbieGuideText[%i++] = "<f0>sends your message to all players. There is also <f1>#shout<f0>, which is like say except a longer range.";
$NewbieGuideText[%i++] = "<f1>#tell<f0> sends a message to a specific person that no one else can read.";
$NewbieGuideText[%i++] = "The syntax is <f1>#tell playername, message.<f0>";
$NewbieGuideText[%i++] = "To level up, you have to get experience points, gained by killing monsters (bots) or doing quests.";
$NewbieGuideText[%i++] = "Leveling up improves your HP and gives you more skill points.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "You join the game in Keldrin Town. The town has a bank, a merchant, a smith, an inn,";
$NewbieGuideText[%i++] = "and an arena to practice fighting. Your starting equipment is a pickaxe, some blue potions, some crystal";
$NewbieGuideText[%i++] = "<f0>blue potions and some money. The banker can hold your money, and keep items in storage for you.";
$NewbieGuideText[%i++] = "Money in the bank cannot be stolen by thieves. Both services are free. You can buy and sell items to";
$NewbieGuideText[%i++] = "the merchant, which works in a similar way to inventory station in Tribes. Click to buy or sell something,";
$NewbieGuideText[%i++] = "and the merchant will quote a price. If it is acceptable, click again to buy or sell the item for that price.";
$NewbieGuideText[%i++] = "<f0>The more of a specific item sold, the less money you get for that item, and the more of an item bought,";
$NewbieGuideText[%i++] = "the less the price. Selling weapons taken from slain monsters is the most common way of making money.";
$NewbieGuideText[%i++] = "If you go upstairs in the inn, you can sleep for a while by typing <f1>#sleep<f0>. Sleeping recovers your HP and";
$NewbieGuideText[%i++] = "mana faster than regular recovery. Wake up by typing <f1>#wake<f0>.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<jl><f0>The arena is a place to practice your fighting skills and gain exp and a little cash.";
$NewbieGuideText[%i++] = "To go in, talk to the porter. The price is 5 coins to fight as long as you wish. When you go in the arena,";
$NewbieGuideText[%i++] = "you can stay on the starting area to watch fights or talk to the manager. Tell him leave if you are ready";
$NewbieGuideText[%i++] = "to go, or fight to join the next battle. When you fight, you are given a knife, a club, and a hatchet to";
$NewbieGuideText[%i++] = "fight with, but your other equipment is held for you. You wait in a separate area until the fight starts.";
$NewbieGuideText[%i++] = "<f0>The fights are 2 on 2, with any places not taken by human players are filled with bots. The bots adapt";
$NewbieGuideText[%i++] = "to the average level of human players currently fighting. Any winning players are awarded prize money.";
$NewbieGuideText[%i++] = "The prizes are randomly chosen from 50, 500, 1000, or 5000 coins. The top prizes are very rare.";
$NewbieGuideText[%i++] = "After the fight, you are taken to the top area where you can watch or talk to the manager again.";
$NewbieGuideText[%i++] = "Most monsters drop rusty or damaged weapons. These function the same as their new counterparts,";
$NewbieGuideText[%i++] = "but less efficiently. (ie less damage and slower swing) To repair them, take them to the blacksmith in ";
$NewbieGuideText[%i++] = "<f0>Keldrin Town. He will fix them for a fee. (The fee is always less than buying the same weapon new.)";
$NewbieGuideText[%i++] = "The smith can also smith certain items together to make a new, more powerful item.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "The pickaxe can be used to mine various gems. To mine, swing your pickaxe at a crystal (large and purple)";
$NewbieGuideText[%i++] = "to get something. There are many crystals- just look in most enemy zones. Sell the gems at a merchant";
$NewbieGuideText[%i++] = "for various prices. This is an alternate way to make money. More mining gets you better gems more often.";
$NewbieGuideText[%i++] = "<jl><f0>The tab menu gives you a few options. You can see what level and class everyone is and which";
$NewbieGuideText[%i++] = "zone they are. You can cut on ignore global to have no global messages be displayed on your screen.";
$NewbieGuideText[%i++] = "You can see your stats and skill points here. You spend skill points here. You can also change luck mode";
$NewbieGuideText[%i++] = "here. (More on these later.)";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "Luck or LCK, in its default mode, protects your pack when you die. If you have luck, you only";
$NewbieGuideText[%i++] = "drop some of your things (anything equipped and cash) and your pack is protected for a while, and only";
$NewbieGuideText[%i++] = "you can pick it up during that time. One luck point is also taken away. If you have no luck, you drop";
$NewbieGuideText[%i++] = "everything and anyone (including monsters) can pick it up. Some people might give it back, but there's";
$NewbieGuideText[%i++] = "<f0>no guarantee. You can set in the tab menu so that instead of it protecting your pack, any blow that";
$NewbieGuideText[%i++] = "would kill you will miss instead at the cost of a luck point.";
$NewbieGuideText[%i++] = "At level 25, you can join a House by talking to the Guild master in Fort Ethren. Joining a house allows";
$NewbieGuideText[%i++] = "you to PK and be PKed by members of other houses. The risk is not without rewards, as you get an EXP";
$NewbieGuideText[%i++] = "bonus that depends on the number of rank points you have. Earn more rank points by PKing members of";
$NewbieGuideText[%i++] = "other houses. You also get double EXP for killing a player as opposed to a bot, so joining a house gives";
$NewbieGuideText[%i++] = "rich rewards.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "An alternate method of PKing is using the target list. Putting someone on your target list allows";
$NewbieGuideText[%i++] = "both of you to hurt each other, but warns the target. If you take that person off, you can no longer";
$NewbieGuideText[%i++] = "hurt each other.";
$NewbieGuideText[%i++] = "<jl><f0>In the Tab menu, you can create a party. In a party, if more than one member hits a monster,";
$NewbieGuideText[%i++] = "you get bonus party EXP. Invite a player by clicking their name in the Tab menu and selecting";
$NewbieGuideText[%i++] = "\"<f1>Invite to party<f0>\".";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "To start fighting monsters, go south over the bridge along the path to Keldrin Mine, the easiest";
$NewbieGuideText[%i++] = "dungeon. You can fight either goblins or gnolls here. The goblins are the easiest and suitable to low";
$NewbieGuideText[%i++] = "levels. Runts are the easiest, then Thieves, then Raiders, then Wizard. Watch out for the Wizard's";
$NewbieGuideText[%i++] = "spells- they can catch you by surprise! Gnolls are harder, and you might wish to leave them to level 5";
$NewbieGuideText[%i++] = "<f0>or so. Pups are easiest, then Shamans, then Scavengers, then Hunters. Pups carry crystal blue potions.";
$NewbieGuideText[%i++] = "Goblins are found in the upper levels, to the right as you enter, and gnolls are in the lower levels, to";
$NewbieGuideText[%i++] = "the left. When you start out, use your pickaxe to kill one monster, then switch to their weapon, as the";
$NewbieGuideText[%i++] = "pickaxe is a poor weapon. If you ask a higher level, they may give you a weapon to start with. ";
$NewbieGuideText[%i++] = "If you get hurt, you can use potions or go to town and sleep. <f1>Blue potions cure 15 hp, and crystal blue";
$NewbieGuideText[%i++] = "<f1>potions cure 60 hp<f0>. Use blue potions first. Energy Vials are useful for recovering mana";
$NewbieGuideText[%i++] = "<f0>If you start to move slowly, you are probably overweight. Go to town and sell items or drop excess";
$NewbieGuideText[%i++] = "items to lower your weight.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "The Stronghold is the next dungeon. It is east of town, over the water and up a big hill. You can";
$NewbieGuideText[%i++] = "fight orcs here. The order of difficultly is Warlock being the easiest, then Berserker, then Ravager,";
$NewbieGuideText[%i++] = "then Slayers. Ravagers carry blue potions. Level 10 is a suggested level to start on orcs. Ogres,";
$NewbieGuideText[%i++] = "the next enemy level, are hidden somewhere in the mine. The temple is the next zone. The temple is ";
$NewbieGuideText[%i++] = "<f0>through the valley from stopping point of east town boat, then east up a hill. Fight undead here. ";
$NewbieGuideText[%i++] = "The Elven Forest is an outdoor zone with many trees. The elves are harder than undead, but watch out for ";
$NewbieGuideText[%i++] = "the bows! After that comes the Traveller's Den (don't fall), and then the hidden Minotaur's Lair.";
$NewbieGuideText[%i++] = "However, there are rumors of a hidden zone for people who have surmounted great challenges...";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>B<f2>-<f0>I<f2>-<f1>G <f0>W<f2>-<f1>A<f2>-<f0>R<f2>-<f1>N<f2>-<f0>I<f2>-<f1>N<f2>-<f0>G<f2>-<f1>!<f2>-<f0>!<f2>-<f1>! <f0>Falling can prove <f1>VERY<f0> damaging! Be careful, or you <f1>WILL<f0> die!";
$NewbieGuideText[%i++] = "<jl>";
$NewbieGuideText[%i++] = "<f1>Miscellaneous-";
$NewbieGuideText[%i++] = "<f0>Learn the <f1>#w<f0> command. Using it gets you information on any spell, command, or weapon. Syntax is:";
$NewbieGuideText[%i++] = "<f1>#w shortsword";
$NewbieGuideText[%i++] = "<f1>#w fullplatearmor";
$NewbieGuideText[%i++] = "<f1>#w fireball";
$NewbieGuideText[%i++] = "<f1>#w beam";
$NewbieGuideText[%i++] = "<f1>#w #bash";
$NewbieGuideText[%i++] = "<f1>#w #hide";
$NewbieGuideText[%i++] = "<f0>And so on. Note the lack of spaces.";
$NewbieGuideText[%i++] = "<jl>Keep potions on you, especially for warriors. They are lifesavers.";
$NewbieGuideText[%i++] = "Learn the spawn spots of the bots. This information is useful if you want to fight a specific enemy.";
$NewbieGuideText[%i++] = "Black statues, Enchanted stones, and Skeleton bones are quest items. Give them to people who ask";
$NewbieGuideText[%i++] = "for them to get a reward. To find the people who want them, explore! There is a lot to find if you just";
$NewbieGuideText[%i++] = "look. Each different server has a different character for your name, so if you play in one server then go";
$NewbieGuideText[%i++] = "<f0>to play in a new one, you have to start a new character.";
$NewbieGuideText[%i++] = "If you want to start a new character in a server, change your name. Only admins can delete your char.";
$NewbieGuideText[%i++] = "The game saves your character occasionally, but you can save it at any time by typing <f1>#savecharacter<f0>.";
$NewbieGuideText[%i++] = "Typing <f1>#compass<f0> (town or dungeon) gives you the direction of the nearest zone of that type if you have";
$NewbieGuideText[%i++] = "3 Sense Heading. Useful if you are lost.";
$NewbieGuideText[%i++] = "<jl>";
$NewbieGuideText[%i++] = "If someone steals from you, DON'T whine about it. That makes you unpopular and thieves certainly won't";
$NewbieGuideText[%i++] = "stop if you annoy them. Moving a lot makes it hard to be stolen from. It also helps to learn who is a thief.";
$NewbieGuideText[%i++] = "<f0>PKing does happen. Like stealing, whining only makes it WORSE. PKers like nothing more than killing";
$NewbieGuideText[%i++] = "whiners. I speak from experience.";
$NewbieGuideText[%i++] = "This should go without saying, but don't be a rude moron. Show some respect to everyone, and it should";
$NewbieGuideText[%i++] = "get shown back.";
$NewbieGuideText[%i++] = "Complaining about lag DOES NOT make it go away. We all know if it's lagging, you DO NOT need to tell";
$NewbieGuideText[%i++] = "<f0>everyone.";
$NewbieGuideText[%i++] = "Spells that target a specific person (the advheals, godlyheal, and advtransport) are locked in at the";
$NewbieGuideText[%i++] = "time you start to cast the spell, not the time the spell actually happens.";
$NewbieGuideText[%i++] = "Some scripts can interfere with RPG. If you are having problems, try turning them off before you play.";
$NewbieGuideText[%i++] = "<jl>";
$NewbieGuideText[%i++] = "To set a password on your character, type <f1>#mypassword password<f0>. Enter the password you typed there";
$NewbieGuideText[%i++] = "in the Other Info box on the Tribes alias selection screen. (The other info blank is on the screen where";
$NewbieGuideText[%i++] = "you pick your voice pack and personal skin and has a model of a running figure) ";
$NewbieGuideText[%i++] = "Some of the bots have things you can buy even if they do not have buy as a keyword, like the bots in";
$NewbieGuideText[%i++] = "the mage tower.";
$NewbieGuideText[%i++] = "<f0>";
$NewbieGuideText[%i++] = "If you try to join a server and it tells you that you are missing a file, you probably do not have the";
$NewbieGuideText[%i++] = "latest update. Go to the website and download the mod package again. If it still doesn't work:";
$NewbieGuideText[%i++] = "Rename your RPG directory to RPGtemp or similar.";
$NewbieGuideText[%i++] = "Download the full ver and install it.";
$NewbieGuideText[%i++] = "<jl>Download the update and install it.";
$NewbieGuideText[%i++] = "If it still does not work, the server probably does not have the latest version. JI and PoG's server,";
$NewbieGuideText[%i++] = "run by the mod creator, always has the latest version- check your server's version against that one.";
$NewbieGuideText[%i++] = "<f0>";
$NewbieGuideText[%i++] = "If you fall off the map, wait a few seconds and type <f1>#recall<f0>. If it doesn't work, keep trying. Also,";
$NewbieGuideText[%i++] = "if you get lost, you can type <f1>#recall<f0> and hold still for 5 minutes to be taken back to town.";
$NewbieGuideText[%i++] = "In Fort Ethren, you can rent bots from a guard on the wall. These bots will fight for you for about";
$NewbieGuideText[%i++] = "an hour. Various commands to control them-";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>#eyes - <f0>Sees what the bot sees.";
$NewbieGuideText[%i++] = "<f1>#possess - <f0>Takes control of the bot.";
$NewbieGuideText[%i++] = "<jl><f1>#revert - <f0>Stops using #eyes and #possess.";
$NewbieGuideText[%i++] = "<f1>#freeze - <f0>Freezes the bot in place.";
$NewbieGuideText[%i++] = "<f1>#cancelfreeze - <f0>Unfreezes the bot.";
$NewbieGuideText[%i++] = "<f1>#attacklos - <f0>Bot guards point at LOS.";
$NewbieGuideText[%i++] = "<f1>#botnormal - <f0>Cancels an #attacklos.";
$NewbieGuideText[%i++] = "<f0>The #shove command moves people out of your way. It requires 5 bashing, and gets stronger with";
$NewbieGuideText[%i++] = "higher level.";
$NewbieGuideText[%i++] = "Tents let you sleep anywhere you set them up. Buy them in Jaten. To set it up, type <f1>#camp<f0>. To sleep";
$NewbieGuideText[%i++] = "in one, go inside and type <f1>#sleep<f0>. To pack it up, type <f1>#uncamp<f0>. You may set it up as many times as you";
$NewbieGuideText[%i++] = "want, but if you leave it set up too long, it disappears.";
$NewbieGuideText[%i++] = "You can mass buy and sell to save time at merchants. While at merchant, hit buy/sell on the item.";
$NewbieGuideText[%i++] = "After it quotes price, say the number you want to buy/sell. Hit buy/sell again, and that amount is sold.";
$NewbieGuideText[%i++] = "This also works with storage.";
$NewbieGuideText[%i++] = "<jl>Nevim, the man in the inn, sells Orbs of Luminance you can use as lights. Be careful, as rain puts";
$NewbieGuideText[%i++] = "them out.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "This guide should help you get started and enjoy Tribes RPG!";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Appendix- Binding keys and running a server";
$NewbieGuideText[%i++] = "<f0>Starting a non-dedicated RPG server is the same as starting a non-dedicated regular tribes server-";
$NewbieGuideText[%i++] = "just select the right map. (RPGmap5) Start a dedicated RPG server by selecting \"dedicated\" on the";
$NewbieGuideText[%i++] = "RPG autoloader. To set admin passwords, put this into autoexec.cs:";
$NewbieGuideText[%i++] = "$AdminPassword[<f1>1<f0>] = \"<f1>aaa<f0>\"; ";
$NewbieGuideText[%i++] = "$AdminPassword[<f1>2<f0>] = \"<f1>bbb<f0>\"; ";
$NewbieGuideText[%i++] = "$AdminPassword[<f1>3<f0>] = \"<f1>ccc<f0>\"; ";
$NewbieGuideText[%i++] = "$AdminPassword[<f1>4<f0>] = \"<f1>ddd<f0>\"; ";
$NewbieGuideText[%i++] = "$AdminPassword[<f1>5<f0>] = \"<f1>eee<f0>\"; ";
$NewbieGuideText[%i++] = "<jl>$loginmsg = \"<f1>Login message here<f0>\"; ";
$NewbieGuideText[%i++] = "<f0>$console::logmode = <f1>0<f0>; ";
$NewbieGuideText[%i++] = "<f0>Change the passwords and the login message to whatever you like. Put the file in the Tribes config";
$NewbieGuideText[%i++] = "folder.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Binding Keys:";
$NewbieGuideText[%i++] = "<f0>Put this in your autoexec.cs folder:";
$NewbieGuideText[%i++] = "editActionMap(\"playmap.sae\"); ";
$NewbieGuideText[%i++] = "bindAction(keyboard0, make, \"<f1>x<f0>\", TO, IDACTION_MOVEFORWARD, 1.000000); ";
$NewbieGuideText[%i++] = "bindCommand(Keyboard0, make, \"<f1>h<f0>\", TO, \"postaction(2048,IDACTION_FIRE1,-0);\"); ";
$NewbieGuideText[%i++] = "<jl>bindCommand(keyboard0, make, \"<f1>0<f0>\", TO, \"use(\\\"<f1>Crystal Blue Potion<f0>\\\");\"); ";
$NewbieGuideText[%i++] = "bindCommand(keyboard0, make, \"<f1>9<f0>\", TO, \"remoteeval(2048, say(0, \\\"<f1>#cast firebomb<f0>\\\"));\"); ";
$NewbieGuideText[%i++] = "<f0>bindCommand(keyboard0, make, \"<f1>8<f0>\", TO, \"remoteeval(2048, say(0, \\\"<f1>#steal<f0>\\\"));\"); ";
$NewbieGuideText[%i++] = "bindCommand(keyboard0, make, \"<f1>7<f0>\", TO, \"remoteeval(2048, say(0, \\\"<f1>#cast cloud<f0>\\\"));\"); ";
$NewbieGuideText[%i++] = "bindCommand(keyboard0, make, \"<f1>6<f0>\", TO, \"remoteeval(2048, say(0, \\\"<f1>#cast melt<f0>\\\"));\"); ";
$NewbieGuideText[%i++] = "<f0>The top one is auto walker. The second is auto fire, which is useful for mining. The third one uses";
$NewbieGuideText[%i++] = "crystal blue potions. The rest do that action as if it was typed in the chat window. Change the character";
$NewbieGuideText[%i++] = "in quotes after \"make\" to whatever you like.";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<jl>Have fun!";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f1>Toaster";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f0>Special thanks to <f1>JeremyIrons<f0> for creating this wonderful mod!";
$NewbieGuideText[%i++] = "";
$NewbieGuideText[%i++] = "<f0>http://dynamic2.gamespy.com/~rpg/";
//echo("TEXT LINE: "@%i);

$DeusRPG::TextLine = 0;
$DeusRPG::MaxTextLine = %i;
%i = "";


function DeusRPGHud::UpdateNewbieGuide::MAIN(%hud) {
//echo($DeusRPG::TextLine);
	%max = round($NewbieGuideHeight / 13.3);
//echo("MaxLines "@%max);
	for(%i = $DeusRPG::TextLine; %i <= $DeusRPG::MaxTextLine; %i++) {
		HUD::AddTextLine(%hud, $NewbieGuideText[%i]);

		if(%ii++ >= %max)
			break;
	}

}


function DeusRPGPack::func16() {	//Down
	if(!$DeusRPG::ViewingGuide)
		return;
	if($DeusRPG::TextLine <= $DeusRPG::MaxTextLine - ($DeusRPG::SkipLines * $DeusRPG::SkipLines))
		$DeusRPG::TextLine += $DeusRPG::SkipLines; //+
	else
		echo("Can't go lower");

	HUD::ToggleDisplay(DeusNewbieGuide::MAIN); HUD::ToggleDisplay(DeusNewbieGuide::MAIN);
}


function DeusRPGPack::func17() {	//Up
	if(!$DeusRPG::ViewingGuide)
		return;
	if($DeusRPG::TextLine >= 0 + $DeusRPG::SkipLines)
		$DeusRPG::TextLine -= $DeusRPG::SkipLines; //-
	else
		echo("Can't go higher");

	HUD::ToggleDisplay(DeusNewbieGuide::MAIN); HUD::ToggleDisplay(DeusNewbieGuide::MAIN);
}

$DeusRPG::ViewingGuide = false;
function DeusRPG::toggleGuide() {

	if(!$DeusRPG::ViewingGuide)
		$DeusRPG::ViewingGuide = true;
	else
		$DeusRPG::ViewingGuide = false;

	HUD::ToggleDisplay(DeusNewbieGuide::MAIN);
}

if($NewbieGuideWidth == "")
	$NewbieGuideWidth = 700;
if($NewbieGuideHeight == "")
	$NewbieGuideHeight = 400; //400h = about 30 lines (13.333)

HUD::New(DeusNewbieGuide::MAIN, DeusRPGHud::UpdateNewbieGuide::MAIN, "20% 50% "@$NewbieGuideWidth@" "@$NewbieGuideHeight);

$DeusRPG::ScriptCheck++;