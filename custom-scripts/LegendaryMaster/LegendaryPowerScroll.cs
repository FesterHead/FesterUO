// ============================================================================
// LegendaryPowerScroll.cs - Legendary Master Tier Upgrade PowerScroll
//
// Origin: FesterUO Custom Scripts (servuo/custom-scripts/LegendaryMaster/)
// Purpose: Dynamic +5 ceiling upgrade scroll awarded by the Legendary Master.
//
// Mechanics:
// - Usable by any character on the account (Blessed for safe transfer).
// - Increases skill cap to next tier (105, 110, 115, 120).
// - Requires 100.0+ base skill to use.
// - If skill < 100.0 or already 120.0: silently ignores without error or consumption.
// ============================================================================

using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Misc
{
    public class LegendaryPowerScroll : SpecialScroll
    {
        [Constructable]
        public LegendaryPowerScroll() : this(SkillName.Swords)
        {
        }

        [Constructable]
        public LegendaryPowerScroll(SkillName skill) : base(skill, 0.0)
        {
            Hue = 0x481;
            LootType = LootType.Blessed;
        }

        public LegendaryPowerScroll(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1049635; // Wonderous Scroll (+5 Skill)
        public override int Message => 1049469;
        public override string DefaultTitle => "<basefont color=#FFFFFF>Legendary Master Scroll (+5 Cap Upgrade):</basefont>";

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add($"a legendary master scroll of {GetName()} (+5 Cap Upgrade)");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.LabelTo(from, $"a legendary master scroll of {GetName()} (+5 Cap Upgrade)");
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060658, "Requires\t100.0+ Base Skill");
            list.Add(1060659, "Ceiling Limits\t105, 110, 115, 120");
        }

        public override bool CanUse(Mobile from)
        {
            if (Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return false;
            }

            Skill skill = from.Skills[Skill];
            if (skill == null)
                return false;

            double minRequired = Config.Get("LegendaryMaster.MinSkillRequired", 100.0);
            double maxCap = Config.Get("LegendaryMaster.MaxSkillCap", 120.0);

            // If skill is less than 100 or already at max cap, silently ignore without error or consumption
            if (skill.Base < minRequired || skill.Cap >= maxCap)
            {
                return false;
            }

            return true;
        }

        public override void Use(Mobile from)
        {
            if (!CanUse(from))
                return;

            Skill skill = from.Skills[Skill];
            if (skill == null)
                return;

            double maxCap = Config.Get("LegendaryMaster.MaxSkillCap", 120.0);
            double scrollInc = Config.Get("LegendaryMaster.ScrollIncrement", 5.0);

            // Ceiling limits step up in increments of 5.0 up to maxCap: 105, 110, 115, 120
            double nextCap = Math.Min(maxCap, (Math.Floor(skill.Cap / scrollInc) + 1.0) * scrollInc);

            skill.Cap = nextCap;

            Effects.SendLocationParticles(EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration), 0, 0, 0, 0, 0, 5060, 0);
            Effects.PlaySound(from.Location, from.Map, 0x243);
            Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

            from.SendMessage(53, $"The master scroll surges with power! Your {skill.Name} skill cap has increased to {nextCap:F0}!");

            Delete();
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
            LootType = LootType.Blessed;
        }
    }
}
