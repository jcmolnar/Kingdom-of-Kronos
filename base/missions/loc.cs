function me::getLOS()
{
bottomprint("Current LoS: " @ GameBase::getLOSInfo(getManagerID(), 99999, 0));
}