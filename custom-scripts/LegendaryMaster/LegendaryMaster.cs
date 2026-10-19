/*
 * UO Community Script: Legendary Master of Skills
 * Original Author: Keith (Resource #2244)
 * Source: https://www.servuo.dev/archive/legendary-master-of-skills.2244/
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * Provides an NPC questmaster that offers combat tasks to Grandmaster (100.0+)
 * players seeking PowerScrolls without running Champion Spawns.
 *
 * Enhancements & Fixes:
 * - Fixed player kill credit: Direct player weapon/spell kills now properly count toward tasks (previously only pet/summon kills counted).
 * - Fixed fatal ArgumentOutOfRangeException: Removed invalid taskInfos[pm.Serial] list indexing on speech.
 * - Reward Gump & Selection: Tasks no longer require an incoming skill request; completing a trial presents an interactive gump (LegendarySkillGump) to choose the desired skill reward.
 * - Dynamic +5 Master PowerScrolls: Awards a LegendaryPowerScroll that dynamically increases the user's skill cap to the next ceiling limit (105, 110, 115, 120) for any alt on the account, requiring 100+ skill without error or scroll consumption if unqualified.
 * - Double-click interaction: Supports double-clicking the NPC as well as speaking "give task" / "task" / "quest".
 * - Fixed countdown display: Formatted remaining time in human-readable integer minutes instead of raw DateTime timestamps.
 * - Fixed creature name display: Displays clean creature names (e.g. "Shadow Wyrm", "Balron") instead of C# class Type names.
 * - Fixed creature selection off-by-one: Restored White Wyrm to the random selection pool.
 * - State persistence: Added WorldSave / WorldLoad persistence so active player tasks and unclaimed completed trials survive server restarts.
 * - Externalized configuration: Time limits and bonus scroll chances are parameterized in servuo/Config/LegendaryMaster/LegendaryMaster.cfg.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Misc
{
    public class LegendaryMaster : BaseCreature
    {
        private static readonly string PersistencePath = Path.Combine("Saves", "LegendaryMaster", "Persistence.bin");
        private static List<SkillTaskInfo> m_TaskInfos = new List<SkillTaskInfo>();

        private static List<Type> m_CreatureList;

        public static List<Type> CreatureList
        {
            get
            {
                if (m_CreatureList == null || m_CreatureList.Count == 0)
                {
                    LoadCreatures();
                }

                return m_CreatureList;
            }
        }

        public static void LoadCreatures()
        {
            string defaultList = "Balron, ShadowWyrm, AncientLich, AncientWyrm, SkeletalDragon, GreaterDragon, Succubus, RottingCorpse, BloodElemental, PoisonElemental, SerpentineDragon, BoneDemon, RuneBeetle, Yamandon, WhiteWyrm";
            string configStr = Config.Get("LegendaryMaster.Creatures", defaultList);

            var list = new List<Type>();
            string[] names = configStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawName in names)
            {
                string name = rawName.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                Type t = ScriptCompiler.FindTypeByName(name);
                if (t != null && typeof(BaseCreature).IsAssignableFrom(t))
                {
                    if (!list.Contains(t))
                        list.Add(t);
                }
                else
                {
                    Console.WriteLine($"[LegendaryMaster]: Warning - creature type '{name}' could not be resolved as a valid BaseCreature.");
                }
            }

            if (list.Count == 0)
            {
                list.AddRange(new[]
                {
                    typeof(Balron),
                    typeof(ShadowWyrm),
                    typeof(AncientLich),
                    typeof(AncientWyrm),
                    typeof(SkeletalDragon),
                    typeof(WhiteWyrm)
                });
            }

            m_CreatureList = list;
        }

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static void Initialize()
        {
            EventSink.CreatureDeath += EventSink_CreatureDeath;
            EventSink.SkillCapChange += EventSink_SkillCapChange;
            LoadCreatures();
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                PersistencePath,
                writer =>
                {
                    writer.Write(1); // version

                    // Clean expired tasks (keep completed tasks waiting for reward selection!)
                    m_TaskInfos.RemoveAll(t => !t.Completed && t.TimeLimit <= DateTime.UtcNow);

                    writer.Write(m_TaskInfos.Count);

                    foreach (var task in m_TaskInfos)
                    {
                        writer.Write((int)task.PlayerSerial);
                        writer.Write(task.TargetType != null ? task.TargetType.FullName : string.Empty);
                        writer.Write(task.TimeLimit);
                        writer.Write(task.TaskAmount);
                        writer.Write(task.Completed);
                    }
                });
        }

        private static void OnLoad()
        {
            Persistence.Deserialize(
                PersistencePath,
                reader =>
                {
                    int version = reader.ReadInt();
                    int count = reader.ReadInt();

                    m_TaskInfos = new List<SkillTaskInfo>(count);

                    for (int i = 0; i < count; i++)
                    {
                        if (version >= 1)
                        {
                            Serial serial = reader.ReadInt();
                            string typeName = reader.ReadString();
                            DateTime timeLimit = reader.ReadDateTime();
                            int amount = reader.ReadInt();
                            bool completed = reader.ReadBool();

                            Type targetType = ScriptCompiler.FindTypeByFullName(typeName);

                            if (completed || (targetType != null && timeLimit > DateTime.UtcNow))
                            {
                                m_TaskInfos.Add(new SkillTaskInfo
                                {
                                    PlayerSerial = serial,
                                    TargetType = targetType,
                                    TimeLimit = timeLimit,
                                    TaskAmount = amount,
                                    Completed = completed
                                });
                            }
                        }
                        else
                        {
                            Serial serial = reader.ReadInt();
                            SkillName skill = (SkillName)reader.ReadInt();
                            double cap = reader.ReadDouble();
                            string typeName = reader.ReadString();
                            DateTime timeLimit = reader.ReadDateTime();
                            int amount = reader.ReadInt();

                            Type targetType = ScriptCompiler.FindTypeByFullName(typeName);

                            if (targetType != null && timeLimit > DateTime.UtcNow)
                            {
                                m_TaskInfos.Add(new SkillTaskInfo
                                {
                                    PlayerSerial = serial,
                                    TargetType = targetType,
                                    TimeLimit = timeLimit,
                                    TaskAmount = amount,
                                    Completed = false
                                });
                            }
                        }
                    }
                });
        }

        private static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer == null || e.Creature == null)
                return;

            if (!CreatureList.Contains(e.Creature.GetType()))
                return;

            if (m_TaskInfos == null || m_TaskInfos.Count == 0)
                return;

            PlayerMobile pm = null;

            if (e.Killer is PlayerMobile player)
            {
                pm = player;
            }
            else if (e.Killer is BaseCreature bc)
            {
                if (bc.Controlled && bc.ControlMaster is PlayerMobile cm)
                    pm = cm;
                else if (bc.Summoned && bc.SummonMaster is PlayerMobile sm)
                    pm = sm;
            }

            if (pm == null)
                return;

            SkillTaskInfo currentTask = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial && !t.Completed);

            if (currentTask == null)
                return;

            if (DateTime.UtcNow > currentTask.TimeLimit)
            {
                pm.SendMessage(53, "You ran out of time for your skill task!");
                m_TaskInfos.Remove(currentTask);
                return;
            }

            if (currentTask.TargetType == e.Creature.GetType())
            {
                currentTask.TaskAmount--;

                int extraMinutes = Config.Get("LegendaryMaster.ExtraTimeMinutes", 10);
                currentTask.TimeLimit += TimeSpan.FromMinutes(extraMinutes);

                if (currentTask.TaskAmount <= 0)
                {
                    currentTask.Completed = true;

                    pm.SendMessage(53, "Congratulations! You have completed your combat trial! Choose your skill reward.");
                    pm.CloseGump(typeof(LegendarySkillGump));
                    pm.SendGump(new LegendarySkillGump(pm));

                    TryGiveStatScroll(pm);
                }
                else
                {
                    currentTask.TargetType = GetRandomCreature();
                    int remainingMinutes = Math.Max(1, (int)(currentTask.TimeLimit - DateTime.UtcNow).TotalMinutes);
                    pm.SendMessage(53, $"Target eliminated! {currentTask.TaskAmount} left to kill. Next target: {FormatCreatureName(currentTask.TargetType)} ({remainingMinutes} minutes remaining).");
                }
            }
        }

        private static void TryGiveStatScroll(PlayerMobile pm)
        {
            if (!Config.Get("LegendaryMaster.EnableStatScrolls", true))
                return;

            double statChance = Config.Get("LegendaryMaster.StatScrollChance", 0.01);

            if (Utility.RandomDouble() < statChance)
            {
                double highChance = Config.Get("LegendaryMaster.HighStatScrollChance", 0.01);
                int maxBonus = Config.Get("LegendaryMaster.MaxStatScrollBonus", 25);
                int bonus = (Utility.RandomDouble() < highChance) ? Math.Min(maxBonus, RandomStatScrollLevel()) : Math.Min(maxBonus, 5);

                pm.AddToBackpack(new StatCapScroll(pm.StatCap + bonus));
                pm.SendLocalizedMessage(1049524); // You have received a scroll of power!
                pm.SendMessage(53, $"Bonus reward: You have received a +{bonus} Stat Cap Scroll!");
            }
        }

        private static int RandomStatScrollLevel()
        {
            double random = Utility.RandomDouble();

            if (random <= 0.10)
                return 25;
            if (random <= 0.25)
                return 20;
            if (random <= 0.45)
                return 15;
            if (random <= 0.70)
                return 10;

            return 5;
        }

        private static Type GetRandomCreature()
        {
            return CreatureList[Utility.Random(CreatureList.Count)];
        }

        public static string FormatCreatureName(Type type)
        {
            if (type == null)
                return "Unknown";

            return Regex.Replace(type.Name, "(\\B[A-Z])", " $1");
        }

        public override bool IsInvulnerable => true;

        [Constructable]
        public LegendaryMaster() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "Legendary Master of Skills";
            Title = "the Master of Skills";

            Body = 0x190;
            Hue = Race.RandomSkinHue();

            HairItemID = 0x203C;
            HairHue = 0x481;

            FacialHairItemID = 0x203E;
            FacialHairHue = 0x481;

            InitStats(150, 150, 150);

            SetWearable(new HoodedShroudOfShadows(), 0x481);
            SetWearable(new Sandals(), 0x481);

            Blessed = true;
            CantWalk = true;
            SpeechHue = 53;
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            int range = Config.Get("LegendaryMaster.InteractionRange", 5);

            if (from is PlayerMobile pm && pm.InRange(Location, range))
            {
                HandleTaskInteraction(pm);
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            int range = Config.Get("LegendaryMaster.InteractionRange", 5);

            if (e.Mobile is PlayerMobile pm && pm.InRange(Location, range))
            {
                string speech = e.Speech.Trim();

                if (speech.StartsWith("give task", StringComparison.OrdinalIgnoreCase) ||
                    speech.Equals("task", StringComparison.OrdinalIgnoreCase) ||
                    speech.Equals("quest", StringComparison.OrdinalIgnoreCase))
                {
                    HandleTaskInteraction(pm);
                }
                else if (speech.StartsWith("remove task", StringComparison.OrdinalIgnoreCase) ||
                         speech.StartsWith("cancel task", StringComparison.OrdinalIgnoreCase))
                {
                    var task = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial);
                    if (task != null)
                    {
                        m_TaskInfos.Remove(task);
                        SayTo(pm, "Your task has been cancelled. You are free to undertake a new quest at any time.");
                    }
                    else
                    {
                        SayTo(pm, "You do not currently have any active tasks.");
                    }
                }
                else if (Utility.RandomDouble() < Config.Get("LegendaryMaster.IdleBarkChance", 0.15))
                {
                    SayTo(pm, "Looking to expand your skills beyond mortal limits? Speak 'give task' or double-click me to undertake a combat trial for a Master Power Scroll!");
                }
            }

            base.OnSpeech(e);
        }

        private void HandleTaskInteraction(PlayerMobile pm)
        {
            if (pm.IsStaff())
            {
                SayTo(pm, "I am on duty, waiting for mortal adventurers to assist!");
                return;
            }

            var existing = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial);
            if (existing != null)
            {
                if (existing.Completed)
                {
                    SayTo(pm, "Your trial is complete! Select which skill you wish to reward.");
                    pm.CloseGump(typeof(LegendarySkillGump));
                    pm.SendGump(new LegendarySkillGump(pm));
                    return;
                }

                if (DateTime.UtcNow > existing.TimeLimit)
                {
                    m_TaskInfos.Remove(existing);
                    SayTo(pm, "Your previous task expired. You may now undertake a new quest.");
                }
                else
                {
                    int remaining = Math.Max(1, (int)(existing.TimeLimit - DateTime.UtcNow).TotalMinutes);
                    SayTo(pm, $"You already have an active task! Kill {existing.TaskAmount} {FormatCreatureName(existing.TargetType)} ({remaining} minutes left).");
                    return;
                }
            }

            int minKills = Config.Get("LegendaryMaster.BaseMinKills", 1);
            int maxKills = Math.Max(minKills, Config.Get("LegendaryMaster.BaseMaxKills", 1));
            int killCount = Utility.RandomMinMax(minKills, maxKills);

            AssignTask(pm, GetRandomCreature(), killCount);
        }

        private void AssignTask(PlayerMobile pm, Type target, int amount)
        {
            int startMinutes = Config.Get("LegendaryMaster.TaskTimeMinutes", 180);

            var info = new SkillTaskInfo
            {
                PlayerSerial = pm.Serial,
                TargetType = target,
                TimeLimit = DateTime.UtcNow + TimeSpan.FromMinutes(startMinutes),
                TaskAmount = amount,
                Completed = false
            };

            m_TaskInfos.Add(info);

            Effects.SendBoltEffect(pm, true);
            SayTo(pm, $"Task assigned! Slay {amount} {FormatCreatureName(target)} within {startMinutes} minutes to earn a Legendary Master Power Scroll (+5 Upgrade) of your choice!");
        }

        public static bool ClaimReward(PlayerMobile pm, SkillName skill)
        {
            var task = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial && t.Completed);
            if (task == null)
                return false;

            m_TaskInfos.Remove(task);

            var scroll = new LegendaryPowerScroll(skill);
            if (!pm.PlaceInBackpack(scroll))
            {
                pm.BankBox.DropItem(scroll);
                pm.SendMessage(53, $"Your backpack was full! The Legendary Master Scroll of {scroll.GetName()} was placed into your bank box.");
            }
            else
            {
                pm.SendMessage(53, $"You have received a Legendary Master Scroll of {scroll.GetName()}!");
            }

            return true;
        }

        private static void EventSink_SkillCapChange(SkillCapChangeEventArgs e)
        {
            if (!Config.Get("LegendaryMaster.AutoAdvanceSkillOnUse", true))
                return;

            if (e.Mobile is PlayerMobile pm && e.Skill != null)
            {
                if (e.NewCap > e.OldCap && e.Skill.Base < e.NewCap)
                {
                    double targetVal = e.NewCap;

                    // Make room under total skill cap if needed by lowering skills set to down
                    int toGain = (int)Math.Round(targetVal * 10.0) - e.Skill.BaseFixedPoint;
                    if (toGain > 0 && pm.Skills.Total + toGain > pm.Skills.Cap)
                    {
                        int needed = (pm.Skills.Total + toGain) - pm.Skills.Cap;
                        for (int i = 0; i < pm.Skills.Length && needed > 0; i++)
                        {
                            Skill other = pm.Skills[i];
                            if (other != e.Skill && other.Lock == SkillLock.Down && other.BaseFixedPoint > 0)
                            {
                                int drop = Math.Min(needed, other.BaseFixedPoint);
                                other.BaseFixedPoint -= drop;
                                needed -= drop;
                            }
                        }
                    }

                    e.Skill.Base = targetVal;
                    pm.SendMessage(68, $"Your {e.Skill.Name} skill has advanced to {targetVal:F1}!");
                }
            }
        }

        public LegendaryMaster(Serial serial) : base(serial)
        {
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

        private class SkillTaskInfo
        {
            public Serial PlayerSerial;
            public Type TargetType;
            public DateTime TimeLimit;
            public bool Completed;
            public int TaskAmount;
        }
    }
}
