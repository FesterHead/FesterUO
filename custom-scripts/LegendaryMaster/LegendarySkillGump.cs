// ============================================================================
// LegendarySkillGump.cs - Legendary Master Skill Selection Gump
//
// Origin: FesterUO Custom Scripts (servuo/custom-scripts/LegendaryMaster/)
// Purpose: Interactive gump presented upon task completion allowing the player
//          to select which skill to receive a Master Power Scroll for.
// ============================================================================

using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Misc
{
    public class LegendarySkillGump : Gump
    {
        public enum Category
        {
            Combat = 0,
            Magic = 1,
            Crafting = 2,
            Wilderness = 3,
            Utility = 4
        }

        private readonly PlayerMobile m_Player;
        private readonly Category m_CurrentCategory;

        // Categorized skill lists, ordered alphabetically by in-game display name
        private static readonly SkillName[] CombatSkills = new SkillName[]
        {
            SkillName.Anatomy,        // Anatomy
            SkillName.Archery,        // Archery
            SkillName.Fencing,        // Fencing
            SkillName.Focus,          // Focus
            SkillName.Healing,        // Healing
            SkillName.Macing,         // Mace Fighting
            SkillName.Parry,          // Parrying
            SkillName.Swords,         // Swordsmanship
            SkillName.Tactics,        // Tactics
            SkillName.Throwing,       // Throwing
            SkillName.Wrestling       // Wrestling
        };

        private static readonly SkillName[] MagicSkills = new SkillName[]
        {
            SkillName.Bushido,        // Bushido
            SkillName.Chivalry,       // Chivalry
            SkillName.EvalInt,        // Evaluating Intelligence
            SkillName.Magery,         // Magery
            SkillName.Meditation,     // Meditation
            SkillName.Mysticism,      // Mysticism
            SkillName.Necromancy,     // Necromancy
            SkillName.Ninjitsu,       // Ninjitsu
            SkillName.MagicResist,    // Resisting Spells
            SkillName.Spellweaving,   // Spellweaving
            SkillName.SpiritSpeak     // Spirit Speak
        };

        private static readonly SkillName[] CraftingSkills = new SkillName[]
        {
            SkillName.Alchemy,        // Alchemy
            SkillName.Blacksmith,     // Blacksmithy
            SkillName.Fletching,      // Bowcraft/Fletching
            SkillName.Carpentry,      // Carpentry
            SkillName.Cooking,        // Cooking
            SkillName.Fishing,        // Fishing
            SkillName.Imbuing,        // Imbuing
            SkillName.Inscribe,       // Inscription
            SkillName.Lumberjacking,  // Lumberjacking
            SkillName.Mining,         // Mining
            SkillName.Tailoring,      // Tailoring
            SkillName.Tinkering       // Tinkering
        };

        private static readonly SkillName[] WildernessSkills = new SkillName[]
        {
            SkillName.AnimalLore,     // Animal Lore
            SkillName.AnimalTaming,   // Animal Taming
            SkillName.Camping,        // Camping
            SkillName.Discordance,    // Discordance
            SkillName.Herding,        // Herding
            SkillName.Musicianship,   // Musicianship
            SkillName.Peacemaking,    // Peacemaking
            SkillName.Provocation,    // Provocation
            SkillName.Tracking,       // Tracking
            SkillName.Veterinary      // Veterinary
        };

        private static readonly SkillName[] UtilitySkills = new SkillName[]
        {
            SkillName.ArmsLore,       // Arms Lore
            SkillName.Begging,        // Begging
            SkillName.Cartography,    // Cartography
            SkillName.DetectHidden,   // Detecting Hidden
            SkillName.Forensics,      // Forensic Evaluation
            SkillName.Hiding,         // Hiding
            SkillName.ItemID,         // Item Identification
            SkillName.Lockpicking,    // Lockpicking
            SkillName.Poisoning,      // Poisoning
            SkillName.RemoveTrap,     // Remove Trap
            SkillName.Snooping,       // Snooping
            SkillName.Stealing,       // Stealing
            SkillName.Stealth,        // Stealth
            SkillName.TasteID         // Taste Identification
        };

        static LegendarySkillGump()
        {
            SortSkills(CombatSkills);
            SortSkills(MagicSkills);
            SortSkills(CraftingSkills);
            SortSkills(WildernessSkills);
            SortSkills(UtilitySkills);
        }

        private static void SortSkills(SkillName[] skills)
        {
            Array.Sort(skills, (a, b) =>
            {
                string aName = SkillInfo.Table[(int)a]?.Name ?? a.ToString();
                string bName = SkillInfo.Table[(int)b]?.Name ?? b.ToString();
                return string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
            });
        }

        public LegendarySkillGump(PlayerMobile player, Category category = Category.Combat) : base(60, 50)
        {
            m_Player = player;
            m_CurrentCategory = category;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Sleek dark stone window background
            AddBackground(0, 0, 546, 535, 0x2422);
            AddAlphaRegion(10, 10, 526, 515);

            // Title & instructions
            AddHtml(20, 20, 506, 25, "<center><basefont color=#F0D060 size=5>Trial Completed: Choose Your Reward</basefont></center>", false, false);
            AddHtml(20, 45, 506, 30, "<center><basefont color=#CCCCCC>Select a skill to receive a Legendary Master Scroll (+5 Cap Upgrade):</basefont></center>", false, false);

            // Category Tab Buttons
            int tabY = 80;
            AddTab(20, tabY, "Combat", Category.Combat);
            AddTab(122, tabY, "Magic", Category.Magic);
            AddTab(224, tabY, "Crafting", Category.Crafting);
            AddTab(326, tabY, "Wilderness", Category.Wilderness);
            AddTab(428, tabY, "Utility", Category.Utility);

            // Skill List Table Header
            AddImageTiled(25, 115, 496, 2, 0x2711);
            AddLabel(75, 122, 0x481, "Skill");
            AddLabel(285, 122, 0x481, "Current Level / Cap");
            AddLabel(435, 122, 0x481, "Next Tier");
            AddImageTiled(25, 142, 496, 2, 0x2711);

            // Render skills for current tab
            SkillName[] activeSkills = GetCategorySkills(category);
            int rowY = 150;

            for (int i = 0; i < activeSkills.Length; i++)
            {
                SkillName skName = activeSkills[i];
                Skill skill = player.Skills[skName];
                double currentVal = skill != null ? skill.Base : 0.0;
                double currentCap = skill != null ? skill.Cap : 100.0;
                int buttonID = 100 + (int)skName;

                // Selection button (radio / arrow icon)
                AddButton(40, rowY + 2, 0x15E1, 0x15E5, buttonID, GumpButtonType.Reply, 0);

                // Skill Name
                string displayName = skill != null ? skill.Name : skName.ToString();
                AddLabel(75, rowY, 0xFF, displayName);

                // Current Value / Cap
                string levelInfo = $"{currentVal:F1} / {currentCap:F0}";
                AddLabel(295, rowY, 0x35, levelInfo);

                // Next Tier indicator
                if (currentCap >= 120.0)
                {
                    AddLabel(440, rowY, 0x22, "MAX (120)");
                }
                else
                {
                    double nextCap = Math.Min(120.0, (Math.Floor(currentCap / 5.0) + 1.0) * 5.0);
                    AddLabel(445, rowY, 0x44, $"+5 -> {nextCap:F0}");
                }

                rowY += 24;
            }

            // Footer note
            AddHtml(25, 498, 496, 25, "<center><basefont color=#999999 size=2>Scrolls are Blessed and may be used by any character on your account.</basefont></center>", false, false);
        }

        private void AddTab(int x, int y, string label, Category cat)
        {
            bool isSelected = (m_CurrentCategory == cat);
            int buttonID = 10 + (int)cat;

            // Highlight selected tab with active border / button graphic
            if (isSelected)
            {
                AddImageTiled(x - 2, y - 2, 98, 26, 0x0A3C);
                AddButton(x, y, 0x0FA5, 0x0FA7, buttonID, GumpButtonType.Reply, 0);
                AddLabel(x + 20, y + 2, 0x35, label);
            }
            else
            {
                AddButton(x, y, 0x0FA5, 0x0FA7, buttonID, GumpButtonType.Reply, 0);
                AddLabel(x + 20, y + 2, 0x7E1, label);
            }
        }

        private static SkillName[] GetCategorySkills(Category cat)
        {
            switch (cat)
            {
                case Category.Combat:
                    return CombatSkills;
                case Category.Magic:
                    return MagicSkills;
                case Category.Crafting:
                    return CraftingSkills;
                case Category.Wilderness:
                    return WildernessSkills;
                case Category.Utility:
                default:
                    return UtilitySkills;
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            PlayerMobile pm = sender.Mobile as PlayerMobile;
            if (pm == null)
                return;

            int id = info.ButtonID;

            if (id == 0) // Closed / Cancelled
            {
                pm.SendMessage(53, "You can claim your reward at any time by speaking to or double-clicking the Legendary Master.");
                return;
            }

            // Tab navigation
            if (id >= 10 && id < 20)
            {
                Category newCat = (Category)(id - 10);
                pm.SendGump(new LegendarySkillGump(pm, newCat));
                return;
            }

            // Skill selection (ButtonID: 100 + SkillName)
            if (id >= 100)
            {
                SkillName chosen = (SkillName)(id - 100);

                if (!LegendaryMaster.ClaimReward(pm, chosen))
                {
                    pm.SendMessage(38, "You do not have a completed trial reward waiting.");
                }
            }
        }
    }
}
