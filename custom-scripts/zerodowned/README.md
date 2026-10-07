# zerodowned Custom Scripts

This directory (`servuo/custom-scripts/zerodowned/`) contains custom scripts and utilities authored by **zerodowned** from the ServUO community.

---

## Provenance & Attribution

- **Author**: zerodowned
- **Source Repository**: [https://github.com/zerodowned/Custom-Scripts-for-ServUO](https://github.com/zerodowned/Custom-Scripts-for-ServUO)
- **Script Origins**:
  - [`SOS Decoder/SOSDecoder.cs`](https://github.com/zerodowned/Custom-Scripts-for-ServUO/blob/master/SOS%20Decoder/SOSDecoder.cs)
  - [`Treasure Map Decoder/TreasureMapDecoder.cs`](https://github.com/zerodowned/Custom-Scripts-for-ServUO/tree/master/Treasure%20Map%20Decoder)

---

## Overview & Features

### SOS Instant Transporter (`SOSDecoder.cs`)

`SOSDecoder` is a seafaring quality-of-life item designed to streamline SOS salvage voyages:

- **Instant Boat Transport**: While standing on a stationary boat, double-click the transporter and target an SOS in your backpack. The boat automatically calculates open-water clearance near the sunken ship coordinates and teleports directly to the target location with nautical splash animations.
- **Gold Sink Upgrades**: Single left-clicking an SOS in the player's backpack (or selecting the upgrade options from the `SOSDecoder` context menu) allows upgrading the SOS level:
  - **+1 Level**: Advances the SOS by 1 tier for 10,000 gold.
  - **Max Upgrade**: Advances the SOS directly to Level 3 (the maximum upgradeable level) at 10,000 gold per tier.
  - **Natural Ancient Preservation**: Level 4 ("Ancient SOS") cannot be reached via gold upgrades; ancient messages must be recovered naturally from MIBs.
- **Pre-flight Validations**: Checks that the player is not criminal, overloaded, in combat, in jail, casting a spell, or on a moving vessel.
- **Unlimited Usage**: No charges required; players can freely decode and navigate between SOS salvage coordinates.
- **Procurement & Vendor Stock**: Sold by NPC Fishermen for 10,000 gold (configured via `SOSDecoderCost` in `servuo/Config/ServUO/Vendors.cfg`). Staff can also spawn it via `[add SOSDecoder`.

### Treasure Map Instant Transporter (`TreasureMapDecoder.cs`)

`TreasureMapDecoder` is an exploration quality-of-life item designed for treasure hunters:

- **Moongate to Dig Site**: Double-click from your backpack and target a Treasure Map. Opens a 30-second timed moongate directly onto the chest coordinates (automatically calculating ground Z-elevation).
- **Auto-Decodes Undeciphered Maps**: Targeting an un-decoded map marks it decoded by the player so you don't need to manually decipher it first.
- **Universal Decoded Map Access**: When `DecodedMapsOpenToAll=True` in `Config/ServUO/TreasureMaps.cfg`, players can use the transporter on maps decoded by other characters without restriction.
- **Gold Sink Upgrades**: Single left-clicking a Treasure Map in the player's backpack (or selecting the upgrade options from the `TreasureMapDecoder` context menu) allows upgrading the map tier:
  - **+1 Level**: Advances the map by 1 tier for 10,000 gold.
  - **Max Upgrade**: Advances the map directly to Level 6 (Ingeniously Drawn, the maximum upgradeable level) at 10,000 gold per tier.
  - **Natural Diabolical Preservation**: Level 7 (Diabolically Drawn) cannot be reached via gold upgrades; level 7 maps must be obtained naturally from high-tier encounters/paragons.
- **Completion Check**: Prevents opening gates to already completed/looted treasure maps.
- **Pre-flight Validations**: Checks that the player is not criminal, overloaded, in combat, in jail, or casting a spell.
- **Unlimited Usage**: No charges required; players can freely decode and gate to any of their treasure maps.
- **Procurement & Vendor Stock**: Sold by NPC Mapmakers for 10,000 gold (configured via `TreasureMapDecoderCost` in `servuo/Config/ServUO/Vendors.cfg`). Staff can also spawn it via `[add TreasureMapDecoder`.

