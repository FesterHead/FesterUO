# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Added `SlowSkillGrind.cs` in `custom-scripts/FesterUO/` and configuration in `Config/FesterUO/SlowSkillGrind.cfg` allowing skills to advance past 100.0 without PowerScrolls over progressive hour-paced intervals (Tier 1 [100-105]: 6h; Tier 2 [105-110]: 12h; Tier 3 [110-115]: 18h; Tier 4 [115-120]: 24h) with persistence in `Saves/SlowSkillGrind/Persistence.bin` and player status commands `[SlowGrind` / `[SlowGrindInfo`.
- Added fish fillet exemption filtering and interactive gump (`FestersFishFilterGump`) to `FestersFishingPole` in [`custom-scripts/FesterUO/CustomAutoTools.cs`](custom-scripts/FesterUO/CustomAutoTools.cs):
  - Provides player chat commands (`[ff`, `[FishFilter`, `[FilletFilter`) to open the interactive configuration gump directly from anywhere.
  - Automatically identifies active `ProfessionalFisherQuest` orders from the player's quest log and auto-protects required quest targets (enabled by default) so fish like Red Grouper remain whole in the backpack.
  - Features tabbed browsing across all High Seas fish categories (Deep Water [18 species], Shore [12 species], Dungeon [12 species], Common [2 species]) with active quest target indicators (`[Quest X/Y]`), quick tab selection controls, manual fish checklists, and single-click tooltips showing protection status.
- Added [`30-deployed-ship-limit.patch`](patches/30-deployed-ship-limit.patch) and parameterized `MaxDeployedShips=0` in [`Config/ServUO/Housing.cfg`](Config/ServUO/Housing.cfg), disabling the retail High Seas 1-deployed-ship-per-character limit to allow multiple active vessels without dry-docking prerequisites, and updated ship resolution for Fishmongers and dock masters to prioritize the closest vessel on the player's active facet.
- Added [`31-sea-market-recall.patch`](patches/31-sea-market-recall.patch) and parameterized `AllowSeaMarketRecall=True` in [`Config/ServUO/Expansion.cfg`](Config/ServUO/Expansion.cfg), allowing players to cast Mark, Recall, Gate Travel, and Sacred Journey directly onto the Sea Market floor without requiring an active vessel at dock.
- Added [`32-fish-quest-improvements.patch`](patches/32-fish-quest-improvements.patch) and parameterized `FishQuestAutoComplete=True` and `FishQuestRequireNearbyBoat=False` in [`Config/ServUO/Expansion.cfg`](Config/ServUO/Expansion.cfg), introducing quality-of-life enhancements for High Seas fishing quests:
  - Added double-click interaction to crates (`ShippingCrate.OnDoubleClick`), allowing players to finalize already filled or save-restored orders on demand with feedback, while deferring quest completion and item cleanup via zero-delay timers to prevent deletion during active drop event stacks.
  - Suppresses the "Deliver it to the person specified" instruction from the quest log gump and provides clear, pure tooltips (`Automated Delivery: Order Complete (Double-click crate to claim)` when completed, and `Automated Delivery: Fulfills when filled` while in progress) without side effects during property serialization.
  - Implements `ShippingCrate.TryDropItem` to support assistant scripting tools and automated fish-dropping macros.
  - Safely returns extra or non-quest fish to the boat hold (or player backpack if hold space is unavailable) before removing the completed crate.
  - Allows players to accept fishing quests regardless of vessel distance (`FishQuestRequireNearbyBoat=False`), auto-depositing the shipping crate into their deployed boat's hold wherever it is in the world, and only blocking quest acquisition if no boat is currently deployed.
- Added [`33-decoded-maps-open-to-all.patch`](patches/33-decoded-maps-open-to-all.patch) and parameterized `DecodedMapsOpenToAll=True` in [`Config/ServUO/TreasureMaps.cfg`](Config/ServUO/TreasureMaps.cfg), allowing any character to view map pins, begin digging, and dig up chests for any decoded treasure map without decoder restrictions or Cartography skill prerequisites, supporting Level 0 decoded map promotion for all players, and allowing Mining or Cartography to contribute to digging accuracy radius under the modern system. Updated [`TreasureMapDecoder.cs`](custom-scripts/zerodowned/TreasureMapDecoder.cs) so the instant transporter moongate can be used on maps deciphered by any character.
- Implemented a gold sink upgrade system in [`TreasureMapDecoder.cs`](custom-scripts/zerodowned/TreasureMapDecoder.cs) and [`SOSDecoder.cs`](custom-scripts/zerodowned/SOSDecoder.cs) parameterized via `Config/ServUO/TreasureMaps.cfg` (`UpgradeCostPerLevel=10000`, `MaxUpgradeLevel=6`, `SOSUpgradeCostPerLevel=10000`, `SOSMaxUpgradeLevel=3`):
  - Provides single left-click Context Menu entries (`Upgrade` for +1 tier and `Promote` for Max tier) on Treasure Maps and SOS messages in the player's backpack, as well as on the decoder instant transporters themselves.
  - Multi-source currency deduction supporting account gold, bank box gold/checks, and backpack gold stacks at 10,000 gp per level.
  - Strict preservation of natural pinnacle tiers: SOS messages cap at Level 3 preventing advancement to Level 4 Ancient SOS, and Treasure Maps cap at Level 6 (Ingeniously Drawn) preserving natural Level 7 Diabolical drops.

- Expanded `ResourceSatchel.cs` to support organization containers (e.g. bags, pouches, wooden boxes) containing valid resources with recursive weight neutralization (100% weight reduction) and isolated item counts, and updated automated resource harvesting deposit routing to intelligently detect and merge with matching resource stacks located inside organization sub-bags.
- Expanded `ResourceSatchel.cs` to store all maps (`MapItem` except `TreasureMap`, including `BlankMap`, vendor `PresetMap`, and crafted maps like `LocalMap`, `CityMap`, `SeaChart`, `WorldMap`) as well as `BlankScroll` with 100% weight reduction and isolated item counts, supporting cartography training and map storage.
- Updated `FesterUOGuideBook.cs` to add player commands `[guide`, `[guidebook`, and `[getguide` (providing or replacing any outdated copy in the backpack with a fresh edition and opening it), added dedicated pages for the Legendary Master of Skills (quest trials, Grandmaster 100.0 prerequisite, and spoken commands `give task <skill>` / `remove task`), added `[guide` to Player Commands 4, and updated satchel details.
- Adjusted `ApexHuntConfig.TriggerChance` from 10% (0.10) to 2% (0.02) to tune automated event frequency for private shard pacing.
- Renamed and transformed `TillermanGump.cs` into `SayGump.cs` (`[say`, `[tillerman`, `[house`, `[boat`), creating a universal quick-speech gump accessible anywhere without vessel boarding restrictions. Reduced gump dimensions by ~51% (155x185 vs 260x225), removed the close footer text, and added a dedicated House management section supporting "Lock Down", "Secure", "Release", "Unsecure", "Trash Barrel", "Ban", and "Eject" with native speech keyword triggers.
- Tuned `LegendaryMaster` configuration in `Config/LegendaryMaster/LegendaryMaster.cfg` for two-player co-op play: relaxed task timer to 180 minutes (with +20 min kill extension), set required kills to a flat 1 kill per task (`BaseMinKills=1`, `BaseMaxKills=1`, `KillsPerTier=0`), increased stat cap scroll chance to 5% (high tier 2%), and refined the creature pool to focus on formidable dungeon encounters and apex beasts (`Dragon`, `WhiteWyrm`, `GreaterDragon`, `ShadowWyrm`, `AncientWyrm`, `SkeletalDragon`, `SerpentineDragon`, `Balron`, `Succubus`, `BoneDemon`, `AncientLich`, `RottingCorpse`, `RuneBeetle`, `Yamandon`, `PoisonElemental`, `BloodElemental`).
- Enhanced `LegendaryMaster.cs` and added `AutoAdvanceSkillOnUse=true` (`Config/LegendaryMaster/LegendaryMaster.cfg`): fixed task reward calculation to always snap to the next clean 5.0 tier boundary (e.g. 100.6 accurately awards a 105 scroll), and hooked `EventSink.SkillCapChange` to instantly advance the character's base skill to the scroll tier upon consuming a PowerScroll (with automatic down-skill reduction if total skills are capped).

### Fixed
- Suppressed extraneous .NET CLI workload verification warning (`An issue was encountered verifying workloads`) during dynamic script compilation by configuring `DOTNET_SKIP_WORKLOAD_INTEGRITY_CHECK=true` in `Dockerfile` and `docker-compose.yaml`.
- Stripped trailing whitespace in [`patches/32-fish-quest-improvements.patch`](patches/32-fish-quest-improvements.patch) ensuring clean, warning-free patch application during build and verification.
- Resolved compiler warning CS0108 in `LootFilterGump.cs` by renaming static array `Entries` to `FilterEntries` to prevent shadowing the inherited base `Gump.Entries` property.
- Fixed premature gump closures and setting corruption in `[LootFilter` (`LootFilterGump.cs`) caused by button ID calculations using raw bit flag enum values (`AosAttribute`, `AosWeaponAttribute`, `SAAbsorptionAttribute`). Replaced arithmetic button ID encoding with a typed index-mapped entry architecture supporting safe toggle buttons, increment/decrement adjustment arrows, direct text entry inputs, and an explicit Apply button.

### Removed
- Removed `CorpseFinder.cs` and the `[Corpse` player command due to quest arrow dismissal failures and cross-dungeon map inaccuracies. Updated `FesterUOGuideBook.cs` and shard documentation accordingly.

## [1.0.0] - 2026-09-30

### Added
- Extracted FesterUO into an independent standalone repository and Docker Compose deployment targeting ServUO Publish 57 with .NET 10 build and Mono runtime.
- Added [29-loot-filter.patch](patches/29-loot-filter.patch) and custom script system in `custom-scripts/LootFilter/` providing an ARPG-style loot visibility filter (`[LootFilter`) for ground and corpse equipment across 8 categories and 87+ item properties with numeric threshold filtering and equipment-only protection guarantees.
- Added [28-dyes-target-handler.patch](patches/28-dyes-target-handler.patch) and custom script system in `custom-scripts/RuneBookandSpellBookDyeTubs/` providing customizable dye tubs for runebooks and spellbooks with 16 preset color palettes and visual palette gumps.
- Added [27-hunter-bestiary.patch](patches/27-hunter-bestiary.patch) and custom script system in `custom-scripts/ExileHunterBestiary/` providing a 193-creature horizontal progression bestiary with collectible monster treatises, damage mastery bonuses, blessed `HunterBestiary` grimoire, and book gump (`[Bestiary`).
- Added custom script system in `custom-scripts/ApexHunt/` providing an automated server-wide PvM hunting competition system (`[ApexHunt`, `[ApexHuntToggle`, `[ApexHuntTop`, `[ApexHuntStart`, `[ApexHuntStop`) with tiered target creature selection, movable HUD tracker, and leaderboard gump.
- Added [26-sanctuary-ward.patch](patches/26-sanctuary-ward.patch) and `SanctuaryWard.cs` in `custom-scripts/FesterUO/` providing the Talisman of Sanctuary (`SanctuaryTalisman`), a blessed item suppressing hostile creature aggro outdoors while allowing mobs to defend themselves if attacked.
- Added `HarvestConfig.cs` in `custom-scripts/FesterUO/` and settings in `Config/FesterUO/Harvest.cfg` to dynamically configure resource bank respawn times and capacities with GM inspection command `[HarvestInfo`.
- Added `TillermanGump.cs` in `custom-scripts/FesterUO/` providing player command `[tillerman` to open an intelligent boat navigation control gump.
- Added `TreasureMapDecoder.cs` and `SOSDecoder.cs` in `custom-scripts/zerodowned/` providing instant transporter items for maps and sunken treasure.
- Added `RemoteBank.cs` in `custom-scripts/FesterUO/` providing player command `[rbank` for remote personal bank access.
- Added `WildernessReagents.cs` in `custom-scripts/FesterUO/` and `Config/FesterUO/Reagents.cfg` providing automated wilderness ground reagent spawning across Trammel and Felucca.
- Added custom gathering and utility scripts in `custom-scripts/FesterUO/` (`ResourceSatchel.cs`, `CustomAutoTools.cs`) providing auto-smelting, auto-sawing, auto-filleting, corpse hide conversion, sheep shearing, area crop scything, and automatic resource routing with 100% weight reduction.
- Added `CorpseFinder.cs` in `custom-scripts/FesterUO/` providing `[Corpse` navigation command directing an in-game quest arrow straight to fallen bodies.
- Added `FesterUOGuideBook.cs` in `custom-scripts/FesterUO/` providing an in-game reference tome distributed to the first character per account.
- Added `GlobalChat.cs` in `custom-scripts/FesterUO/` providing `[c <message>` and `[chat <message>` server-wide broadcast commands in cyan text.
- Added explicit runtime configuration files in `Config/` (`Accounts.cfg`, `AutoRestart.cfg`, `AutoSave.cfg`, `Champions.cfg`, `DataPath.cfg`, `Expansion.cfg`, `General.cfg`, `Harvest.cfg`, `Housing.cfg`, `Loot.cfg`, `PlayerCaps.cfg`, `Server.cfg`, `Stables.cfg`, `TreasureMaps.cfg`, `Vendors.cfg`, `VetRewards.cfg`).
- Added `manage-patches.sh` CLI utility with idempotent patch application, already-applied detection, upstream synchronization, and Dockerfile commit alignment.

### Changed
- Configured [PlayerCaps.cfg](Config/ServUO/PlayerCaps.cfg) to support dual-character playstyles: `TotalStatCap=900`, individual stat caps to `250` (scroll ceiling `300`), `TotalSkillCap=24000` (2400.0%), and disabled anti-macro and stat gain delays.
- Configured vendor trade commodity baseline to 640 in [Vendors.cfg](Config/ServUO/Vendors.cfg) and disabled low-demand restock decay.
- Enhanced harvest notifications for auto-tools to state specific ore and wood types colored according to native resource hues.
