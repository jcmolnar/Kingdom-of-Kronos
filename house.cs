$HouseName[1] = "HouseKronos";
$HouseName[2] = "HouseArbal";
$HouseName[3] = "HouseCurama";
$HouseName[4] = "HouseYuliple";

//$HouseStartUpEq[1] = "AntivaRobe 1";
//$HouseStartUpEq[2] = "FenyarRobe 1";
//$HouseStartUpEq[3] = "TemminRobe 1";
//$HouseStartUpEq[4] = "VenkRobe 1";
$HouseStartUpEq[1] = "";
$HouseStartUpEq[2] = "";
$HouseStartUpEq[3] = "";
$HouseStartUpEq[4] = "";


$HouseMember[HouseYuliple] = 0;
$HouseMember[HouseCurama] = 0;
$HouseMember[HouseArbal] = 0;
$HouseMember[HouseKronos] = 0;

function GetHouseNumber(%n)
{
	dbecho($dbechoMode, "GetHouseNumber(" @ %n @ ")");

	for(%i = 1; $HouseName[%i] != ""; %i++)
	{
		//if($HouseName[%i] == %n)
		if(String::ICompare($HouseName[%i], %n) == 0)
			return %i;
	}
	return "";
}

function BootFromCurrentHouse(%clientId, %echo)
{
	dbecho($dbechoMode, "BootFromCurrentHouse(" @ %clientId @ ", " @ %echo @ ")");

	%h = fetchData(%clientId, "MyHouse");

	if(%h != "")
	{
		UnequipMountedStuff(%clientId);

		%hn = GetHouseNumber(%h);
		if(%echo) Client::sendMessage(%clientId, $MsgRed, "You have been booted from " @ $HouseName[%hn] @ " and have lost all rank points.");

		storeData(%clientId, "MyHouse", "");
		storeData(%clientId, "RankPoints", 0);
		$HouseMember[%h] -= 1;

		return %hn;
	}
	else
		return -1;
}

function JoinHouse(%clientId, %hn, %echo)
{
	dbecho($dbechoMode, "JoinHouse(" @ %clientId @ ", " @ %hn @ ", " @ %echo @ ")");

	storeData(%clientId, "MyHouse", $HouseName[%hn]);
	storeData(%clientId, "RankPoints", $joinHouseRankPoints);
	$HouseMember[$HouseName[%hn]] += 1;
	$HouseChecked[Client::getName(%clientId)] = 1;

	if(%echo) Client::sendMessage(%clientId, $MsgBeige, "You have joined " @ $HouseName[%hn] @ " and have been awarded " @ $joinHouseRankPoints @ " rank points.");
}