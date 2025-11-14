// ---------------------------------------------------------------------------
// include.cs -- Version 1.11 -- October 27, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.writerscripts.com
// ---------------------------------------------------------------------------


// A simple replacement for the exec() function.
//
// Inspired by (and mostly compatible with) the Presto Pack include.cs
//
// Use in your scripts wherever you want to call another script file,
// and the script will only be executed if there hasn't already been
// a script executed with the same name.
//
// Pass 'force' as any parameter to force execution of the script.
//
// Or, pass 'flat' as any parameter to ignore all path information,
// pretending all the script files were executed in one big directory,
// regardless of which directory they are actually in.
//
// (Note that 'flat' and 'force' are mutually exclusive.)
//
// Pass 'novol' as any parameter to only include files found OUTSIDE of
// Tribes volume (.vol) files -- to include "individual" .cs files only.
//
// Pass 'debug' as any parameter, or define the variable $Debug::echo
// as 'true' somewhere in your scripts, to see some debug information
// echoed to the console when the include function is called.
//
//
// Returns:
//
// true      -- if the file was included
// false     -- if the file was already included before
// notfound  -- if the file doesn't exist
// error     -- if exec() reports an error, which it usually only does if
//              the file doesn't exist...unfortunately exec() returns true
//              even if a script breaks on a syntax error!
//
//
// Examples:
//
// include("myscript.cs");
//
// ...will only execute myscript.cs if it hasn't already been executed
//
// include("myscript.cs", force);
//
// ...will execute myscript.cs regardless of what was run before
//
// include("myscript.cs", novol);
//
// ...will execute myscript.cs only if it's found outside a .vol
//
// include("myscript.cs", debug);
//
// ...will print a message to the console to show what happened
//
// include("mydir\\myscript.cs", flat);
//
// ...will only execute "mydir\\myscript.cs" if no other file named
//    "myscript.cs" was executed -- including "anotherdir\\myscript.cs"
//
// include("myscript.cs", debug, force);
// include("myscript.cs", force, debug);
//
// ...either of these will force execution of the script AND print
//    the debug info to the console
//
// include("myscript.cs", debug, flat);
// include("myscript.cs", flat, debug);
//
// ...either of these will print the debug info to the console
//    AND only execute the script if no other file named "myscript.cs"
//    in any directory was executed.


function include(%file, %p1, %p2, %p3)
{
    %foundFile = File::findFirst(%file);
    %foundOutside = isFile("config\\" @ %file);

    %debug = %force = %executed = %flat = %novol = false; // not really necessary but I'm feeling paranoid :)

    %test = %p1 @ %p2 @ %p3;
    
    // Check for debug argument or global $Debug::echo flag
    if( (String::findSubStr(%test, "debug") != -1) || $Debug::echo || $PrestoPref::ScriptNoise )
        %debug = true;

    // Check for flat argument
    if(String::findSubStr(%test, "flat") != -1)
        %flat = true;
    
    // Check for force argument
    if(String::findSubStr(%test, "force") != -1)
    {
        %force = true;
        %flat = false;
    }

    // Check for novol argument
    if(String::findSubStr(%test, "novol") != -1)
        %novol = true;

    if(%novol && !%foundOutside)
    {
        if(%debug)
            echo(%file @ " was not found outside a vol");

        return notfound;
    }
    
    if(%foundFile == "")
    {
        // this event is here for Presto...I don't think it's ever been used, though :) 
        Event::Trigger(eventIncludeFileNotFound, %file);

        if( Event::Returned(eventIncludeFileNotFound, mute) )
            return false;

        if(%debug)
            echo(%file @ " not found");

        return notfound;
    }

    %file = %foundFile;

    // exec() will automatically append the .cs extension
    // if it isn't present -- since we track the included
    // scripts by the name passed in %file, this could result
    // in a script being run twice (checking for "include"
    // won't find a match with "include.cs" and vice versa).

    // Automatically add the .cs extention if it's missing
    if( String::findSubStr(%file, ".cs") == -1)
        %file = %file @ ".cs";

    // Make %flatfile a copy of %file but without any path
    %flatfile = %file;
    while( (%backslashPos = String::findSubStr(%flatfile, "\\")) != -1 )
        %flatfile = String::getSubStr(%flatfile, %backslashPos + 1, 1024);

    if(%debug)
    {
        if(%flat)
            echo("include called with flat mode on -- paths will be ignored in inclusion test");
        else
            echo("include called with flat mode off -- paths will be used in inclusion test");
    }

    if( %force || (!%flat && !$Included::[%file]) || (%flat && !$Included::flat[%flatfile]) )
    {
        // Keep track of how many times file was included
        $Included::[%file]++;
        $Included::flat[%flatfile]++;

        %executed = exec(%file); // exec() returns false if it fails (could just be a typo in file name :)
        if(!%executed)
        {
            // Adjust count to reflect how many times file was included succesfully :)
            if($Included::[%file]-- < 0)
                $Included::[%file] = 0;

            if(!$Included::flat[%flatfile]-- < 0)
                $Included::flat[%flatfile] = 0;

            return error;
        }

        if(%debug)
        {
            if(%flat)
            {
                %includecount = $Included::flat[%flatfile];

                // It bugs me to read "1 times" :)
                %pluralize = "";
                if(%includecount != 1)
                    %pluralize = "s";

                echo("A file named " @ %flatfile @ " has been executed " @ %includecount @ " time" @ %pluralize);
            }
            else
            {
                %includecount = $Included::[%file];

                // It bugs me to read "1 times" :)
                %pluralize = "";
                if(%includecount != 1)
                    %pluralize = "s";

                echo(%file @ " has been executed " @ %includecount @ " time" @ %pluralize);
            }
        }
    }
    else if(%debug)
    {
        if(%flat)
            echo(%flatfile @ " not executed -- a file by that name has been included already!");
        else
            echo(%file @ " not executed -- a file by that name has been included already!");
    }

    // Return true if we included & executed the file, false if not
    return %executed;
}

// For compatibility with Presto's pack:
function ReInclude(%file)
{
    Include(%file, force, debug);
}

// Check how many times a script has been included
//
// Pass 'flat' as the second argument to ignore any path information,
// pretending all the script files were executed in one big directory
// regardless of which directory they are actually in.
//
// Note -- use this if you want to test whether a script was included
//         without actually executing it
function included(%file, %flat)
{
    // Check for flat argument
    if(%flat == "flat")
        %flat = true;

    // Automatically add the .cs extention if it's missing
    if( String::findSubStr(%file, ".cs") == -1)
        %file = %file @ ".cs";

    // Make %flatfile a copy of %file but without any path
    %flatfile = %file;
    while( (%backslashPos = String::findSubStr(%flatfile, "\\")) != -1 )
    {
        %flatfile = String::getSubStr(%flatfile, %backslashPos + 1, 1024);
    }

    if(%flat)
    {
        return $Included::flat[%flatfile] + 0; // + 0 converts "" to 0
    }
    else
    {
        return $Included::[%file] + 0; // + 0 converts "" to 0
    }
}

include("writer\\version.cs");
version("writer\\include.cs", "1.11", "Lorne Laliberte", "Oct 27, 2000");
