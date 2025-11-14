// --------------------------------------------------------------------------------------
// MADHUDPP v1.1 For Presto's PRESTOPAK v.92 and higher - By Prodigy  - April 15,1999
// --------------------------------------------------------------------------------------
//	Welcome to the world of MADHUD.
//	
//	Anyway to make this work, make sure this file is in your
//	c:\dynamix\tribes\config directory and simply add this line:
//	AFTER the exec("presto\\install.cs"); line
//
//	  exec("madhud.cs"); 	
//
//	into your autoexec.cs file.
//
//
//	Updates will be posted regularly and Ideas are appreciated at:
//	
//	MADHUD info  matt1001@earthlink.com
//
// --------------------------------------------------------------------------------------
// Preferences
// --------------------------------------------------------------------------------------
// MADHUD Location - Set the following 2 lines to your desired start location. 
// The X,Y coordinates set the position of the top left corner of the HUD.
// $madhudX = # of pixels from the left of the screen
// $madhudY = # of pixels down from the top of the screen
//
	$madHudX = 0;
	$madHudY = 80;
//
// MADHUD Auto Start
// Set the following line to make MADHUD start up automatically.
// 0 sets the autostart to false
// 1 sets the autostart to true
	
	$madhudAutostart = 1;


// --------------------------------------------------------------------------------------
// Includes
// --------------------------------------------------------------------------------------
Include("presto\\Event.cs");
Include("presto\\HUD.cs");
Include("presto\\TeamTrak.cs");
Include("presto\\KillTrak.cs");

// --------------------------------------------------------------------------------------
//Toggles the HUD on and off.................
// --------------------------------------------------------------------------------------

editActionMap("playMap.sae");
bindCommand(keyboard0, make, control, shift, "h", TO, "madhud::Toggle();");
bindCommand(keyboard0, break, control, shift, "h", TO, "");
bindCommand(keyboard0, make, control, shift, "r", TO, "madhud::Reset();");
bindCommand(keyboard0, break, control,shift, "r", TO, "");
$madhud::StartConnectTime = timestamp();

// --------------------------------------------------------------------------------------
//MADHUD Load
// --------------------------------------------------------------------------------------

function madhud::Create()
{
	echo("Create");	
	if($madhud::Loaded == 1)
		return;

	$madhud::Loaded = 1;

	//initialize the starting coords
	if ($madHudX == "") $madHudX = 0;
	if ($madHudY == "") $madHudY = 65;

	$madhudBase = newObject("madhud_Base", SimGui::Control, $madHudX, $madHudY, 50, 265);

	$madhud[0] = newObject("madhud_Frame", FearGui::FearGuiMenu, 0, 0, 48, 263);
	$madhud[1] = newObject("madhud_FFlag", FearGui::FearGuiMenu, 2, 2, 44, 46);
	$madhud[2] = newObject("madhud_EFlag", FearGui::FearGuiMenu, 2, 49, 44, 46);
	$madhud[3] = newObject("madhud_StatsFrame", FearGui::FearGuiMenu, 2, 96, 44, 85);
	$madhud[4] = newObject("madhud_InvFrame", FearGui::FearGuiMenu, 2, 182, 44, 79);
	$madhud[5] = newObject("madhud_FFlagText", FearGuiFormattedText, 0, 5, 40, 42);
	$madhud[6] = newObject("madhud_EFlagText", FearGuiFormattedText, 0, 50, 40, 42);
	$madhud[7] = newObject("madhud_LblHome", FearGuiFormattedText, 7, 0, 5, 10);
	$madhud[8] = newObject("madhud_LblHScore", FearGuiFormattedText, 10,34, 20, 10);
	$madhud[9] = newObject("madhud_LblAway", FearGuiFormattedText, 7,44, 5, 10);
	$madhud[10] = newObject("madhud_LblAScore", FearGuiFormattedText, 10,88, 20, 4);
	$madhud[11] = newObject("madhud_StatIcons", FearGuiFormattedText, 4, 95, 20, 10);
	$madhud[12] = newObject("madhud_InvIcons", FearGuiFormattedText, 1, 186, 20, 10);
	$madhud[13] = newObject("madhud_LblKills", FearGuiFormattedText, 21, 100, 20, 10);
	$madhud[14] = newObject("madhud_LblDeaths", FearGuiFormattedText, 21, 121, 20, 10);
	$madhud[15] = newObject("madhud_LblTimer", FearGuiFormattedText, 14, 142, 20, 10);
	$madhud[16] = newObject("madhud_LblKpmRatio", FearGuiFormattedText, 14, 163, 20, 10);
	$madhud[17] = newObject("madhud_LblMine", FearGuiFormattedText, 33,186, 20, 10);
	$madhud[18] = newObject("madhud_LblGrenade", FearGuiFormattedText, 33,204, 20, 10);
	$madhud[19] = newObject("madhud_LblBeacon", FearGuiFormattedText, 33,222, 20, 10);
	$madhud[20] = newObject("madhud_LblRepair", FearGuiFormattedText, 32,240, 20, 10);

	$madhudInfo = newObject("madhud_InfoString", FearGuiFormattedText, $madHudX + 50, $madHudY + 95, 25, 50);
	$madhudFFlag = newObject("madhud_FFlagInfoString", FearGuiFormattedText, $madHudX + 50, $madHudY + 2, 25, 50);
	$madhudEFlag = newObject("madhud_EFlagInfoString", FearGuiFormattedText, $madHudX + 50, $madHudY + 49, 25, 50);

	for(%i = 0; $madhud[%i] != ""; %i++)
		addToSet("madhud_Base", $madhud[%i]); 

	addToSet(PlayGui, $madhudBase);
	addToSet(PlayGui, $madhudInfo);
	addToSet(PlayGui, $madhudFFlag);
	addToSet(PlayGui, $madhudEFlag);
 

	Control::setValue("madhud_LblHome", "<Blbl_Home.bmp>");
	Control::setValue("madhud_LblAway", "<Blbl_Away.bmp>");
	Control::setValue("madhud_StatIcons", "<BMadstats.bmp>");
	Control::setValue("madhud_InvIcons", "<BMadInv.bmp>");

	Control::setVisible("madhud_InfoString",0);
	Control::setVisible("madhud_HFlagInfoString",0);
	Control::setVisible("madhud_EFlagInfoString",0);
	
	madhud::starttimer();
	madhud::updatehud();
}

// --------------------------------------------------------------------------------------
//MADHUD - HUD EVENTS
// --------------------------------------------------------------------------------------

Event::Attach(eventExit, madhud::Exit);
Event::Attach(eventKillTrak, madhud::fragevents);
Event::Attach(eventFlagsUpdated, madhud::flagevents);
Event::Attach(eventConnected, madhud::init);
Event::Attach(eventChangeMission, madhud::changemission);

// --------------------------------------------------------------------------------------
//MADHUD - HUD Functions
// --------------------------------------------------------------------------------------
function madhud::startTimer() 
	{
    $timer++;
    madhud::timer($timer);
    }

function madhud::timer(%time)
	{
    if (%time != $timer)
        return;

    $madhud::MineCount = getItemCount("Mine");
	$madhud::GrenadeCount = getItemCount("Grenade");
	$madhud::BeaconCount = getItemCount("Beacon");	
	$madhud::ConnectTime = String::getSubStr((getsimtime() / 60),0,5);
	
	$madhud::DecimalPosition = String::findSubStr($madhud::ConnectTime, ".");
	
	if($madhud::DecimalPosition == 1)
		{
		$madhud::ConnectTimeMin = String::getSubStr($madhud::ConnectTime, 0, 1);  
		$madhud::ConnectTimeSec = String::getSubStr((String::getSubStr($madhud::ConnectTime, 2, 2) * ".6"),0,2);
		
		$madhud::SecDecimalPosition = String::findSubStr($madhud::ConnectTimeSec, ".");
			
			if($madhud::SecDecimalPosition == 1)
				{
				$madhud::ConnectTimeSec = String::getSubStr((String::getSubStr($madhud::ConnectTime, 2, 2) * ".6"),0,1);
				$madhud::ConnectTime =($madhud::ConnectTimeMin @ ":0" @ $madhud::ConnectTimeSec);
				}
			else
				$madhud::ConnectTime =($madhud::ConnectTimeMin @ ":" @ $madhud::ConnectTimeSec);
		}
	
	if($madhud::DecimalPosition == 2)
		{
		$madhud::ConnectTimeMin = String::getSubStr($madhud::ConnectTime, 0, 2);  
		$madhud::ConnectTimeSec = String::getSubStr((String::getSubStr($madhud::ConnectTime, 2, 2) * ".6"),0,2);
		
		$madhud::SecDecimalPosition = String::findSubStr($madhud::ConnectTimeSec, ".");
			if($madhud::SecDecimalPosition == 1)
				{
				$madhud::ConnectTimeSec = String::getSubStr((String::getSubStr($madhud::ConnectTime, 2, 2) * ".6"),0,1);
				$madhud::ConnectTime =($madhud::ConnectTimeMin @ ":0" @ $madhud::ConnectTimeSec);
				}
			else
				$madhud::ConnectTime =($madhud::ConnectTimeMin @ ":" @ $madhud::ConnectTimeSec);

		$madhud::ConnectTime =($madhud::ConnectTimeMin @ ":" @ $madhud::ConnectTimeSec);
		}
	
	
	$madhud::KPMRatio = $madhud::KillCount / ( String::getSubStr((getsimtime() / 60),0,5));
	$madhud::KPMDisplay = String::getSubStr($madhud::KPMRatio,0,4); 
	schedule("madhud::timer("@%time@");",2);
	madhud::UpdateHUD();
    }

//=======================================================================================
function madhud::Toggle()
{
	echo("Toggle");
	if($madhud::Loaded == 1)
		madhud::Remove();
	else
		madhud::Create();
		madhud::updatehud();

}

//=======================================================================================
function madhud::Reset()
{
	echo("resetting");
	$madhud::KillCount = 0;
	$madhud::DeathCount = 0;
	$madhud::FriendlyCarrier = "";
	$madhud::EnemyCarrier = "";
	$madhud::FlagSafe = 1;
	$madhud::EnemyFlagSafe = 1;
	$madhud::StatusString = "";
	$madhud::FFlagInfoString = "";
	$madhud::EFlagInfoString = "";
	$madhud::GrenadeCount = 0;
	$madhud::MineCount = 0;
	$madhud::PackType = "";
	ResetSimTime();

	if($madhud::Loaded == 1)
		{
		madhud::updatehud();
		}
	else
		{
		madhud::Create();
		madhud::updatehud();
		}
}

function madhud::Init()
	{
	if($madhud::Loaded == 1)
		return;
		
	echo("Initializing");
	
	if($madhudAutoStart)
		{
		if(activategroup(playgui))
			{
			echo("playgui");
			madhud::reset();
			}
		else
			echo("No playgui");
			schedule("madhud::Init();",2);
		}
		
	}
	

function madhud::ChangeMission()
{
	echo("changing mission");
	$madhud::KillCount = 0;
	$madhud::DeathCount = 0;
	$madhud::FriendlyCarrier = "";
	$madhud::EnemyCarrier = "";
	$madhud::FlagSafe = 1;
	$madhud::EnemyFlagSafe = 1;
	$madhud::StatusString = "";
	$madhud::FFlagInfoString = "";
	$madhud::EFlagInfoString = "";
	$madhud::GrenadeCount = 0;
	$madhud::MineCount = 0;
	$madhud::PackType = "";
	ResetSimTime();
	madhud::Remove();
	
	madhud::init();
	
}

function madhud::Remove()
	
{
	echo("Remove");
	if($madhud::Loaded == 0)
		return;

	$madhud::Loaded = 0;
	for(%i = 0; $madhud[%i] != ""; %i++)
		deleteObject($madhud[%i]);
		deleteObject($madhudinfo);
		deleteObject($madhudeflag);
		deleteObject($madhudfflag);
}

function madhud::exit()
{
	$madhud::Loaded = 0;
	for(%i = 0; $madhud[%i] != ""; %i++)
		deleteObject($madhud[%i]);
		
}
//=======================================================================================
function madhud::PopUp(%objectname, %hold)
{
	$madhud::CurrentObject = %objectname;
	Control::setVisible(%objectname, 1);
	
	if(%hold==0)
		schedule("madhud::PopDown($madhud::CurrentObject);", 4);
	}

//=======================================================================================
function madhud::PopDown(%objectname)
	{
	Control::setVisible(%objectname, 0);
	}


// --------------------------------------------------------------------------------------
//MADHUD - Frag Events
// --------------------------------------------------------------------------------------
//=======================================================================================
function madhud::FragEvents(%killer, %victim, %weapon)
{
		
	%me = getManagerId();
	if (%victim == %me)
		
		{
		$madhud::DeathCount++;
		$madhud::Fragstring = "<f0>Death Added: <f2>" @ client::getname(%killer) @ "\n<f0>Weapon: <f2>" @ %weapon;
		madhud::UpdateHud();

		Control::setValue("madhud_InfoString", $madhud::Fragstring);
		madhud::PopUp(madhud_InfoString, 0);
		return;

		}
	if (%killer == %me)
		{
		$madhud::KillCount++;
		$madhud::Fragstring = "<f0>Kill Added: <f2>" @ client::getname(%victim) @"\n<f0>Weapon: <f2>" @ %weapon;
		madhud::UpdateHud();

		Control::setValue("madhud_InfoString", $madhud::Fragstring);
		madhud::PopUp(madhud_InfoString, 0);
		return;
		}
}
	


// --------------------------------------------------------------------------------------
//MADHUD - FLAGEVENTS 
// --------------------------------------------------------------------------------------
function madhud::flagevents()
{
	
	%teamFlag = Team::GetFlagLocation(Team::Friendly());
	%enemyFlag = Team::GetFlagLocation(Team::Enemy());
	
	echo(%teamflag,%enemyflag);
	if (%teamFlag == $Trak::locationHome)
		{
		$madhud::FlagSafe = 1;
		$madhud::EnemyCarrier = "";
		madhud::PopDown(madhud_FFlagInfoString);
		}
	else if (%teamFlag == $Trak::locationField)
		$madhud::EnemyCarrier = "--DROPPED--";
	else if (%teamFlag == "")
		$madhud::EnemyCarrier = "-UNKNOWN-";
	else
		{
		$madhud::FlagSafe = 0;
		$madhud::EnemyCarrier = %teamflag;
		madhud::PopUp(madhud_FFlagInfoString, 1);
		}

	if (%enemyFlag == $Trak::locationHome)
		{
		$madhud::FriendlyCarrier = "";
		$madhud::EnemyFlagSafe = 1;
		madhud::PopDown(madhud_EFlagInfoString);
		}
	else if (%enemyFlag == $Trak::locationField)
		$madhud::FriendlyCarrier = "--DROPPED--";
	
	else if (%enemyFlag == "")
		$madhud::FriendlyCarrier = "-UNKNOWN-";
	else
	{
		if (%enemyFlag == client::getname(getManagerId()))
			{
			$madhud::FriendlyCarrier = %enemyFlag;
			$madhud::EnemyFlagSafe = 0;
			madhud::PopUp(madhud_EFlagInfoString, 1);
			}
		else
			{
			$madhud::FriendlyCarrier = %enemyFlag;
			$madhud::EnemyFlagSafe = 0;
			madhud::PopUp(madhud_EFlagInfoString, 1);
			}
	}

madhud::UpdateHUD();
}



// --------------------------------------------------------------------------------------
//MADHUD - MAIN LINE 
// --------------------------------------------------------------------------------------
function madhud::UpdateHUD()

{
	if($madhud::EnemyCarrier != "")
		$madhud::EnemyCarrierStatus = "\n<f2>" @ $madhud::EnemyCarrier;

	if($madhud::FriendlyCarrier != "")
		$madhud::FriendlyCarrierStatus = "\n<f2>" @ $madhud::FriendlyCarrier;

	if($madhud::FlagSafe == 1)
		{
		$madhud::FlagStatus = "<Bflag_atbase.bmp>";
		}
	else
		{
		$madhud::FlagStatus = "<Bflag_notatbase.bmp>";
		$madhud::FFlagInfoString = "<f1>Kill:<f2>" @ $madhud::EnemyCarrierStatus;
		}
        
	if($madhud::EnemyFlagSafe == 1)
		{
		$madhud::EnemyFlagStatus = "<Bflag_enemycaptured.bmp>";
		}
	else
		{
		$madhud::EnemyFlagStatus = "<Bflag_notatbase.bmp>";
		$madhud::EFlagInfoString = "<f1>Help:<f2>" @ $madhud::FriendlyCarrierStatus;
		}
		
	if($madhud::KillCount == "")
		$madhud::KillCount = 0;

	if($madhud::DeathCount == "")
		$madhud::DeathCount = 0;

	if($madhud::MineCount == "")
		$madhud::MineCount = 0;

	if($madhud::GrenadeCount == "")
		$madhud::GrenadeCount = 0;

	if($madhud::BeaconCount == "")
		$madhud::BeaconCount = 0;

	if (getItemCount("Repair Kit") > 0)
		$madhud::RepairKitStatus = "Y";
	else $madhud::RepairKitStatus = "N";

	$madhud::FFlagStatusString = $madhud::FlagStatus;
	$madhud::EFlagStatusString = $madhud::EnemyFlagStatus;

	Control::setValue("madhud_FFlagText", $madhud::FFLagStatusString);
	Control::setValue("madhud_EFlagText", $madhud::EFlagStatusString);
	Control::setValue("madhud_FFlagInfoString", $madhud::FFlagInfoString);
	Control::setValue("madhud_EFlagInfoString", $madhud::EFlagInfoString);
	Control::setValue("madhud_LblKills", "<f2>" @ $madhud::KillCount);
	Control::setValue("madhud_LblDeaths", "<f2>" @ $madhud::DeathCount);
	Control::setValue("madhud_LblTimer", "<f2>" @ $madhud::ConnectTime);
	Control::setValue("madhud_LblKpmRatio", "<f2>" @ $madhud::KPMDisplay);
	Control::setValue("madhud_LblMine", "<f2>" @ $madhud::MineCount);
	Control::setValue("madhud_LblGrenade", "<f2>" @ $madhud::GrenadeCount);
	Control::setValue("madhud_LblBeacon", "<f2>" @ $madhud::BeaconCount);
	Control::setValue("madhud_LblRepair", "<f2>" @ $madhud::RepairKitStatus);
	
}

//=======================================================================================
