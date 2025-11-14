//----------------------------------------------------------------
//----------------    BWAdmin Presto Support   -------------------
//----------------------------------------------------------------
//
//	bwadminsupport.cs (v0.933)				Wizard_TPG, October 2000
//
//	This script, on connection to a server, checks for compatible
//	bwadmin support.  If it is not present it will enable legacy
//	message parsing support.
//
//	This will only work for mods with bwadmin script support v 5
//	and later.
//
//----------------------------------------------------------------
//
//	Scripters: Using PrestoPlus Bwadmin Support
//
//		Of the 5 types of support included, killinfo and objective
//		info is already used by PrestoPlus.  Should you wish this info
//		simply connect to the following events:
//
//		Event::Trigger(eventKillTrak, $KillTrak::killer, $KillTrak::victim, $KillTrak::weapon);
//		Event::Trigger(eventBwadminSetObjList, %client, %i, %num, %objName, %type, %status);
//
//		The other 3 types of support are NOT used by PrestoPlus by default and
//		are therefor not requested unless a third part script registers to get
//		the support.  To register you use this:
//
//		BWAdminSupport::Register(%type);
//		Where %type is StationInfo, PilotingInfo or WeaponChangeInfo
//			eg. BWAdminSupport::Register(StationInfo);
//
//		It will then request support on connection to a server and you can
//		determine if support is available via these variables:
//
//			$BWAdminSupport::bwadminObjectiveInfo
//			$BWAdminSupport::bwadminKillInfo
//			$BWAdminSupport::bwadminStationInfo
//			$BWAdminSupport::bwadminPilotingInfo
//			$BWAdminSupport::bwadminWeaponChangeInfo
//
//		These variables will have a true value if there is support available.
//
//		If support is available the data will be sent in the following events:
//
//			event::trigger(eventBWAdminPilotingInfo, %active);
//			event::trigger(eventBWAdminWeaponChangeInfo, %weapon);
//			event::trigger(eventBWAdminStationInfo, %type, %noofkits);
//
//
//		This should make it quite simple to include bwadmin support in your scripts :)
//
//
//----------------------------------------------------------------

//	If you have any trouble with kills or death counts being doubled
//	on bwadmin servers try putting a false value here.

$bwadmin::prestoSupport = true;


//================================================================
//----------------------------------------------------------------

Include("presto\\Event.cs");
//Include("presto\\Install.cs");


function BWAdminSupport::Register(%type)
{
	//Types can be StationInfo, PilotingInfo or WeaponChangeInfo
	$BWAdminSupport::Register[%type] = true;
	return;
}

function BWAdminSupport::Reset()
{
	if(!$bwadmin::prestoSupport)
	{
		$BWAdminSupport::bwadminObjectiveInfo = false;
		$BWAdminSupport::bwadminKillInfo = false;
		event::trigger(eventKillModeVersion, false);
		return;
	}
	else
	{
		remoteEval (2048, bwadmin::isCompatible, getManagerId());
		schedule::add("BWAdminSupport::LegacySafe();",15);
		return;
	}
	return;
}

function BWAdminSupport::LegacySafe()
{
	//this function is run if no bwadmin support can be found after x seconds
	if($BWAdminSupport::Version != "")
		return;

	echo("** No Compatible bwadmin support detected, reverting to legacy support");
	$BWAdminSupport::bwadminObjectiveInfo = false;
	$BWAdminSupport::bwadminKillInfo = false;
	$BWAdminSupport::bwadminStationInfo = false;
	$BWAdminSupport::bwadminPilotingInfo = false;
	$BWAdminSupport::bwadminWeaponChangeInfo = false;
	event::trigger(eventBWAdminSupportRegistered, false);
	event::trigger(eventKillModeVersion, false);
	return;
}

function remotebwadmin::Compatible(%client, %version)
{
	schedule::cancel("BWAdminSupport::LegacySafe();");
	event::trigger(eventBWAdminSupportRegistered, %version);
	$BWAdminSupport::Version = %version;
	if(%version >= 5)
	{
		//Check for support
		remoteEval (2048, bwadmin::EnableSupport, getManagerId(),"ObjectiveInfo");
		remoteEval (2048, bwadmin::EnableSupport, getManagerId(),"KillInfo");
		if($BWAdminSupport::Register[StationInfo])
			remoteEval (2048, bwadmin::EnableSupport, getManagerId(),"StationType");
		else
			$BWAdminSupport::bwadminStationInfo = false;
		if($BWAdminSupport::Register[PilotingInfo])
			remoteEval (2048, bwadmin::EnableSupport, getManagerId(),"PilotingInfo");
		else
			$BWAdminSupport::bwadminPilotingInfo = false;
		if($BWAdminSupport::Register[WeaponChangeInfo])
			remoteEval (2048, bwadmin::EnableSupport, getManagerId(),"WeaponChangeInfo");
		else
			$BWAdminSupport::bwadminWeaponChangeInfo = false;
		return;
	}
	else
	{
		$BWAdminSupport::bwadminObjectiveInfo = false;
		$BWAdminSupport::bwadminKillInfo = false;
		$BWAdminSupport::bwadminStationInfo = false;
		$BWAdminSupport::bwadminPilotingInfo = false;
		$BWAdminSupport::bwadminWeaponChangeInfo = false;
		event::trigger(eventKillModeVersion, false);
		return;
	}
}


function remotebwadmin::SupportVerify(%client, %type, %enable)
{
	if(%enable)
	{
		if(%type == "ObjectiveInfo")
		{
			echo("** Compatible bwadmin ",%type," support detected");
			$BWAdminSupport::bwadminObjectiveInfo = true;
		}
		else if(%type == "KillInfo")
		{
			echo("** Compatible bwadmin ",%type," support detected");
			$BWAdminSupport::bwadminKillInfo = true;
			event::trigger(eventKillModeVersion, "true");
		}
		else if(%type == "StationType")
		{
			echo("** Compatible bwadmin ",%type," support detected");
			$BWAdminSupport::bwadminStationInfo = true;
			event::trigger(eventStationInfoVersion, true);
		}
		else if(%type == "PilotingInfo")
		{
			echo("** Compatible bwadmin ",%type," support detected");
			$BWAdminSupport::bwadminPilotingInfo = true;
			event::trigger(eventPilotingInfoVersion, true);
		}
		else if(%type == "WeaponChangeInfo")
		{
			echo("** Compatible bwadmin ",%type," support detected");
			$BWAdminSupport::bwadminWeaponChangeInfo = true;
			event::trigger(eventWeaponChangeInfoVersion, true);
		}
	}
	else
	{
		if(%type == "ObjectiveInfo")
		{
			$BWAdminSupport::bwadminObjectiveInfo = false;
			echo("** No Compatible bwadmin ",%type,", support detected, reverting to legacy support");
		}
		else if(%type == "KillInfo")
		{
			$BWAdminSupport::bwadminKillInfo = false;
			event::trigger(eventKillModeVersion, false);
			echo("** No Compatible bwadmin ",%type,", support detected, reverting to legacy support");
		}
		else if(%type == "StationType")
		{
			$BWAdminSupport::bwadminStationInfo = false;
			event::trigger(eventStationInfoVersion, false);
			echo("** No Compatible bwadmin ",%type," support detected, reverting to legacy support");
		}
		else if(%type == "PilotingInfo")
		{
			$BWAdminSupport::bwadminPilotingInfo = false;
			event::trigger(eventPilotingInfoVersion, false);
			echo("** No Compatible bwadmin ",%type,", support detected, reverting to legacy support");
		}
		else if(%type == "WeaponChangeInfo")
		{
			$BWAdminSupport::bwadminWeaponChangeInfo = false;
			event::trigger(eventWeaponChangeInfoVersion, false);
			echo("** No Compatible bwadmin ",%type,", support detected, reverting to legacy support");
		}
		else
		{
			if(%type != "")
			{
				$BWAdminSupport::bwadminInfo[%type] = false;
				event::trigger(eventInfoVersion,%type, false);
				echo("** No Compatible bwadmin ",%type,", support detected, reverting to legacy support");
			}
		}
	}
	return;
}

function BWAdminSupport::ResetVariables()
{
	deleteVariables("$BWAdminSupport::bwadmin*");
	deleteVariables("$BWAdminSupport::Version");
	return;
}

//Note: remotebwadmin::setObjList can be found in teamtrak
//		remotebwadmin::killinfo can be found in killtrak

function remotebwadmin::PilotingInfo(%client, %active)
{
	event::trigger(eventBWAdminPilotingInfo, %active);
	return;
}

function remotebwadmin::WeaponChangeInfo(%client, %weapon)
{
	event::trigger(eventBWAdminWeaponChangeInfo, %weapon);
	return;
}

function remotebwadmin::StationInfo(%client, %type, %noofkits)
{
	event::trigger(eventBWAdminStationInfo, %type, %noofkits);
	return;
}


Event::Attach(eventConnectionAccepted, BWAdminSupport::Reset);
Event::Attach(eventConnectionRejected, BWAdminSupport::ResetVariables);
Event::Attach(eventConnectionLost, BWAdminSupport::ResetVariables);
Event::Attach(eventConnectionTimeout, BWAdminSupport::ResetVariables);
Event::Attach(eventExit, BWAdminSupport::ResetVariables);

