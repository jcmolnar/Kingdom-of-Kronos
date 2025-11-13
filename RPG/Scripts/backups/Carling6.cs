function IP::LogCheck(%clientid,%ip)
{
	%ip = TrimIP(%ip);
	%name = client::getname(%clientid);
	$iplog = %ip;
	echo(">>> Connect: "@%name@" - "@%ip);
	export("iplog", "temp\\-0_IPLOG_"@ %name @ ".cs", true);
	$iplog = "";
}

function Client::Smith(%clientid,%cropped)
{
	for(%i=1;$SmithComboResult[%i]!="";%i++)
	{
		if(String::ICompare(getword($SmithComboResult[%i],0), getword(%cropped,1)) == 0)
		{
			%items = $SmithCombo[%i];
			break;
		}
	}

	if(%items != "")
	{
		if(HasThisStuff(%clientid,%items))
		{
			%num = GetSmithCombo(%items);
			%cost = GetSmithComboCost(%clientid, %num);
			
			if(fetchdata(%clientid,"COINS") >= %cost)
			{
				TakeThisStuff(%clientid,%items);
				TakeThisStuff(%clientid,"COINS "@%cost);
				GiveThisStuff(%clientid,$SmithComboResult[%num],true);
				Client::Sendmessage(%clientid,2,"blacksmith tells you, There you go! I hope you enjoy it "@Client::getName(%clientid)@"!");
			}
			else
				Client::Sendmessage(%clientid,2,"blacksmith tells you, You cant afford my fee of "@%cost@" coins! Get out of my sight!!");
		}
		else
			Client::Sendmessage(%clientid,2,"blacksmith tells you, You dont have the items i need to make this item.");
	}
	else
		Client::Sendmessage(%clientid,2,"blacksmith tells you, I dont know how to make a that item.");
}

//PCRPG Custom Source
//Thanks Parts!
//Functions specifically made to enhance server security.

function isValidSpell(%input)
{
	if(String::Icompare(GetWord(%input, 0), "transport") == 0 || String::Icompare(GetWord(%input, 0), "teleport") == 0 || String::Icompare(GetWord(%input, 0), "advtransport") == 0)
	{
		if(isValidTransDest(String::NEWgetSubStr(%input, (String::len(GetWord(%input, 0)) + 1), 99999)) == False)
		{
			return False;
		}
		else
		{
			return True;
		}
	}

	for(%i = 1; $Spell::keyword[%i] != ""; %i++)
	{
		if(String::Icompare(%input, $Spell::keyword[%i]) == 0)
		{
			return True;
		}
	}

	return False;
}

function isValidTransDest(%location)
{
	%result = GetZoneByKeywords(0, %location, 3);

	dbecho(1, "The result: " @ %result @ ".  Input was " @ %location);

	if(%result != False)
	{
		return True;
	}

	return False;
}
