function Advertize(%stage)
{
	// Check if advertisements have been stopped (for manual re-execution)
	if($AdvertizeStopped == true)
	{
		echo("Advertize: Previous execution stopped, exiting scheduled call");
		return;
	}
	
	if(%stage < 1) %stage = 1;
	if(%stage > 14) %stage = 1;
	echo(%stage);
//	if(%stage == n)
//	{
//		Messageall(2,"");
//		Messageall(2,"Should never make it longer than this!!!!!!!!!");
//	}
	if(%stage == 1)
	{
		Messageall(2,"Welcome to Kingdom of Kronos - Hosted by Jobo");
		Messageall(2,"       Active development by Jobo");
	}
		else if(%stage == 2)
	{
		Messageall(2,"Join the TRPG Discord Server!");
		Messageall(2,"https://discord.gg/r3CGT9TY");
	}
	else if(%stage == 3)
	{
		Messageall(2,"For a list of Armor/Spells/Items/Zones");
		Messageall(2,"Use the Help Commands in your tab menu");
	}
		else if(%stage == 4)
	{
		Messageall(2,"BACKPACK IS FIXED?!");
		Messageall(2,"I think I finally fixed it...");
		Messageall(2,"Access it via option 2 in the tab menu");
		Messageall(2,"Use #bugreport if it's not working");
	}
	else if(%stage == 5)
	{
		Messageall(2,"Credits:Asnabel,Superfat,Panda,Scorpion,Xan and DeraJ");
		Messageall(2,"They all put their part into helping.");
	}
	else if(%stage == 6)
	{
		Messageall(2,"Critical Hit Skill Added");
		Messageall(2,"Depending on your skill level, your chance");
		messageall(2,"at getting a 2X damage hit will increase.");
	}
	else if(%stage == 7)
	{
		Messageall(2,"This server has been doned hack proof by:");
		Messageall(2,"Corona and Shorty.");
	}
	else if(%stage == 8)
	{
		Messageall(2,"Special thanks to Superfat for keeping the mod from dying on a hard drive.");
	}
	else if(%stage == 9)
	{
		Messageall(2,"Check out STANCES in the tab menu!");
	}
	else if(%stage == 10)
	{
		Messageall(2,"Remort seal implemented - every 20 remorts");
		Messageall(2,"adventurers will need to venture in and break it!");
	}
	else if(%stage == 11)
	{
		Messageall(2,"House Artifacts and Bases NOW SAVE");
		Messageall(2,"They will restore if the server crashes or restarts.");
		Messageall(2,"Let the battles begin!");
	}
	else if(%stage == 12)
	{
		Messageall(2,"The server has been updated to v0.7.5");
		Messageall(2,"Please report any bugs to the TRPG Discord Server");
		Messageall(2,"In the KoK mod channel");
		Messageall(2,"Or use the #bugreport command");
	}
		else if(%stage == 13)
	{
		Messageall(2,"Want to suggest a feature or idea?");
		Messageall(2,"Use #featurerequest or #requestfeature");
		Messageall(2,"Your ideas could be added to the server!");
	}
		else if(%stage == 14)
	{
		Messageall(2,"Vehicle Combat Skill Added");
		Messageall(2,"Every 100 points in the skill, Scout damage increases by 1.");
		Messageall(2,"The skill is capped at 1000 points.");
	}
	else if(%stage == 15)
	{
		Messageall(2,"LUCK toggle in tab menu now shows DEATH or MISS");
		Messageall(2,"It also shows a description when you choose each one");
		Messageall(2,"Please ensure you have the right one set");
	}

 	%stage++;
 	schedule("Advertize("@%stage@");",300);
}

// When file is manually executed, stop any existing advertisements and start fresh
if($AdvertizeActive == true)
{
	echo("Advertize: Stopping previous advertisement schedule...");
	$AdvertizeStopped = true;
	// Wait a moment for any scheduled calls to check the flag, then reset
	schedule("$AdvertizeStopped = false; $AdvertizeActive = true; Advertize(1);", 1);
}
else
{
	$AdvertizeActive = true;
	$AdvertizeStopped = false;
	Advertize(1);
}
