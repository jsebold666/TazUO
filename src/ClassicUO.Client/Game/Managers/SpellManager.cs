using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public SpellState State;
        public SpellDefinition CurrentSpell = SpellDefinition.EmptySpell;
        public SpellBookType BookType = SpellBookType.Unknown;
        private readonly List<GameObject> _spellAreaTiles = new List<GameObject>();
        private readonly Dictionary<int, SpellArea> _spellAreas;
        public SpellManager() {
            _spellAreas = new Dictionary<int, SpellArea>();
            Load();
        }
        
        public void SetCurrentSpell(int spellId)
        {
            CurrentSpell =  SpellDefinition.FullIndexGetSpell(spellId);
            BookType = GetSpellBookType(CurrentSpell.ID);
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
            ClearSpellAreas();
            SetCurrentSpell(0);
        }
        private void ClearSpellAreas()
        {
            if (_spellAreaTiles.Count != 0)
            {
                _spellAreaTiles.ForEach(s => s.Hue = 0);
                _spellAreaTiles.Clear();
            }
        }

        public void Process()
        {
            if (CurrentSpell == null || CurrentSpell == SpellDefinition.EmptySpell ||  !UIManager.IsMouseOverWorld || (!TargetManager.IsTargeting && CurrentSpell.ID != (int)HotkeyAction.CastEarthquake))
            {
                ClearSpellAreas();
                return;
            }
            
            if (TargetManager.TargetingState == CursorTarget.Position)
            {
                if (BookType == SpellBookType.Magery)
                {
                    if (SelectedObject.Object is GameObject o)
                    {
                        //o.Hue = 0x0021;
                        ClearSpellAreas();
                        if (_spellAreas.TryGetValue(CurrentSpell.ID, out var area))
                        {
                            if (area.Range > 0)
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
                        }
                        return;
                    }
                    ClearSpellAreas();
                    return;
                }
                ClearSpellAreas();
                return;
            }
            ClearSpellAreas();
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
                xml.WriteStartElement("areas");

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

            XmlElement root = doc["areas"];

            if (root != null)
            {
                foreach (XmlElement xml in root.GetElementsByTagName("area"))
                {
                    var area = new SpellArea(xml);
                    _spellAreas.Add(area.Id, area);
                }
            }
        }
    }

    internal class SpellArea
    {
        public int Id;
        public bool IsLinear;
        public int Range;
        public ushort Hue;
        public SpellArea(int id, int range, bool isLinear, ushort hue = 0)
        {
            Id = id;
            Range = range;
            IsLinear = isLinear;
            Hue = hue;
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
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("area");
            writer.WriteAttributeString("id", Id.ToString());
            writer.WriteAttributeString("range", Range.ToString());
            writer.WriteAttributeString("isLinear", IsLinear.ToString());
            writer.WriteAttributeString("hue", Hue.ToString());
            writer.WriteEndElement();
        }
    }
}
