#region References
using System;
using System.Collections.Generic;
using System.IO;
using Server.Commands;
using Server.Mobiles;
#endregion

namespace Server.Custom
{
    public static class SlowSkillGrind
    {
        private static readonly string PersistencePath = Path.Combine("Saves", "SlowSkillGrind", "Persistence.bin");

        // PlayerSerial -> (SkillName -> LastGainUtc)
        private static Dictionary<Serial, Dictionary<SkillName, DateTime>> m_Gains =
            new Dictionary<Serial, Dictionary<SkillName, DateTime>>();

        public static void Initialize()
        {
            EventSink.SkillCheck += OnSkillCheck;
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;

            CommandSystem.Register("SlowGrind", AccessLevel.Player, SlowGrind_OnCommand);
            CommandSystem.Register("SlowGrindInfo", AccessLevel.Player, SlowGrind_OnCommand);
        }

        #region Config Properties
        public static bool Enabled => Config.Get("SlowSkillGrind.Enabled", true);
        public static double MinSkillRequired => Config.Get("SlowSkillGrind.MinSkillRequired", 100.0);
        public static double MaxSkillCap => Config.Get("SlowSkillGrind.MaxSkillCap", 120.0);

        public static double Tier1Hours => Config.Get("SlowSkillGrind.Tier1Hours", 6.0);
        public static double Tier2Hours => Config.Get("SlowSkillGrind.Tier2Hours", 12.0);
        public static double Tier3Hours => Config.Get("SlowSkillGrind.Tier3Hours", 18.0);
        public static double Tier4Hours => Config.Get("SlowSkillGrind.Tier4Hours", 24.0);

        public static bool NotifyPlayer => Config.Get("SlowSkillGrind.NotifyPlayer", true);
        #endregion

        public static TimeSpan GetCooldownForSkill(Skill skill)
        {
            double hours;
            if (skill.Base < 105.0)
                hours = Tier1Hours;
            else if (skill.Base < 110.0)
                hours = Tier2Hours;
            else if (skill.Base < 115.0)
                hours = Tier3Hours;
            else
                hours = Tier4Hours;

            // 50 gains of 0.1 skill points per 5.0 tier
            double secondsPerGain = Math.Max(1.0, (hours * 3600.0) / 50.0);
            return TimeSpan.FromSeconds(secondsPerGain);
        }

        public static int GetCurrentTier(Skill skill)
        {
            if (skill.Base < 105.0) return 1;
            if (skill.Base < 110.0) return 2;
            if (skill.Base < 115.0) return 3;
            return 4;
        }

        public static double GetTierHours(int tier)
        {
            switch (tier)
            {
                case 1: return Tier1Hours;
                case 2: return Tier2Hours;
                case 3: return Tier3Hours;
                default: return Tier4Hours;
            }
        }

        private static void OnSkillCheck(SkillCheckEventArgs e)
        {
            if (!Enabled)
                return;

            PlayerMobile pm = e.From as PlayerMobile;
            if (pm == null || !pm.Alive)
                return;

            Skill skill = e.Skill;
            if (skill == null || skill.Lock != SkillLock.Up)
                return;

            // Must be at or above GM and below the ultimate 120 cap
            if (skill.Base < MinSkillRequired || skill.Base >= MaxSkillCap)
                return;

            // If the player has room under their current cap (e.g. consumed a PowerScroll),
            // standard ServUO skill training handles gains at normal speed
            if (skill.Base < skill.Cap)
                return;

            if (skill.Cap >= MaxSkillCap)
                return;

            // Check total skill cap capacity
            if (pm.Skills.Total + 1 > pm.Skills.Cap)
            {
                Skill downSkill = FindDownSkill(pm, skill);
                if (downSkill == null)
                    return; // No skill set to decrease; player cannot gain
            }

            TimeSpan cooldown = GetCooldownForSkill(skill);

            if (!m_Gains.TryGetValue(pm.Serial, out var playerGains))
            {
                playerGains = new Dictionary<SkillName, DateTime>();
                m_Gains[pm.Serial] = playerGains;
            }

            if (playerGains.TryGetValue(skill.SkillName, out DateTime lastGain))
            {
                if (DateTime.UtcNow < lastGain + cooldown)
                    return; // Cooldown not yet elapsed
            }

            // Cooldown has elapsed -> grant breakthrough gain!
            playerGains[skill.SkillName] = DateTime.UtcNow;

            // Lower any skill set to down if at total skill cap
            if (pm.Skills.Total + 1 > pm.Skills.Cap)
            {
                Skill downSkill = FindDownSkill(pm, skill);
                if (downSkill != null)
                {
                    downSkill.BaseFixedPoint -= 1;
                }
            }

            // Advance both skill cap and skill level together by 0.1
            skill.CapFixedPoint = Math.Min((int)(MaxSkillCap * 10), skill.CapFixedPoint + 1);
            skill.BaseFixedPoint = Math.Min(skill.CapFixedPoint, skill.BaseFixedPoint + 1);

            if (NotifyPlayer)
            {
                pm.SendMessage(0x59, $"[Mastery] Your persistence in {skill.Name} pushes past mortal limits to {skill.Base:F1}!");
                pm.PlaySound(0x1F2);
                pm.FixedEffect(0x375A, 10, 15);
            }

            EventSink.InvokeSkillGain(new SkillGainEventArgs(pm, skill, 1));
        }

        private static Skill FindDownSkill(PlayerMobile pm, Skill exceptSkill)
        {
            foreach (Skill s in pm.Skills)
            {
                if (s != exceptSkill && s.Lock == SkillLock.Down && s.BaseFixedPoint >= 1)
                    return s;
            }
            return null;
        }

        [Usage("SlowGrind")]
        [Aliases("SlowGrindInfo")]
        [Description("Displays current slow skill grind cooldowns and progress for Grandmaster skills.")]
        private static void SlowGrind_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null)
                return;

            if (!Enabled)
            {
                pm.SendMessage(0x35, "The Slow Skill Grind system is currently disabled.");
                return;
            }

            pm.SendMessage(0x35, "=== Grandmaster Skill Progression (Slow Grind) ===");

            int trackedCount = 0;
            m_Gains.TryGetValue(pm.Serial, out var playerGains);

            foreach (Skill skill in pm.Skills)
            {
                if (skill.Base >= MinSkillRequired)
                {
                    trackedCount++;
                    int tier = GetCurrentTier(skill);
                    double tierHours = GetTierHours(tier);
                    TimeSpan cooldown = GetCooldownForSkill(skill);

                    string status;
                    if (skill.Base >= MaxSkillCap)
                    {
                        status = "PINNACLE (120.0 Achieved)";
                    }
                    else if (skill.Base < skill.Cap)
                    {
                        status = $"PowerScroll Active (Cap: {skill.Cap:F1})";
                    }
                    else if (playerGains != null && playerGains.TryGetValue(skill.SkillName, out DateTime lastGain))
                    {
                        TimeSpan elapsed = DateTime.UtcNow - lastGain;
                        if (elapsed >= cooldown)
                        {
                            status = "Ready for breakthrough gain!";
                        }
                        else
                        {
                            TimeSpan remaining = cooldown - elapsed;
                            status = $"Next gain in {remaining.Minutes:D2}m {remaining.Seconds:D2}s (Tier {tier}: {tierHours:F0}h pace)";
                        }
                    }
                    else
                    {
                        status = $"Ready for breakthrough gain! (Tier {tier}: {tierHours:F0}h pace)";
                    }

                    pm.SendMessage(0x59, $"{skill.Name}: {skill.Base:F1}/{skill.Cap:F1} - {status}");
                }
            }

            if (trackedCount == 0)
            {
                pm.SendMessage(0x3B, $"You have not yet reached {MinSkillRequired:F0}.0 (Grandmaster) in any skills.");
            }
        }

        #region Persistence
        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                PersistencePath,
                writer =>
                {
                    writer.Write(0); // version

                    writer.Write(m_Gains.Count);
                    foreach (var kvp in m_Gains)
                    {
                        writer.Write((int)kvp.Key);
                        writer.Write(kvp.Value.Count);

                        foreach (var skillKvp in kvp.Value)
                        {
                            writer.Write((int)skillKvp.Key);
                            writer.Write(skillKvp.Value);
                        }
                    }
                });
        }

        private static void OnLoad()
        {
            Persistence.Deserialize(
                PersistencePath,
                reader =>
                {
                    reader.ReadInt(); // version
                    int count = reader.ReadInt();

                    m_Gains = new Dictionary<Serial, Dictionary<SkillName, DateTime>>(count);

                    for (int i = 0; i < count; i++)
                    {
                        Serial serial = reader.ReadInt();
                        int skillCount = reader.ReadInt();

                        var playerDict = new Dictionary<SkillName, DateTime>(skillCount);
                        for (int j = 0; j < skillCount; j++)
                        {
                            SkillName skill = (SkillName)reader.ReadInt();
                            DateTime lastGain = reader.ReadDateTime();
                            playerDict[skill] = lastGain;
                        }

                        m_Gains[serial] = playerDict;
                    }
                });
        }
        #endregion
    }
}
