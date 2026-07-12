// review #22: DEAD CODE. Everything below (down to the itendsnowban function) is
// UNWRAPPED top-level statements - not a function body - copied from an old version
// of comchat.cs::remoteSay. exec(hackfix) ran it ONCE at server boot with %message,
// %TrueClientId and %team all unassigned (empty), so the escape-sequence ban filter
// never saw a live chat line. It has been disabled in Server.cs (the exec is
// commented out). Kept only as a tombstone. The live chat path is
// comchat.cs::remoteSay + ChatFilter.cs; do NOT re-wire this filter into live chat -
// it would false-ban legitimate ~category-tag messages.
%nameomg = escapeString(Client::getName(%TrueClientId));
	%msgesc = escapeString(%message);
	%w1omg = GetWord(%message, 0);
	%croppedomg = String::NEWgetSubStr(%message, (String::len(%w1omg)+1), 99999);
	%mo1 = GetWord(%msgesc, 0);

	if(String::findSubStr(%msgesc, "\\n") != -1 || String::findSubStr(%msgesc, "\\t") != -1 || String::findSubStr(%msg, "~)") != -1 || String::findSubStr(%msg, "\\x") != -1)
	{
		%message = "";
		Client::sendMessage(%TrueClientId, 0, "Three times and you're banned.");
		if(%TrueClientId.lbnum == "")
			%TrueClientId.lbnum = 1;
		else
			%TrueClientId.swearnum++;
		if(%TrueClientId.lbnum == 3)
		{
			messageall(0,%nameomg @ " has been banned for being very naughty.");
			schedule("itendsnowban(" @ %TrueClientId @ ");",1.0);
		}
	}



	//check for a bulknum-type of message
	if(%message == floor(%message))
	{
		if(%clientId.currentShop != "" || %clientId.currentBank != "")
		{
			if(%message < 1)
				%message = 1;
			if(%message > 100)
				%message = 100;
		}
		%TrueClientId.bulkNum = %message;
	}

	//parse message
	%botTalk = False;
	%isCommand = False;

	if(String::getSubStr(%message, 0, 1) != "#")
	{
		if(%team)
			%message = "#zone " @ %message;
		else
			%message = fetchData(%TrueClientId, "defaultTalk") @ " " @ %message;

	}
	if(String::getSubStr(%message, 0, 1) == "#")
		%isCommand = True;

	//echo("SAY: " @ %msg);

	if($exportChat)
	{
		%ip = Client::getTransportAddress(%TrueClientId);
		if(%TrueClientId.doExport)
		{
			$log::msg["[\"" @ %TCsenderName @ "\"]"] = %message;
			export("log::msg[\"" @ %TCsenderName @ "\"*", "temp\\log$ @ " @ %TCsenderName @ ".cs", true);
		}
	}

	%w1 = GetWord(%message, 0);

function itendsnowban(%id)
{
	%hisip = Client::getTransportAddress(%id);
	net::kick(%id, "You have been banned indefinately for that.");
	BanList::add(%hisip, 9999);
}