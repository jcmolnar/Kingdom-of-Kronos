function Client::cancelMenu(%clientId)
{
   if(!%clientId.menuLock)
   {
      %clientId.selClient = "";
      %clientId.menuMode = "";
      %clientId.menuLock = "";
      // Removed remoteEval(%clientId, "CancelMenu") - this was causing "CANCEL: Unknown command" errors
      // Menu state is already cleared server-side above, client will handle menu closure automatically
      Client::setMenuScoreVis(%clientId, false);
   }
}

function Client::buildMenu(%clientId, %menuTitle, %menuCode, %cancellable)
{
   if(Client::getName(%clientId) != "")
   {
      Client::setMenuScoreVis(%clientId, true);
      %clientId.menuLock = !%cancellable;
      %clientId.menuMode = %menuCode;
      
      // Reset menu item counter for this client when building a new menu
      $ClientMenuCount[%clientId] = 0;
      
      remoteEval(%clientId, "NewMenu", %menuTitle);
   }
}

function Client::addMenuItem(%clientId, %option, %code)
{
   if(Client::getName(%clientId) != "")
   {
      // Track menu item count per client on server side to prevent sending too many items
      // This prevents the client from receiving more items than the limit
      if($ClientMenuCount[%clientId] == "")
         $ClientMenuCount[%clientId] = 0;
      
      // Limit to 24 items per menu to prevent "Error adding menu to CurServerMenu"
      if($ClientMenuCount[%clientId] >= 24)
         return;  // Silently skip - client-side limit will also catch this
      
      remoteEval(%clientId, "AddMenuItem", %option, %code);
      $ClientMenuCount[%clientId]++;
   }
}

function remoteCancelMenu(%server)
{
   if(%server != 2048)
      return;
   if(isObject(CurServerMenu))
      deleteObject(CurServerMenu);
   
   // Reset menu item counter when menu is cancelled
   $CurServerMenuCount = 0;
   $CurServerMenuLimitWarning = "";
}

// STUB: Silently catch stray "CANCEL" commands from clients
// Some client-side scripts send "CANCEL" instead of "CancelMenu"
// This prevents "CANCEL: Unknown command" console spam
function remoteCANCEL(%clientId)
{
   // Silently handle - just cancel any active menu for this client
   if(%clientId != "" && %clientId != -1)
      Client::cancelMenu(%clientId);
}

function remoteNewMenu(%server, %title)
{
   if(%server != 2048)
      return;

   if(isObject(CurServerMenu))
      deleteObject(CurServerMenu);

   newObject(CurServerMenu, ChatMenu, %title);
   setCMMode(PlayChatMenu, 0);
   setCMMode(CurServerMenu, 1);
   
   // Reset menu item counter and warning flag for new menu
   $CurServerMenuCount = 0;
   $CurServerMenuLimitWarning = "";
}

function remoteAddMenuItem(%server, %title, %code)
{
   if(%server != 2048)
      return;
   
   // Tribes 1 ChatMenu has a limit - testing shows it's around 24-28 items in practice
   // Prevent adding items beyond a safe limit to avoid "Error adding menu to CurServerMenu"
   $CurServerMenuMaxItems = 24;  // Very conservative limit to prevent errors
   
   if($CurServerMenuCount == "")
      $CurServerMenuCount = 0;
   
   if($CurServerMenuCount >= $CurServerMenuMaxItems)
   {
      // Skip adding menu items beyond the limit to prevent "Error adding menu to CurServerMenu"
      // Log a warning only once per menu to avoid spam
      if($CurServerMenuLimitWarning == "")
      {
         echo("WARNING: Menu item limit reached (" @ $CurServerMenuMaxItems @ " items). Some menu options may not appear.");
         $CurServerMenuLimitWarning = True;
      }
      return;
   }
   
   if(!isObject(CurServerMenu))
   {
      echo("WARNING: remoteAddMenuItem - CurServerMenu does not exist");
      return;
   }
   
   // Validate parameters before adding
   if(%title == "" || %title == -1)
   {
      echo("WARNING: remoteAddMenuItem - Invalid title parameter");
      return;
   }
   
   // Attempt to add the menu item
   // Note: addCMCommand may still fail even within the limit, but we've done our best
   // The Tribes engine will print "Error adding menu to CurServerMenu" if this fails
   // We limit to 24 items to minimize the chance of this error
   addCMCommand(CurServerMenu, %title, clientMenuSelect, %code);
   $CurServerMenuCount++;
}

function clientMenuSelect(%code)
{
   deleteObject(CurServerMenu);
   
   // Reset menu item counter when menu item is selected and menu is deleted
   $CurServerMenuCount = 0;
   $CurServerMenuLimitWarning = "";
   
   remoteEval(2048, menuSelect, %code);
}

function remoteMenuSelect(%clientId, %code)
{
   %mm = %clientId.menuMode;
   if(%mm == "")
      return;
   if(String::findSubStr(%code, "\"") != -1 ||
      String::findSubStr(%code, "\\") != -1)  // no quotes or escapes
      return;

   %evalString = "processMenu" @ %mm @ "(" @ %clientId @ ", \"" @ %code @ "\");";
   %clientId.menuMode = "";
   %clientId.menuLock = "";
   dbecho(2, "MENU: " @ %clientId @ "- " @ %evalString);
   eval(%evalString);
   if(%clientId.menuMode == "")
   {
      Client::setMenuScoreVis(%clientId, false);
      %clientId.selClient = "";
   }
}
