create table bladeOwnership(
	ownershipKey integer primary key autoincrement,
	state,
	VMDeployState,
	currentOwner,
	nextOwner,
	lastKeepAlive,
	currentSnapshot
	);

create table bladeConfiguration(
	bladeConfigKey integer primary key autoincrement,
	ownershipID unique,
    iscsiIP unique,
    bladeIP unique,
    iLOIP unique,
    kernelDebugPort unique,
	currentlyHavingBIOSDeployed,
	currentlyBeingAVMServer,
	vmDeployState,
	lastDeployedBIOS,

	foreign key (ownershipID) references bladeOwnership(ownershipKey)
	);

create table VMConfiguration(
	vmConfigKey integer primary key autoincrement,
	indexOnServer,
	ownershipID unique,
	parentBladeID,
	parentBladeIP,
	memoryMB,
	cpuCount,
	vmxPath,
    iscsiIP unique,
	VMIP,
	eth0MAC,
	eth1MAC,
	displayname,
    kernelDebugPort,
    kernelDebugKey,
	
	foreign key (parentBladeID ) references bladeConfiguration(bladeConfigKey),
	foreign key (ownershipID) references bladeOwnership(ownershipKey)
	);
