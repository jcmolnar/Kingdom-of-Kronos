// ---------------------------------------------------------------------------
// needs.cs -- Version 1.1 -- November 4, 1999
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

//include("writer\\version.cs");
//version("writer\\needs.cs", "1.1", "Lorne Laliberte", "- Nov 4, 1999 - show any missing required files on console");

$Enabled["writer\\needs.cs"] = true;

function needs(%needy, %f0, %f1, %f2, %f3, %f4, %f5, %f6, %f7, %f8, %f9, %f10, %f11, %f12, %f13, %f14, %f15, %f16, %f17, %f18, %f19, %f20 )
{
    for(%i = 0; %f[%i] != ""; %i++)
    {
        %file = getword(%f[%i], 0);

        if(%file == -1)
            continue;

        if(File::findFirst(%file) == "")
            echo("ERROR: " @ %needy @ " requires " @ %file @ " but it was not found");

        if(!included(%file))
            echo("ERROR: " @ %needy @ " requires " @ %file @ " but it has not been included");

        if(!$Enabled[%file])
            echo("ERROR: " @ %needy @ " requires " @ %file @ " but it is disabled!");

        %version = getword(%f[%i], 1);

        if(%version == -1)
            continue;

        %vc = version::compare(%file, %version);
        %vf = version(%file);

        if(!String::Compare(%vf, "")) // %vf == ""
            echo("ERROR: " @ %needy @ " requires version " @ %version @ " of " @ %file);

        else if(%vc == -1)
            echo("ERROR: " @ %needy @ " requires version " @ %version @ " of " @ %file @ " but it is only " @ %vf);

        else if(%vc == 1)
            echo("NOTICE: " @ %needy @ " requires version " @ %version @ " of " @ %file @ " but it is " @ %vf);
    }

    return;
}


function wants(%needy, %f0, %f1, %f2, %f3, %f4, %f5, %f6, %f7, %f8, %f9, %f10, %f11, %f12, %f13, %f14, %f15, %f16, %f17, %f18, %f19, %f20 )
{
    for(%i = 0; %f[%i] != ""; %i++)
    {
        %file = getword(%f[%i], 0);

        if(%file == -1)
            continue;

        if(File::findFirst(%file) == "")
            echo("WARNING: " @ %needy @ " recommends " @ %file @ " but it was not found");

        if(!included(%file))
            echo("WARNING: " @ %needy @ " recommends " @ %file @ " but it has not been included");

        if(!$Enabled[%file])
            echo("WARNING: " @ %needy @ " recommends " @ %file @ " but it is disabled");

        %version = getword(%f[%i], 1);

        if(%version == -1)
            continue;

        %vc = version::compare(%file, %version);
        %vf = version(%file);

        if(!String::Compare(%vf, "")) // %vf == ""
            echo("WARNING: " @ %needy @ " recommends version " @ %version @ " of " @ %file);

        else if(%vc == -1)
            echo("WARNING: " @ %needy @ " recommends version " @ %version @ " of " @ %file @ " but it is only " @ %vf);

        else if(%vc == 1)
            echo("NOTICE: " @ %needy @ " recommends version " @ %version @ " of " @ %file @ " but it is " @ %vf);
    }

    return;
}

needs("writer\\needs.cs", "writer\\version.cs 1.7");
