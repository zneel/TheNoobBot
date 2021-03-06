﻿using System;
using System.Collections.Generic;
using System.Threading;
using nManager.Helpful;
using nManager.Wow.Helpers;
using nManager.Wow.ObjectManager;
using nManager.Wow.Patchables;

namespace nManager.Wow.Class
{
    public class Spell
    {
        #region Fields

        public UInt32 Id;
        public List<uint> Ids = new List<uint>();
        public bool KnownSpell;
        public float CastTime;
        public int Cost;
        public string Icon = "";
        public string IsFunnel = "";
        public float MaxRangeHostile;
        public float MinRangeHostile;
        public float MaxRangeFriend;
        public float MinRangeFriend;
        public string Name = "";
        public string NameInGame = "";
        public string PowerType = "";
        public string Rank = "";

        #endregion Fields

        #region Constructors

        private static readonly DBC<DBCStruct.SpellRec> DBCSpell = new DBC<DBCStruct.SpellRec>((int) Addresses.DBC.Spell);

        private static readonly DBC<DBCStruct.SpellMiscRec> DBCSpellMisc = new DBC<DBCStruct.SpellMiscRec>((int) Addresses.DBC.SpellMisc);

        private static readonly DBC<DBCStruct.SpellCastTimesRec> DBCSpellCastTimes = new DBC<DBCStruct.SpellCastTimesRec>((int) Addresses.DBC.SpellCastTimes);

        private static readonly DBC<DBCStruct.SpellRangeRec> DBCSpellRange = new DBC<DBCStruct.SpellRangeRec>((int) Addresses.DBC.SpellRange);

        /// <summary>
        /// Initializes a new instance of the <see cref="Spell"/> class. This class management an spell of your wow player.
        /// </summary>
        /// <param name="spellId">The spell ID.</param>
        public Spell(uint spellId)
        {
            lock (this)
            {
                try
                {
                    Id = spellId;

                    DBCStruct.SpellRec spellRec = DBCSpell.GetRow((int) Id);

                    if (spellRec.SpellId >= 0)
                    {
                        if (spellId == spellRec.SpellId)
                        {
                            DBCStruct.SpellCastTimesRec castTimeRec =
                                DBCSpellCastTimes.GetRow(DBCSpellMisc.GetRow(spellRec.SpellMiscId).SpellCastTimesId);
                            try
                            {
                                CastTime = (float) castTimeRec.CastTime/1000;
                            }
                            catch
                            {
                                CastTime = 0;
                            }

                            DBCStruct.SpellRangeRec range = DBCSpellRange.GetRow(DBCSpellMisc.GetRow(spellRec.SpellMiscId).SpellRangeId);
                            if (range.Id != 0)
                            {
                                MaxRangeHostile = range.MaxRangeHostile;
                                MinRangeHostile = range.MinRangeHostile;
                                MaxRangeFriend = range.MaxRangeFriend;
                                MinRangeFriend = range.MinRangeFriend;
                            }
                            else
                            {
                                MaxRangeHostile = 0;
                                MinRangeHostile = 0;
                                MaxRangeFriend = 0;
                                MinRangeFriend = 0;
                            }

                            Name = SpellManager.SpellListManager.SpellNameById(spellId);
                            try
                            {
                                NameInGame = Memory.WowMemory.Memory.ReadUTF8String(spellRec.Name);
                            }
                            catch
                            {
                                NameInGame = "";
                            }

                            if (MaxRangeHostile < 5.0f)
                                MaxRangeHostile = 5.0f;
                            if (MaxRangeFriend < 5.0f)
                                MaxRangeFriend = 5.0f;
                            KnownSpell = SpellManager.ExistSpellBookLUA(NameInGame);
                            Ids.AddRange(SpellManager.SpellListManager.SpellIdByName(Name));
                            Ids.Add(Id);
                            return;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell(uint spellId): " + exception);
                }
                CastTime = 0;
                MaxRangeHostile = 5.0f;
                MinRangeHostile = 0f;
                MaxRangeFriend = 5.0f;
                MinRangeFriend = 0f;
                NameInGame = "";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Spell"/> class. This class manage spells of your wow player.
        /// </summary>
        /// <param name="spellName">English name of the spell.</param>
        public Spell(string spellName)
        {
            Spell tSpell = null;
            try
            {
                uint.TryParse(spellName, out Id);
                if (Id > 0)
                {
                    tSpell = new Spell(Id);
                }
                else
                {
                    foreach (Spell s in SpellManager.SpellBook())
                    {
                        if (s.Name == spellName)
                        {
                            tSpell = s;
                            break;
                        }
                    }
                }
                if (tSpell == null)
                {
                    Logging.WriteDebug("Spell(string spellName): spellName=" + spellName + " => Failed");
                    return;
                }
                Logging.WriteDebug("Spell(string spellName): spellName=" + spellName + ", Id found: " + tSpell.Id +
                                   ", Name found: " + tSpell.Name + ", NameInGame found: " + tSpell.NameInGame + ", KnownSpell: " + tSpell.KnownSpell);
                Id = tSpell.Id;
                CastTime = tSpell.CastTime;
                Cost = tSpell.Cost;
                Icon = tSpell.Icon;
                IsFunnel = tSpell.IsFunnel;
                MaxRangeHostile = tSpell.MaxRangeHostile;
                MinRangeHostile = tSpell.MinRangeHostile;
                MaxRangeFriend = tSpell.MaxRangeFriend;
                MinRangeFriend = tSpell.MinRangeFriend;
                Name = tSpell.Name;
                NameInGame = tSpell.NameInGame;
                PowerType = tSpell.PowerType;
                Rank = tSpell.Rank;
                KnownSpell = tSpell.KnownSpell;
                Ids.AddRange(tSpell.Ids);
                Ids.Add(Id);
            }
            catch (Exception exception)
            {
                Logging.WriteError("Spell(string spellName): " + exception);
            }
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Check if the spell is currently available for use.
        /// </summary>
        /// <value>
        /// Return <c>true</c> if spell usable; otherwise, <c>false</c>.
        /// </value>
        public bool IsSpellUsable
        {
            get
            {
                try
                {
                    return SpellManager.SpellUsableLUA(NameInGame);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > IsSpellUsable: " + exception);
                }
                return false;
            }
        }

        /// <summary>
        /// Return true if the player distance from casting a spell to a Hostile unit is good.
        /// Else, return false.
        /// </summary>
        public bool IsHostileDistanceGood
        {
            get
            {
                try
                {
                    return ObjectManager.ObjectManager.Target.GetDistance <= MaxRangeHostile &&
                           (ObjectManager.ObjectManager.Target.GetDistance >= MinRangeHostile);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > IsHostileDistanceGood: " + exception);
                    return true;
                }
            }
        }

        /// <summary>
        /// Return true if the player distance from casting a spell to a friendly unit is good.
        /// Else, return false.
        /// </summary>
        public bool IsFriendDistanceGood
        {
            get
            {
                try
                {
                    return ObjectManager.ObjectManager.Target.GetDistance <= MaxRangeFriend &&
                           (ObjectManager.ObjectManager.Target.GetDistance >= MaxRangeFriend);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > IsFriendlyDistanceGood: " + exception);
                    return true;
                }
            }
        }

        /// <summary>
        /// Gets player buff Stack.
        /// </summary>
        public int BuffStack
        {
            get
            {
                try
                {
                    return ObjectManager.ObjectManager.Me.BuffStack(Ids);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > BuffStack: " + exception);
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets if the nearest object named like your spell name is your.
        /// </summary>
        /// <value>
        /// return <c>true</c> if [summoned/created by me]; otherwise, <c>false</c>.
        /// </value>
        public bool CreatedBySpell
        {
            get
            {
                try
                {
                    List<WoWUnit> woWUnit = ObjectManager.ObjectManager.GetWoWUnitByName(NameInGame);

                    if (woWUnit.Count > 0)
                    {
                        WoWUnit nearestWoWUnit = ObjectManager.ObjectManager.GetNearestWoWUnit(woWUnit);
                        if (nearestWoWUnit.IsValid && nearestWoWUnit.IsAlive)
                        {
                            if (nearestWoWUnit.SummonedBy == ObjectManager.ObjectManager.Me.Guid ||
                                nearestWoWUnit.CreatedBy == ObjectManager.ObjectManager.Me.Guid)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.WriteError("Spell > CreatedBySpell: " + e);
                }
                return false;
            }
        }

        /// <summary>
        /// Gets if the nearest object named like your spell name is your.
        /// Check for the max range prior to recast it.
        /// </summary>
        /// <value>
        /// return <c>true</c> if [summoned/created by me]; otherwise, <c>false</c>.
        /// </value>
        public bool CreatedBySpellInRange(uint maxrange = 40)
        {
            try
            {
                List<WoWUnit> woWUnit = ObjectManager.ObjectManager.GetWoWUnitByName(NameInGame);

                if (woWUnit.Count > 0)
                {
                    WoWUnit nearestWoWUnit = ObjectManager.ObjectManager.GetNearestWoWUnit(woWUnit);
                    if (nearestWoWUnit.IsValid && nearestWoWUnit.IsAlive)
                    {
                        if ((nearestWoWUnit.SummonedBy == ObjectManager.ObjectManager.Me.Guid ||
                             nearestWoWUnit.CreatedBy == ObjectManager.ObjectManager.Me.Guid) &&
                            nearestWoWUnit.GetDistance <= maxrange)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteError("Spell > CreatedBySpellInRange: " + e);
            }
            return false;
        }

        /// <summary>
        /// Gets target buff Stack.
        /// </summary>
        public int TargetBuffStack
        {
            get
            {
                try
                {
                    return ObjectManager.ObjectManager.Target.BuffStack(Ids);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > TargetBuffStack: " + exception);
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets if your player have this spell in buff.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [have buff]; otherwise, <c>false</c>.
        /// </value>
        public bool HaveBuff
        {
            get
            {
                try
                {
                    return ObjectManager.ObjectManager.Me.HaveBuff(Ids);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > HaveBuff: " + exception);
                }
                return false;
            }
        }

        /// <summary>
        /// Gets if the target have this spell in buff.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [target have buff]; otherwise, <c>false</c>.
        /// </value>
        public bool TargetHaveBuff
        {
            get
            {
                try
                {
                    return ObjectManager.ObjectManager.Target.HaveBuff(Ids);
                }
                catch (Exception exception)
                {
                    Logging.WriteError("Spell > TargetHaveBuff: " + exception);
                    return false;
                }
            }
        }

        /// <summary>
        /// Cast Spell.
        /// </summary>
        public void Launch()
        {
            try
            {
                Launch(CastTime == 0);
            }
            catch (Exception exception)
            {
                Logging.WriteError("Spell > Launch(): " + exception);
            }
        }

        /// <summary>
        /// Cast Spell.
        /// </summary>
        /// <param name="StopMove">if set to <c>true</c> [Don't move during cast].</param>
        /// <param name="waitIsCast">if set to <c>true</c> [Wait the cast end].</param>
        /// <param name="ignoreIfCast"> </param>
        public void Launch(bool StopMove, bool waitIsCast = true, bool ignoreIfCast = false)
        {
            try
            {
                int t = 200;
                while (ObjectManager.ObjectManager.Me.IsCast && !ignoreIfCast)
                {
                    Thread.Sleep(5);
                    t--;
                    if (t < 0) return;
                }
                Logging.WriteFight("Cast " + NameInGame);
                if (StopMove)
                {
                    if (ObjectManager.ObjectManager.Me.GetMove)
                        MovementManager.StopMoveTo();
                }
                SpellManager.CastSpellByNameLUA(NameInGame);
                Thread.Sleep(600);
                while (ObjectManager.ObjectManager.Me.IsCast && waitIsCast)
                {
                    Thread.Sleep(5);
                }
            }
            catch (Exception exception)
            {
                Logging.WriteError(
                    "Spell > Launch(bool StopMove, bool waitIsCast = true, bool ignoreIfCast = false): " + exception);
            }
        }

        internal void Update()
        {
            try
            {
                KnownSpell = SpellManager.ExistSpellBookLUA(NameInGame);
            }
            catch (Exception exception)
            {
                Logging.WriteError("Spell > Update(): " + exception);
            }
        }

        #endregion Methods
    }

    public class SpellList
    {
        public SpellList()
        {
        }

        public SpellList(uint id, string name = "")
        {
            Id = id;
            Name = name;
        }

        public uint Id { get; set; }
        public string Name { get; set; }
    }
}