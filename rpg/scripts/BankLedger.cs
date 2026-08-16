//=============================================================================
// Chunked bank ledger
//
// BANK stores the remainder and BANK_CHUNKS stores whole million-coin chunks.
// No arithmetic operation needs to reconstruct the full balance as a number.
// Save format 1 persists BANK in field 6, chunks in 64, the format in 65, and
// the untouched pre-migration BANK text in field 66 for rollback/auditing.
//=============================================================================

$Bank::ChunkBase = 1000000;
$Bank::SaveFormat = 1;

function Bank::RawChunks(%clientId)
{
	%v = GetDataFromArray(%clientId, "BANK_CHUNKS");
	if(%v == "" || %v < 0)
		return 0;
	return floor(%v);
}

function Bank::RawRemainder(%clientId)
{
	%v = GetDataFromArray(%clientId, "BANK");
	if(%v == "" || %v < 0)
		return 0;
	return floor(%v);
}

function Bank::SetParts(%clientId, %chunks, %remainder)
{
	%chunks = floor(%chunks);
	%remainder = floor(%remainder);
	if(%chunks < 0)
		%chunks = 0;

	if(%remainder >= $Bank::ChunkBase)
	{
		%carry = floor(%remainder / $Bank::ChunkBase);
		%chunks += %carry;
		%remainder -= %carry * $Bank::ChunkBase;
	}
	else if(%remainder < 0)
	{
		%borrow = floor((-%remainder + $Bank::ChunkBase - 1) / $Bank::ChunkBase);
		%chunks -= %borrow;
		%remainder += %borrow * $Bank::ChunkBase;
	}

	if(%chunks < 0)
	{
		%chunks = 0;
		%remainder = 0;
	}
	if(%chunks > $Kronos::BalanceCap)
	{
		%chunks = $Kronos::BalanceCap;
		%remainder = $Bank::ChunkBase - 1;
	}

	%clientType = GetClientDataType(%clientId);
	SetDataInArray(%clientId, "BANK_CHUNKS", %chunks, %clientType);
	SetDataInArray(%clientId, "BANK", %remainder, %clientType);
	SetDataInArray(%clientId, "BANK_FORMAT", $Bank::SaveFormat, %clientType);
}

function Bank::IsUnsignedDecimal(%value)
{
	%value = %value @ "";
	%len = String::len(%value);
	if(%len <= 0)
		return false;
	for(%i = 0; %i < %len; %i++)
	{
		%c = String::getSubStr(%value, %i, 1);
		if(String::findSubStr("0123456789", %c) == -1)
			return false;
	}
	return true;
}

function Bank::TrimLeadingZeros(%value)
{
	%value = %value @ "";
	%len = String::len(%value);
	%i = 0;
	while(%i < %len - 1 && String::getSubStr(%value, %i, 1) == "0")
		%i++;
	return String::getSubStr(%value, %i, %len - %i);
}

// Returns "chunks remainder". Plain decimal strings are split without ever
// converting the full value to an engine number, preserving the save exactly.
function Bank::SplitAmount(%value)
{
	%value = %value @ "";
	if(%value == "" || String::getSubStr(%value, 0, 1) == "-")
		return "0 0";

	// Legacy save text is normally plain decimal. Detect exponent notation
	// first; truncating at its decimal point would turn "1.2e+10" into "1".
	%hasExponent = String::findSubStr(%value, "e") != -1 ||
		String::findSubStr(%value, "E") != -1;
	if(!%hasExponent)
	{
		%dot = String::findSubStr(%value, ".");
		if(%dot != -1)
			%value = String::getSubStr(%value, 0, %dot);
		%value = Bank::TrimLeadingZeros(%value);

		if(Bank::IsUnsignedDecimal(%value))
		{
			%len = String::len(%value);
			if(%len <= 6)
				return "0 " @ (%value + 0);
			%chunksText = String::getSubStr(%value, 0, %len - 6);
			%remainderText = String::getSubStr(%value, %len - 6, 6);
			return (%chunksText + 0) @ " " @ (%remainderText + 0);
		}
	}

	// Computed costs can stringify in exponent notation. Scale before floor()
	// so the int32-returning floor never sees the full large value.
	%n = %value + 0;
	if(%n <= 0)
		return "0 0";
	%chunks = floor(%n / $Bank::ChunkBase);
	%remainder = SafeFloor(%n - (%chunks * $Bank::ChunkBase));
	return %chunks @ " " @ %remainder;
}

function Bank::SetLegacy(%clientId, %value)
{
	%parts = Bank::SplitAmount(%value);
	Bank::SetParts(%clientId, GetWord(%parts, 0), GetWord(%parts, 1));
}

function Bank::Load(%clientId, %legacyRemainder, %savedChunks, %savedFormat, %legacyBackup)
{
	if(%savedFormat == $Bank::SaveFormat)
	{
		Bank::SetParts(%clientId, %savedChunks, %legacyRemainder);
		SetDataInArray(%clientId, "BANK_LEGACY_BACKUP", %legacyBackup,
			GetClientDataType(%clientId));
		SetDataInArray(%clientId, "BANK_NEEDS_FILE_BACKUP", "",
			GetClientDataType(%clientId));
	}
	else
	{
		SetDataInArray(%clientId, "BANK_LEGACY_BACKUP", %legacyRemainder,
			GetClientDataType(%clientId));
		SetDataInArray(%clientId, "BANK_NEEDS_FILE_BACKUP", true,
			GetClientDataType(%clientId));
		Bank::SetLegacy(%clientId, %legacyRemainder);
		echo("[BANK-MIGRATE] " @ Client::getName(%clientId) @ " legacy=" @
			%legacyRemainder @ " preserved=" @ Bank::GetText(%clientId));
	}
}

function Bank::GetText(%clientId)
{
	%chunks = Bank::RawChunks(%clientId);
	%remainder = Bank::RawRemainder(%clientId);
	if(%chunks <= 0)
		return %remainder;

	%tail = %remainder @ "";
	while(String::len(%tail) < 6)
		%tail = "0" @ %tail;
	return %chunks @ %tail;
}

function Bank::FormatText(%value)
{
	%value = Bank::TrimLeadingZeros(%value @ "");
	%len = String::len(%value);
	if(%len <= 3)
		return %value;
	%first = %len % 3;
	if(%first == 0)
		%first = 3;
	%out = String::getSubStr(%value, 0, %first);
	for(%i = %first; %i < %len; %i += 3)
		%out = %out @ "," @ String::getSubStr(%value, %i, 3);
	return %out;
}

function Bank::Format(%clientId)
{
	return Bank::FormatText(Bank::GetText(%clientId));
}

function Bank::FormatAmount(%amount)
{
	%parts = Bank::SplitAmount(%amount);
	%text = Bank::PartsText(GetWord(%parts, 0), GetWord(%parts, 1));
	return Bank::FormatText(%text);
}

function Bank::CompareParts(%leftChunks, %leftRemainder, %rightChunks, %rightRemainder)
{
	if(%leftChunks < %rightChunks) return -1;
	if(%leftChunks > %rightChunks) return 1;
	if(%leftRemainder < %rightRemainder) return -1;
	if(%leftRemainder > %rightRemainder) return 1;
	return 0;
}

function Bank::CompareAmounts(%left, %right)
{
	%leftParts = Bank::SplitAmount(%left);
	%rightParts = Bank::SplitAmount(%right);
	return Bank::CompareParts(GetWord(%leftParts, 0), GetWord(%leftParts, 1),
		GetWord(%rightParts, 0), GetWord(%rightParts, 1));
}

// Round amount * integerPercent / 100 without passing a large value to round()
// or floor(). The residual calculation is bounded below 200 million for the
// economy percentages used by buy/sell pricing.
function Bank::ScalePercent(%amount, %integerPercent)
{
	return Bank::ScaleRatio(%amount, %integerPercent, 100);
}

function Bank::ScaleRatio(%amount, %numerator, %denominator)
{
	%numerator = SafeFloor(%numerator);
	%denominator = SafeFloor(%denominator);
	if(%numerator <= 0 || %denominator <= 0)
		return 0;
	%parts = Bank::SplitAmount(%amount);
	%chunks = GetWord(%parts, 0);
	%remainder = GetWord(%parts, 1);

	%chunkGroups = floor(%chunks / %denominator);
	%chunkRemainder = %chunks % %denominator;
	%resultChunks = %chunkGroups * %numerator;
	%baseWhole = floor($Bank::ChunkBase / %denominator);
	%baseFraction = $Bank::ChunkBase % %denominator;
	%smallResidual = (%chunkRemainder * %baseFraction) + %remainder;
	%residualWhole = (%chunkRemainder * %baseWhole) + floor(%smallResidual / %denominator);
	%residualFraction = %smallResidual % %denominator;
	%wholeGroups = floor(%residualWhole / 10000);
	%wholeRemainder = %residualWhole % 10000;
	%scaledGroups = %wholeGroups * %numerator;
	%resultChunks += floor(%scaledGroups / 100);
	%scaledResidual = ((%scaledGroups % 100) * 10000) +
		(%wholeRemainder * %numerator) +
		floor((%residualFraction * %numerator) / %denominator + 0.5);
	if(%scaledResidual >= $Bank::ChunkBase)
	{
		%carry = floor(%scaledResidual / $Bank::ChunkBase);
		%resultChunks += %carry;
		%scaledResidual -= %carry * $Bank::ChunkBase;
	}
	return Bank::PartsText(%resultChunks, %scaledResidual);
}

function Bank::SubtractSmall(%amount, %smallAmount)
{
	%parts = Bank::SplitAmount(%amount);
	%chunks = GetWord(%parts, 0);
	%remainder = GetWord(%parts, 1) - SafeFloor(%smallAmount);
	if(%remainder < 0)
	{
		%chunks--;
		%remainder += $Bank::ChunkBase;
	}
	if(%chunks < 0)
		return 0;
	return Bank::PartsText(%chunks, %remainder);
}

function Bank::CanAfford(%clientId, %amount)
{
	%parts = Bank::SplitAmount(%amount);
	return Bank::CompareParts(Bank::RawChunks(%clientId), Bank::RawRemainder(%clientId),
		GetWord(%parts, 0), GetWord(%parts, 1)) >= 0;
}

function Bank::Credit(%clientId, %amount)
{
	%parts = Bank::SplitAmount(%amount);
	%addChunks = GetWord(%parts, 0);
	%addRemainder = GetWord(%parts, 1);
	if(%addChunks == 0 && %addRemainder == 0)
		return false;

	%chunks = Bank::RawChunks(%clientId) + %addChunks;
	%remainder = Bank::RawRemainder(%clientId) + %addRemainder;
	Bank::SetParts(%clientId, %chunks, %remainder);
	return true;
}

function Bank::Debit(%clientId, %amount)
{
	%parts = Bank::SplitAmount(%amount);
	%subChunks = GetWord(%parts, 0);
	%subRemainder = GetWord(%parts, 1);
	if(%subChunks == 0 && %subRemainder == 0)
		return false;
	if(Bank::CompareParts(Bank::RawChunks(%clientId), Bank::RawRemainder(%clientId),
		%subChunks, %subRemainder) < 0)
		return false;

	%chunks = Bank::RawChunks(%clientId) - %subChunks;
	%remainder = Bank::RawRemainder(%clientId) - %subRemainder;
	Bank::SetParts(%clientId, %chunks, %remainder);
	return true;
}

// Compatibility path for legacy storeData(..., "BANK", ...) callers.
function Bank::StoreCompat(%clientId, %amount, %special)
{
	if(%special == "inc")
	{
		if(%amount < 0)
			return Bank::Debit(%clientId, -%amount);
		return Bank::Credit(%clientId, %amount);
	}
	if(%special == "dec")
		return Bank::Debit(%clientId, %amount);
	Bank::SetLegacy(%clientId, %amount);
	return true;
}

function Bank::DepositFromCoins(%clientId, %amount)
{
	%coins = fetchData(%clientId, "COINS");
	if(%amount == "" || %amount == "all")
		%amount = %coins;
	%amount = SafeFloor(%amount);
	if(%amount <= 0 || %amount > %coins)
		return 0;
	if(!Bank::Credit(%clientId, %amount))
		return 0;
	storeData(%clientId, "COINS", %amount, "dec");
	return %amount;
}

function Bank::WithdrawToCoins(%clientId, %amount)
{
	%coins = fetchData(%clientId, "COINS");
	%headroom = $Kronos::BalanceCap - %coins;
	if(%headroom <= 0)
		return 0;

	if(%amount == "" || %amount == "all")
	{
		if(Bank::CanAfford(%clientId, %headroom))
			%amount = %headroom;
		else if(Bank::RawChunks(%clientId) == 0)
			%amount = Bank::RawRemainder(%clientId);
		else
			%amount = %headroom;
	}
	%amount = SafeFloor(%amount);
	if(%amount <= 0 || %amount > %headroom || !Bank::CanAfford(%clientId, %amount))
		return 0;
	if(!Bank::Debit(%clientId, %amount))
		return 0;
	storeData(%clientId, "COINS", %amount, "inc");
	return %amount;
}

// Purchases spend carried coins first, then debit the exact remainder from the
// chunked bank. The bank debit happens before the wallet write so a failed
// debit cannot partially charge the player.
function Bank::CanPay(%clientId, %amount)
{
	%parts = Bank::SplitAmount(%amount);
	%chunks = GetWord(%parts, 0);
	%remainder = GetWord(%parts, 1);
	%coins = fetchData(%clientId, "COINS");
	%coinChunks = floor(%coins / $Bank::ChunkBase);
	%coinRemainder = %coins - (%coinChunks * $Bank::ChunkBase);
	%chunks -= %coinChunks;
	%remainder -= %coinRemainder;
	if(%remainder < 0)
	{
		%chunks--;
		%remainder += $Bank::ChunkBase;
	}
	if(%chunks < 0 || (%chunks == 0 && %remainder <= 0))
		return true;
	return Bank::CompareParts(Bank::RawChunks(%clientId), Bank::RawRemainder(%clientId),
		%chunks, %remainder) >= 0;
}

function Bank::Pay(%clientId, %amount)
{
	%parts = Bank::SplitAmount(%amount);
	%chunks = GetWord(%parts, 0);
	%remainder = GetWord(%parts, 1);
	if(%chunks == 0 && %remainder == 0)
		return true;

	%coins = fetchData(%clientId, "COINS");
	if(%chunks == 0 && %remainder <= %coins)
	{
		storeData(%clientId, "COINS", %remainder, "dec");
		return true;
	}
	if(!Bank::CanPay(%clientId, %amount))
		return false;

	%coinChunks = floor(%coins / $Bank::ChunkBase);
	%coinRemainder = %coins - (%coinChunks * $Bank::ChunkBase);
	%chunks -= %coinChunks;
	%remainder -= %coinRemainder;
	if(%remainder < 0)
	{
		%chunks--;
		%remainder += $Bank::ChunkBase;
	}
	%bankCharge = Bank::PartsText(%chunks, %remainder);
	if(!Bank::Debit(%clientId, %bankCharge))
		return false;
	storeData(%clientId, "COINS", 0);
	// The carried-coins fast path above says nothing extra; only tell the
	// player when the price reached past their wallet into the bank.
	Client::sendMessage(%clientId, $MsgWhite, "Your carried coins didn't cover that - " @ Bank::FormatAmount(%bankCharge) @ " coins were paid from your bank. (Bank: " @ Bank::Format(%clientId) @ ")");
	return true;
}

// Coin income fills the int32-safe wallet and preserves overflow in the bank.
function Bank::CreditCoins(%clientId, %amount)
{
	%parts = Bank::SplitAmount(%amount);
	%chunks = GetWord(%parts, 0);
	%remainder = GetWord(%parts, 1);
	if(%chunks == 0 && %remainder == 0)
		return false;

	%coins = fetchData(%clientId, "COINS");
	%headroom = $Kronos::BalanceCap - %coins;
	if(%chunks == 0 && %remainder <= %headroom)
	{
		SetDataInArray(%clientId, "COINS", %coins + %remainder, GetClientDataType(%clientId));
		return true;
	}

	if(%headroom > 0)
	{
		SetDataInArray(%clientId, "COINS", $Kronos::BalanceCap, GetClientDataType(%clientId));
		%headroomChunks = floor(%headroom / $Bank::ChunkBase);
		%headroomRemainder = %headroom - (%headroomChunks * $Bank::ChunkBase);
		%chunks -= %headroomChunks;
		%remainder -= %headroomRemainder;
		if(%remainder < 0)
		{
			%chunks--;
			%remainder += $Bank::ChunkBase;
		}
	}
	%overflow = Bank::PartsText(%chunks, %remainder);
	%credited = Bank::Credit(%clientId, %overflow);
	// Income past the carry cap lands here silently otherwise; players read a
	// full wallet as lost income. Short per-client throttle (kill streaks at
	// cap fire this on every coin drop), with throttled amounts ACCUMULATED in
	// chunk parts so every banked coin is reported by a later message rather
	// than skipped.
	if(%credited)
	{
		%oParts = Bank::SplitAmount(%overflow);
		%clientId.ovfMsgChunks += GetWord(%oParts, 0);
		%clientId.ovfMsgRem += GetWord(%oParts, 1);
		if(%clientId.ovfMsgRem >= $Bank::ChunkBase)
		{
			%clientId.ovfMsgChunks++;
			%clientId.ovfMsgRem -= $Bank::ChunkBase;
		}
		if(getSimTime() - %clientId.bankOverflowMsgTime > 3)
		{
			%clientId.bankOverflowMsgTime = getSimTime();
			%txt = Bank::FormatText(Bank::PartsText(%clientId.ovfMsgChunks, %clientId.ovfMsgRem));
			%clientId.ovfMsgChunks = "";
			%clientId.ovfMsgRem = "";
			Client::sendMessage(%clientId, $MsgWhite, "Your pockets are full - " @ %txt @ " coins were deposited into your bank. (Bank: " @ Bank::Format(%clientId) @ ")");
		}
	}
	return %credited;
}

function Bank::PadRemainder(%remainder)
{
	%tail = %remainder @ "";
	while(String::len(%tail) < 6)
		%tail = "0" @ %tail;
	return %tail;
}

function Bank::PartsText(%chunks, %remainder)
{
	if(%chunks <= 0)
		return %remainder;
	%tail = Bank::PadRemainder(%remainder);
	return %chunks @ %tail;
}

function Bank::TotalWithCoinsText(%clientId)
{
	%chunks = Bank::RawChunks(%clientId);
	%remainder = Bank::RawRemainder(%clientId) + fetchData(%clientId, "COINS");
	if(%remainder >= $Bank::ChunkBase)
	{
		%chunks += floor(%remainder / $Bank::ChunkBase);
		%remainder = %remainder % $Bank::ChunkBase;
	}
	if(%chunks <= 0)
		return %remainder;
	%tail = Bank::PadRemainder(%remainder);
	return %chunks @ %tail;
}
