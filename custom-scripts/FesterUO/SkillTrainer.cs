// ============================================================================
// SkillTrainer.cs - Fundamental Skill Trainer NPC Mobile & Interactive Gump
//
// Origin: FesterUO Custom Scripts (servuo/custom-scripts/FesterUO/)
// Purpose: An NPC mentor placed by GMs that allows players to train fundamental
//          skills under 40.0 up to 40.0 for a flat fee of 1,000 gold each.
//
// Licensed under the GNU General Public License v3.0 (GPL-3.0)
//
// Features:
// - Placed via [add SkillTrainer; defaults to frozen, blessed, and invulnerable.
// - Interaction: Double-clicking, speech keywords ("train", "skill", "learn",
//   "teach", "help"), or right-click context menu ("Talk") opens the gump.
// - Interactive Gump: Displays all player skills currently below 40.0, sorted
//   alphabetically with current levels, target level (40.0), and gold cost.
// - Multi-page pagination (10 skills per page) handles new characters with
//   dozens of untrained skills cleanly.
// - Gold payment: Consumes gold from character backpack first, then draws from
//   bank balance if configured (Config/FesterUO/SkillTrainer.cfg).
// - Skill Cap Management: Respects the character's total skill cap; lowers
//   skills flagged with SkillLock.Down if extra room is required.
// - Instant Refresh: Automatically updates the gump upon training each skill,
//   enabling quick sequential training.
// ============================================================================

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.FesterUO
{
    public class SkillTrainer : BaseCreature
    {
        public override bool CanTeach => false;
        public override bool IsInvulnerable => true;
        public override bool ClickTitle => true;
        public override bool ShowFameTitle => false;

        [Constructable]
        public SkillTrainer() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Title = Config.Get("SkillTrainer.TrainerTitle", "the Skill Trainer");
            Name = "Novice Trainer";

            InitStats(100, 100, 100);

            Female = Utility.RandomBool();
            Body = Female ? 0x191 : 0x190;
            Hue = Race.RandomSkinHue();

            SpeechHue = 53;
            Frozen = true;
            CantWalk = true;
            Blessed = true;
            Direction = Direction.South;

            InitOutfit();
        }

        public SkillTrainer(Serial serial) : base(serial)
        {
        }

        public virtual void InitOutfit()
        {
            int hairHue = Utility.RandomHairHue();
            Utility.AssignRandomHair(this, hairHue);
            if (!Female)
            {
                Utility.AssignRandomFacialHair(this, hairHue);
            }

            int robeHue = Utility.RandomList(0x482, 0x47E, 0x59, 0x21, 0x53);
            SetWearable(new Robe(robeHue));
            SetWearable(new Sandals(0x482));
            SetWearable(new WizardsHat(robeHue));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            int range = Config.Get("SkillTrainer.InteractionRange", 4);

            if (e.Mobile is PlayerMobile pm && pm.InRange(Location, range))
            {
                string text = e.Speech.Trim();
                if (text.IndexOf("train", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("skill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("learn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("teach", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("help", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    OpenTrainerGump(pm);
                    double targetLevel = Config.Get("SkillTrainer.TargetSkillLevel", 40.0);
                    int goldCost = Config.Get("SkillTrainer.GoldCost", 1000);
                    SayTo(pm, $"Greetings! I can train any of your fundamental skills up to {targetLevel:F1} for {goldCost:#,0} gold.");
                    e.Handled = true;
                }
            }

            base.OnSpeech(e);
        }

        public override void OnDoubleClick(Mobile from)
        {
            int range = Config.Get("SkillTrainer.InteractionRange", 4);

            if (from is PlayerMobile pm && pm.InRange(Location, range))
            {
                OpenTrainerGump(pm);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && from is PlayerMobile pm && pm.InRange(Location, Config.Get("SkillTrainer.InteractionRange", 4)))
            {
                list.Add(new SimpleContextMenuEntry(pm, 6146, m => OpenTrainerGump(pm))); // Talk
            }
        }

        public void OpenTrainerGump(PlayerMobile pm, int page = 0)
        {
            if (pm == null || pm.Deleted || !pm.Alive)
                return;

            pm.CloseGump(typeof(SkillTrainerGump));
            pm.SendGump(new SkillTrainerGump(pm, this, page));
        }

        public void TrainSkill(PlayerMobile pm, SkillName skillName, int returnPage)
        {
            if (pm == null || pm.Deleted || !pm.Alive)
                return;

            int range = Config.Get("SkillTrainer.InteractionRange", 4);
            if (!pm.InRange(Location, range))
            {
                pm.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            Skill skill = pm.Skills[skillName];
            if (skill == null)
                return;

            double targetLevel = Config.Get("SkillTrainer.TargetSkillLevel", 40.0);
            int goldCost = Config.Get("SkillTrainer.GoldCost", 1000);
            bool allowBank = Config.Get("SkillTrainer.AllowBankGold", true);

            if (skill.Base >= targetLevel)
            {
                pm.SendMessage(38, $"Your {skill.Name} is already at or above {targetLevel:F1}.");
                OpenTrainerGump(pm, returnPage);
                return;
            }

            double effectiveCap = Math.Min(targetLevel, skill.Cap);
            int targetFixed = (int)Math.Round(effectiveCap * 10.0);
            int pointsNeeded = targetFixed - skill.BaseFixedPoint;

            if (pointsNeeded <= 0)
            {
                pm.SendMessage(38, $"Your {skill.Name} cannot be trained any higher right now.");
                OpenTrainerGump(pm, returnPage);
                return;
            }

            // Check skill points / skill cap limits
            int freePoints = pm.Skills.Cap - pm.Skills.Total;
            if (freePoints < 0)
                freePoints = 0;

            int freeablePoints = 0;
            for (int i = 0; i < pm.Skills.Length; ++i)
            {
                Skill s = pm.Skills[i];
                if (s != skill && s.Lock == SkillLock.Down)
                {
                    freeablePoints += s.BaseFixedPoint;
                }
            }

            if ((freePoints + freeablePoints) < pointsNeeded)
            {
                pm.SendMessage(38, $"You need {pointsNeeded / 10.0:F1} skill points, but only {(freePoints + freeablePoints) / 10.0:F1} points are available under your total skill cap ({pm.Skills.Total / 10.0:F1}/{pm.Skills.Cap / 10.0:F1}). Please set unused skills to 'Down' in your skill list to free up points.");
                OpenTrainerGump(pm, returnPage);
                return;
            }

            // Check gold funds
            int totalGold = 0;
            if (pm.Backpack != null)
                totalGold += pm.Backpack.GetAmount(typeof(Gold));

            if (allowBank)
                totalGold += Banker.GetBalance(pm);

            if (goldCost > 0 && totalGold < goldCost)
            {
                pm.SendMessage(38, $"You cannot afford this training. It costs {goldCost:#,0} gold, but you only have {totalGold:#,0} gold.");
                OpenTrainerGump(pm, returnPage);
                return;
            }

            // Deduct gold
            if (goldCost > 0)
            {
                int leftCost = goldCost;
                if (pm.Backpack != null)
                    leftCost -= pm.Backpack.ConsumeUpTo(typeof(Gold), leftCost);

                if (leftCost > 0 && allowBank)
                    Banker.Withdraw(pm, leftCost);
            }

            // Free points from Down skills if needed
            int needToFree = pointsNeeded - freePoints;
            for (int i = 0; needToFree > 0 && i < pm.Skills.Length; ++i)
            {
                Skill s = pm.Skills[i];
                if (s == skill || s.Lock != SkillLock.Down)
                    continue;

                if (s.BaseFixedPoint <= needToFree)
                {
                    needToFree -= s.BaseFixedPoint;
                    s.BaseFixedPoint = 0;
                }
                else
                {
                    s.BaseFixedPoint -= needToFree;
                    needToFree = 0;
                }
            }

            // Apply trained skill
            skill.BaseFixedPoint = targetFixed;
            if (skill.Lock == SkillLock.Down)
            {
                skill.SetLockNoRelay(SkillLock.Up);
            }

            // Effects & Feedback
            pm.PlaySound(0x247);
            pm.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);

            pm.SendMessage(68, $"You have trained {skill.Name} to {skill.Base:F1} for {goldCost:#,0} gold.");
            SayTo(pm, $"I have trained thee in the ways of {skill.Name}!");

            OpenTrainerGump(pm, returnPage);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            _ = reader.ReadInt();
        }
    }

    public class SkillTrainerGump : Gump
    {
        private const int PageSize = 10;

        private readonly PlayerMobile m_Player;
        private readonly SkillTrainer m_Trainer;
        private readonly int m_Page;

        public SkillTrainerGump(PlayerMobile player, SkillTrainer trainer, int page = 0) : base(60, 50)
        {
            m_Player = player;
            m_Trainer = trainer;
            m_Page = Math.Max(0, page);

            player.CloseGump(typeof(SkillTrainerGump));

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            double targetLevel = Config.Get("SkillTrainer.TargetSkillLevel", 40.0);
            int goldCost = Config.Get("SkillTrainer.GoldCost", 1000);
            bool allowBank = Config.Get("SkillTrainer.AllowBankGold", true);

            // Collect all player skills that are below targetLevel
            var trainableSkills = new List<Skill>();
            for (int i = 0; i < player.Skills.Length; ++i)
            {
                Skill s = player.Skills[i];
                if (s != null && s.Base < targetLevel)
                {
                    trainableSkills.Add(s);
                }
            }

            // Sort alphabetically by localized/display skill name
            trainableSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            int totalCount = trainableSkills.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            if (m_Page >= totalPages)
            {
                m_Page = totalPages - 1;
            }

            int totalGold = 0;
            if (player.Backpack != null)
                totalGold += player.Backpack.GetAmount(typeof(Gold));
            if (allowBank)
                totalGold += Banker.GetBalance(player);

            AddPage(0);

            // Textured dark slate window background
            AddBackground(0, 0, 530, 510, 9200);
            AddImageTiled(10, 10, 510, 490, 2624);
            AddAlphaRegion(10, 10, 510, 490);

            // Title & instructions
            AddHtml(20, 18, 490, 25, "<center><basefont color=#F0D060 size=5>Fundamental Skill Trainer</basefont></center>", false, false);
            AddHtml(20, 44, 490, 20, $"<center><basefont color=#CCCCCC>Train any skill below {targetLevel:F1} up to {targetLevel:F1} for {goldCost:#,0} gold</basefont></center>", false, false);

            // Player Status Bar (Gold Available & Skill Total/Cap)
            AddImageTiled(25, 68, 480, 2, 9354);
            string goldInfo = $"<basefont color=#FFD700>Gold: </basefont><basefont color=#FFFFFF>{totalGold:#,0} gp</basefont>";
            string skillInfo = $"<basefont color=#87CEEB>Skills: </basefont><basefont color=#FFFFFF>{player.Skills.Total / 10.0:F1} / {player.Skills.Cap / 10.0:F1}</basefont>";
            AddHtml(30, 74, 230, 20, goldInfo, false, false);
            AddHtml(280, 74, 220, 20, $"<div align=right>{skillInfo}</div>", false, false);
            AddImageTiled(25, 96, 480, 2, 9354);

            if (totalCount == 0)
            {
                AddHtml(40, 190, 450, 100,
                    $"<center><basefont color=#00FF7F size=5>All Skills Mastered!</basefont><br><br>" +
                    $"<basefont color=#CCCCCC>All of your skills are currently at or above {targetLevel:F1}.<br>" +
                    $"You have mastered all fundamental training available from this mentor.</basefont></center>",
                    false, false);

                AddButton(225, 460, 4017, 4019, 0, GumpButtonType.Reply, 0); // Close
                AddLabel(260, 460, 1152, "Close");
                return;
            }

            // Table Column Headers
            AddLabel(30, 104, 0x481, "Train");
            AddLabel(80, 104, 0x481, "Skill");
            AddLabel(265, 104, 0x481, "Current");
            AddLabel(350, 104, 0x481, "Target");
            AddLabel(435, 104, 0x481, "Cost");
            AddImageTiled(25, 126, 480, 2, 9354);

            // Render skills for current page
            int startIndex = m_Page * PageSize;
            int countOnPage = Math.Min(PageSize, totalCount - startIndex);
            int rowY = 134;

            for (int i = 0; i < countOnPage; ++i)
            {
                Skill sk = trainableSkills[startIndex + i];
                int buttonID = 100 + (int)sk.SkillName;

                // Train button (Jewel/arrow)
                AddButton(30, rowY, 4005, 4007, buttonID, GumpButtonType.Reply, 0);

                // Skill Name
                AddLabel(80, rowY, 0xFF, sk.Name);

                // Current Value
                AddLabel(265, rowY, 0x35, $"{sk.Base:F1}");

                // Target Level
                AddLabel(350, rowY, 0x44, $"{targetLevel:F1}");

                // Cost
                AddLabel(435, rowY, 0x35, $"{goldCost:#,0} gp");

                rowY += 28;
            }

            // Bottom Separator
            AddImageTiled(25, 420, 480, 2, 9354);

            // Pagination Controls
            if (m_Page > 0)
            {
                AddButton(30, 432, 4014, 4016, 1, GumpButtonType.Reply, 0); // Prev
                AddLabel(65, 432, 1152, "Previous");
            }

            AddHtml(165, 434, 200, 20, $"<center><basefont color=#BBBBBB>Page {m_Page + 1} of {totalPages} ({totalCount} skills)</basefont></center>", false, false);

            if (m_Page + 1 < totalPages)
            {
                AddButton(465, 432, 4005, 4007, 2, GumpButtonType.Reply, 0); // Next
                AddLabel(430, 432, 1152, "Next");
            }

            // Close Button
            AddButton(225, 465, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddLabel(260, 465, 1152, "Close");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!(sender.Mobile is PlayerMobile pm) || pm.Deleted || !pm.Alive)
                return;

            if (m_Trainer == null || m_Trainer.Deleted)
                return;

            int id = info.ButtonID;

            if (id == 0) // Close
            {
                return;
            }

            if (id == 1) // Previous Page
            {
                pm.SendGump(new SkillTrainerGump(pm, m_Trainer, m_Page - 1));
                return;
            }

            if (id == 2) // Next Page
            {
                pm.SendGump(new SkillTrainerGump(pm, m_Trainer, m_Page + 1));
                return;
            }

            if (id >= 100) // Train skill
            {
                SkillName chosen = (SkillName)(id - 100);
                m_Trainer.TrainSkill(pm, chosen, m_Page);
            }
        }
    }
}
