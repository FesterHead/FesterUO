# Legendary Master of Skills

This directory (`servuo/custom-scripts/LegendaryMaster/`) contains the **Legendary Master of Skills** NPC questmaster system.

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - Legendary Master of Skills (Resource #2244)](https://www.servuo.dev/archive/legendary-master-of-skills.2244/)
- **Original Author**: Keith (a.k.a. Jack / Keith on servuo.dev)
- **License**: GNU General Public License v3.0 (GPL-3.0)
- **Integration**: Placed in `servuo/custom-scripts/LegendaryMaster/` and compiled dynamically by ServUO into `Scripts.dll`.

---

## System Overview

The **Legendary Master of Skills** is an interactive questmaster NPC designed for small shards and solo adventurers as an alternative path to obtain PowerScrolls without running Champion Spawns:
1. Players approach the Master by speaking nearby or double-clicking him to undertake a combat challenge (no upfront skill name needed).
2. The Master tasks the adventurer with slaying high-level dungeon bosses within an allotted countdown timer.
3. Upon slaying the final target, an interactive selection gump (`LegendarySkillGump`) opens allowing the player to pick any skill to receive a **Legendary Master Power Scroll**.
4. The awarded scroll is Blessed, transferable to other characters on the account, and dynamically advances the recipient's skill cap to the next ceiling tier (105, 110, 115, or 120), requiring 100.0+ base skill to use.

---

## Player Speech & Interaction Commands

Players interact with the NPC by double-clicking him or speaking nearby (within 5 tiles):

| Interaction / Command | Description | Example |
| :--- | :--- | :--- |
| Double-click NPC | Starts a new combat trial or reopens the reward selection gump if a trial was completed. | Double-click |
| `give task`<br>`task`<br>`quest` | Requests a new combat trial (or reopens the reward selection gump). | `give task`<br>`task` |
| `remove task`<br>`cancel task` | Abandons the currently active task so a new one can be started. | `remove task` |

---

## Quest Rules & Requirements

1. **One Active Task**:
   - Players may only have one active task at any given time.
2. **Target Pool**:
   - Tasks require hunting creatures randomly drawn from high-level dungeon encounters across Britannia, Ilshenar, Malas, and Tokuno (configurable in `LegendaryMaster.cfg`):
     - **Balron** (Hythloth, Abyss)
     - **Shadow Wyrm** (Destard)
     - **Ancient Lich** (Deceit)
     - **Ancient Wyrm** (Destard)
     - **Skeletal Dragon** (Ankh Dungeon)
     - **Greater Dragon** (Destard)
     - **Succubus** (Abyss, Hythloth)
     - **Rotting Corpse** (Deceit)
     - **Blood Elemental** (Blood Dungeon)
     - **Poison Elemental** (Destard, Shame)
     - **Serpentine Dragon** (Ilshenar)
     - **Bone Demon** (Doom)
     - **Rune Beetle** (Tokuno)
     - **Yamandon** (Tokuno)
     - **White Wyrm** (Ice Dungeon)
3. **Time Limit & Extensions**:
   - Tasks grant an initial countdown (default 180 minutes).
   - Each confirmed target kill adds bonus time (default +20 minutes) to the countdown.
   - Kills qualify when performed directly by the player, their pets, or their summons.
4. **Reward Gump & Alt Flexibility**:
   - Completing a task immediately displays `LegendarySkillGump` categorized into 5 tabs: **Combat**, **Magic**, **Crafting** (including Fishing, Mining, Lumberjacking, Carpentry, Fletching, Tinkering, Alchemy, Cooking, Inscription, Blacksmithy, Tailoring, and Imbuing), **Wilderness**, and **Utility**.
   - If closed prematurely, speaking to or double-clicking the Master reopens the gump.
   - The awarded `LegendaryPowerScroll` can be transferred to any character on the player's account.

---

## Rewards

- **Legendary Master Power Scroll**: A Blessed +5 ceiling upgrade scroll for the selected skill.
  - Requires at least 100.0 base skill to use.
  - Dynamically raises the user's skill cap to the next available ceiling limit: 105, 110, 115, or 120.
  - If the character's skill is less than 100.0 or already at 120.0, the scroll is not consumed and no error is displayed.
  - When consumed, with `AutoAdvanceSkillOnUse=true`, it immediately sets the base skill to the new cap.
- **Bonus Stat Cap Scroll**: A 5% chance (configurable) to receive an additional Stat Cap Scroll (+5 up to +25).

---

## Configuration (`servuo/Config/LegendaryMaster/LegendaryMaster.cfg`)

All task timers, kill counts, scaling factors, skill limits, stat scroll rewards, NPC behavior, and the target creature pool are externalized in [`servuo/Config/LegendaryMaster/LegendaryMaster.cfg`](../../Config/LegendaryMaster/LegendaryMaster.cfg):

```ini
# --- Quest Timers ---
TaskTimeMinutes=180
ExtraTimeMinutes=20

# --- Kill Counts & Difficulty Scaling ---
BaseMinKills=1
BaseMaxKills=1
KillsPerTier=1

# --- Skill Progression Limits ---
MinSkillRequired=100.0
MaxSkillCap=120.0
ScrollIncrement=5.0
AutoAdvanceSkillOnUse=true

# --- Bonus Stat Cap Scrolls ---
EnableStatScrolls=true
StatScrollChance=0.01
HighStatScrollChance=0.01
MaxStatScrollBonus=25

# --- NPC Behavior ---
InteractionRange=5
IdleBarkChance=0.15

# --- Target Creature Pool ---
Creatures=Dragon, WhiteWyrm, GreaterDragon, ShadowWyrm, AncientWyrm, SkeletalDragon, SerpentineDragon, Balron, Succubus, BoneDemon, AncientLich, RottingCorpse, RuneBeetle, Yamandon, PoisonElemental, BloodElemental
```

Changes take effect upon restarting the container:
```bash
docker compose restart servuo
```

---

## In-Game Staff Commands

Spawn a Legendary Master NPC in a public hub or town plaza:

```text
[add LegendaryMaster
```

---

## Enhancements & Bug Fixes Applied

The following improvements were made over the original community script release:
- **Direct Player Kill Credit**: Fixed an issue where only pet/summon kills registered progress; direct weapon and spell kills now properly advance the task.
- **Fatal Index Out of Range Fix**: Replaced `taskInfos[pm.Serial]` indexing on speech with safe entity lookups to prevent server crashes.
- **Immediate Task Completion**: Corrected the task counter decrement order so rewards are awarded immediately upon defeating the final required creature.
- **Human-Readable Timers & Creature Names**: Formatted countdown messages in remaining minutes and cleaned creature class names (e.g. "Shadow Wyrm" instead of `Server.Mobiles.ShadowWyrm`).
- **Complete Creature Pool**: Fixed an off-by-one random selection bug that previously prevented White Wyrms from ever being selected.
- **Flexible & Case-Insensitive Speech**: Made speech parsing case-insensitive and accepted both display names and enum names.
- **World Persistence**: Added WorldSave/WorldLoad state persistence to `Saves/LegendaryMaster/Persistence.bin` so active player quests survive server restarts.
