/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * Interactive paginated configuration gump providing toggles and threshold entry
 * for 87+ weapon, armor, jewelry, and clothing item properties. Registers [LootFilter command.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Commands;

namespace Server.Engines.LootFilter
{
    public class LootFilterGump : Gump
    {
        private PlayerMobile m_Player;
        private LootFilterAttachment m_Attachment;
        private LootFilterCategory m_Category;
        private int m_RowIndex;

        public static void Initialize()
        {
            CommandSystem.Register("LootFilter", AccessLevel.Player, new CommandEventHandler(LootFilter_OnCommand));
        }

        [Usage("LootFilter")]
        [Description("Opens the loot filter interface.")]
        private static void LootFilter_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm != null)
            {
                pm.CloseGump(typeof(LootFilterGump));
                pm.SendGump(new LootFilterGump(pm));
            }
        }

        #region Filter Entry Definitions
        private abstract class FilterEntry
        {
            public string Name { get; }
            public LootFilterCategory Category { get; }

            protected FilterEntry(string name, LootFilterCategory category)
            {
                Name = name;
                Category = category;
            }

            public abstract bool IsEnabled(LootFilterSettings s);
            public abstract void Toggle(LootFilterSettings s);
            public abstract int GetValue(LootFilterSettings s);
            public abstract void SetValue(LootFilterSettings s, int val);

            public void Adjust(LootFilterSettings s, int delta)
            {
                SetValue(s, Math.Max(0, GetValue(s) + delta));
            }
        }

        private class AosAttributeEntry : FilterEntry
        {
            public AosAttribute Attribute { get; }

            public AosAttributeEntry(string name, LootFilterCategory category, AosAttribute attr)
                : base(name, category)
            {
                Attribute = attr;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledAttributes.Contains(Attribute);

            public override void Toggle(LootFilterSettings s)
            {
                if (s.EnabledAttributes.Contains(Attribute))
                    s.EnabledAttributes.Remove(Attribute);
                else
                    s.EnabledAttributes.Add(Attribute);
            }

            public override int GetValue(LootFilterSettings s)
            {
                s.Attributes.TryGetValue(Attribute, out int v);
                return v;
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                s.Attributes[Attribute] = Math.Max(0, val);
            }
        }

        private class WeaponAttributeEntry : FilterEntry
        {
            public AosWeaponAttribute Attribute { get; }

            public WeaponAttributeEntry(string name, LootFilterCategory category, AosWeaponAttribute attr)
                : base(name, category)
            {
                Attribute = attr;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledWeaponAttributes.Contains(Attribute);

            public override void Toggle(LootFilterSettings s)
            {
                if (s.EnabledWeaponAttributes.Contains(Attribute))
                    s.EnabledWeaponAttributes.Remove(Attribute);
                else
                    s.EnabledWeaponAttributes.Add(Attribute);
            }

            public override int GetValue(LootFilterSettings s)
            {
                s.WeaponAttributes.TryGetValue(Attribute, out int v);
                return v;
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                s.WeaponAttributes[Attribute] = Math.Max(0, val);
            }
        }

        private class ArmorAttributeEntry : FilterEntry
        {
            public AosArmorAttribute Attribute { get; }

            public ArmorAttributeEntry(string name, LootFilterCategory category, AosArmorAttribute attr)
                : base(name, category)
            {
                Attribute = attr;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledArmorAttributes.Contains(Attribute);

            public override void Toggle(LootFilterSettings s)
            {
                if (s.EnabledArmorAttributes.Contains(Attribute))
                    s.EnabledArmorAttributes.Remove(Attribute);
                else
                    s.EnabledArmorAttributes.Add(Attribute);
            }

            public override int GetValue(LootFilterSettings s)
            {
                s.ArmorAttributes.TryGetValue(Attribute, out int v);
                return v;
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                s.ArmorAttributes[Attribute] = Math.Max(0, val);
            }
        }

        private class ExtWeaponAttributeEntry : FilterEntry
        {
            public ExtendedWeaponAttribute Attribute { get; }

            public ExtWeaponAttributeEntry(string name, LootFilterCategory category, ExtendedWeaponAttribute attr)
                : base(name, category)
            {
                Attribute = attr;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledExtendedWeaponAttributes.Contains(Attribute);

            public override void Toggle(LootFilterSettings s)
            {
                if (s.EnabledExtendedWeaponAttributes.Contains(Attribute))
                    s.EnabledExtendedWeaponAttributes.Remove(Attribute);
                else
                    s.EnabledExtendedWeaponAttributes.Add(Attribute);
            }

            public override int GetValue(LootFilterSettings s)
            {
                s.ExtendedWeaponAttributes.TryGetValue(Attribute, out int v);
                return v;
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                s.ExtendedWeaponAttributes[Attribute] = Math.Max(0, val);
            }
        }

        private class AbsorptionAttributeEntry : FilterEntry
        {
            public SAAbsorptionAttribute Attribute { get; }

            public AbsorptionAttributeEntry(string name, LootFilterCategory category, SAAbsorptionAttribute attr)
                : base(name, category)
            {
                Attribute = attr;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledAbsorptionAttributes.Contains(Attribute);

            public override void Toggle(LootFilterSettings s)
            {
                if (s.EnabledAbsorptionAttributes.Contains(Attribute))
                    s.EnabledAbsorptionAttributes.Remove(Attribute);
                else
                    s.EnabledAbsorptionAttributes.Add(Attribute);
            }

            public override int GetValue(LootFilterSettings s)
            {
                s.AbsorptionAttributes.TryGetValue(Attribute, out int v);
                return v;
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                s.AbsorptionAttributes[Attribute] = Math.Max(0, val);
            }
        }

        private class ResistEntry : FilterEntry
        {
            public int ResistIndex { get; }

            public ResistEntry(string name, int resistIndex)
                : base(name, LootFilterCategory.Resists)
            {
                ResistIndex = resistIndex;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledResistances[ResistIndex];

            public override void Toggle(LootFilterSettings s) => s.EnabledResistances[ResistIndex] = !s.EnabledResistances[ResistIndex];

            public override int GetValue(LootFilterSettings s)
            {
                switch (ResistIndex)
                {
                    case 0: return s.MinResistPhysical;
                    case 1: return s.MinResistFire;
                    case 2: return s.MinResistCold;
                    case 3: return s.MinResistPoison;
                    case 4: return s.MinResistEnergy;
                    default: return 0;
                }
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                val = Math.Max(0, val);
                switch (ResistIndex)
                {
                    case 0: s.MinResistPhysical = val; break;
                    case 1: s.MinResistFire = val; break;
                    case 2: s.MinResistCold = val; break;
                    case 3: s.MinResistPoison = val; break;
                    case 4: s.MinResistEnergy = val; break;
                }
            }
        }

        private class DamageEntry : FilterEntry
        {
            public int DamageIndex { get; }

            public DamageEntry(string name, int damageIndex)
                : base(name, LootFilterCategory.Damage)
            {
                DamageIndex = damageIndex;
            }

            public override bool IsEnabled(LootFilterSettings s) => s.EnabledDamages[DamageIndex];

            public override void Toggle(LootFilterSettings s) => s.EnabledDamages[DamageIndex] = !s.EnabledDamages[DamageIndex];

            public override int GetValue(LootFilterSettings s)
            {
                switch (DamageIndex)
                {
                    case 0: return s.MinDamagePhysical;
                    case 1: return s.MinDamageFire;
                    case 2: return s.MinDamageCold;
                    case 3: return s.MinDamagePoison;
                    case 4: return s.MinDamageEnergy;
                    case 5: return s.MinDamageChaos;
                    case 6: return s.MinDamageDirect;
                    default: return 0;
                }
            }

            public override void SetValue(LootFilterSettings s, int val)
            {
                val = Math.Max(0, val);
                switch (DamageIndex)
                {
                    case 0: s.MinDamagePhysical = val; break;
                    case 1: s.MinDamageFire = val; break;
                    case 2: s.MinDamageCold = val; break;
                    case 3: s.MinDamagePoison = val; break;
                    case 4: s.MinDamageEnergy = val; break;
                    case 5: s.MinDamageChaos = val; break;
                    case 6: s.MinDamageDirect = val; break;
                }
            }
        }

        private static readonly FilterEntry[] FilterEntries = new FilterEntry[]
        {
            // Primary (11)
            new AosAttributeEntry("Strength", LootFilterCategory.Primary, AosAttribute.BonusStr),
            new AosAttributeEntry("Dexterity", LootFilterCategory.Primary, AosAttribute.BonusDex),
            new AosAttributeEntry("Intelligence", LootFilterCategory.Primary, AosAttribute.BonusInt),
            new AosAttributeEntry("Hit Points", LootFilterCategory.Primary, AosAttribute.BonusHits),
            new AosAttributeEntry("Stamina", LootFilterCategory.Primary, AosAttribute.BonusStam),
            new AosAttributeEntry("Mana", LootFilterCategory.Primary, AosAttribute.BonusMana),
            new AosAttributeEntry("Hit Point Regeneration", LootFilterCategory.Primary, AosAttribute.RegenHits),
            new AosAttributeEntry("Stamina Regeneration", LootFilterCategory.Primary, AosAttribute.RegenStam),
            new AosAttributeEntry("Mana Regeneration", LootFilterCategory.Primary, AosAttribute.RegenMana),
            new AosAttributeEntry("Luck", LootFilterCategory.Primary, AosAttribute.Luck),
            new AosAttributeEntry("Night Sight", LootFilterCategory.Primary, AosAttribute.NightSight),

            // Combat (11)
            new AosAttributeEntry("Hit Chance Increase", LootFilterCategory.Combat, AosAttribute.AttackChance),
            new AosAttributeEntry("Defense Chance Increase", LootFilterCategory.Combat, AosAttribute.DefendChance),
            new AosAttributeEntry("Damage Increase", LootFilterCategory.Combat, AosAttribute.WeaponDamage),
            new AosAttributeEntry("Swing Speed Increase", LootFilterCategory.Combat, AosAttribute.WeaponSpeed),
            new AosAttributeEntry("Reflect Physical Damage", LootFilterCategory.Combat, AosAttribute.ReflectPhysical),
            new WeaponAttributeEntry("Battle Lust", LootFilterCategory.Combat, AosWeaponAttribute.BattleLust),
            new AbsorptionAttributeEntry("Resonance: Fire", LootFilterCategory.Combat, SAAbsorptionAttribute.ResonanceFire),
            new AbsorptionAttributeEntry("Resonance: Cold", LootFilterCategory.Combat, SAAbsorptionAttribute.ResonanceCold),
            new AbsorptionAttributeEntry("Resonance: Poison", LootFilterCategory.Combat, SAAbsorptionAttribute.ResonancePoison),
            new AbsorptionAttributeEntry("Resonance: Energy", LootFilterCategory.Combat, SAAbsorptionAttribute.ResonanceEnergy),
            new AbsorptionAttributeEntry("Resonance: Kinetic", LootFilterCategory.Combat, SAAbsorptionAttribute.ResonanceKinetic),

            // Magic (9)
            new AosAttributeEntry("Spell Damage Increase", LootFilterCategory.Magic, AosAttribute.SpellDamage),
            new AosAttributeEntry("Faster Casting", LootFilterCategory.Magic, AosAttribute.CastSpeed),
            new AosAttributeEntry("Faster Cast Recovery", LootFilterCategory.Magic, AosAttribute.CastRecovery),
            new AosAttributeEntry("Lower Mana Cost", LootFilterCategory.Magic, AosAttribute.LowerManaCost),
            new AosAttributeEntry("Lower Reagent Cost", LootFilterCategory.Magic, AosAttribute.LowerRegCost),
            new AosAttributeEntry("Spell Channeling", LootFilterCategory.Magic, AosAttribute.SpellChanneling),
            new AbsorptionAttributeEntry("Casting Focus", LootFilterCategory.Magic, SAAbsorptionAttribute.CastingFocus),
            new ArmorAttributeEntry("Soul Charge", LootFilterCategory.Magic, AosArmorAttribute.SoulCharge),
            new AosAttributeEntry("Enhance Potions", LootFilterCategory.Magic, AosAttribute.EnhancePotions),

            // Resists (5)
            new ResistEntry("Physical Resistance", 0),
            new ResistEntry("Fire Resistance", 1),
            new ResistEntry("Cold Resistance", 2),
            new ResistEntry("Poison Resistance", 3),
            new ResistEntry("Energy Resistance", 4),

            // Damage (7)
            new DamageEntry("Physical Damage %", 0),
            new DamageEntry("Fire Damage %", 1),
            new DamageEntry("Cold Damage %", 2),
            new DamageEntry("Poison Damage %", 3),
            new DamageEntry("Energy Damage %", 4),
            new DamageEntry("Chaos Damage %", 5),
            new DamageEntry("Direct Damage %", 6),

            // HitsLeech (7)
            new WeaponAttributeEntry("Hit Life Leech", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitLeechHits),
            new WeaponAttributeEntry("Hit Stamina Leech", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitLeechStam),
            new WeaponAttributeEntry("Hit Mana Leech", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitLeechMana),
            new WeaponAttributeEntry("Hit Lower Attack", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitLowerAttack),
            new WeaponAttributeEntry("Hit Lower Defense", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitLowerDefend),
            new WeaponAttributeEntry("Hit Mana Drain", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitManaDrain),
            new WeaponAttributeEntry("Hit Fatigue", LootFilterCategory.HitsLeech, AosWeaponAttribute.HitFatigue),

            // HitsMagic (10)
            new WeaponAttributeEntry("Hit Magic Arrow", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitMagicArrow),
            new WeaponAttributeEntry("Hit Harm", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitHarm),
            new WeaponAttributeEntry("Hit Fireball", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitFireball),
            new WeaponAttributeEntry("Hit Lightning", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitLightning),
            new WeaponAttributeEntry("Hit Curse", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitCurse),
            new WeaponAttributeEntry("Hit Physical Area", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitPhysicalArea),
            new WeaponAttributeEntry("Hit Fire Area", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitFireArea),
            new WeaponAttributeEntry("Hit Cold Area", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitColdArea),
            new WeaponAttributeEntry("Hit Poison Area", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitPoisonArea),
            new WeaponAttributeEntry("Hit Energy Area", LootFilterCategory.HitsMagic, AosWeaponAttribute.HitEnergyArea),

            // Special (10)
            new WeaponAttributeEntry("Self Repair", LootFilterCategory.Special, AosWeaponAttribute.SelfRepair),
            new WeaponAttributeEntry("Splintering Weapon", LootFilterCategory.Special, AosWeaponAttribute.SplinteringWeapon),
            new WeaponAttributeEntry("Blood Drinker", LootFilterCategory.Special, AosWeaponAttribute.BloodDrinker),
            new ExtWeaponAttributeEntry("Bane", LootFilterCategory.Special, ExtendedWeaponAttribute.Bane),
            new AbsorptionAttributeEntry("Damage Eater", LootFilterCategory.Special, SAAbsorptionAttribute.EaterDamage),
            new AbsorptionAttributeEntry("Fire Eater", LootFilterCategory.Special, SAAbsorptionAttribute.EaterFire),
            new AbsorptionAttributeEntry("Cold Eater", LootFilterCategory.Special, SAAbsorptionAttribute.EaterCold),
            new AbsorptionAttributeEntry("Poison Eater", LootFilterCategory.Special, SAAbsorptionAttribute.EaterPoison),
            new AbsorptionAttributeEntry("Energy Eater", LootFilterCategory.Special, SAAbsorptionAttribute.EaterEnergy),
            new AbsorptionAttributeEntry("Kinetic Eater", LootFilterCategory.Special, SAAbsorptionAttribute.EaterKinetic)
        };
        #endregion

        public LootFilterGump(PlayerMobile pm, LootFilterCategory category = LootFilterCategory.Primary) : base(50, 50)
        {
            m_Player = pm;
            m_Attachment = LootFilterAttachment.GetAttachment(pm);
            if (m_Attachment != null && m_Attachment.Settings == null)
            {
                m_Attachment.Settings = new LootFilterSettings();
            }
            m_Category = category;

            AddPage(0);
            
            // Main Window
            AddBackground(0, 0, 600, 580, 9270);
            AddAlphaRegion(10, 10, 580, 560);

            // Header Frame
            AddImageTiled(15, 15, 570, 40, 2624);
            AddAlphaRegion(15, 15, 570, 40);

            // Title
            AddHtml(15, 25, 420, 20, "<BASEFONT COLOR=#FCCA03><CENTER>ULTIMATE ARPG LOOT FILTER</CENTER></BASEFONT>", false, false);
            
            // Info Button
            AddButton(370, 25, 4011, 4013, 2, GumpButtonType.Reply, 0);
            AddHtml(405, 25, 45, 20, "<BASEFONT COLOR=#00BFFF>INFO</BASEFONT>", false, false);

            // Toggle Filter
            bool globalEnabled = m_Attachment != null && m_Attachment.Settings.Enabled;
            AddHtml(460, 25, 70, 20, globalEnabled ? "<BASEFONT COLOR=#00FF00>ENABLED</BASEFONT>" : "<BASEFONT COLOR=#FF0000>DISABLED</BASEFONT>", false, false);
            AddButton(535, 25, globalEnabled ? 2154 : 2151, globalEnabled ? 2154 : 2151, 1, GumpButtonType.Reply, 0);

            // Left Category Frame
            AddImageTiled(15, 65, 140, 500, 2624);
            AddAlphaRegion(15, 65, 140, 500);

            int y = 75;
            AddCategoryButton(25, ref y, "Primary", LootFilterCategory.Primary);
            AddCategoryButton(25, ref y, "Combat", LootFilterCategory.Combat);
            AddCategoryButton(25, ref y, "Magic", LootFilterCategory.Magic);
            AddCategoryButton(25, ref y, "Resistances", LootFilterCategory.Resists);
            AddCategoryButton(25, ref y, "Damage", LootFilterCategory.Damage);
            AddCategoryButton(25, ref y, "Hit Leech", LootFilterCategory.HitsLeech);
            AddCategoryButton(25, ref y, "Hit Magic", LootFilterCategory.HitsMagic);
            AddCategoryButton(25, ref y, "Special", LootFilterCategory.Special);

            // Right Content Frame
            AddImageTiled(165, 65, 420, 500, 2624);
            AddAlphaRegion(165, 65, 420, 500);

            // Content Headers
            AddHtml(180, 75, 50, 20, "<BASEFONT COLOR=#FFFFFF>On/Off</BASEFONT>", false, false);
            AddHtml(215, 75, 200, 20, "<BASEFONT COLOR=#FFFFFF>Property Name</BASEFONT>", false, false);
            AddHtml(420, 75, 60, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>Minimum</CENTER></BASEFONT>", false, false);
            AddHtml(495, 75, 80, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>Adjust</CENTER></BASEFONT>", false, false);
            
            // Header Divider
            AddImageTiled(175, 95, 400, 2, 2624);
            AddAlphaRegion(175, 95, 400, 2);

            RenderCategory(m_Category);

            // Bottom Action Area in Right Content Frame
            AddImageTiled(175, 515, 400, 1, 2624);
            AddAlphaRegion(175, 515, 400, 1);

            AddHtml(180, 527, 280, 20, "<BASEFONT COLOR=#999999>Type numbers or use arrows</BASEFONT>", false, false);

            // Apply Button
            AddButton(475, 524, 4005, 4007, 5, GumpButtonType.Reply, 0);
            AddHtml(510, 526, 60, 20, "<BASEFONT COLOR=#00FF00>Apply</BASEFONT>", false, false);
        }

        private void AddCategoryButton(int x, ref int y, string label, LootFilterCategory cat)
        {
            bool active = m_Category == cat;
            
            if (active)
            {
                AddImageTiled(x - 5, y - 5, 120, 42, 2624);
                AddAlphaRegion(x - 5, y - 5, 120, 42);
            }

            AddButton(x, y + 6, active ? 4006 : 4005, active ? 4007 : 4006, 10 + (int)cat, GumpButtonType.Reply, 0);
            
            AddHtml(x + 35, y + 6, 90, 40, active ? $"<BASEFONT COLOR=#FCCA03>{label}</BASEFONT>" : $"<BASEFONT COLOR=#999999>{label}</BASEFONT>", false, false);

            y += 45;

            // Category Divider
            if (cat != LootFilterCategory.Special)
            {
                AddImageTiled(x - 5, y, 120, 2, 2624);
                AddAlphaRegion(x - 5, y, 120, 2);
            }
            y += 10;
        }

        private void RenderCategory(LootFilterCategory cat)
        {
            int y = 105;
            m_RowIndex = 0;

            for (int i = 0; i < FilterEntries.Length; i++)
            {
                var entry = FilterEntries[i];
                if (entry.Category != cat)
                    continue;

                int val = entry.GetValue(m_Attachment.Settings);
                bool isEnabled = entry.IsEnabled(m_Attachment.Settings);

                AddEntry(i, entry.Name, val, isEnabled, ref y);
            }
        }

        private void AddEntry(int index, string name, int val, bool isEnabled, ref int y)
        {
            m_RowIndex++;
            int x = 180;

            if (m_RowIndex % 2 != 0)
            {
                AddImageTiled(x - 10, y - 5, 410, 35, 2624);
                AddAlphaRegion(x - 10, y - 5, 410, 35);
            }

            // Toggle On/Off button
            AddButton(x, y + 2, isEnabled ? 2154 : 2151, isEnabled ? 2154 : 2151, 1000 + index, GumpButtonType.Reply, 0);
            
            // Property Name
            AddHtml(x + 35, y + 2, 200, 20, isEnabled ? $"<BASEFONT COLOR=#FFFFFF>{name}</BASEFONT>" : $"<BASEFONT COLOR=#777777>{name}</BASEFONT>", false, false);
            
            // Numeric Input Box
            AddImageTiled(x + 240, y, 60, 24, 2624);
            AddAlphaRegion(x + 240, y, 60, 24);
            
            AddTextEntry(x + 245, y + 2, 50, 20, 0x481, index + 1, val.ToString(), 5);
            
            // Adjust buttons: Up / Down
            AddButton(x + 325, y + 3, 2435, 2436, 2000 + index, GumpButtonType.Reply, 0);
            AddButton(x + 355, y + 3, 2437, 2438, 3000 + index, GumpButtonType.Reply, 0);
            
            y += 35;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Attachment == null || m_Attachment.Settings == null)
                return;

            int id = info.ButtonID;

            // Always save any text entered in the numeric boxes on the current page first
            SaveTextEntries(info);

            if (id == 0) // Closed / Right Click
            {
                return;
            }

            if (id == 1) // Enable/Disable Global
            {
                m_Attachment.Settings.Enabled = !m_Attachment.Settings.Enabled;
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            if (id == 2) // Info button
            {
                m_Player.CloseGump(typeof(LootFilterInfoGump));
                m_Player.SendGump(new LootFilterInfoGump(m_Player));
                return;
            }

            if (id == 5) // Apply Button
            {
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            if (id >= 10 && id < 10 + 8) // Change Category
            {
                m_Category = (LootFilterCategory)(id - 10);
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            // Toggles
            if (id >= 1000 && id < 1000 + FilterEntries.Length)
            {
                int index = id - 1000;
                FilterEntries[index].Toggle(m_Attachment.Settings);
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            // Increments
            if (id >= 2000 && id < 2000 + FilterEntries.Length)
            {
                int index = id - 2000;
                FilterEntries[index].Adjust(m_Attachment.Settings, 1);
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            // Decrements
            if (id >= 3000 && id < 3000 + FilterEntries.Length)
            {
                int index = id - 3000;
                FilterEntries[index].Adjust(m_Attachment.Settings, -1);
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            // Fallback: refresh gump rather than silently closing
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void SaveTextEntries(RelayInfo info)
        {
            if (info == null || m_Attachment == null || m_Attachment.Settings == null)
                return;

            for (int i = 0; i < FilterEntries.Length; i++)
            {
                if (FilterEntries[i].Category != m_Category)
                    continue;

                TextRelay relay = info.GetTextEntry(i + 1);
                if (relay != null && !string.IsNullOrWhiteSpace(relay.Text))
                {
                    if (int.TryParse(relay.Text.Trim(), out int parsedVal))
                    {
                        FilterEntries[i].SetValue(m_Attachment.Settings, parsedVal);
                    }
                }
            }
        }
    }
}
