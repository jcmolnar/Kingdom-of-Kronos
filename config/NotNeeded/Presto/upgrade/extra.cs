// Misc.cs by Mental Trousers and Wizard
//------------=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=------------
// This file holds a couple of functions from Zears moreString.cs. I've had to put these
// here because Zears scripts (eg moreString.cs) require the Presto pack, and this upgrade
// requires moreString.cs. So you can see, theres a problem.
// This is also a tidy way of getting around any upgrade issues with moreString.cs.
//
//------------=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=------------
function String::len(%string)
{
    for(%length=0; String::getSubStr(%string, %length, 1)!=""; %length=%length+10)
    {} // it's all done above!

    for(%length=%length-10; String::getSubStr(%string, %length, 1)!=""; %length++)
    {} // it's all done above!
    return %length;
}

function String::replace(%string, %search, %replace)
{
    if(%search == %replace || String::findSubStr(%replace, %search) != -1) // prevent infinite loops
        return %string;

    while((%idx = String::findSubStr(%string, %search)) != -1)
    {
        %len = String::len(%string);
        %front = String::getSubStr(%string, 0, %idx);
        %idx += String::len(%search);
        %back = String::getSubStr(%string, %idx, %len - %idx);
        %string = %front @ %replace @ %back;
    }
    return %string;
}

function String::getWordCount(%string)
{
    for(%num = 0; getWord(%string, %num) != -1; %num++)
    {} // it's all done above!
    return %num;
}

function string::occurs (%str, %pat)
{
	for (%count=0; (%ind=string::findSubStr(%str, %pat))!=-1; %count++)
		%str=string::getSubStr (%str, %ind+2, 999);
	return %count;
}

function event::returnedAndClear(%event, %target_return_value)
{
	%result=false;
    // Check normal returns:
    for( %i = 1; %i <= $Event::returnCount[%event]; %i++)
    {
        // Compare the return value with our target
        if($Event::return[%event, %i] == %target_return_value)
			{
            %result=true; // match found
			break;
			}
    }

    // Check extra returns:
    for( %i = 1; %i <= $Event::extraReturnCount[%event]; %i++)
    {
        // Compare the return value with our target
        if($Event::extraReturn[%event, %i] == %target_return_value)
			{
            %result=true; // match found
			break;
			}
     }

	$Event::returnCount[%event]=$Event::extraReturnCount[%event]=0;
    return %result; // match not found
}

function PPPlus::LoadBanner()
{
	Presto::AddScriptBanner(PrestoPlus, "<B3,4:presto\\upgrade\\PrestoPlusBanner.bmp>");
}

