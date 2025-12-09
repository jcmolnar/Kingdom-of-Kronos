//Deployable building-script pack
ItemImageData DepBasePackImage
{
	shapeFile = "shieldPack";
	mountPoint = 2;
	mountOffset = { 0, -0, 0 };
	mass = 0.0;
	firstPerson = false;
};
ItemData DepBasePack
{
	description = "Deployable Base Pack";
	shapeFile = "shieldPack";
	className = "Belt";
	heading = "dDeployables";
	imageType = DepBasePackImage;
	shadowDetailMask = 4;
	mass = 0.0;
	elasticity = 0.2;
        price = 40000;
	hudIcon = "deployable";
	showWeaponBar = true;
	hiliteOnActive = true;
};
function DepBasePack::onUse(%player,%item)
{
	dbecho($dbechoMode, "DepBasePack::onUse(" @ %player @ ", " @ %item @ ")");

	// For items in standard inventory (legacy), deploy directly
	// Belt items are deployed via Belt::DeployItem() from the menu
	Player::deployItem(%player,%item);
}
function DepBasePack::onDeploy(%player,%item,%pos)
{
	dbecho($dbechoMode, "DepBasePack::onDeploy(" @ %player @ ", " @ %item @ ", " @ %pos @ ")");

	%clientId = Player::getClient(%player);
	
	if (DepBasePack::deployShape(%player,%item))
	{
		if(%clientId.adminLevel < 4)
		{
			// Check if deploying from Belt (new system) or standard inventory (legacy)
			if(%clientId.beltDeploying != "")
			{
				// Remove from Belt storage
				Belt::TakeThisStuff(%clientId, %item, 1);
				%clientId.beltDeploying = "";
			}
			else
			{
				// Legacy: remove from standard inventory
				Player::decItemCount(%player,%item);
			}
		}
		else
		{
			// Admin - don't consume item, just clear flag
			%clientId.beltDeploying = "";
		}
	}
	else
	{
		// Deployment failed - clear flag
		%clientId.beltDeploying = "";
	}
}
function DepBasePack::deployShape(%player,%item)
{
	dbecho($dbechoMode, "DepBasePack::deployShape(" @ %player @ ", " @ %item @ ")");

	%clientId = Player::getClient(%player);
	if (GameBase::getLOSInfo(%player,3))
	{
		if (Vector::dot($los::normal,"0 0 1") > 0.7)
		{
			if(Zone::getType(fetchData(%clientId, "zone")) != "PROTECTED" || %clientId.adminLevel >= 4)
			{
				%rot = GameBase::getRotation(%player);
				DeployBase(%clientId, "base1.cs", $los::position, %rot);
			}
			else 
			{
				Client::sendMessage(%clientId,0,"You are not allowed to deploy bases inside protected territory.");
				return false;
			}
		}
	}
	else 
	{
		Client::sendMessage(%clientId,0,"Deploy position out of range");
		return false;
	}
}