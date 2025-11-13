//-------------------------------//
// #build from scratch.  18.2.04 //
// Traces of Tridents TvT build  //
// code may be found in code!    //
//-------------------------------//

function Carling::Build(%clientid,%cropped)
{
	Client::SendMessage(%clientid,1,"Sorry... #build disabled till further notice");
	return;
	%item = getword(%cropped,0);
	%check = $building[%item, 0];

	if(%cropped != "list")
	{
		if(%cropped != "" && %cropped != -1) 
		{
			if(%check != "") 
			{
				if($building[%clientid] == "" || $building[%clientid] == -1)
				{
					if(GameBase::getLOSinfo(Client::getOwnedObject(%ClientId), 80))
					{
						%pos = $los::position;
						%target = $los::object;
						%obj = getObjectType(%target);
						%oname = object::getname(%target);

						if(%obj == $building[%item, 1])
						{
							if(HasThisStuff(%clientid,$building[%item, 2]))
							{
								Carling::TempBuild(%clientid,%item,%pos);
							}
							else
								Client::sendmessage(%clientid,1,"You need "@$building[%item, 2]@" to build "@%item@".");
						}
						else
							Client::sendmessage(%clientid,0,%item@" cannot be built here.");
					}
					else
						Client::sendMessage(%clientid,0,"Build: Nothing in range!");
				}
				else
					Client::sendMessage(%clientid,0,"You are currently considering building something.");
			}
			else
				Client::SendMessage(%clientid,0,"Build: Invalid item, try #build list");
		}
		else
			Client::sendmessage(%clientid,0,"Syntax Error: #build tavern");
	}
	else
		Client::sendmessage(%clientid,0,"Comming soon...");
}

function Carling::TempBuild(%clientid,%item,%pos)
{
	%object = newObject("", InteriorShape, %item@".dis");
	addToSet("MissionCleanup", %object);

	%pos = GameBase::setPosition(%object, %pos);
	%rot = GameBase::setRotation(%object, gamebase::getrotation(%clientid));

	$buildinglist[%object, "HP"] = 1;
	$buildinglist[%object, "TEMP"] = True;
	$buildinglist[%object, "OWNER"] = client::getname(%clientid);
	$buildinglist[%object, "TYPE"] = $building[%item, 0];
	$buildinglist[%object, "POS"] = GameBase::getPosition(%object);
	$buildinglist[%object, "ROT"] = GameBase::getRotation(%object);
	$building[%clientid] = %object;

	client::sendmessage(%clientid,0,"Build: Your building is now spawned in temp form.");
	client::sendmessage(%clientid,0,"Build: #addpos [x y z] #addrot [x y z] #finish build");
}

function Carling::FinalBuild(%clientid,%cropped)
{
	if(%cropped == "build")
	{
		if($buildloop[%clientid] == "")
		{
			%object = $building[%clientid];
			%item = $buildinglist[%object, "TYPE"];	
			$buildloop[%clientid] = ($building[%item, 4] / 5);
			$buildPPos[%clientid] = Gamebase::getposition(%clientid);
			Carling::BuildLoop(%clientid);
		}
		else
			Client::sendmessage(%clientid,0,"You are allready building!");
	}
	else
		Client::sendmessage(%clientid,0,"Syntax: #finish build");
}

function Carling::BuildLoop(%clientid)
{
	if($building[%clientid] != "")
	{
		if($buildloop[%clientid] > 0)
		{
			if(Gamebase::getposition(%clientid) == $buildPPos[%clientid])
			{
				CenterPrint(%clientid, "<JC>\n<f2>Constructing\n <f0>Time Left: <F1>"@5*$buildloop[%clientid]@"\n\n",5);
				$buildloop[%clientid]--;
				schedule("Carling::BuildLoop("@ %clientid @");", 5);
			}
			else
			{
				%object = $building[%clientid];
				Carling::BuildKill(%object);
				Client::sendmessage(%clientid,1,"You moved to far from the build site.");
			}
		}
		else
			Carling::CompleteBuild(%clientid);
	}
}

function Carling::CompleteBuild(%clientid)
{
	%object = $building[%clientid];
	%item = $buildinglist[%object, "TYPE"];

	TakeThisStuff(%clientid,$building[%item, 2]);

	$buildinglist[%object, "HP"] = $building[%item, 3];
	$buildinglist[%object, "POS"] = GameBase::getPosition(%object);
	$buildinglist[%object, "ROT"] = GameBase::getRotation(%object);
	$buildinglist[%object, "TEMP"] = "";
	$building[%clientid] = "";
	$buildlist = $buildlist@" "@%object;
	$buildloop[%clientid] = "";
	Carling::BuildSave();
	Client::SendMessage(%clientid,0,"Build: Complete!");
}

function Carling::BuildHit(%clientid,%object,%weapon)
{

	playSound(SoundHitChain, GameBase::getPosition(%clientid));

	%hp = $buildinglist[%object, "HP"];
	%hp--;

	if(%hp <= 0)
	{
		Carling::BuildKill(%object);
		Client::sendmessage(%clientid,1,"You have destoryed a building!");
	}
	else
		$buildinglist[%object, "HP"] = %hp;
}

function Carling::BuildKill(%object)
{
	%owner = $buildinglist[%object, "OWNER"];
	%clientid = NEWgetClientBYName(%owner);

	if(%clientid != -1 && ($buildinglist[%object, "TEMP"] == "True"))
		$building[%clientid] = "";

	$buildinglist[%object, "HP"] = "";
	$buildinglist[%object, "TEMP"] = "";
	$buildinglist[%object, "OWNER"] = "";
	$buildinglist[%object, "TYPE"] = "";
	$buildinglist[%object, "POS"] = "";
	$buildinglist[%object, "ROT"] = "";
	$buildloop[%clientid] = "";

	if(isobject(%object))
		deleteObject(%object);
}

function Carling::BuildAddPos(%clientid,%cropped)
{
	%object = $building[%clientid];
	if(%object != "")
	{
		if(%cropped != "")
		{
			%pos = gamebase::getposition(%object);
			%npos = Vector::add(%pos, %cropped);
			%dist = vector::getDistance(%npos,$buildinglist[%object, "POS"]);

			if(%dist <= 50)
				gamebase::setposition(%object,%npos);
			else
				Client::sendmessage(%clientid,0,"Error: Carn't be moved that far from orginal point");
		}
		else
			client::sendmessage(%clientid,0,"#addpos [X] [Y] [Z]   ex. #addpos 0 2 2");
	}
	else
		client::sendmessage(%clientid,1,"You are not building any thing.");
}

function Carling::BuildAddRot(%clientid,%cropped)
{
	%object = $building[%clientid];
	if(%object != "")
	{
		if(%cropped != "")
		{
			%rot = gamebase::getrotation(%object);
			%nrot = Vector::add(%rot, %cropped);
			gamebase::setrotation(%object,%nrot);
		}
		else
			client::sendmessage(%clientid,0,"#addrot [X] [Y] [Z]   ex. #addrot 0 0 1.57");
	}
	else
		client::sendmessage(%clientid,1,"You are not building any thing.");
}
 
function Carling::TempBuildDist(%clientid,%pos)
{
	%object = $building[%clientid];
	%dist = vector::getDistance(%pos,$buildinglist[%object, "POS"]);

	if(%dist > 150)
	{
		Carling::BuildKill(%object);
		Client::sendmessage(%clientid,1,"You moved to far from the build site.");
	}
}

function Carling::BuildSave()
{
	$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;

	%list = $buildlist;
	%success=0;
	%fail=0;

	for(%i = 0; getWord(%list, %i) != "" && getWord(%list, %i) != -1; %i++)
	{
		%obj = getWord(%list, %i);
		if($buildinglist[%obj,"TYPE"] != "" && $buildinglist[%obj,"TYPE"] != -1)
		{
			$s::save["[" @ %i @ ",0]"] = $buildinglist[%obj, "HP"];
			$s::save["[" @ %i @ ",1]"] = $buildinglist[%obj, "TYPE"];
			$s::save["[" @ %i @ ",2]"] = $buildinglist[%obj, "OWNER"];
			$s::save["[" @ %i @ ",3]"] = GameBase::GetPosition(%obj);
			$s::save["[" @ %i @ ",4]"] = GameBase::GetRotation(%obj);
			$s::save["[" @ %i @ ",5]"] = %obj;
			%success++;
		}
		else
		{
			%fail++;
		}
	}

	$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;
	File::delete("temp\\" @ $missionName @ "_buildingsave_.cs");
	$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;
	export("s::save[*", "temp\\" @ $missionName @ "_buildingsave_.cs", false);
	deletevariables("s::save[*");

	if(%fail<=0)
		return "Build Save: "@%success;
	else 
		return "Build Save: "@%success@" Fail: "@%fail;
}
function BuildLoad()
{
	%filename = $missionName @ "_buildingsave_.cs";

	if(isFile("temp\\" @ %filename))
	{
		$ConsoleWorld::DefaultSearchPath = $ConsoleWorld::DefaultSearchPath;	//thanks Presto
		exec(%filename);
		for(%i = 0; $s::save[%i, 0] != ""; %i++)
		{
			%fname = $building[$s::save[%i,1], 0] @ ".dis";
			%obj = newObject(2048 @ " " @ $s::save[%i,2], InteriorShape, %fname);
			$buildinglist[%obj, "HP"] = $s::save[%i,0]; 
			$buildinglist[%obj, "TYPE"] = $s::save[%i,1];
			$buildinglist[%obj, "OWNER"] = $s::save[%i,2];
			GameBase::SetPosition(%obj,$s::save[%i,3]);
			GameBase::SetRotation(%obj,$s::save[%i,4]);
			$buildlist = $buildlist @" "@ %obj;
		}
		deletevariables("s::save[*");
	}
}



/// umm ya what ppl can build. also unfinished list. timer is all set
/// to weird numberes and hp is all way off and items taken arnt finnished.
$building[lhouse, 0] = "lhouse"; 				//dis name
$building[lhouse, 1] = "SimTerrain";			//build on
$building[lhouse, 2] = "Hatchet 1 PickAxe 1";		//cost to build
$building[lhouse, 3] = 5000;					//hp of build
$building[lhouse, 4] = 250;					//build time

$building[arena1, 0] = "arena1";
$building[arena1, 1] = "SimTerrain";
$building[arena1, 2] = "";
$building[arena1, 3] = 5000;
$building[arena1, 4] = 150;

$building[towerf, 0] = "towerf";
$building[towerf, 1] = "SimTerrain";
$building[towerf, 2] = "";
$building[towerf, 3] = 5000;
$building[towerf, 4] = 300;

$building[tavern, 0] = "tavern";
$building[tavern, 1] = "SimTerrain";
$building[tavern, 2] = "";
$building[tavern, 3] = 5000;
$building[tavern, 4] = 300;

$building[house1, 0] = "house1";
$building[house1, 1] = "SimTerrain";
$building[house1, 2] = "";
$building[house1, 3] = 5000;
$building[house1, 4] = 300;
