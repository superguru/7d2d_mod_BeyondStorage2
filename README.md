# Beyond Storage 2

Mod to allow crafting, repairing, reloading, refueling and upgrading items and blocks using items from nearby storage.  

This version is for 7 Days to Die v2, which is why the mod is called 'Beyond Storage 2'.

The current source repository is located at https://github.com/superguru/7d2d_mod_BeyondStorage2.

* In v2.3.5, make it more server admin friendly, add more console commands, less debug logging, change config properties at runtime
* In v2.3.0, remove old performance related config options, such as EnableXYZ, and rename onlyStorageCrates option to pullFromPlayerCraftedNonCrates, to make it clearer that player crafted wall safes, etc. can be excluded
* In v2.2.7, add lockpicking and payments from storage sources
* In v2.2.5, add console command `bsconfig` to display the current configuration
* In v2.2.4, add live recipe tracking updates when anything becomes available, like a workstation crafted item, a cooked item, a dew collector completion, etc.
* In v2.2.3, add ability to keep track of available ammo from all pullable sources
* In v2.2.0, pull from drones, paint from all storages, supports slot locking for containers, only support 7D2D 2.x and later
* In v2.1.4, final version to support 7D2D 2.1
* In v2.1.3, items can be pulled from nearby Dew Collectors (configurable).
* In v2.1.0, items can be pulled from nearby Workstations (configurable).

#### Pull Order:
  - Player Backpack (as per vanilla game)
  - Player Toolbelt (as per vanilla game)
  - Then:
    1. Drones <<== Added this because of community requests
    2. Dew Collectors
    3. Workstations
    4. Containers (storage crates, etc.)
    5. Vehicles

## Installation

Use a Mod Manager to install the mod, or unzip the contents of this mod into your 7 Days to Die Mods folder.

INSTALL THE SAME VERSION OF THE MOD ON BOTH SERVER AND CLIENT.

See the Troubleshooting section on the mod page if you have any issues.

The compiled and packaged mod is available from https://www.nexusmods.com/7daystodie/mods/7809

## Configuration

The mod can be configured by editing the `config.json` file in the `Mods/BeyondStorage2/Config` folder.

Please refer to the mod description on Nexus Mods for details of the configuration options.

## License
This mod is licensed under the MIT License. See the LICENSE file in the root of the repository for details.

## History

Originally created by aedenthorn as 'CraftFromContainers' for 7 Days to Die. Source code is at https://github.com/aedenthorn/7D2DMods.

It has been refactored and updated for 7 Days to Die v1 by unv-annihilator. See https://github.com/unv-annihilator/7D2D_Mods/tree/main for that fork.


## Credits
- [aedenthorn](https://github.com/aedenthorn) for the original mod
- [unv-annihilator](https://github.com/unv-annihilator) for the 7 Days to Die v1 fork
- [superguru](https://github.com/superguru) for the 7 Days to Die v2 refactor
- [gazorper](https://next.nexusmods.com/profile/gazorper/mods) for the Beyond Storage 2 mod
- The 7 Days to Die community for their support and contributions



