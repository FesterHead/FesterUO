# Treasure Map and SOS Storage Book

This directory (`custom-scripts/TMap/`) contains the Treasure Map and SOS Message storage book system for ServUO.

---

## Provenance & Attribution

- **Community Script**: Treasure Map and SOS Storage Book
- **Author**: 4737Carlin (January 12, 2025)
- **Source**: [https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/](https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/)

---

## Overview & Features

`TMapBook.cs` and `TMapGumps.cs` implement a specialized storage and progression container modelled after the classic Bulk Order Book architecture:

- **High Capacity**: Stores up to 500 Treasure Maps and SOS messages in a single blessed, securable tome.
- **Filtering & Search**: Filter entries by type (Treasure Map vs. SOS), facet, decoding status, and difficulty level.
- **Modern ServUO Loot Support**: Seamlessly extracts modern treasure maps while retaining package type and treasure level information.
- **In-Book Gold Sink Upgrades**: Upgrade Treasure Maps and SOS messages directly within the book UI using gold drawn from backpack, bank, or account balance:
  - **+1 Button**: Upgrades the entry to the next level (+1 tier) for 10,000 gp.
  - **Max Button**: Upgrades the entry directly to the highest allowable level for 10,000 gp per level gained.
  - **Upgrade All (+1) Button**: Convenient footer button upgrading every qualifying map and SOS in the book by +1 level in a single click.
  - Strict preservation of natural pinnacle tiers: SOS messages cap at Level 3 (Ancient SOS cannot be reached), and Treasure Maps cap at Level 6 (Diabolical cannot be reached).
- **Mapmaker Vendor Available**: Sold by NPC Mapmakers across Britannia for 1,000 gold (configured via `Config/ServUO/Vendors.cfg`).

---

## Modifications & Enhancements (FesterUO)

This subsystem was customized from the original 4737Carlin community release to adapt it for the FesterUO shard:

### What Was Removed
- **"Price" and "Set" Columns**: Removed the vendor pricing columns from the player backpack book interface to make room for progression controls.
- **"Price all" Button**: Removed the bulk pricing footer action in favor of batch upgrading.
- **Individual Pricing Prompts**: Removed text prompt popups for setting map prices when browsing books from personal backpacks.

### What Was Added
- **In-Gump `+1` Upgrade Button**: Upgrades a specific entry by 1 difficulty level for 10,000 gp.
- **In-Gump `Max` Upgrade Button**: Upgrades a specific entry directly to the highest allowable level (10,000 gp per level gained).
- **`Upgrade All (+1)` Batch Button**: Footer button that iterates through the entire tome and advances all eligible maps and SOS messages by +1 level in a single click.
- **Multi-Source Gold Deduction**: Checks and consumes currency automatically across backpack stacks, bank box gold/checks, and account gold balance.
- **Runtime Configuration**: Parameterized costs and level caps via `Config/ServUO/TreasureMaps.cfg` (`UpgradeCostPerLevel=10000`, `MaxUpgradeLevel=6`, `SOSUpgradeCostPerLevel=10000`, `SOSMaxUpgradeLevel=3`).
- **Pinnacle Tier Safeguards**: Strictly prevents advancing SOS messages to Level 4 (Ancient SOS remains a natural-only drop) and Treasure Maps to Level 7 (Diabolical remains a natural-only drop).
- **Context Menu Cliloc Fix**: Corrected the book renaming context menu entry from legacy `6216` to official client Cliloc `1011299` ("Rename book").

---

## Included Files

- [`TMapBook.cs`](TMapBook.cs): Core storage item definition, serialization, filtering, entry management, upgrade parameters, and context menus.
- [`TMapGumps.cs`](TMapGumps.cs): Multi-page interactive UI for browsing, filtering, map withdrawal, and in-book upgrades.
