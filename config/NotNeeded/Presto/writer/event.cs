// ---------------------------------------------------------------------------
// event.cs -- Version 2.7 -- February 16, 2000
// by Lorne Laliberte (writer@planetstarsiege.com)
//
// This script is based on the original Event.cs by Steve Eisner, a.k.a. Presto
// The absolutely brilliant idea to add an event-handling system to Tribes was
// his -- I've just polished up the implementation and added a few extras here
// and there. :)
//
// http://www.writerscripts.com
// ---------------------------------------------------------------------------

//include("writer\\version.cs");
//version("writer\\event.cs", "2.7", "Lorne Laliberte", "Feb 16, 2000");

// Attach a function to an event
//
// Returns true if function is added
// Returns false if function was already attached
function Event::Attach(%event, %function)
{
    // We will add this function only if it isn't already attached to this event
    if(!$Event::index[%event, %function])
    {
        // If this is a new event, add it to the master event list
        if(!Event::Exists(%event))
            $Event::list[$Event::totalEvents++] = %event;

        // Increment count of functions attached to this event.
        // Since we'll be adding an element to the end of the $Event::function array, we
        // can use the same value for the index into our $Event::function array.
        // Storing that index in our $Event::index array makes it easier to
        // quickly determine which element to access in the $Event::function array
        // when we only know the event and the function.  (Otherwise we'd have to
        // iterate through the $Event::function array looking for a match.)
        %index = $Event::index[%event, %function] = $Event::count[%event]++;

        // Add this function to the event
        $Event::function[%event, %index] = %function;

        // Set flag if this function is a statement, clear it if it's just a function name
    	$Event::isStatement[%event, %index] = (String::FindSubStr(%function, ";") != -1);

        return true;
    }
    else
    {
        // We may want to test to see if our function was attached...if nothing else
        // this allows a script to know whether it's the first time it tried to attach
        // the function :)
        return false;
    }
}

// Detach a function from an event -- note that the order of the attached functions
// is not preserved
//
// Returns true if function was detached
// Returns false if function wasn't attached to begin with
function Event::Detach(%event, %function)
{
    // Return false if function wasn't attached, might be useful
    if(!$Event::index[%event, %function])
        return false;

    // Get the index of the function to detach
    %index = $Event::index[%event, %function];

    // If the function we're detaching was triggered by the event we're detaching
    // it from, the function we're replacing it with (from the end of the index)
    // wouldn't get triggered until the next time the event is triggered.
    // $Event::FunctionStillAttached holds the index of the function currently
    // being triggered...if it matches the index of the function we're detaching,
    // setting it to 0 will make the trigger routine process the replacement
    // function after we move it into this index.
    if(%index == $Event::FunctionStillAttached)
        $Event::FunctionStillAttached = 0;

    // Move the last function attached to this event into the index
    // of the function we want to detach
    // Note that this does NOT preserve the order of the attached functions!
    $Event::function[%event, %index] = $Event::function[%event, $Event::count[%event]];
    $Event::isStatement[%event, %index] = $Event::isStatement[%event, $Event::count[%event]];

    // Update the index array element for the function we just moved
    $Event::index[%event, $Event::function[%event, %index]] = %index;

    // Clear flag to show the function we detached isn't attached
    $Event::index[%event, %function] = 0;

    // Decrement the count of functions attached to this event
    $Event::count[%event]--;

    // Not really necessary, but just in case something weird happens,
    // don't let the count fall below zero
    if($Event::count[%event] < 0)
        $Event::count[%event] = 0;

    // Return true to say "function was attached, we detached it" :)
    return true;
}


// Call all the functions attached to an event and pass from 0 to 9
// parameters to each function
//
// Note:  Use Event::Trigger(<event name>, <parameters to pass>) wherever
// you want an event that functions can be attached to
//
// Returns the event to make Event::Returned calls Presto pack compatible
function Event::Trigger(%event, %p0, %p1, %p2, %p3, %p4, %p5, %p6, %p7, %p8, %p9)
{
    if(!$Event::count[%event])
    {
        $Event::returnCount[%event] = 0;
        return %event;
    }

    // Call every attached function in turn
    %i = 1;
    while(%i <= $Event::count[%event])
    {
        $Event::FunctionStillAttached = %i;
        %function = $Event::function[%event, %i];

        if($Event::isStatement[%event, %i])
        {
            // This function is a statement so don't pass any parameters
            // Note that we store the return value in two different arrays so we can retrieve it
            // by index or by function
            $Event::return[%event, %i] = $Event::return[%event, %function] = eval(%function);
        }
        else
        {
            // This function is not a statement so pass the parameters to it
            // Note that we store the return value in two different arrays so we can retrieve it
            // by index or by function
            $Event::return[%event, %i] = $Event::return[%event, %function] = eval(%function @ "(%p0,%p1,%p2,%p3,%p4,%p5,%p6,%p7,%p8,%p9);" );
        }
        // Reprocess index if function was detached (and new function replaced it at this index)
        if($Event::FunctionStillAttached)
            %i++;
    }

    // Set number of events that returned...in this case, it's always all of them
    $Event::returnCount[%event] = $Event::count[%event];

    return %event; // makes my Event::Returned() function Presto pack compatible :)
}


// Call all the functions attached to an event, passing from 0 to 9
// parameters to each function, and stopping at the first function that
// returns %value
//
// Returns true if an attached function returned %value, otherwise returns false
function Event::TriggerUntil(%value, %event, %p0, %p1, %p2, %p3, %p4, %p5, %p6, %p7, %p8, %p9)
{
    if(!$Event::count[%event])
    {
        $Event::returnCount[%event] = 0;
        return false;
    }

    // Call every attached function in turn
    %i = 1;  %return = "";
    while(%i <= $Event::count[%event])
    {
        $Event::FunctionStillAttached = %i;

        %function = $Event::function[%event, %i];
        if($Event::isStatement[%event, %i])
        {
            // This function is a statement so don't pass any parameters
            // Note that we store the return value in two different arrays so we can retrieve it
            // by index or by function
            %return = $Event::return[%event, %i] = $Event::return[%event, %function] = eval(%function);
        }
        else
        {
            // This function is not a statement so pass the parameters to it
            // Note that we store the return value in two different arrays so we can retrieve it
            // by index or by function
            %return = $Event::return[%event, %i] = $Event::return[%event, %function] = eval(%function @ "(%p0,%p1,%p2,%p3,%p4,%p5,%p6,%p7,%p8,%p9);" );
        }

        // Stop at first function whose return value matches %value
        if(%return == %value)
        {
            $Event::returnCount[%event] = %i;
            return %i;
        }
        // Reprocess index if function was detached (and new function replaced it at this index)
        if($Event::FunctionStillAttached)
            %i++;
    }
    $Event::returnCount[%event] = %i;
    return false;
}


// Call all the functions attached to an event, passing from 0 to 9
// parameters to each function, and stopping at the first function that
// finds the contents of %value in the return
//
// Returns true if an attached function returned %value, otherwise returns false
function Event::TriggerUntil::SubStr(%value, %event, %p0, %p1, %p2, %p3, %p4, %p5, %p6, %p7, %p8, %p9)
{
    if(!$Event::count[%event])
    {
        $Event::returnCount[%event] = 0;
        return false;
    }

    // Call every attached function in turn
    %i = 1;  %return = "";
    while(%i <= $Event::count[%event])
    {
        $Event::FunctionStillAttached = %i;

        %function = $Event::function[%event, %i];
        if($Event::isStatement[%event, %i])
        {
            // This function is a statement so don't pass any parameters
            // Note that we store the return value in two different arrays so we can retrieve it
            // by index or by function
            %return = $Event::return[%event, %i] = $Event::return[%event, %function] = eval(%function);
        }
        else
        {
            // This function is not a statement so pass the parameters to it
            // Note that we store the return value in two different arrays so we can retrieve it
            // by index or by function
            %return = $Event::return[%event, %i] = $Event::return[%event, %function] = eval(%function @ "(%p0,%p1,%p2,%p3,%p4,%p5,%p6,%p7,%p8,%p9);" );
        }

        // Stop at first function whose return value matches %value
        if(String::findSubStr(%return, %value) != -1)
        {
            $Event::returnCount[%event] = %i;
            return %i;
        }
        // Reprocess index if function was detached (and new function replaced it at this index)
        if($Event::FunctionStillAttached)
            %i++;
    }
    $Event::returnCount[%event] = %i;
    return false;
}


// Check to see if a given event exists (i.e. at least one function has been
// attached to the event)
function Event::Exists(%event)
{
    return $Event::count[%event];
}


// Returns the number of functions attached to an event,
// or the number of events that exist
function Event::GetCount(%event)
{
    if(%event == "")
        return $Event::totalEvents;
    else
    	return $Event::count[%event];
}


// Check to see if a specific return value was among the values returned
// by all the functions attached to an event
function Event::Returned(%event, %target_return_value)
{
    for( %i = 1; %i <= $Event::returnCount[%event]; %i++)
    {
        // Compare the return value with our target
        if($Event::return[%event, %i] == %target_return_value)
            return true; // match found
    }
    return false; // match not found
}


// Check to see if a specific return value (string) was found in one of the values
// returned by all the functions attached to an event
function Event::Returned::SubStr(%event, %target_return_value)
{
    for( %i = 1; %i <= $Event::returnCount[%event]; %i++)
    {
        // Compare the return value with our target
        if(String::findSubStr($Event::return[%event, %i], %target_return_value) != -1)
            return true; // match found
    }
    return false; // match not found
}


// Check how many functions attached to an event returned a specific return value
function Event::countMatchingReturns(%event, %target_return_value)
{
    %found = 0;
    for( %i = 1; %i <= $Event::returnCount[%event]; %i++)
    {
        // Compare the return value with our target
        if($Event::return[%event, %i] == %target_return_value)
            %found++; // another match found
    }
    return %found; // return number of matching return values
}


// Check how many functions attached to an event included a specific string
// in their return value
function Event::countMatchingReturns::SubStr(%event, %target_return_value)
{
    %found = 0;
    for( %i = 1; %i <= $Event::returnCount[%event]; %i++)
    {
        // Compare the return value with our target
        if(String::findSubStr($Event::return[%event, %i], %target_return_value) != -1)
            %found++; // another match found
    }
    return %found; // return number of matching return values
}


// Get the value returned from a specific function attached to an event
//
// Note that this can only check normal and "tagged" return values, and
// is only what was returned the last time this function was executed,
// not necessarily the last time this event was triggered -- be careful.
function Event::getReturn(%event, %function)
{
    return $Event::return[%event, %function];
}


// Include the default events:
include("presto\\events.cs", force);
