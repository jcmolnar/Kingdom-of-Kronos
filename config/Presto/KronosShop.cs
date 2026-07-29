//==============================================
// KronosShop.cs - modern shop / inventory screen for Kingdom of Kronos
//==============================================
// Replaces the stock CmdInventory gui mode ("I" key + merchant shops)
// for HUD clients. The server (KronosHUD_Server.cs) pushes the item
// rows; actions send the SAME remoteEval messages the stock gui
// buttons sent (buyItem/sellItem/useItem/dropItem + index), so all
// server economy logic is unchanged. Vanilla clients never see any of
// this - the server gates on the KHudOn handshake.
//
// Rendering: ScriptGL panels in playGui onPostDraw (the score dialog
// is opened server-side for the mouse cursor; its stock controls are
// off-screen). Mouse input arrives via the Hudbot callbacks owned by
// KronosMenu.cs, which dispatches here while the shop is open.
//
// Layout mirrors the KronosMenu panels: left pane = your items
// (Sell/Use/Drop), right pane = merchant stock (Buy). Long lists scroll
// with a right-side scrollbar (drag the thumb, or click the track),
// wired through KronosMenu's drag system as "sbinv" / "sbst".
//==============================================

$KS::MaxRows = 14;   // max item rows shown before the pane scrolls

// ============================================
// Server pushes
// ============================================

function remoteKShopOpen(%server, %mode, %shopName)
{
	if(%server != 2048)
		return;

	$KS::open = %mode;
	$KS::shopName = %shopName;
	$KS::invN = 0;
	$KS::stN = 0;
	$KS::scroll[inv] = 0;
	$KS::scroll[st] = 0;
	$KS::sel[inv] = -1;
	$KS::sel[st] = -1;
	$KS::selItem[inv] = "";
	$KS::selItem[st] = "";
}

function remoteKShopClose(%server)
{
	if(%server != 2048)
		return;
	$KS::open = "";
}

// Coin balances for bank mode pane headers (carried / banked)
function remoteKBankCoins(%server, %coins, %bank)
{
	if(%server != 2048)
		return;
	$KS::coins = %coins;
	$KS::bank = %bank;
}

// Own inventory row. %kind: "d" = ItemData (%ref = index, stock
// buyItem/sellItem protocol), "b" = belt item (%ref = belt item name,
// KShopBelt* protocol)
function remoteKShopInv(%server, %i, %kind, %ref, %cnt, %heading, %desc)
{
	if(%server != 2048)
		return;
	$KS::invKind[%i] = %kind;
	$KS::invRef[%i] = %ref;
	$KS::invCnt[%i] = %cnt;
	$KS::invHead[%i] = %heading;
	$KS::invName[%i] = %desc;
}

function remoteKShopInvCount(%server, %n)
{
	if(%server != 2048)
		return;
	$KS::invN = %n;
	KronosShop::buildDisp("inv");
}

// Merchant stock row (same kind/ref scheme as inventory rows)
function remoteKShopStock(%server, %i, %kind, %ref, %price, %heading, %desc)
{
	if(%server != 2048)
		return;
	$KS::stKind[%i] = %kind;
	$KS::stRef[%i] = %ref;
	$KS::stPrice[%i] = %price;
	$KS::stHead[%i] = %heading;
	$KS::stName[%i] = %desc;
}

function remoteKShopStockCount(%server, %n)
{
	if(%server != 2048)
		return;
	$KS::stN = %n;
	KronosShop::buildDisp("st");
}

// Chunked tooltip response. The server splits long WhatIs text to stay below
// the remoteEval string limit.
function remoteKShopTipBegin(%server)
{
	if(%server != 2048)
		return;
	$KS::tipBuild = "";
}

function remoteKShopTipPart(%server, %text)
{
	if(%server != 2048)
		return;
	$KS::tipBuild = $KS::tipBuild @ %text;
}

function remoteKShopTipDone(%server)
{
	if(%server != 2048)
		return;
	$KS::tipText = KronosHUD::stripTags($KS::tipBuild);
	$KS::tipFor = $KS::tipRequestKey;
	$KS::tipReady = true;
}

// ============================================
// Display list - sorts rows by heading then name, inserts heading
// rows, restores the selection (by ItemData index) after a re-push
// ============================================

function KronosShop::rowKey(%p, %i)
{
	if(%p == "inv")
		return $KS::invHead[%i] @ "|" @ $KS::invName[%i];
	return $KS::stHead[%i] @ "|" @ $KS::stName[%i];
}

function KronosShop::buildDisp(%p)
{
	if(%p == "inv")
		%n = $KS::invN;
	else
		%n = $KS::stN;

	// insertion sort row indices by heading|name
	for(%i = 0; %i < %n; %i++)
		%ord[%i] = %i;
	for(%i = 1; %i < %n; %i++)
	{
		%v = %ord[%i];
		%vk = KronosShop::rowKey(%p, %v);
		%j = %i - 1;
		%moving = true;
		while(%moving)
		{
			if(%j < 0)
				%moving = false;
			else if(String::ICompare(KronosShop::rowKey(%p, %ord[%j]), %vk) > 0)
			{
				%ord[%j + 1] = %ord[%j];
				%j--;
			}
			else
				%moving = false;
		}
		%ord[%j + 1] = %v;
	}

	// build display rows, inserting a heading row on category change.
	// heading strings are "bWeapons" style - first char is the stock
	// sort key, the rest is the label
	%d = 0;
	%lastHead = "<none>";
	for(%i = 0; %i < %n; %i++)
	{
		%r = %ord[%i];
		if(%p == "inv")
			%head = $KS::invHead[%r];
		else
			%head = $KS::stHead[%r];
		if(%head != %lastHead)
		{
			$KS::dispType[%p, %d] = "head";
			$KS::dispRef[%p, %d] = String::getSubStr(%head, 1, 99);
			%d++;
			%lastHead = %head;
		}
		$KS::dispType[%p, %d] = "item";
		$KS::dispRef[%p, %d] = %r;
		%d++;
	}
	$KS::dispN[%p] = %d;

	// clamp scroll to the new list length (render re-clamps to the visible
	// window too), restore selection by ItemData index
	if($KS::scroll[%p] >= %d)
		$KS::scroll[%p] = %d - 1;
	if($KS::scroll[%p] < 0)
		$KS::scroll[%p] = 0;

	$KS::sel[%p] = -1;
	if($KS::selItem[%p] != "")
	{
		for(%i = 0; %i < %n; %i++)
		{
			if(%p == "inv")
				%id = $KS::invKind[%i] @ "|" @ $KS::invRef[%i];
			else
				%id = $KS::stKind[%i] @ "|" @ $KS::stRef[%i];
			if(%id == $KS::selItem[%p])
				$KS::sel[%p] = %i;
		}
		if($KS::sel[%p] == -1)
			$KS::selItem[%p] = "";
	}
}

// ============================================
// Layout (mirrors the KronosMenu panels)
// ============================================

function KronosShop::computeLayout(%sw, %sh)
{
	// SIZES scale toward the reference (szW/szH); POSITIONS anchor to the
	// real screen - mirrors KronosMenu::computeLayout so the shop matches
	// the TAB menu panels at every resolution / UI scale.
	%k = KronosMenu::uiScale(%sh);
	%szW = %sw * %k;
	%szH = %sh * %k;

	$KSL::pad    = floor(%szW * 0.012);
	$KSL::w      = floor(%szW * 0.38);
	$KSL::titleH = floor(%szH * 0.05);
	$KSL::rowH   = floor(%szH * 0.034);

	// shared movable positions: inv pane follows the menu panel, stock pane
	// follows the players panel (so dragging either screen moves both).
	$KSL::lx     = floor($pref::Kronos::menuX * %sw);
	$KSL::ly     = floor($pref::Kronos::menuY * %sh);
	$KSL::rx     = floor($pref::Kronos::playersX * %sw);
	$KSL::ry     = floor($pref::Kronos::playersY * %sh);
	$KSL::invRowY0 = $KSL::ly + $KSL::titleH + floor($KSL::pad / 2);
	$KSL::stRowY0  = $KSL::ry + $KSL::titleH + floor($KSL::pad / 2);
}

// ============================================
// Rendering
// ============================================

function KronosShop::render(%dimensions)
{
	if($KS::open == "" || $KS::open == false)
	{
		$Panel::sbInvShown = false;
		$Panel::sbStShown = false;
		return;
	}

	%sw = getword(%dimensions, 0);
	%sh = getword(%dimensions, 1);
	KronosShop::computeLayout(%sw, %sh);

	$KS::btnN = 0;

	// reset drag rects (info box is a menu-only thing; stock pane only
	// exists in a merchant shop) so dragHit doesn't match stale panels
	$Panel::infoShown = false;
	$Panel::plW = 0;
	$Panel::sbStShown = false;
	$KS::hoverSeen = false;

	if($KS::open == "bank")
	{
		KronosShop::renderPane("inv", $KSL::lx, $KSL::ly, "Your Items   (Coins: $" @ $KS::coins @ ")");
		KronosShop::renderPane("st", $KSL::rx, $KSL::ry, "Bank Storage   (Bank: $" @ $KS::bank @ ")");
	}
	else
	{
		KronosShop::renderPane("inv", $KSL::lx, $KSL::ly, "Your Items   (Gold: " @ $KH::gold @ ")");
		if($KS::open == "shop")
		{
			%title = $KS::shopName;
			if(%title == "" || %title == -1)
				%title = "Merchant";
			KronosShop::renderPane("st", $KSL::rx, $KSL::ry, %title);
		}
	}

	if(!$KS::hoverSeen)
	{
		$KS::hoverKey = "";
		$KS::tipReady = false;
	}
	KronosShop::renderTip(%sw, %sh);
}

function KronosShop::renderPane(%p, %px, %py, %title)
{
	%pad = $KSL::pad;
	%w = $KSL::w;
	%rowH = $KSL::rowH;
	%titleH = $KSL::titleH;
	%y = %py;
	%rowY0 = %py + %titleH + floor(%pad / 2);
	%fontTitle = floor(%titleH * 0.62);
	%fontItem = floor(%rowH * 0.62);

	// stash this pane's rect for drag hit-testing (title bar = top titleH).
	// inv pane shares the menu panel's stored position, stock pane the
	// players panel's - so KronosMenu::dragHit/dragMove move both screens.
	if(%p == "inv")
	{
		$Panel::menuX = %px;  $Panel::menuY = %py;  $Panel::menuW = %w;  $Panel::menuTH = %titleH;
	}
	else
	{
		$Panel::plX = %px;    $Panel::plY = %py;    $Panel::plW = %w;    $Panel::plTH = %titleH;
	}

	// visible window into the (possibly longer) display list
	%total = $KS::dispN[%p];
	%visible = %total;
	if(%visible > $KS::MaxRows)
		%visible = $KS::MaxRows;
	if(%visible < 1)
		%visible = 1;
	%maxScroll = %total - %visible;
	if(%maxScroll < 0)
		%maxScroll = 0;
	if($KS::scroll[%p] > %maxScroll)
		$KS::scroll[%p] = %maxScroll;
	if($KS::scroll[%p] < 0)
		$KS::scroll[%p] = 0;
	%first = $KS::scroll[%p];
	$KS::visible[%p] = %visible;   // for handleClick

	%scrW = floor(%rowH * 0.45);
	if(%scrW < 6)
		%scrW = 6;
	%hasScroll = (%total > %visible);

	// pane height: title + item rows + one button row + pad
	%ph = %titleH + (%rowH * (%visible + 1)) + %pad;

	// hovered slot (exclude the scrollbar gutter)
	%hovSlot = -1;
	if($KM::mouseOn && $KM::mouseY >= %rowY0 && $KM::mouseY < %rowY0 + (%visible * %rowH)
		&& $KM::mouseX >= %px + %pad && $KM::mouseX < %px + %w - %pad - %scrW)
		%hovSlot = floor(($KM::mouseY - %rowY0) / %rowH);

	if(%hovSlot >= 0)
	{
		%hd = %first + %hovSlot;
		if(%hd < %total && $KS::dispType[%p, %hd] == "item")
		{
			%hr = $KS::dispRef[%p, %hd];
			if(%p == "inv")
				KronosShop::hoverTip($KS::invKind[%hr], $KS::invRef[%hr]);
			else
				KronosShop::hoverTip($KS::stKind[%hr], $KS::stRef[%hr]);
		}
	}

	// ---- rectangles ----
	glDisable($GL_TEXTURE_2D);
	glBlendFunc($GL_SRC_ALPHA, $GL_ONE_MINUS_SRC_ALPHA);

	glColor4ub(12, 14, 22, 238);
	glRectangle(%px, %y, %w, %ph);
	glColor4ub(85, 140, 210, 220);
	glRectangle(%px, %y, %w, 2);
	glColor4ub(85, 140, 210, 90);
	glRectangle(%px, %y + %ph - 1, %w, 1);
	glRectangle(%px, %y, 1, %ph);
	glRectangle(%px + %w - 1, %y, 1, %ph);
	glColor4ub(85, 140, 210, 140);
	glRectangle(%px + %pad, %y + %titleH - 2, %w - (%pad * 2), 1);

	// row tints
	%iy = %rowY0;
	for(%s = 0; %s < %visible; %s++)
	{
		%d = %first + %s;
		if(%d >= %total)
			break;
		if($KS::dispType[%p, %d] == "item")
		{
			%r = $KS::dispRef[%p, %d];
			if(%r == $KS::sel[%p] && $KS::sel[%p] != -1)
			{
				glColor4ub(85, 140, 210, 80);
				glRectangle(%px + 2, %iy, %w - 4, %rowH);
			}
			else if(%s == %hovSlot)
			{
				glColor4ub(120, 170, 235, 45);
				glRectangle(%px + 2, %iy, %w - 4, %rowH);
			}
		}
		else
		{
			glColor4ub(70, 115, 180, 60);
			glRectangle(%px + 2, %iy, %w - 4, %rowH);
		}
		%iy += %rowH;
	}

	// button row (slot %visible)
	%btnY = %rowY0 + (%visible * %rowH);
	if(%p == "inv")
	{
		if($KS::open == "bank")
		{
			KronosShop::button(%px + %pad, %btnY, "Deposit", "deposit", %rowH, %fontItem);
			KronosShop::button(%px + %pad + floor(%w * 0.30), %btnY, "Dep All $", "depcoins", %rowH, %fontItem);
			KronosShop::button(%px + %pad + floor(%w * 0.72), %btnY, "Close", "close", %rowH, %fontItem);
		}
		else
		{
			if($KS::open == "shop")
				KronosShop::button(%px + %pad, %btnY, "Sell", "sell", %rowH, %fontItem);
			KronosShop::button(%px + %pad + floor(%w * 0.24), %btnY, "Use", "use", %rowH, %fontItem);
			KronosShop::button(%px + %pad + floor(%w * 0.48), %btnY, "Drop", "drop", %rowH, %fontItem);
			KronosShop::button(%px + %pad + floor(%w * 0.72), %btnY, "Close", "close", %rowH, %fontItem);
		}
	}
	else
	{
		if($KS::open == "bank")
		{
			KronosShop::button(%px + %pad, %btnY, "Withdraw", "withdraw", %rowH, %fontItem);
			KronosShop::button(%px + %pad + floor(%w * 0.42), %btnY, "W/D All $", "wdcoins", %rowH, %fontItem);
		}
		else
			KronosShop::button(%px + %pad, %btnY, "Buy", "buy", %rowH, %fontItem);
	}

	// scrollbar (right gutter over the row area) - high transparency
	if(%hasScroll)
	{
		%trackX = %px + %w - %scrW - 2;
		%trackY = %rowY0;
		%trackH = %visible * %rowH;

		if(%p == "inv")
		{
			$Panel::sbInvX = %trackX;  $Panel::sbInvY = %trackY;
			$Panel::sbInvW = %scrW;    $Panel::sbInvH = %trackH;
			$Panel::sbInvVis = %visible;  $Panel::sbInvTot = %total;
			$Panel::sbInvShown = true;
			%dragging = ($Drag::active && $Drag::id == "sbinv");
		}
		else
		{
			$Panel::sbStX = %trackX;  $Panel::sbStY = %trackY;
			$Panel::sbStW = %scrW;    $Panel::sbStH = %trackH;
			$Panel::sbStVis = %visible;  $Panel::sbStTot = %total;
			$Panel::sbStShown = true;
			%dragging = ($Drag::active && $Drag::id == "sbst");
		}

		glColor4ub(120, 150, 200, 30);
		glRectangle(%trackX, %trackY, %scrW, %trackH);

		%thumbH = floor(%trackH * %visible / %total);
		if(%thumbH < %rowH)
			%thumbH = %rowH;
		%travel = %trackH - %thumbH;
		if(%travel < 1)
			%travel = 1;
		%frac = 0;
		if(%maxScroll > 0)
			%frac = %first / %maxScroll;     // 0 = top of list, 1 = bottom
		%thumbY = %trackY + floor(%frac * %travel);
		%thumbA = 75;
		if(%dragging)
			%thumbA = 155;
		glColor4ub(150, 185, 235, %thumbA);
		glRectangle(%trackX + 1, %thumbY, %scrW - 2, %thumbH);
	}
	else if(%p == "inv")
		$Panel::sbInvShown = false;
	// (sbStShown was reset in render() before the panes)

	// ---- text ----
	glColor4ub(235, 240, 255, 245);
	glSetFont("Verdana", %fontTitle, $GLEX_SMOOTH, 4);
	glDrawString(%px + %pad, %y + floor(%titleH * 0.16), %title);

	glSetFont("Verdana", %fontItem, $GLEX_SMOOTH, 0);
	%numX = %px + floor(%w * 0.80);
	%iy = %rowY0;
	for(%s = 0; %s < %visible; %s++)
	{
		%d = %first + %s;
		if(%d >= %total)
			break;
		%ty = %iy + floor((%rowH - %fontItem) / 2) - 1;
		if($KS::dispType[%p, %d] == "head")
		{
			glColor4ub(170, 200, 240, 240);
			glDrawString(%px + %pad, %ty, $KS::dispRef[%p, %d]);
		}
		else
		{
			%r = $KS::dispRef[%p, %d];
			glColor4ub(225, 230, 240, 230);
			if(%p == "inv")
			{
				glDrawString(%px + %pad + floor(%pad * 0.8), %ty, $KS::invName[%r]);
				glColor4ub(255, 255, 255, 210);
				glDrawString(%numX, %ty, $KS::invCnt[%r]);
			}
			else
			{
				glDrawString(%px + %pad + floor(%pad * 0.8), %ty, $KS::stName[%r]);
				if($KS::open == "bank")
				{
					// bank mode: stPrice carries the STORED COUNT, not a price
					glColor4ub(255, 255, 255, 210);
					glDrawString(%px + floor(%w * 0.80), %ty, $KS::stPrice[%r]);
				}
				else
				{
					glColor4ub(255, 210, 75, 220);
					glDrawString(%px + floor(%w * 0.70), %ty, "$" @ $KS::stPrice[%r]);
				}
			}
		}
		%iy += %rowH;
	}

	// button labels (rects were drawn by KronosShop::button)
	for(%i = 0; %i < $KS::btnN; %i++)
	{
		if($KS::btnPane[%i] != %p)
			continue;
		glColor4ub(235, 240, 255, 240);
		glDrawString($KS::btnX[%i] + floor(%pad * 0.6),
			$KS::btnY[%i] + floor(($KS::btnH[%i] - %fontItem) / 2) - 1, $KS::btnLabel[%i]);
	}
}

function KronosShop::hoverTip(%kind, %ref)
{
	%key = %kind @ "|" @ %ref;
	$KS::hoverSeen = true;
	if($KS::hoverKey != %key)
	{
		$KS::hoverKey = %key;
		$KS::hoverAt = getSimTime();
		$KS::tipReady = false;
		$KS::tipRequested = false;
	}

	if(!$KS::tipRequested && getSimTime() - $KS::hoverAt >= 0.35)
	{
		$KS::tipRequested = true;
		$KS::tipRequestKey = %key;
		remoteEval(2048, KShopTip, %kind, %ref);
	}
}

function KronosShop::wrapTip(%text, %maxW, %font)
{
	$KST::lineN = 0;
	glSetFont("Verdana", %font, $GLEX_SMOOTH, 0);
	%line = "";
	for(%i = 0; (%word = getWord(%text, %i)) != -1 && $KST::lineN < 12; %i++)
	{
		if(%line == "")
			%try = %word;
		else
			%try = %line @ " " @ %word;
		if(getWord(glGetStringDimensions(%try), 0) > %maxW && %line != "")
		{
			$KST::line[$KST::lineN] = %line;
			$KST::lineN++;
			%line = %word;
		}
		else
			%line = %try;
	}
	if(%line != "" && $KST::lineN < 12)
	{
		$KST::line[$KST::lineN] = %line;
		$KST::lineN++;
	}
}

function KronosShop::renderTip(%sw, %sh)
{
	if(!$KS::hoverSeen || !$KS::tipReady || $KS::tipFor != $KS::hoverKey)
		return;

	%font = floor(%sh * 0.012);
	if(%font < 9) %font = 9;
	%pad = floor(%font * 0.65);
	%w = floor(%sw * 0.30);
	if(%w < 260) %w = 260;
	if(%w > 520) %w = 520;
	KronosShop::wrapTip($KS::tipText, %w - (%pad * 2), %font);
	%lineH = %font + floor(%font * 0.35);
	%h = ($KST::lineN * %lineH) + (%pad * 2);
	%x = $KM::mouseX + 18;
	%y = $KM::mouseY + 18;
	if(%x + %w > %sw) %x = $KM::mouseX - %w - 18;
	if(%y + %h > %sh) %y = %sh - %h - 8;
	if(%x < 8) %x = 8;
	if(%y < 8) %y = 8;

	glColor4ub(8, 11, 18, 244);
	glRectangle(%x, %y, %w, %h);
	glColor4ub(85, 140, 210, 220);
	glRectangle(%x, %y, %w, 2);
	glSetFont("Verdana", %font, $GLEX_SMOOTH, 0);
	glColor4ub(232, 237, 245, 245);
	for(%i = 0; %i < $KST::lineN; %i++)
		glDrawString(%x + %pad, %y + %pad + (%i * %lineH), $KST::line[%i]);
}

// Draws the button chip and records its rect for click hit-testing
function KronosShop::button(%x, %y, %label, %act, %rowH, %font)
{
	glSetFont("Verdana", %font, $GLEX_SMOOTH, 0);
	%tw = getword(glGetStringDimensions(%label), 0);
	%bw = %tw + floor($KSL::pad * 1.2);
	%bh = %rowH - 4;

	glColor4ub(70, 115, 180, 160);
	glRectangle(%x, %y + 2, %bw, %bh);

	%i = $KS::btnN;
	$KS::btnX[%i] = %x;
	$KS::btnY[%i] = %y + 2;
	$KS::btnW[%i] = %bw;
	$KS::btnH[%i] = %bh;
	$KS::btnLabel[%i] = %label;
	$KS::btnAct[%i] = %act;
	// pane ownership: left pane buttons sit left of the right pane
	if(%x < $KSL::rx)
		$KS::btnPane[%i] = "inv";
	else
		$KS::btnPane[%i] = "st";
	$KS::btnN++;
}

// ============================================
// Mouse input (dispatched from KronosMenu.cs onMouseLMB)
// ============================================

function KronosShop::handleClick(%x, %y)
{
	// buttons first (their rects overlap the row grid)
	for(%i = 0; %i < $KS::btnN; %i++)
	{
		if(%x >= $KS::btnX[%i] && %x < $KS::btnX[%i] + $KS::btnW[%i]
			&& %y >= $KS::btnY[%i] && %y < $KS::btnY[%i] + $KS::btnH[%i])
		{
			KronosShop::doAction($KS::btnAct[%i]);
			return;
		}
	}

	// item rows - pick the pane by x, then use that pane's row origin
	if($KSL::rowH < 1)
		return;

	%p = "";
	%rowY0 = 0;
	if(%x >= $KSL::lx + $KSL::pad && %x < $KSL::lx + $KSL::w - $KSL::pad)
	{
		%p = "inv";
		%rowY0 = $KSL::invRowY0;
	}
	else if(($KS::open == "shop" || $KS::open == "bank") && %x >= $KSL::rx + $KSL::pad && %x < $KSL::rx + $KSL::w - $KSL::pad)
	{
		%p = "st";
		%rowY0 = $KSL::stRowY0;
	}
	if(%p == "")
		return;
	if(%y < %rowY0)
		return;
	%slot = floor((%y - %rowY0) / $KSL::rowH);
	if(%slot >= $KS::visible[%p])
		return;

	%d = $KS::scroll[%p] + %slot;
	if(%d >= $KS::dispN[%p])
		return;
	if($KS::dispType[%p, %d] != "item")
		return;

	%r = $KS::dispRef[%p, %d];
	$KS::sel[%p] = %r;
	if(%p == "inv")
		$KS::selItem[%p] = $KS::invKind[%r] @ "|" @ $KS::invRef[%r];
	else
		$KS::selItem[%p] = $KS::stKind[%r] @ "|" @ $KS::stRef[%r];
}

function KronosShop::doAction(%act)
{
	%verb = getword(%act, 0);

	if(%verb == "close")
	{
		remoteEval(2048, KShopClose);
		return;
	}

	// Timegate the item actions (0.3s) - rapid-fire drops spawn
	// interpenetrating thrown items that can clip through the floor
	// and vanish, and the server gates at 0.25s anyway, so pace the
	// clicks instead of wasting them
	%now = GetSimTime();
	if($KS::lastAct != "" && (%now - $KS::lastAct) < 0.3)
		return;
	$KS::lastAct = %now;

	// ---- bank actions (coins all-in/out + item deposit/withdraw) ----
	if(%verb == "depcoins")
	{
		remoteEval(2048, KBankCoinsDeposit);
		schedule("remoteEval(2048, KShopSync);", 0.5);
		return;
	}
	if(%verb == "wdcoins")
	{
		remoteEval(2048, KBankCoinsWithdraw);
		schedule("remoteEval(2048, KShopSync);", 0.5);
		return;
	}
	if(%verb == "withdraw")
	{
		%r = $KS::sel[st];
		if(%r == -1 || %r == "")
			return;
		remoteEval(2048, KBankWithdraw, $KS::stRef[%r]);
		schedule("remoteEval(2048, KShopSync);", 0.5);
		return;
	}
	if(%verb == "deposit")
	{
		%r = $KS::sel[inv];
		if(%r == -1 || %r == "")
			return;
		remoteEval(2048, KBankDeposit, $KS::invRef[%r]);
		schedule("remoteEval(2048, KShopSync);", 0.5);
		return;
	}

	// item actions - ItemData rows send exactly the messages the stock
	// gui buttons sent; belt rows use the KShopBelt* server handlers
	if(%verb == "buy")
	{
		%r = $KS::sel[st];
		if(%r == -1 || %r == "")
			return;
		if($KS::stKind[%r] == "b")
			remoteEval(2048, KShopBeltBuy, $KS::stRef[%r]);
		else
			remoteEval(2048, buyItem, $KS::stRef[%r]);
	}
	else
	{
		%r = $KS::sel[inv];
		if(%r == -1 || %r == "")
			return;
		if($KS::invKind[%r] == "b")
		{
			if(%verb == "sell")
				remoteEval(2048, KShopBeltSell, $KS::invRef[%r]);
			else if(%verb == "use")
				remoteEval(2048, KShopBeltUse, $KS::invRef[%r]);
			else if(%verb == "drop")
				remoteEval(2048, KShopBeltDrop, $KS::invRef[%r]);
		}
		else
		{
			if(%verb == "sell")
				remoteEval(2048, sellItem, $KS::invRef[%r]);
			else if(%verb == "use")
				remoteEval(2048, useItem, $KS::invRef[%r]);
			else if(%verb == "drop")
				remoteEval(2048, dropItem, $KS::invRef[%r]);
		}
	}

	// counts changed - ask the server for fresh rows
	schedule("remoteEval(2048, KShopSync);", 0.5);
}

// ============================================
// Draw hook - replaces KronosMenu.cs's definition (this file loads
// last); calls the menu render, the examine overlay, then the shop
// ============================================

// ============================================
// Initialize
// ============================================

$KS::open = "";
$KS::lastAct = "";
$KS::invN = 0;
$KS::stN = 0;
$KS::btnN = 0;
$KS::dispN[inv] = 0;
$KS::dispN[st] = 0;
$KS::sel[inv] = -1;
$KS::sel[st] = -1;
$KS::scroll[inv] = 0;
$KS::scroll[st] = 0;
$KS::visible[inv] = 0;
$KS::visible[st] = 0;
$KS::coins = 0;
$KS::bank = 0;
$Panel::sbInvShown = false;
$Panel::sbStShown = false;

echo("KronosShop: modern shop/inventory screen loaded");
