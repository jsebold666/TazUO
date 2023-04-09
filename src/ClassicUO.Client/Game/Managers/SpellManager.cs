using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static ClassicUO.Game.GameObjects.Mobile;

namespace ClassicUO.Game.Managers
{
    public enum SpellState
    {
        None = 0,
        Casting = 1,	// We are in the process of casting (that is, waiting GetCastTime() and doing animations). Spell casting may be interupted in this state.
        Sequencing = 2	// Casting completed, but the full spell sequence isn't. Usually waiting for a target response. Some actions are restricted in this state (using skills for example).
    }

    internal class SpellManager
    {

        public SpellState State = SpellState.None;
        public SpellDefinition CurrentSpell = SpellDefinition.EmptySpell;
        public SpellBookType BookType = SpellBookType.Unknown;
        private readonly List<GameObject> _spellAreaTiles = new List<GameObject>();
        private readonly Dictionary<int, SpellArea> _spellAreas;

        public IReadOnlyDictionary<int, SpellArea> GetAllSpellAreas => _spellAreas;
        public SpellManager() {
            _spellAreas = new Dictionary<int, SpellArea>();
            Load();
            MessageManager.MessageReceived += MessageManager_MessageReceived;
        }

        public SpellArea[] SpellAreas { get { return _spellAreas.Values.ToArray(); } }

        public void ClearSpellAreas()
        {
            _spellAreas.Clear();
        }
        public bool GameObjectInSpellTiles(GameObject o)
        {
            return _spellAreaTiles.Exists(t => ReferenceEquals(o, t));
        }
        public bool TryGetActiveSpellArea(out SpellArea area)
        {
            return _spellAreas.TryGetValue(CurrentSpell.ID, out area);
        }
        public void AddSpellArea(SpellArea area)
        {
            _spellAreas.Add(area.Id, area);
        }

        private void MessageManager_MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Parent != null && ReferenceEquals(e.Parent, World.Player) && State != SpellState.Casting)
            {
                if (SpellDefinition.WordToTargettype.TryGetValue(e.Text, out var spell))
                {
                    SetCurrentSpell(spell);
                }
            }
        }

        public void SetCurrentSpell(int spellId)
        {
            SetCurrentSpell(SpellDefinition.FullIndexGetSpell(spellId));
        }
        public void SetCurrentSpell(SpellDefinition spell)
        {
            CurrentSpell = spell;
            if (CurrentSpell == SpellDefinition.EmptySpell)
            {
                State = SpellState.None;
                BookType = SpellBookType.Unknown;
                TargetManager.Reset();
                return;
            }
            State = SpellState.Casting;
            BookType = GetSpellBookType(spell.ID);            
        }

        private SpellBookType GetSpellBookType(int id)
        {
            if (id >= 1 && id <= 64)
            {
                return SpellBookType.Magery;
            }

            if (id >= 101 && id <= 117)
            {
                return SpellBookType.Necromancy;
            }

            if (id >= 201 && id <= 210)
            {
                return SpellBookType.Chivalry;
            }

            if (id >= 401 && id <= 406)
            {
                return SpellBookType.Bushido;
            }

            if (id >= 501 && id <= 508)
            {
                return SpellBookType.Ninjitsu;
            }

            if (id >= 601 && id <= 616)
            {
                return SpellBookType.Spellweaving;
            }

            if (id >= 678 && id <= 693)
            {
                return SpellBookType.Mysticism;
            }

            if (id >= 701 && id <= 745)
            {
                return SpellBookType.Mastery;
            }

            return SpellBookType.Unknown;
        }


        private bool IsEastToWest(int sX, int sY, int dX, int dY)
        {
            int x = sX - dX;
            int y = sY - dY;
            int rx = (x - y) * 44;
            int ry = (x + y) * 44;

            bool eastToWest;

            if (rx >= 0 && ry >= 0)
            {
                eastToWest = false;
            }
            else if (rx >= 0)
            {
                eastToWest = true;
            }
            else if (ry >= 0)
            {
                eastToWest = true;
            }
            else
            {
                eastToWest = false;
            }
            return eastToWest;
        }

        public void CancelSpell()
        {
            ClearSpellAreasTiles();
            SetCurrentSpell(0);
        }
        private void ClearSpellAreasTiles()
        {
            if (_spellAreaTiles.Count != 0)
            {
                _spellAreaTiles.ForEach(s => s.Hue = 0);
                _spellAreaTiles.Clear();
            }
        }

        public void Process()
        {
            //|| !UIManager.IsMouseOverWorld
            if (!ProfileManager.CurrentProfile.SpellAreaHighlights ||  State == SpellState.None || !_spellAreas.TryGetValue(CurrentSpell.ID, out var area) )
            {
                CurrentSpell = SpellDefinition.EmptySpell;
                State = SpellState.None;
                ClearSpellAreasTiles();
                return;
            }

            ClearSpellAreasTiles();
            if (area.Origin == SpellAreaOrigin.Caster && area.Range > 0)
            {
                for (int i = (-1 * area.Range); i <= area.Range; ++i)
                {
                    Vector3 loc = new Vector3(World.Player.X + i ,World.Player.Y + i, World.Player.Z);
                    var startY = area.IsLinear ? 0 : -1;
                    for (int j = (startY * area.Range); j <= area.Range; ++j)
                    {
                        if (!area.IsLinear)
                        {
                            loc = new Vector3(World.Player.X + i, World.Player.Y + j, World.Player.Z);
                        }
                        for (GameObject t = World.Map.GetTile((int)loc.X, (int)loc.Y, false); t != null; t = t.TNext)
                        {
                            t.Hue = area.Hue;
                            _spellAreaTiles.Add(t);
                        }
                    }
                }
            }
            if (area.Origin == SpellAreaOrigin.Target)
            {
                if (!TargetManager.IsTargeting && State == SpellState.Sequencing)
                {
                    CancelSpell();
                    return;
                }
                if (TargetManager.IsTargeting && State == SpellState.Casting)
                {
                    State = SpellState.Sequencing;
                }
                if (SelectedObject.Object is not GameObject o)
                {
                    ClearSpellAreasTiles();
                    return;
                }

                if (TargetManager.TargetingState == CursorTarget.Object || TargetManager.IsTargeting)
                {
                    if (area.Range == 0)
                    {
                        var loc = new Vector3(o.X, o.Y, o.Z);
                        for (GameObject t = World.Map.GetTile((int)loc.X, (int)loc.Y, false); t != null; t = t.TNext)
                        {
                            t.Hue = area.Hue;
                            _spellAreaTiles.Add(t);
                        }
                        return;
                    }
                    var eastToWest = IsEastToWest(World.Player.X, World.Player.Y, o.X, o.Y);
                    for (int i = (-1 * area.Range); i <= area.Range; ++i)
                    {
                        Vector3 loc = new Vector3(eastToWest ? o.X + i : o.X, eastToWest ? o.Y : o.Y + i, o.Z);
                        var startY = area.IsLinear ? 0 : -1;
                        for (int j = (startY * area.Range); j <= area.Range; ++j)
                        {
                            if (!area.IsLinear)
                            {
                                loc = new Vector3(o.X + i, o.Y + j, o.Z);
                            }
                            for (GameObject t = World.Map.GetTile((int)loc.X, (int)loc.Y, false); t != null; t = t.TNext)
                            {
                                t.Hue = area.Hue;
                                _spellAreaTiles.Add(t);
                            }
                        }
                    }
                }
            }

            return;
            
            if (new[] { CursorTarget.Object, CursorTarget.Position }.Contains(TargetManager.TargetingState))
            {
                State = SpellState.Sequencing;
                if (BookType == SpellBookType.Magery)
                {
                    if (SelectedObject.Object is GameObject o)
                    {
                        //o.Hue = 0x0021;
                        ClearSpellAreasTiles();
                            if (TargetManager.TargetingState == CursorTarget.Object)
                            {
                                var loc = new Vector3(o.X, o.Y, o.Z);
                                for (GameObject t = World.Map.GetTile((int)loc.X, (int)loc.Y, false); t != null; t = t.TNext)
                                {
                                    t.Hue = area.Hue;
                                    _spellAreaTiles.Add(t);
                                }
                            } else if (area.Range > 0)
                            {
                                var eastToWest = IsEastToWest(World.Player.X, World.Player.Y, o.X, o.Y);
                                for (int i = (-1 * area.Range); i <= area.Range; ++i)
                                {
                                    Vector3 loc = new Vector3(eastToWest ? o.X + i : o.X, eastToWest ? o.Y : o.Y + i, o.Z);
                                    var startY = area.IsLinear ? 0 : -1;
                                    for (int j = (startY * area.Range); j <= area.Range; ++j)
                                    {
                                        if (!area.IsLinear)
                                        {
                                            loc = new Vector3(o.X + i, o.Y + j, o.Z);
                                        }
                                        for (GameObject t = World.Map.GetTile((int)loc.X, (int)loc.Y, false); t != null; t = t.TNext)
                                        {
                                            t.Hue = area.Hue;
                                            _spellAreaTiles.Add(t);
                                        }
                                    }
                                }
                            }                            
                        return;
                    }
                    ClearSpellAreasTiles();
                    return;
                }
                ClearSpellAreasTiles();
                return;
            }
            ClearSpellAreasTiles();
        }

        private void CreateDefault()
        {
            _spellAreas.Clear();
            //linears
            _spellAreas.Add((int)HotkeyAction.CastWallOfStone, new SpellArea((int)HotkeyAction.CastWallOfStone, 1, true, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastFireField, new SpellArea((int)HotkeyAction.CastFireField, 2, true, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastPoisonField, new SpellArea((int)HotkeyAction.CastPoisonField, 2, true, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastParalyzeField, new SpellArea((int)HotkeyAction.CastParalyzeField, 2, true, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastEnergyField, new SpellArea((int)HotkeyAction.CastEnergyField, 2, true, 0x0021));

            //areas
            _spellAreas.Add((int)HotkeyAction.CastChainLightning, new SpellArea((int)HotkeyAction.CastChainLightning, 2, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastMeteorSwam, new SpellArea((int)HotkeyAction.CastMeteorSwam, 2, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastMassCurse, new SpellArea((int)HotkeyAction.CastMassCurse, 2, false, 0x0021));

            _spellAreas.Add((int)HotkeyAction.CastMassDispel, new SpellArea((int)HotkeyAction.CastMassDispel, 8, false, 0x0389));
            _spellAreas.Add((int)HotkeyAction.CastReveal, new SpellArea((int)HotkeyAction.CastReveal, 6, false, 0x0389));
            _spellAreas.Add((int)HotkeyAction.CastArchCure, new SpellArea((int)HotkeyAction.CastArchCure, 2, false, 0x0042));
            _spellAreas.Add((int)HotkeyAction.CastArchProtection, new SpellArea((int)HotkeyAction.CastArchProtection, 3, false, 0x0042));


            _spellAreas.Add((int)HotkeyAction.CastHailStorm, new SpellArea((int)HotkeyAction.CastHailStorm, 2, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastMassSleep, new SpellArea((int)HotkeyAction.CastMassSleep, 3, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastNetherCyclone, new SpellArea((int)HotkeyAction.CastNetherCyclone, 2, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastNetherBlast, new SpellArea((int)HotkeyAction.CastNetherBlast, 1, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastPoisonStrike, new SpellArea((int)HotkeyAction.CastPoisonStrike, 2, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastFlamingShot, new SpellArea((int)HotkeyAction.CastFlamingShot, 5, false, 0x0021));
            _spellAreas.Add((int)HotkeyAction.CastPlayingTheOdds, new SpellArea((int)HotkeyAction.CastPlayingTheOdds, 5, false, 0x0021));
            //5 + focus
            _spellAreas.Add((int)HotkeyAction.CastWildFire, new SpellArea((int)HotkeyAction.CastWildFire, 5, false, 0x0021));


            //3 + focus
            _spellAreas.Add((int)HotkeyAction.CastThinderstorm, new SpellArea((int)HotkeyAction.CastThinderstorm, 3, false, 0x0021, SpellAreaOrigin.Caster));
            _spellAreas.Add((int)HotkeyAction.CastWither, new SpellArea((int)HotkeyAction.CastWither, 4, false, 0x0021, SpellAreaOrigin.Caster));
            //1 + mageryskill / 15 ex: 1 + (100/15) = 8
            _spellAreas.Add((int)HotkeyAction.CastEarthquake, new SpellArea((int)HotkeyAction.CastEarthquake, 8, false, 0x0021, SpellAreaOrigin.Caster));
            //5 + focus
            _spellAreas.Add((int)HotkeyAction.CastEssenceOfWind, new SpellArea((int)HotkeyAction.CastEssenceOfWind, 5, false, 0x0021, SpellAreaOrigin.Caster));

            _spellAreas.Add((int)HotkeyAction.CastDispelEvil, new SpellArea((int)HotkeyAction.CastDispelEvil, 8, false, 0x0021, SpellAreaOrigin.Caster));
            _spellAreas.Add((int)HotkeyAction.CastHolyLight, new SpellArea((int)HotkeyAction.CastHolyLight, 3, false, 0x0021, SpellAreaOrigin.Caster));

            foreach(var area in _spellAreas)
            {
                if (area.Key >= 65 && area.Key <= 81)
                {
                    area.Value.Id += 36;
                }else if (area.Key >= 82 && area.Key <= 91)
                {
                    area.Value.Id += 119;
                }
                else if (area.Key >= 92 && area.Key <= 97)
                {
                    area.Value.Id += 309;
                }
                else if (area.Key >= 106 && area.Key <= 120)
                {
                    area.Value.Id += 495;
                }
                else if (area.Key >= 122 && area.Key <= 137)
                {
                    area.Value.Id += 556;
                }
                else if (area.Key >= 138 && area.Key <= 182)
                {
                    area.Value.Id += 563;
                }
            }
        }

        public void Save()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "spells.xml");

            using (XmlTextWriter xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("spell_areas");

                foreach (SpellArea area in _spellAreas.Values)
                {
                    area.Save(xml);
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }
        public void Load()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "spells.xml");
            if (!File.Exists(path))
            {
                CreateDefault();
                Save();

                return;
            }

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());

                return;
            }

            _spellAreas.Clear();

            XmlElement root = doc["spell_areas"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("spell_area"))
                {
                    var area = new SpellArea(xml);
                    _spellAreas.Add(area.Id, area);
                }
            }
        }
    }

    public enum SpellAreaOrigin
    {
        Target,
        Caster
    }
    internal class SpellArea
    {
        public int Id;
        public bool IsLinear;
        public int Range;
        public ushort Hue;
        public SpellAreaOrigin Origin;
        public SpellArea(int id, int range, bool isLinear, ushort hue = 0, SpellAreaOrigin origin = SpellAreaOrigin.Target)
        {
            Id = id;
            Range = range;
            IsLinear = isLinear;
            Hue = hue;
            Origin = origin;
        }


        public SpellArea(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }
            if (!int.TryParse(xml.GetAttribute("id"), out Id))
            {
                return;
            };
            int.TryParse(xml.GetAttribute("range"), out Range);
            bool.TryParse(xml.GetAttribute("isLinear"), out IsLinear);
            ushort.TryParse(xml.GetAttribute("hue"), out Hue);
            Enum.TryParse(xml.GetAttribute("origin"), out Origin);
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("spell_area");
            writer.WriteAttributeString("id", Id.ToString());
            writer.WriteAttributeString("range", Range.ToString());
            writer.WriteAttributeString("isLinear", IsLinear.ToString());
            writer.WriteAttributeString("hue", Hue.ToString());
            writer.WriteAttributeString("origin", Origin.ToString("d"));
            writer.WriteEndElement();
        }
    }
}
