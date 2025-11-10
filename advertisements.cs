function Advertize(%stage)
{
	if(%stage < 1) %stage = 1;
	if(%stage > 8) %stage = 1;
	echo(%stage);
//	if(%stage == n)
//	{
//		Messageall(2,"");
//		Messageall(2,"Should never make it longer than this!!!!!!!!!");
//	}
	if(%stage == 1)
	{
		Messageall(2,"Welcome to Kingdom of Kronos");
		Messageall(2,"Sorry for that extended downtime.");
		Messageall(2,"Suggestions? Comments? Need help? Email me at dsboyx@gmail.com");
	}
	else if(%stage == 2)
	{
		Messageall(2,"For spells/items/weapons/armor type:");
		Messageall(2,"#offensivespells #defensivespells #neutralspells");
		Messageall(2,"#slash #pierce #bludge #defense #armor");
	}
	else if(%stage == 3)
	{
		Messageall(2,"Your Host: Jobo");
		Messageall(2,"Credits: Asnabel, Superfat, Panda, Scorpion, Xan and DeraJ");
		Messageall(2,"They all put their part into helping.");
	}
	else if(%stage == 4)
	{
		Messageall(2,"Critical hit Skill added.");
		Messageall(2,"Depending on how high the skill is, your chance");
		messageall(2,"at getting a successful double damage hit will increase.");
	}
	else if(%stage == 5)
	{
		Messageall(2,"This server has been doned hack proof by:");
		Messageall(2,"Corona and Shorty.");
	}
	else if(%stage == 6)
	{
		Messageall(2,"Special thanks to Superfat for keeping the mod alive.");
	}

 	%stage++;
 	schedule("Advertize("@%stage@");",300);
}
Advertize(1);
