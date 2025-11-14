// ---------------------------------------------------------------------------
// whatif.cs -- Version 1.1 -- April 12, 1999
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

//include("writer\\version.cs");
//version("writer\\whatif.cs", "1.1", "Lorne Laliberte", "- April 12, 1999 - pseudo conditional operator");

$Enabled["writer\\whatif.cs"] = true;

// A pseudo ternary conditional operator for Tribes
//
// Use this to use if in an expression
//
function wif(%test, %if_true, %if_false)
{
    if(%test)
        return %if_true;
    else
        return %if_false;
}

