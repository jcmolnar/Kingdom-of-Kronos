// ---------------------------------------------------------------------------
// inventory.cs -- Version 3.2 -- June 9, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

//include("writer\\version.cs");
//version("writer\\inventory.cs", "3.2", "Lorne Laliberte", "- Apr 25, 2000 - dynamic inventory table");

include("presto\\writer\\event.cs");
//include("presto\\writer\\needs.cs");
include("presto\\writer\\sort.cs");

$Enabled["writer\\inventory.cs"] = true;


function Inv::getItemType(%itemname)
{
    if(String::Compare($Inv::Type[%itemname], "")) // !=
        return $Inv::Type[%itemname];
    else
    {
        %type = getItemType(%itemname);
        if(%type > 0)
            return %type;
        else
            return "";
    }
}


// Convert an item name to its equivalent $Inv:: variable name
//
function Inv::makeName(%itemname)
{
    if(%itemname == "")
        return "";

    %i = 0;
    %newstring = "$Inv::";

    while( (%char = String::getSubStr(%itemname, %i, 1)) != "" )
    {
        if(%char == " ")
            %char = "_";
        else if(!String::Compare(%char, "-"))
            %char = "_";
        else if(!String::Compare(%char, "+"))
            %char = "_";
        else if(%char == "=")
            %char = "_";
        else if(%char == "*")
            %char = "_";
        else if(%char == "/")
            %char = "_";
        else if(%char == ",")
            %char = ""; // might have to make this "_" but only Pantheon uses "," so far

        // I'll add more if it becomes necessary :)

        %newstring = %newstring @ %char;
        %i++;
    }

    return %newstring;
}


// Test whether an item exists
//
function Inv::exists(%itemname)
{
    return (getItemType(%itemname) != -1);
}


// Force a specific variable name onto an inventory item
// (and optionally force the itemtype too)
//
function Inv::force(%itemvarname, %itemname, %itemtype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    eval("$Inv::" @ %itemvarname @ "=" @ %itemtype @ ";");
}


function Inv::Init(%itemname, %itemnameWhenFound, %ammoname, %ammonameWhenFound, %itemtype, %ammotype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(%itemtype != -1) // item exists on this server
    {
        %itemvarname = Inv::makename(%itemname);

        if($Inventory::createServerList)
            AddExportText(%itemvarname @ " = " @ %itemtype);

        // Assign the type # for %itemname to $Inv::<%itemvarname>
        eval(%itemvarname @ "=" @ %itemtype @ ";");

        // Assign the item name to our $Inv::Name array
        $Inv::Name[%itemtype] = %itemname;

        // Assign the item type to our $Inv::Name array -- this allows us to
        // use the inventory.cs workarounds for mod authors who do things like
        // assigning two item type IDs to the Thunderbolt and giving different
        // items the same name
        $Inv::Type[%itemname] = %itemtype;

        if(%itemtype != getItemType(%itemname))
            $Inv::Type[getItemType(%itemname)] = %itemtype;

        // Give us a way to recognize this item when it's picked up
        if(%itemnameWhenFound != "")
        {
            $Inv::Name[%itemnameWhenFound] = %itemname;
            $Inv::Type[%itemnameWhenFound] = %itemtype;
        }

        if(%ammoname != "") // %ammoname arg provided
        {
            if(%ammotype == "")
                %ammotype = getItemType(%ammoname);

            if(%ammotype != -1) // item exists on this server
            {
                %ammovarname = Inv::makename(%ammoname);

                if($Inventory::createServerList)
                    AddExportText(%ammovarname @ " = " @ %ammotype);

                // Associate this ammo name with the item (weapon)
                $Inv::Ammo[%itemtype] = %ammoname;
                $Inv::Ammo[%itemname] = %ammoname;

                // Assign the type # for %ammoname to $Inv::<%ammovarname>
                eval(%ammovarname @ "=" @ %ammotype @ ";");

                // Assign the ammo name to our $Inv::Name array
                $Inv::Name[%ammotype] = %ammoname;

                // Assign the ammo type to our $Inv::Type array
                $Inv::Type[%ammoname] = %ammotype;

                // Give us a way to recognize this ammo when it's picked up
                if(%ammonameWhenFound != "")
                {
                    $Inv::Name[%ammonameWhenFound] = %ammoname;
                    $Inv::Type[%ammonameWhenFound] = %ammotype;
                }
            }
        }
        return true;
    }
    else // item has been removed, replaced or renamed on this server
    {
        return false;
    }
}


function Inv::initWeapon(%itemname, %itemnameWhenFound, %ammoname, %ammonameWhenFound, %fireTime, %reloadTime, %minEnergy, %maxEnergy, %spinUp, %spinDown, %itemtype, %ammotype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::weaponName::added[%itemname])
    {
        $Inv::weaponName::list[$Inv::weaponName::count++] = %itemname;
        $Inv_temp::weaponName::added[%itemname]++;
    }

    if(%ammoname != "")
    {
        if(!$Inv_temp::name::added[%ammoname])
        {
            $Inv::name::list[$Inv::name::count++] = %ammoname;
            $Inv_temp::name::added[%ammoname]++;

            if($Inventory::createMasterList)
                AddExportText(Inv::makename(%ammoname));
        }

        if(!$Inv_temp::ammoName::added[%ammoname])
        {
            $Inv::ammoName::list[$Inv::ammoName::count++] = %ammoname;
            $Inv_temp::ammoName::added[%ammoname]++;
        }
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::weapon::added[%itemtype])
        {
            $Inv::WeaponList[$Inv::WeaponCount++] = %itemtype;
            $Inv_temp::weapon::added[%itemtype]++;
        }

        $Inv::isWeapon[%itemtype] =
        $Inv::isWeapon[%itemname] = true;

        $Inv::fireTime[%itemtype] =
        $Inv::fireTime[%itemname] = %fireTime;

        $Inv::reloadTime[%itemtype] =
        $Inv::reloadTime[%itemname] = %reloadTime;

        $Inv::minEnergy[%itemtype] =
        $Inv::minEnergy[%itemname] = %minEnergy;

        $Inv::maxEnergy[%itemtype] =
        $Inv::maxEnergy[%itemname] = %maxEnergy;

        $Inv::spinUp[%itemtype] =
        $Inv::spinUp[%itemname] = %spinUp;

        $Inv::spinDown[%itemtype] =
        $Inv::spinDown[%itemname] = %spinDown;

        if(%ammoname != "")
        {
            if(%ammotype == "")
                %ammotype = getItemType(%ammoname);

            if(%ammotype != -1)
            {
                if(!$Inv_temp::ammo::added[%ammotype])
                {
                    $Inv::AmmoList[$Inv::AmmoCount++] = %ammotype;
                    $Inv_temp::ammo::added[%ammotype]++;
                }

                $Inv::isAmmo[%ammotype] =
                $Inv::isAmmo[%ammoname] = true;
            }
        }
        return Inv::Init(%itemname, %itemnameWhenFound, %ammoname, %ammonameWhenFound, %itemtype, %ammotype);
    }
    return false;
}

function Inv::initTool(%itemname, %itemnameWhenFound, %ammoname, %ammonameWhenFound, %fireTime, %reloadTime, %minEnergy, %maxEnergy, %spinUp, %spinDown, %itemtype, %ammotype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);


    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::equipmentName::added[%itemname])
    {
        $Inv::equipmentName::list[$Inv::equipmentName::count++] = %itemname;
        $Inv_temp::equipmentName::added[%itemname]++;
    }

    if(!$Inv_temp::toolName::added[%itemname])
    {
        $Inv::toolName::list[$Inv::toolName::count++] = %itemname;
        $Inv_temp::toolName::added[%itemname]++;
    }

    if(%ammoname != "")
    {
        if(!$Inv_temp::name::added[%ammoname])
        {
            $Inv::name::list[$Inv::name::count++] = %ammoname;
            $Inv_temp::name::added[%ammoname]++;

            if($Inventory::createMasterList)
                AddExportText(Inv::makename(%ammoname));
        }

        if(!$Inv_temp::ammoName::added[%ammoname])
        {
            $Inv::ammoName::list[$Inv::ammoName::count++] = %ammoname;
            $Inv_temp::ammoName::added[%ammoname]++;
        }
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::equipment::added[%itemtype])
        {
            $Inv::EquipmentList[$Inv::EquipmentCount++] = %itemtype;
            $Inv_temp::equipment::added[%itemtype]++;
        }

        if(!$Inv_temp::tool::added[%itemtype])
        {
            $Inv::ToolList[$Inv::ToolCount++] = %itemtype;
            $Inv_temp::tool::added[%itemtype]++;
        }

        $Inv::isEquipment[%itemtype] = $Inv::isTool[%itemtype] =
        $Inv::isEquipment[%itemname] = $Inv::isTool[%itemname] = true;

        $Inv::fireTime[%itemtype] =
        $Inv::fireTime[%itemname] = %fireTime;

        $Inv::reloadTime[%itemtype] =
        $Inv::reloadTime[%itemname] = %reloadTime;

        $Inv::minEnergy[%itemtype] =
        $Inv::minEnergy[%itemname] = %minEnergy;

        $Inv::maxEnergy[%itemtype] =
        $Inv::maxEnergy[%itemname] = %maxEnergy;

        $Inv::spinUp[%itemtype] =
        $Inv::spinUp[%itemname] = %spinUp;

        $Inv::spinDown[%itemtype] =
        $Inv::spinDown[%itemname] = %spinDown;

        if(%ammoname != "")
        {
            if(%ammotype == "")
                %ammotype = getItemType(%ammoname);

            if(%ammotype != -1)
            {
                if(!$Inv_temp::ammo::added[%ammotype])
                {
                    $Inv::AmmoList[$Inv::AmmoCount++] = %ammotype;
                    $Inv_temp::ammo::added[%ammotype]++;
                }
                $Inv::isAmmo[%ammotype] =
                $Inv::isAmmo[%ammoname] = true;
            }
        }
        return Inv::Init(%itemname, %nameWhenFound, %ammoname, %itemtype, %ammotype);
    }
    return false;
}

function Inv::InitAmmo(%itemname, %itemnameWhenFound, %itemtype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::ammoName::added[%itemname])
    {
        $Inv::ammoName::list[$Inv::ammoName::count++] = %itemname;
        $Inv_temp::ammoName::added[%itemname]++;
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::ammo::added[%itemtype])
        {
            $Inv::AmmoList[$Inv::AmmoCount++] = %itemtype;
            $Inv_temp::ammo::added[%itemtype]++;
        }
        $Inv::isAmmo[%itemtype] =
        $Inv::isAmmo[%itemname] = true;
        return Inv::Init(%itemname, %nameWhenFound, "", "", %itemtype);
    }
    return false;
}

function Inv::InitArmor(%itemname, %maxWeapons, %energyCapacity, %jetRate, %rechargeRate, %itemtype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::weaponName::added[%itemname])
    {
        $Inv::armorName::list[$Inv::armorName::count++] = %itemname;
        $Inv_temp::armorName::added[%itemname]++;
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::armor::added[%itemtype])
        {
            $Inv::ArmorList[$Inv::ArmorCount++] = %itemtype;
            $Inv_temp::armor::added[%itemtype]++;
        }
        $Inv::isArmor[%itemtype] =
        $Inv::isArmor[%itemname] = true;

        $Inv::maxWeapons[%itemtype] =
        $Inv::maxWeapons[%itemname] = %maxWeapons;

        $Inv::energyCapacity[%itemtype] =
        $Inv::energyCapacity[%itemname] = %energyCapacity;

        $Inv::jetRate[%itemtype] =
        $Inv::jetRate[%itemname] = %jetRate;

        $Inv::rechargeRate[%itemtype] =
        $Inv::rechargeRate[%itemname] = %rechargeRate;

        return Inv::Init(%itemname, "", "", "", %itemtype);
    }
    return false;
}


function Inv::initPack(%itemname, %itemnameWhenFound, %isDeployable, %canBeToggled, %minEnergy, %maxEnergy, %defaultsOn, %ammoname, %ammonameWhenFound, %itemtype, %ammotype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::packName::added[%itemname])
    {
        $Inv::packName::list[$Inv::packName::count++] = %itemname;
        $Inv_temp::packName::added[%itemname]++;
    }

    if(%isDeployable)
    {
        if(!$Inv_temp::deployableName::added[%itemname])
        {
            $Inv::deployableName::list[$Inv::deployableName::count++] = %itemname;
            $Inv_temp::deployableName::added[%itemname]++;
        }
    }

    if(%canBeToggled)
    {
        if(!$Inv_temp::toggleableName::added[%itemname])
        {
            $Inv::toggleableName::list[$Inv::toggleableName::count++] = %itemname;
            $Inv_temp::toggleableName::added[%itemname]++;
        }
    }

    if(%ammoname != "")
    {
        if(!$Inv_temp::name::added[%ammoname])
        {
            $Inv::name::list[$Inv::name::count++] = %ammoname;
            $Inv_temp::name::added[%ammoname]++;

            if($Inventory::createMasterList)
                AddExportText(Inv::makename(%ammoname));
        }

        if(!$Inv_temp::ammoName::added[%ammoname])
        {
            $Inv::ammoName::list[$Inv::ammoName::count++] = %ammoname;
            $Inv_temp::ammoName::added[%ammoname]++;
        }
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::pack::added[%itemtype])
        {
            $Inv::PackList[$Inv::PackCount++] = %itemtype;
            $Inv_temp::pack::added[%itemtype]++;
        }

        if(%isDeployable)
        {
            if(!$Inv_temp::deployable::added[%itemtype])
            {
                $Inv::DeployableList[$Inv::DeployableCount++] = %itemtype;
                $Inv_temp::deployable::added[%itemtype]++;
            }
        }

        if(%canBeToggled)
        {
            if(!$Inv_temp::toggleable::added[%itemtype])
            {
                $Inv::ToggleableList[$Inv::ToggleableCount++] = %itemtype;
                $Inv_temp::toggleable::added[%itemtype]++;
            }
        }

        $Inv::isPack[%itemtype] =
        $Inv::isPack[%itemname] = true;

        $Inv::isDeployable[%itemtype] =
        $Inv::isDeployable[%itemname] = %isDeployable;

        $Inv::canBeToggled[%itemtype] =
        $Inv::canBeToggled[%itemname] = %canBeToggled;

        $Inv::minEnergy[%itemtype] =
        $Inv::minEnergy[%itemname] = %minEnergy;

        $Inv::maxEnergy[%itemtype] =
        $Inv::maxEnergy[%itemname] = %maxEnergy;

        $Inv::defaultsOn[%itemtype] =
        $Inv::defaultsOn[%itemname] = %defaultsOn;

        if(%ammoname != "")
        {
            if(%ammotype == "")
                %ammotype = getItemType(%ammoname);

            if(%ammotype != -1)
            {
                if(!$Inv_temp::ammo::added[%ammotype])
                {
                    $Inv::AmmoList[$Inv::AmmoCount++] = %ammotype;
                    $Inv_temp::ammo::added[%ammotype]++;
                }
                $Inv::isAmmo[%ammotype] =
                $Inv::isAmmo[%ammoname] = true;
            }
        }
        return Inv::Init(%itemname, %itemnameWhenFound, %ammoname, %ammonameWhenFound, %itemtype, %ammotype);
    }
    return false;
}

function Inv::InitDeployable(%itemname, %itemnameWhenFound, %minEnergy, %maxEnergy, %itemtype)
{
    Inv::initPack(%itemname, %itemnameWhenFound, true, "", %minEnergy, %maxEnergy, "", "", %itemtype, "", "");
}

function Inv::InitToggleable(%itemname, %itemnameWhenFound, %minEnergy, %maxEnergy, %defaultsOn, %ammoname, %ammonameWhenFound, %itemtype, %ammotype)
{
    Inv::initPack(%itemname, %itemnameWhenFound, false, true, %minEnergy, %maxEnergy, %defaultsOn, %ammoname, %ammonameWhenFound, %itemtype, %ammotype);
}


function Inv::InitMisc(%itemname, %itemnameWhenFound, %itemtype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::equipmentName::added[%itemname])
    {
        $Inv::equipmentName::list[$Inv::equipmentName::count++] = %itemname;
        $Inv_temp::equipmentName::added[%itemname]++;
    }

    if(!$Inv_temp::miscName::added[%itemname])
    {
        $Inv::miscName::list[$Inv::miscName::count++] = %itemname;
        $Inv_temp::miscName::added[%itemname]++;
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::equipment::added[%itemtype])
        {
            $Inv::EquipmentList[$Inv::EquipmentCount++] = %itemtype;
            $Inv_temp::equipment::added[%itemtype]++;
        }

        if(!$Inv_temp::misc::added[%itemtype])
        {
            $Inv::MiscList[$Inv::MiscCount++] = %itemtype;
            $Inv_temp::misc::added[%itemtype]++;
        }

        $Inv::isEquipment[%itemtype] = $Inv::isMisc[%itemtype] =
        $Inv::isEquipment[%itemname] = $Inv::isMisc[%itemname] = true;
        return Inv::Init(%itemname, %itemnameWhenFound, "", %itemtype);
    }
    return false;
}


function Inv::InitThrowable(%itemname, %itemnameWhenFound, %itemtype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);


    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::equipmentName::added[%itemname])
    {
        $Inv::equipmentName::list[$Inv::equipmentName::count++] = %itemname;
        $Inv_temp::equipmentName::added[%itemname]++;
    }

    if(!$Inv_temp::throwableName::added[%itemname])
    {
        $Inv::throwableName::list[$Inv::throwableName::count++] = %itemname;
        $Inv_temp::throwableName::added[%itemname]++;
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::equipment::added[%itemtype])
        {
            $Inv::EquipmentList[$Inv::EquipmentCount++] = %itemtype;
            $Inv_temp::equipment::added[%itemtype]++;
        }

        if(!$Inv_temp::throwable::added[%itemtype])
        {
            $Inv::ThrowableList[$Inv::ThrowableCount++] = %itemtype;
            $Inv_temp::throwable::added[%itemtype]++;
        }

        $Inv::isEquipment[%itemtype] = $Inv::isThrowable[%itemtype] =
        $Inv::isEquipment[%itemname] = $Inv::isThrowable[%itemname] = true;
        return Inv::Init(%itemname, %itemnameWhenFound, "", %itemtype);
    }
    return false;
}


function Inv::InitVehicle(%itemname, %passengers, %itemtype)
{
    if(%itemtype == "")
        %itemtype = getItemType(%itemname);

    if(!$Inv_temp::name::added[%itemname])
    {
        $Inv::name::list[$Inv::name::count++] = %itemname;
        $Inv_temp::name::added[%itemname]++;

        if($Inventory::createMasterList)
            AddExportText(Inv::makename(%itemname));
    }

    if(!$Inv_temp::vehicleName::added[%itemname])
    {
        $Inv::vehicleName::list[$Inv::vehicleName::count++] = %itemname;
        $Inv_temp::vehicleName::added[%itemname]++;
    }

    if(%itemtype != -1)
    {
        if(!$Inv_temp::vehicle::added[%itemtype])
        {
            $Inv::VehicleList[$Inv::VehicleCount++] = %itemtype;
            $Inv_temp::vehicle::added[%itemtype]++;
        }
        $Inv::isVehicle[%itemtype] =
        $Inv::isVehicle[%itemname] = true;

        $Inv::passengers[%itemtype] =
        $Inv::passengers[%itemname] = %passengers;

        return Inv::Init(%itemname, "", "", %itemtype);
    }
    return false;
}


function Inv::compare(%a, %b)
{
    %r = String::ICompare($Inv::Name[%a], $Inv::Name[%b]);

    if(%r > 0)
        return 1;
    else if(%r < 0)
        return -1;
    else
        return 0;
}


// Set up the inventory table variables
//
function Inv::Initialize()
{
    %filename = "";
    if($Inventory::createMasterList)
    {
        flushExportText();
        newobject(inventorySave,fearguiformattedtext,0,0,0,0);
        %filename = "temp\\inv_master.txt";
    }
    else if($Inventory::createServerList)
    {
        flushExportText();
        newobject(inventorySave,fearguiformattedtext,0,0,0,0);
        %filename = "temp\\inv_server.txt";
    }

    echo("Clearing Inventory Table");

    deleteVariables("$Inv::*");

    echo("Initializing Inventory Table");

    // Special:

    Inv::InitWeapon("Weapon");
    Inv::InitAmmo("Ammo");
    Inv::InitPack("Backpack"); // this one could be a bit squirrely

    // Call external files with mod-specific inventory table data

    %i = 0;
    while((%modname = getWord($modList, %i)) != -1)
    {
//        // handle names with spaces like so?
//        if(String::findSubStr($ServerMod, "Renegades 2k") != -1)
//            %modname = "renegades2k";

        if(File::findFirst("presto\\writer\\inventory\\" @ %modname @ ".cs") != "")
            exec("presto\\writer\\inventory\\" @ %modname @ ".cs");

        %i++;
    }


    // For compatibility with previous versions of inventory.cs:
    Event::Trigger(eventAddToInventoryTable);

    // Handling the "Scout" items here to hopefully take care of mod problems
    if(Inv::exists("Scout"))
    {
        if(String::findSubStr($ServerMod, "Renegades 2k") != -1)
        {
            Inv::force("Scout_Armor", "Scout");
            Inv::InitArmor("Scout");
            $Inv::isArmor["Scout"] = true;

            Inv::InitVehicle("Scout_Vehicle", "", 102);
            echo("Assuming Scout vehicle item type number is 102");
        }
        else if(String::findSubStr($ServerMod, "Pantheon") != -1)
        {
            $Inv::isArmor["Scout"] = false;
            Inv::InitVehicle("Scout");
        }
        else if(String::findSubStr($ServerMod, "HaVoC") != -1)
        {
            Inv::force("Scout_Armor", "Scout");
            Inv::InitArmor("Scout");
            $Inv::isArmor["Scout"] = true;

            Inv::InitVehicle("Scout_Vehicle", "", 17);
            echo("Assuming Scout vehicle item type number is 17");
        }
        else if(getItemType("Scout") == 2)
        {
            Inv::force("Scout_Armor", "Scout");
            Inv::InitArmor("Scout");
            $Inv::isArmor["Scout"] = true;

            if(Inv::exists("Scout Vehicle"))
            {
                Inv::InitVehicle("Scout Vehicle");
            }
            else
            {
                %lastarmor = 0;
                for(%i = 1; %i <= $Inv::ArmorCount; %i++)
                {
                    if($Inv::ArmorList[%i] > %lastarmor)
                        %lastarmor = $Inv::ArmorList[%i];
                }
                Inv::InitVehicle("Scout_Vehicle", "", %lastarmor + 1);
                echo("Assuming Scout vehicle item type number is " @ %lastarmor + 1);
            }
        }
        else
        {
            // Base, or relatively well-designed mod
            $Inv::isArmor["Scout"] = false;
            Inv::InitVehicle("Scout");
            Inv::force("Scout_Vehicle", "Scout");
            echo("Assuming Scout vehicle item type number is " @ getItemType("Scout"));
        }
    }

    if($Inventory::createMasterList || $Inventory::createServerList)
    {
        exportObjectToScript(inventorySave, %filename, true);
        deleteobject(inventorySave);
        flushExportText();
    }

    echo();
    echo("Sorting Inventory Tables:");

    echo("...master name list");
    sortArray("$Inv::name::list",           1, $Inv::name::count);
    echo("...weapon name list");
    sortArray("$Inv::weaponName::List",     1, $Inv::weaponName::count);
    echo("...ammo name list");
    sortArray("$Inv::ammoName::List",       1, $Inv::ammoName::count);
    echo("...tool name list");
    sortArray("$Inv::toolName::List",       1, $Inv::toolName::count);
    echo("...armor name list");
    sortArray("$Inv::armorName::List",      1, $Inv::armorName::count);
    echo("...pack name list");
    sortArray("$Inv::packName::List",       1, $Inv::packName::count);
    echo("...deployable name list");
    sortArray("$Inv::deployableName::List", 1, $Inv::deployableName::count);
    echo("...toggleable name list");
    sortArray("$Inv::toggleableName::List", 1, $Inv::toggleableName::count);
    echo("...misc name list");
    sortArray("$Inv::miscName::List",       1, $Inv::miscName::count);
    echo("...throwable name list");
    sortArray("$Inv::throwableName::List",  1, $Inv::throwableName::count);
    echo("...equipment name list");
    sortArray("$Inv::equipmentName::List",  1, $Inv::equipmentName::count);
    echo("...vehicle name list");
    sortArray("$Inv::vehicleName::List",    1, $Inv::vehicleName::count);


    echo("...weapon list");
    sortArray("$Inv::WeaponList",     1, $Inv::Weapon::count, false, "Inv::compare");
    echo("...ammo list");
    sortArray("$Inv::AmmoList",       1, $Inv::Ammo::count, false, "Inv::compare");
    echo("...tool list");
    sortArray("$Inv::ToolList",       1, $Inv::Tool::count, false, "Inv::compare");
    echo("...armor list");
    sortArray("$Inv::ArmorList",      1, $Inv::Armor::count, false, "Inv::compare");
    echo("...pack list");
    sortArray("$Inv::PackList",       1, $Inv::Pack::count, false, "Inv::compare");
    echo("...deployable list");
    sortArray("$Inv::DeployableList", 1, $Inv::Deployable::count, false, "Inv::compare");
    echo("...toggleable list");
    sortArray("$Inv::ToggleableList", 1, $Inv::Toggleable::count, false, "Inv::compare");
    echo("...misc list");
    sortArray("$Inv::MiscList",       1, $Inv::Misc::count, false, "Inv::compare");
    echo("...throwable list");
    sortArray("$Inv::ThrowableList",  1, $Inv::Throwable::count, false, "Inv::compare");
    echo("...equipment list");
    sortArray("$Inv::EquipmentList",  1, $Inv::Equipment::count, false, "Inv::compare");
    echo("...vehicle list");
    sortArray("$Inv::VehicleList",    1, $Inv::Vehicle::count, false, "Inv::compare");

    echo("Inventory Tables Sorted!");
    echo();

    echo("Clearing Temporary Inventory Data");

    deleteVariables("$Inv_temp*");

    Event::Trigger(eventInventoryTableReady);
}


function inventory::init()
{
    if($Enabled["writer\\inventory.cs"])
    {
//        needs("writer\\inventory.cs", "writer\\event.cs 2.7",
//                                      "writer\\sort.cs 1.1");
        Event::Attach(eventConnected, "Inv::Initialize();");
    }
    else
    {
        inventory::exit();
    }
}
Event::Attach(writerInit, "inventory::init();");


function inventory::exit()
{
    Event::Detach(eventConnected, "Inv::Initialize();");
}
Event::Attach(writerExit, "inventory::exit();");


if(!$UsingWriterLoader) inventory::init();
