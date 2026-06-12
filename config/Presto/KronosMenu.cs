//==============================================
// KronosMenu.cs - modern ScriptGL TAB menu for Kingdom of Kronos
//==============================================
// ARCHITECTURE
//
// Transport: 100% stock. remoteNewMenu/remoteAddMenuItem build the
// engine ChatMenu "CurServerMenu" (number/letter hotkeys + the
// menuSelect round-trip unchanged), and additionally record the items
// so a modern panel can be drawn via ScriptGL onPostDraw.
//
// Stock visuals: every control in the score-screen gui files
// (base\gui\Score.gui and lr_score.gui) is moved off-screen by a
// one-time binary patch (positions 8000,8000). The score DIALOG still
// opens on TAB - invisible - which keeps the engine cursor on and
// gives us a live control tree for mouse input.
//
// Mouse clicks: the engine deletes the score dialog on every close
// and re-loads it from the .gui FILE on every TAB, so runtime-created
// controls don't survive. Instead, at startup this script has the
// ENGINE rebuild both .gui files from pristine templates
// (KM_lr_base.gui / KM_score_base.gui) with invisible named
// SimGui::ActiveCtrl click zones baked in - menu rows AND player-list
// rows - each carrying a consoleCommand. Every TAB push instantiates
// them automatically. ActiveCtrls load inactive, so while the menu is
// open we re-assert Control::setActive each frame.
//
// Player list: REQUEST-DRIVEN server push. On every NewMenu the
// client sends remoteEval(2048, KMGetPlayers); the server answers
// with KMPlayer rows + KMPlayerCount (KronosHUD_Server.cs). Vanilla
// clients never ask, never get pushed, and keep their stock engine
// scoreboard. Clicking a row sends the SAME message the stock
// scoreboard sent - remoteEval(2048, SelectClient, id) - so all the
// server-side selClient machinery (player-specific menu options,
// KronosMenu_SendPlayerInfo) works unchanged.
//
// Character info: the server feeds the stock bottom info box via
// remoteEval(client, "setInfoLine", n, text). remoteSetInfoLine is a
// base-script function (client.cs), overridden here to capture the
// text for our info panel. Vanilla clients keep the stock InfoCtrlBox.
//
// All panels are TOP-ANCHORED with fixed row slots so zone N is
// always at the same screen position no matter the item count.
//
// KronosMenu::probe() - run from console while TAB is open - reports
// which gui file the engine actually loaded and whether zones are
// alive. KronosMenu::disable() stops the panel (debug only).
//==============================================

if($KM::enabled == "")
	$KM::enabled = true;

$KM::MaxZones = 15;   // menu rows
$KM::MaxPRows = 16;   // player-list rows (matches $KronosMenu::MaxListRows server-side)
$KM::LayoutVer = "v2"; // bump to force a zone-file rebuild after layout changes

// ============================================
// Shared layout - single source of truth.
// Used by BOTH the panel renderer and the zone-file builder, in
// real screen pixels. The builder rescales into the gui file's
// canvas space.
// ============================================

function KronosMenu::computeLayout(%sw, %sh)
{
	$KML::pad    = floor(%sw * 0.012);
	$KML::w      = floor(%sw * 0.38);
	$KML::titleH = floor(%sh * 0.05);
	$KML::rowH   = floor(%sh * 0.034);
	$KML::y      = floor(%sh * 0.16);
	$KML::rowY0  = $KML::y + $KML::titleH + floor($KML::pad / 2);

	// menu panel (left) / player list panel (right)
	$KML::mx     = floor(%sw * 0.08);
	$KML::px     = floor(%sw * 0.54);

	// character info panel (bottom-left, under the menu)
	$KML::iy     = floor(%sh * 0.75);
	$KML::lineH  = floor(%sh * 0.026);
}

// ============================================
// Menu transport overrides (base: scripts.vol menu.cs)
// ============================================

function remoteNewMenu(%server, %title)
{
	if(%server != 2048)
		return;

	if(isObject(CurServerMenu))
		deleteObject(CurServerMenu);

	newObject(CurServerMenu, ChatMenu, %title);
	if(isObject(PlayChatMenu))
		setCMMode(PlayChatMenu, 0);
	setCMMode(CurServerMenu, 1);

	$KM::title = %title;
	$KM::count = 0;
	$KM::active = true;

	// ask the server for the player list (vanilla-safe: only clients
	// running this script ever send the request)
	remoteEval(2048, KMGetPlayers);
}

function remoteAddMenuItem(%server, %title, %code)
{
	if(%server != 2048)
		return;

	addCMCommand(CurServerMenu, %title, clientMenuSelect, %code);

	// First character of the label is the engine hotkey
	%idx = $KM::count;
	$KM::key[%idx] = String::getSubStr(%title, 0, 1);
	$KM::label[%idx] = String::getSubStr(%title, 1, 999);
	$KM::code[%idx] = %code;
	$KM::count++;
}

function remoteCancelMenu(%server)
{
	if(%server != 2048)
		return;

	if(isObject(CurServerMenu))
		deleteObject(CurServerMenu);
	$KM::active = false;
	$KM::selId = "";
	for(%i = 1; %i <= 6; %i++)
		$KM::info[%i] = "";
}

// Called by the engine ChatMenu on hotkey press, and by clickOption
function clientMenuSelect(%code)
{
	if(isObject(CurServerMenu))
		deleteObject(CurServerMenu);
	$KM::active = false;
	remoteEval(2048, menuSelect, %code);
}

// Helper to reset keyboard focus back to the Canvas by deleting and 
// recreating the focused control on the next frame (avoids C++ use-after-free).
function KronosMenu::resetFocus(%name, %parent, %pos, %ext, %cmd)
{
	if(isObject(%name))
	{
		deleteObject(%name);
		%newObj = newObject(%name, "SimGui::ActiveCtrl", getWord(%pos, 0), getWord(%pos, 1), getWord(%ext, 0), getWord(%ext, 1), "", %cmd);
		addToSet(%parent, %newObj);
		Control::setActive(%name, true);
	}
}

// Called by the baked-in ActiveCtrl menu-row zones (consoleCommand)
function KronosMenu::clickOption(%idx)
{
	if(!$KM::active || !$KM::enabled)
		return;
	if(%idx < 0 || %idx >= $KM::count)
		return;
	if($KM::code[%idx] == "")
		return;

	%name = "KMZoneH_" @ %idx;
	if(!isObject(%name))
		%name = "KMZoneL_" @ %idx;

	if(isObject(%name))
	{
		%parent = getGroup(%name);
		%pos = %name.position;
		%ext = %name.extent;
		%cmd = %name.consoleCommand;
		schedule("KronosMenu::resetFocus(\"" @ %name @ "\", " @ %parent @ ", \"" @ %pos @ "\", \"" @ %ext @ "\", \"" @ %cmd @ "\");", 0.05);
	}

	clientMenuSelect($KM::code[%idx]);
}

// Called by the baked-in ActiveCtrl player-row zones (consoleCommand).
// Same message the stock scoreboard click sent (FearGuiScoreList).
function KronosMenu::clickPlayer(%idx)
{
	if(!$KM::active || !$KM::enabled)
		return;
	if(%idx < 0 || %idx >= $KM::plCount)
		return;
	%id = $KM::plId[%idx];
	if(%id == "" || %id == -1)
		return;
	$KM::selId = %id;

	%name = "KMProwH_" @ %idx;
	if(!isObject(%name))
		%name = "KMProwL_" @ %idx;

	if(isObject(%name))
	{
		%parent = getGroup(%name);
		%pos = %name.position;
		%ext = %name.extent;
		%cmd = %name.consoleCommand;
		schedule("KronosMenu::resetFocus(\"" @ %name @ "\", " @ %parent @ ", \"" @ %pos @ "\", \"" @ %ext @ "\", \"" @ %cmd @ "\");", 0.05);
	}

	remoteEval(2048, SelectClient, %id);
}

// ============================================
// Server data handlers
// ============================================

// Player list rows (KronosHUD_Server.cs remoteKMGetPlayers)
function remoteKMPlayer(%server, %idx, %id, %lvl, %remort, %class, %name)
{
	if(%server != 2048)
		return;
	$KM::plId[%idx] = %id;
	$KM::plLvl[%idx] = %lvl;
	$KM::plRL[%idx] = %remort;
	$KM::plClass[%idx] = %class;
	$KM::plName[%idx] = %name;
}

function remoteKMPlayerCount(%server, %sent, %total)
{
	if(%server != 2048)
		return;
	$KM::plCount = %sent;
	$KM::plTotal = %total;
}

// Character info lines. Stock base client.cs writes these into the
// (now hidden) InfoCtrlBox; we capture them for the info panel
// instead. The server sends them for own stats (Game::menuRequest)
// and for a selected player (remoteSelectClient).
function remoteSetInfoLine(%server, %lineNum, %text)
{
	if(%server != 2048)
		return;
	if(%lineNum < 1 || %lineNum > 6)
		return;
	$KM::info[%lineNum] = %text;
}

// ============================================
// Click-zone file builder
// ============================================
// Loads the pristine template, creates named ActiveCtrl zones
// positioned over the panel rows (15 menu + 16 player rows), and
// stores the result back over the live .gui file. The engine writes
// a perfectly-formed file, so no binary surgery is involved. Runs
// once per session (re-runs on resolution change or $KM::LayoutVer
// bump); the rebuilt file takes effect on the next TAB because the
// dialog is re-loaded from disk every time it opens.
//
// %vw/%vh: the canvas size the gui file is authored for. The hi-res
// Score.gui canvas equals the real window resolution; the low-res
// lr_score.gui canvas is the engine's 320x200 low-res surface.

function KronosMenu::makeZone(%name, %cmd, %zx, %zy, %zw, %zh, %sw, %sh, %vw, %vh)
{
	// rescale real-pixel coords into the file's canvas space
	%vx = floor((%zx * %vw) / %sw);
	%vy = floor((%zy * %vh) / %sh);
	%vzw = floor((%zw * %vw) / %sw);
	%vzh = floor((%zh * %vh) / %sh);
	if(%vzw < 1)
		%vzw = 1;
	if(%vzh < 1)
		%vzh = 1;

	%zone = newObject(%name, "SimGui::ActiveCtrl", %vx, %vy, %vzw, %vzh, "", %cmd);
	if(%zone == "0" || %zone == "")
	{
		echo("KronosMenu: FAILED to create ActiveCtrl zone " @ %name);
		return -1;
	}
	return %zone;
}

function KronosMenu::buildOneZoneFile(%src, %dst, %suffix, %sw, %sh, %vw, %vh)
{
	if(isObject(KMEditRoot))
		deleteObject(KMEditRoot);

	loadObject(KMEditRoot, %src);
	if(!isObject(KMEditRoot))
	{
		echo("KronosMenu: FAILED to load template " @ %src);
		return;
	}

	KronosMenu::computeLayout(%sw, %sh);

	// menu option rows (left panel)
	%zx = $KML::mx + $KML::pad;
	%zw = $KML::w - ($KML::pad * 2);
	for(%i = 0; %i < $KM::MaxZones; %i++)
	{
		%zy = $KML::rowY0 + (%i * $KML::rowH);
		%zone = KronosMenu::makeZone("KMZone" @ %suffix @ "_" @ %i, "KronosMenu::clickOption(" @ %i @ ");", %zx, %zy, %zw, $KML::rowH, %sw, %sh, %vw, %vh);
		if(%zone == -1)
		{
			deleteObject(KMEditRoot);
			return;
		}
		addToSet(KMEditRoot, %zone);
	}

	// player list rows (right panel)
	%zx = $KML::px + $KML::pad;
	for(%i = 0; %i < $KM::MaxPRows; %i++)
	{
		%zy = $KML::rowY0 + (%i * $KML::rowH);
		%zone = KronosMenu::makeZone("KMProw" @ %suffix @ "_" @ %i, "KronosMenu::clickPlayer(" @ %i @ ");", %zx, %zy, %zw, $KML::rowH, %sw, %sh, %vw, %vh);
		if(%zone == -1)
		{
			deleteObject(KMEditRoot);
			return;
		}
		addToSet(KMEditRoot, %zone);
	}

	%ok = storeObject(KMEditRoot, %dst);
	deleteObject(KMEditRoot);
	echo("KronosMenu: zone file " @ %dst @ " build: " @ %ok);
}

function KronosMenu::buildZoneFiles(%sw, %sh)
{
	// hi-res score screen: canvas = real window pixels
	KronosMenu::buildOneZoneFile("base\\gui\\KM_score_base.gui", "base\\gui\\Score.gui", "H", %sw, %sh, %sw, %sh);
	// low-res score screen: engine 320x200 surface
	KronosMenu::buildOneZoneFile("base\\gui\\KM_lr_base.gui", "base\\gui\\lr_score.gui", "L", %sw, %sh, 320, 200);
}

// ============================================
// ScriptGL rendering (panels draw UNDER the dialog, but every
// stock dialog control is off-screen, so nothing covers them)
// ============================================

function KronosMenu::render(%dimensions)
{
	%sw = getword(%dimensions, 0);
	%sh = getword(%dimensions, 1);

	// One-time zone-file build (re-runs on resolution change or
	// layout version bump). Skipped while a menu is open so the
	// editing instance's control names can't shadow the live dialog's.
	if($KM::builtFor != %dimensions @ " " @ $KM::LayoutVer && !$KM::active)
	{
		KronosMenu::buildZoneFiles(%sw, %sh);
		$KM::builtFor = %dimensions @ " " @ $KM::LayoutVer;
	}

	if(!$KM::active || !$KM::enabled)
		return;

	// ActiveCtrls load from file inactive - wake the live dialog's
	// zones every frame (cheap, idempotent, survives re-pushes).
	// Only one suffix resolves, depending on which file the engine
	// chose; the other is a no-op.
	for(%i = 0; %i < $KM::count && %i < $KM::MaxZones; %i++)
	{
		Control::setActive("KMZoneH_" @ %i, true);
		Control::setActive("KMZoneL_" @ %i, true);
	}
	for(%i = 0; %i < $KM::plCount && %i < $KM::MaxPRows; %i++)
	{
		Control::setActive("KMProwH_" @ %i, true);
		Control::setActive("KMProwL_" @ %i, true);
	}

	KronosMenu::computeLayout(%sw, %sh);
	%pad = $KML::pad;
	%w = $KML::w;
	%titleH = $KML::titleH;
	%rowH = $KML::rowH;
	%y = $KML::y;

	%chipW = floor(%rowH * 1.1);
	%fontTitle = floor(%titleH * 0.62);
	%fontItem = floor(%rowH * 0.62);

	// extra "+N more" row on the player panel when the list overflows
	%pRows = $KM::plCount;
	%overflow = 0;
	if($KM::plTotal > $KM::plCount)
		%overflow = 1;
	if(%pRows < 1)
		%pRows = 1;

	%mh = %titleH + (%rowH * $KM::count) + %pad;
	%ph = %titleH + (%rowH * (%pRows + %overflow)) + %pad;

	// ---- Pass 1: all rectangles (texture state stays off) ----
	glDisable($GL_TEXTURE_2D);
	glBlendFunc($GL_SRC_ALPHA, $GL_ONE_MINUS_SRC_ALPHA);

	KronosMenu::drawPanelBody($KML::mx, %y, %w, %mh, %pad, %titleH);
	KronosMenu::drawPanelBody($KML::px, %y, %w, %ph, %pad, %titleH);

	// menu row tints + hotkey chips
	%iy = $KML::rowY0;
	for(%i = 0; %i < $KM::count; %i++)
	{
		%half = floor(%i / 2);
		if(%i - (%half * 2) == 1)
		{
			glColor4ub(255, 255, 255, 9);
			glRectangle($KML::mx + 2, %iy, %w - 4, %rowH);
		}
		glColor4ub(70, 115, 180, 150);
		glRectangle($KML::mx + %pad, %iy + 2, %chipW, %rowH - 4);
		%iy += %rowH;
	}

	// player row tints + selection highlight
	%iy = $KML::rowY0;
	for(%i = 0; %i < $KM::plCount; %i++)
	{
		if($KM::selId != "" && $KM::plId[%i] == $KM::selId)
		{
			glColor4ub(85, 140, 210, 70);
			glRectangle($KML::px + 2, %iy, %w - 4, %rowH);
		}
		else
		{
			%half = floor(%i / 2);
			if(%i - (%half * 2) == 1)
			{
				glColor4ub(255, 255, 255, 9);
				glRectangle($KML::px + 2, %iy, %w - 4, %rowH);
			}
		}
		%iy += %rowH;
	}

	// character info panel body
	%hasInfo = false;
	if($KM::info[1] != "")
		%hasInfo = true;
	if(%hasInfo)
	{
		%infoLines = 0;
		for(%i = 1; %i <= 6; %i++)
			if($KM::info[%i] != "")
				%infoLines++;
		%ih = ($KML::lineH * %infoLines) + (%pad * 2);
		KronosMenu::drawPanelBody($KML::mx, $KML::iy, %w, %ih, %pad, 0);
	}

	// ---- Pass 2: all text ----
	// menu title + items
	glColor4ub(235, 240, 255, 245);
	glSetFont("Verdana", %fontTitle, $GLEX_SMOOTH, 4);
	glDrawString($KML::mx + %pad, %y + floor(%titleH * 0.16), $KM::title);

	glSetFont("Verdana", %fontItem, $GLEX_SMOOTH, 0);
	%iy = $KML::rowY0;
	for(%i = 0; %i < $KM::count; %i++)
	{
		%ty = %iy + floor((%rowH - %fontItem) / 2) - 1;
		glColor4ub(255, 255, 255, 235);
		glDrawString($KML::mx + %pad + floor(%chipW * 0.32), %ty, $KM::key[%i]);
		glColor4ub(225, 230, 240, 225);
		glDrawString($KML::mx + %pad + %chipW + floor(%pad * 0.7), %ty, $KM::label[%i]);
		%iy += %rowH;
	}

	// player list title + rows
	glColor4ub(235, 240, 255, 245);
	glSetFont("Verdana", %fontTitle, $GLEX_SMOOTH, 4);
	glDrawString($KML::px + %pad, %y + floor(%titleH * 0.16), "Players (" @ $KM::plTotal @ ")");

	glSetFont("Verdana", %fontItem, $GLEX_SMOOTH, 0);
	%lvX = $KML::px + floor(%w * 0.52);
	%clX = $KML::px + floor(%w * 0.66);
	%iy = $KML::rowY0;
	if($KM::plCount < 1)
	{
		glColor4ub(160, 170, 190, 180);
		glDrawString($KML::px + %pad, %iy + floor((%rowH - %fontItem) / 2) - 1, "(no players)");
	}
	for(%i = 0; %i < $KM::plCount; %i++)
	{
		%ty = %iy + floor((%rowH - %fontItem) / 2) - 1;
		glColor4ub(255, 255, 255, 235);
		glDrawString($KML::px + %pad, %ty, $KM::plName[%i]);

		%lvText = "Lv " @ $KM::plLvl[%i];
		if($KM::plRL[%i] > 0)
			%lvText = %lvText @ " R" @ $KM::plRL[%i];
		glColor4ub(170, 200, 240, 220);
		glDrawString(%lvX, %ty, %lvText);

		glColor4ub(200, 210, 225, 210);
		glDrawString(%clX, %ty, $KM::plClass[%i]);
		%iy += %rowH;
	}
	if(%overflow)
	{
		glColor4ub(160, 170, 190, 180);
		glDrawString($KML::px + %pad, %iy + floor((%rowH - %fontItem) / 2) - 1, "+ " @ ($KM::plTotal - $KM::plCount) @ " more...");
	}

	// character info text
	if(%hasInfo)
	{
		%fontInfo = floor($KML::lineH * 0.78);
		%ty = $KML::iy + %pad;
		for(%i = 1; %i <= 6; %i++)
		{
			if($KM::info[%i] == "")
				continue;
			if(%i == 1)
			{
				glColor4ub(170, 200, 240, 245);
				glSetFont("Verdana", %fontInfo, $GLEX_SMOOTH, 4);
			}
			else
			{
				glColor4ub(225, 230, 240, 225);
				glSetFont("Verdana", %fontInfo, $GLEX_SMOOTH, 0);
			}
			glDrawString($KML::mx + %pad, %ty, $KM::info[%i]);
			%ty += $KML::lineH;
		}
	}
}

// Panel chrome shared by all three panels (rect pass only).
// %titleH = 0 means no title underline.
function KronosMenu::drawPanelBody(%x, %y, %w, %h, %pad, %titleH)
{
	// body
	glColor4ub(12, 14, 22, 238);
	glRectangle(%x, %y, %w, %h);

	// accent border: top bar + thin sides/bottom
	glColor4ub(85, 140, 210, 220);
	glRectangle(%x, %y, %w, 2);
	glColor4ub(85, 140, 210, 90);
	glRectangle(%x, %y + %h - 1, %w, 1);
	glRectangle(%x, %y, 1, %h);
	glRectangle(%x + %w - 1, %y, 1, %h);

	if(%titleH > 0)
	{
		glColor4ub(85, 140, 210, 140);
		glRectangle(%x + %pad, %y + %titleH - 2, %w - (%pad * 2), 1);
	}
}

function ScriptGL::playGui::onPostDraw(%dimensions)
{
	KronosMenu::render(%dimensions);
}

// ============================================
// Console helpers
// ============================================

// Run from console WHILE the TAB menu is open. Empty quotes = control
// not found. Whichever KMZone suffix answers tells us which gui file
// the engine actually loads (H = Score.gui, L = lr_score.gui).
function KronosMenu::probe()
{
	echo("--- KronosMenu::probe ---");
	echo("  $KM::active = " @ $KM::active @ "  count = " @ $KM::count @ "  players = " @ $KM::plCount @ "/" @ $KM::plTotal);
	echo("  KMZoneH_0 visible: '" @ Control::getVisible(KMZoneH_0) @ "'  active: '" @ Control::getActive(KMZoneH_0) @ "'");
	echo("  KMZoneL_0 visible: '" @ Control::getVisible(KMZoneL_0) @ "'  active: '" @ Control::getActive(KMZoneL_0) @ "'");
	echo("  KMProwH_0 visible: '" @ Control::getVisible(KMProwH_0) @ "'  active: '" @ Control::getActive(KMProwH_0) @ "'");
	echo("  LowResServerMenu visible: '" @ Control::getVisible(LowResServerMenu) @ "'");
	echo("  info[1] = " @ $KM::info[1]);
	echo("-------------------------");
}

function KronosMenu::disable()
{
	$KM::enabled = false;
	$KM::active = false;
	echo("KronosMenu: panel disabled. NOTE: the stock menu is moved");
	echo("  off-screen in the score gui files, so no menu will be visible.");
	echo("  Restore the .stockbak files for the stock menu back.");
}

function KronosMenu::enable()
{
	$KM::enabled = true;
	echo("KronosMenu: enabled");
}

// ============================================
// Initialize
// ============================================

$KM::active = false;
$KM::count = 0;
$KM::plCount = 0;
$KM::plTotal = 0;
$KM::selId = "";
$KM::builtFor = "";

echo("KronosMenu: modern TAB menu loaded (clickable + player list)");
