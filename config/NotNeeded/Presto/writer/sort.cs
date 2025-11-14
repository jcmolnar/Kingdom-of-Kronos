// ---------------------------------------------------------------------------
// sort.cs -- Version 1.1 -- April 10, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// http://www.planetstarsiege.com/lorne/
// ---------------------------------------------------------------------------

//include("writer\\version.cs");
//version("writer\\sort.cs", "1.1", "Lorne Laliberte", "- Apr 10, 2000 - sorts arrays");

include("presto\\writer\\whatif.cs");

$Enabled["writer\\sort.cs"] = true;


// Default compare function used when no function is passed to sortArray()
//
function sort::compare(%a, %b)
{
    %r = String::ICompare(%a, %b);

    if(%r > 0)
        return 1;
    else if(%r < 0)
        return -1;
    else
        return 0;
}


// sort an array by performing an insertion sort -- reliable, and faster than quicksort
// for the small arrays we'll be sorting
//
function sortArray(%array, %start, %end, %descending, %function)
{
    if(String::compare(%end, "") && %end <= %start) // %end != "" and <= %start
        return;

    if(%array == "")
    {
        echo("No array name string passed!  Aborting sortArray(" @ %array @ wif(String::compare(%start, ""), ", " @ %start, "")
                                                                          @ wif(String::compare(%end, ""), ", " @ %end, "")
                                                                          @ wif(String::compare(%descending, ""), ", " @ %descending, "")
                                                                          @ wif(String::compare(%function, ""), ", " @ %function, "") @ ")");
        return;
    }

    if(%start == "")
        %start = 0;

    if(%function == "")
        %function = Sort::compare;

    if(%descending)
        %order = 1;
    else
        %order = -1;

    if(String::findSubStr(%function, "%1") != -1)
    {
        if(String::findSubStr(%function, ";") == -1)
            %function = %function @ ";";
    }

    if(String::findSubStr(%function, "%1") != -1)
    {
        if(%end != "")
        {
            if(String::findSubStr(%array, "%1") == -1)
            {
                for(%itemsSorted = %start; %itemsSorted <= %end; %itemsSorted++)
                {
                    %temp = escapestring(eval("%a=" @ %array @ "[" @ %itemsSorted @ "];"));
                    %index = %itemsSorted - 1;

                    while((%index >= %start) && (eval(sprintf(%function, "\"" @ %temp @ "\",\"" @ (%itemAtIndex = escapestring(eval("%a=" @ %array @ "[" @ %index @ "];"))) @ "\"")) == %order))
                    {
                        eval(%array @ "[" @ %index + 1 @ "] = \"" @ %itemAtIndex @ "\";");
                        %index--;
                    }
                    eval(%array @ "[" @ %index + 1 @ "] = \"" @ %temp @ "\";");
                }
                return; // sorted!
            }
            else
            {
                for(%itemsSorted = %start; %itemsSorted <= %end; %itemsSorted++)
                {
                    %temp = escapestring(eval("%a=" @ sprintf(%array, %itemsSorted) @ ";"));
                    %index = %itemsSorted - 1;

                    while((%index >= %start) && (eval(sprintf(%function, "\"" @ %temp @ "\",\"" @ (%itemAtIndex = escapestring(eval("%a=" @ sprintf(%array, %index) @ ";"))) @ "\"")) == %order))
                    {
                        eval(sprintf(%array, %index + 1) @ " = \"" @ %itemAtIndex @ "\";");
                        %index--;
                    }
                    eval(sprintf(%array, %index + 1) @ " = \"" @ %temp @ "\";");
                }
                return; // sorted!
            }
        }
        else
        {
            // don't need the end if none of the array elements are empty strings
            if(String::findSubStr(%array, "%1") == -1)
            {
                %itemsSorted = %start - 1;
                while((%temp = escapestring(eval("%a=" @ %array @ "[" @ %itemsSorted++ @ "];"))) != "")
                {
                    %index = %itemsSorted - 1;

                    while((%index >= %start) && (eval(sprintf(%function, "\"" @ %temp @ "\",\"" @  (%itemAtIndex = escapestring(eval("%a=" @ %array @ "[" @ %index @ "];"))) @ "\"")) == %order))
                    {
                        eval(%array @ "[" @ %index + 1 @ "] = \"" @ %itemAtIndex @ "\";");
                        %index--;
                    }
                    eval(%array @ "[" @ %index + 1 @ "] = \"" @ %temp @ "\";");
                }
                return; // sorted!
            }
            else
            {
                %itemsSorted = %start;
                while((%temp = escapestring(eval("%a=" @ sprintf(%array, %itemsSorted++) @ ";"))) != "")
                {
                    %temp = escapestring(eval("%a=" @ sprintf(%array, %itemsSorted) @ ";"));
                    %index = %itemsSorted - 1;

                    while((%index >= %start) && (eval(sprintf(%function, "\"" @ %temp @ "\",\"" @ (%itemAtIndex = escapestring(eval("%a=" @ sprintf(%array, %index) @ ";"))) @ "\"")) == %order))
                    {
                        eval(sprintf(%array, %index + 1) @ " = \"" @ %itemAtIndex @ "\";");
                        %index--;
                    }
                    eval(sprintf(%array, %index + 1) @ " = \"" @ %temp @ "\";");
                }
                return; // sorted!
            }
        }
        return;
    }

    if(%end != "")
    {
        if(String::findSubStr(%array, "%1") == -1)
        {
            for(%itemsSorted = %start; %itemsSorted <= %end; %itemsSorted++)
            {
                %temp = escapestring(eval("%a=" @ %array @ "[" @ %itemsSorted @ "];"));
                %index = %itemsSorted - 1;

                while((%index >= %start) && (%test = eval(%function @ "(\"" @ %temp @ "\",\"" @ (%itemAtIndex = escapestring(eval("%a=" @ %array @ "[" @ %index @ "];"))) @ "\");") == %order))
                {
                    eval(%array @ "[" @ %index + 1 @ "] = \"" @ %itemAtIndex @ "\";");
                    %index--;
                }
                eval(%array @ "[" @ %index + 1 @ "] = \"" @ %temp @ "\";");
            }
            return; // sorted!
        }
        else
        {
            for(%itemsSorted = %start; %itemsSorted <= %end; %itemsSorted++)
            {
                %temp = escapestring(eval("%a=" @ sprintf(%array, %itemsSorted) @ ";"));
                %index = %itemsSorted - 1;

                while((%index >= %start) && (eval(%function @ "(\"" @ %temp @ "\",\"" @ (%itemAtIndex = escapestring(eval("%a=" @ sprintf(%array, %index) @ ";"))) @ "\");") == %order))
                {
                    eval(sprintf(%array, %index + 1) @ " = \"" @ %itemAtIndex @ "\";");
                    %index--;
                }
                eval(sprintf(%array, %index + 1) @ " = \"" @ %temp @ "\";");
            }
            return; // sorted!
        }
    }
    else
    {
        // don't need the length if none of the array elements are empty strings
        if(String::findSubStr(%array, "%1") == -1)
        {
            %itemsSorted = %start - 1;
            while((%temp = escapestring(eval("%a=" @ %array @ "[" @ %itemsSorted++ @ "];"))) != "")
            {
                %index = %itemsSorted - 1;

                while((%index >= %start) && (eval(%function @ "(\"" @ %temp @ "\",\"" @  (%itemAtIndex = escapestring(eval("%a=" @ %array @ "[" @ %index @ "];"))) @ "\");") == %order))
                {
                    eval(%array @ "[" @ %index + 1 @ "] = \"" @ %itemAtIndex @ "\";");
                    %index--;
                }
                eval(%array @ "[" @ %index + 1 @ "] = \"" @ %temp @ "\";");
            }
            return; // sorted!
        }
        else
        {
            %itemsSorted = %start;
            while((%temp = escapestring(eval("%a=" @ sprintf(%array, %itemsSorted++) @ ";"))) != "")
            {
                %temp = escapestring(eval("%a=" @ sprintf(%array, %itemsSorted) @ ";"));
                %index = %itemsSorted - 1;

                while((%index >= %start) && (eval(%function @ "(\"" @ %temp @ "\",\"" @ (%itemAtIndex = escapestring(eval("%a=" @ sprintf(%array, %index) @ ";"))) @ "\");") == %order))
                {
                    eval(sprintf(%array, %index + 1) @ " = \"" @ %itemAtIndex @ "\";");
                    %index--;
                }
                eval(sprintf(%array, %index + 1) @ " = \"" @ %temp @ "\";");
            }
            return; // sorted!
        }
    }
}


function shiftArrayRight(%array, %index)
{
    if(%array == "")
    {
        echo("No array name string passed!  Aborting shiftArrayRight(" @ %array @ ")");
        return;
    }

    if(String::findSubStr(%array, "%1") == -1)
    {
        %current = eval("%a=" @ %array @ "[" @ %index @ "];");

        // skip to the next item
        %index++;
        while(%current != "")
        {
            // get what will become the next item from our current position in array
            %temp = eval("%a=" @ %array @ "[" @ %index @ "];");

            // store the previous item (%current) into the current position
            eval(%array @ "[" @ %index @ "] = \"" @ escapestring(%current) @ "\";");

            // move on to the next item
            %current = %temp;
            %index++;
        }
        return;
    }
    else
    {
        %current = eval("%a=" @ sprintf(%array, %index) @ ";");

        // skip to the next item
        %index++;

        // shift the array right to make room for %item
        while(%current != "")
        {
            // get what will become the next item from our current position in array
            %temp = eval("%a=" @ sprintf(%array, %index) @ ";");

            // store the previous item (%current) into the current position
            eval(sprintf(%array, %index) @ " = \"" @ escapestring(%current) @ "\";");

            // move on to the next item
            %current = %temp;
            %index++;
        }
        return;
    }
}


function addToSorted(%item, %array, %start, %end, %nodupes)
{
    if(%array == "")
    {
        echo("No array name string passed!  Aborting addToSorted(" @ %array @ wif(String::compare(%item, ""), ", \"" @ escapestring(%item) @ "\"", "")
                                                                            @ wif(String::compare(%array, ""), ", " @ %array, "")
                                                                            @ wif(String::compare(%start, ""), ", " @ %start, "")
                                                                            @ wif(String::compare(%end, ""), ", " @ %end, "")
                                                                            @ wif(String::compare(%nodupes, ""), ", " @ %nodupes, "") @ ")");
        return;
    }


    if(%start == "")
        %start = 0;

    if(%end == "")
    {
        // use linear search pattern
        if(String::findSubStr(%array, "%1") == -1)
        {
            %index = %start;
            while((%current = eval("%a=" @ %array @ "[" @ %index @ "];")) != "")
            {
                if(String::ICompare(%current, %item) > 0) // %current > %item
                {
                    // make room in the array for %item
                    shiftArrayRight(%array, %index);

                    // plonk %item into the array
                    eval(%array @ "[" @ %index @ "] = \"" @ escapestring(%item) @ "\";");

                    // ...and stop searching
                    return %index;
                }
                %index++;
            }
            // reached the end, add the item here
            eval(%array @ "[" @ %index @ "] = \"" @ escapestring(%item) @ "\";");
            return %index;
        }
        else
        {
            %index = %start;
            while((%current = eval("%a=" @ sprintf(%array, %index) @ ";")) != "")
            {
                if(String::ICompare(%current, %item) > 0) // %current > %item
                {
                    // make room in the array for %item
                    shiftArrayRight(%array, %index);

                    // plonk %item into the array
                    eval(sprintf(%array, %index) @ " = \"" @ escapestring(%item) @ "\";");

                    // ...and stop searching
                    return %index;
                }
                %index++;
            }
            // reached the end, add the item here
            eval(sprintf(%array, %index) @ " = \"" @ escapestring(%item) @ "\";");
            return %index;
        }
    }
    else
    {
        // use binary search pattern
        if(String::findSubStr(%array, "%1") == -1)
        {
            // check end first, in case we're building from sorted data
            if(String::Icompare(%item, eval("%a=" @ %array @ "[" @ %end @ "];")) > 0)
            {
                // add item to end of array
                eval(%array @ "[" @ %end++ @ "] = \"" @ escapestring(%item) @ "\";");
                return %end;
            }

            while(%end >= %start)
            {
                %index = floor((%start + %end) / 2);

                %test = String::Icompare(%item, eval("%a=" @ %array @ "[" @ %index @ "];"));
                if(%test < 0 ) // %item < [%index]
                {
                    %end = %index - 1;
                }
                else if(%test > 0) // %item > [%index]
                {
                    %start = %index + 1;
                }
                else
                {
                    // matching item found

                    // check whether we want to allow duplicates
                    if(%nodupes)
                        return %index;

                    %index++;

                    // make room for %item at %index
                    shiftArrayRight(%array, %index);

                    // plonk item into array at %index
                    eval(%array @ "[" @ %index @ "] = \"" @ escapestring(%item) @ "\";");

                    return %index;
                }
            }
            // item belongs at %start

            // make room for %item at %start
            shiftArrayRight(%array, %start);

            // plonk item into array at %start
            eval(%array @ "[" @ %start @ "] = \"" @ escapestring(%item) @ "\";");

            return %start;
        }
        else
        {
            // check end first, in case we're building from sorted data
            if(String::Icompare(%item, eval("%a=" @ %array @ "[" @ %end @ "];")) > 0)
            {
                // add item to end of array
                eval(sprintf(%array, %end++) @ " = \"" @ escapestring(%item) @ "\";");
                return %end;
            }

            while(%end >= %start)
            {
                %index = floor((%start + %end) / 2);

                %test = String::Icompare(%item, eval("%a=" @ sprintf(%array, %index) @ ";"));
                if(%test < 0 ) // %item < [%index]
                {
                    %end = %index - 1;
                }
                else if(%test > 0) // %item > [%index]
                {
                    %start = %index + 1;
                }
                else
                {
                    // matching item found

                    // check whether we want to allow duplicates
                    if(%nodupes)
                        return %index;

                    %index++;

                    // make room for %item at %index
                    shiftArrayRight(%array, %index);

                    // plonk item into array at %index
                    eval(sprintf(%array, %index) @ " = \"" @ escapestring(%item) @ "\";");

                    return %index;
                }
            }
            // item belongs at %start

            // make room for %item at %start
            shiftArrayRight(%array, %start);

            // plonk item into array at %start
            eval(sprintf(%array, %start) @ " = \"" @ escapestring(%item) @ "\";");

            return %start;
        }
    }
}


function sort::init()
{
    if($Enabled["writer\\sort.cs"])
    {
//        needs("writer\\sort.cs", "writer\\whatif.cs 1.1");
    }
    else
    {
        sort::exit();
    }
}
Event::Attach(writerInit, "sort::init();");


function sort::exit()
{
}
//Event::Attach(writerExit, "sort::exit();");


if(!$UsingWriterLoader) sort::init();
