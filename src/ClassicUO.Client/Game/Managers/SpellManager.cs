using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
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
        private SpellState _state = SpellState.None; 
        public SpellDefinition CurrentSpell = SpellDefinition.EmptySpell;
        public SpellBookType BookType = SpellBookType.Unknown;
        private readonly List<GameObject> _spellAreaTiles = new List<GameObject>();
        private readonly Dictionary<int, SpellArea> _spellAreas = new Dictionary<int, SpellArea>();
        private CastBarManager _castBarManager = new CastBarManager();

        public IReadOnlyDictionary<int, SpellArea> GetAllSpellAreas => _spellAreas;
        public SpellManager() {
            Load();
            MessageManager.MessageReceived += MessageManager_MessageReceived;
        }

        public SpellArea[] SpellAreas { get { return _spellAreas.Values.ToArray(); } }

        public SpellState State { get => _state; private set
            {
                _state = value;
            }
        }

        public SpellDefinition[] GetAvailableSpells()
        {
            var existingKeys = GetAllSpellAreas.Keys;
            return SpellsMagery.GetAllSpells.Values
                .Concat(SpellsNecromancy.GetAllSpells.Values)
                .Concat(SpellsChivalry.GetAllSpells.Values)
                .Concat(SpellsSpellweaving.GetAllSpells.Values)
                .Concat(SpellsMysticism.GetAllSpells.Values)
                .Concat(SpellsMastery.GetAllSpells.Values).Where(def => !existingKeys.Contains(def.ID)).OrderBy(d => d.Name).ToArray();
        }

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
        public bool TryGetSpellDuration(int id, out long duration)
        {
            return _castBarManager.SpellDurations.TryGetValue(id, out duration);
        }
        public void AddSpellArea(SpellArea area, long duration = 2500)
        {            
            if (_castBarManager.SpellDurations.ContainsKey(area.Id))
            {
                _castBarManager.SpellDurations[area.Id] = duration;
            }
            else
            {
                _castBarManager.SpellDurations.Add(area.Id, duration);
            }
            _spellAreas.Add(area.Id, area);
        }
        public void RemoveSpellAreaById(int id)
        {
            _spellAreas.Remove(id);
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
            _castBarManager.Spell = CurrentSpell;
            if (CurrentSpell == SpellDefinition.EmptySpell)
            {
                State = SpellState.None;
                _castBarManager.State = SpellState.None;
                BookType = SpellBookType.Unknown;
                TargetManager.Reset();
                return;
            }
            State = SpellState.Casting;
            _castBarManager.State = SpellState.Casting;
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
            if (State == SpellState.Casting)
            {
                return;
            }
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

        public void Update()
        {
            //|| !UIManager.IsMouseOverWorld
            if (!ProfileManager.CurrentProfile.SpellAreaHighlights ||  State == SpellState.None || !_spellAreas.TryGetValue(CurrentSpell.ID, out var area) )
            {
                CurrentSpell = SpellDefinition.EmptySpell;
                State = SpellState.None;
                ClearSpellAreasTiles();
                return;
            }
            if (TargetManager.IsTargeting && State == SpellState.Casting)
            {
                State = SpellState.Sequencing;
                _castBarManager.State = SpellState.Sequencing;
            }
            if (!TargetManager.IsTargeting && State == SpellState.Sequencing)
            {
                CancelSpell();
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
        }
        
        public void DrawCasterBar(UltimaBatcher2D batcher)
        {
            _castBarManager.DrawCasterBar(batcher);
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
        public static int HotKeyActionToSpellId(HotkeyAction action)
        {
            var actionInt = (int)action;
            if (actionInt >= 65 && actionInt <= 81)
            {
                return actionInt += 36;
            }
            else if (actionInt >= 82 && actionInt <= 91)
            {
                return actionInt += 119;
            }
            else if (actionInt >= 92 && actionInt <= 97)
            {
                return actionInt += 309;
            }
            else if (actionInt >= 106 && actionInt <= 120)
            {
                return actionInt += 495;
            }
            else if (actionInt >= 122 && actionInt <= 137)
            {
                return actionInt += 556;
            }
            else if (actionInt >= 138 && actionInt <= 182)
            {
                return actionInt += 563;
            }
            return actionInt;
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
                xml.WriteStartElement("spells");
                xml.WriteStartElement("spell_areas");

                foreach (SpellArea area in _spellAreas.Values)
                {
                    area.Save(xml);
                }
                xml.WriteEndElement();

                xml.WriteStartElement("spell_durations");
                foreach (var pair in _castBarManager.SpellDurations)
                {
                    xml.WriteStartElement("spell_duration");
                    xml.WriteAttributeString("id", pair.Key.ToString());
                    xml.WriteAttributeString("duration", pair.Value.ToString());
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();

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
                _castBarManager.CreateDefault();
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

            XmlElement root = doc["spells"];

            if (root != null)
            {
                XmlElement _subRoot = root["spell_areas"];
                if (_subRoot != null)
                {
                    foreach (XmlElement xml in _subRoot.GetElementsByTagName("spell_area"))
                    {
                        var area = new SpellArea(xml);
                        _spellAreas.Add(area.Id, area);
                    }
                }
                _subRoot = root["spell_durations"];
                if (_subRoot != null)
                {
                    foreach (XmlElement xml in _subRoot.GetElementsByTagName("spell_duration"))
                    {
                        if (xml == null || !int.TryParse(xml.GetAttribute("id"), out var spellId))
                        {
                            return;
                        }
                        if (int.TryParse(xml.GetAttribute("duration"), out var duration))
                        {
                            _castBarManager.SpellDurations.Add(spellId, duration);
                        }
                    }
                }
            }

            
        }
    }

    internal class CastBarManager
    {
        public static double CastDelaySecondsPerTick => 0.25;
        public SpellState State = SpellState.None;
        private SpellDefinition _spell = SpellDefinition.EmptySpell;
        public SpellDefinition Spell { get => _spell; 
            set
            {
                _spell = value;
                _lastCreation = Time.Ticks;
                _castEnd = _lastCreation + GetCastDelay(value.ID, World.Player);
            }
        }
        public Dictionary<int,long> SpellDurations = new Dictionary<int,long>();

        private uint _lastCreation = Time.Ticks;
        private uint _castEnd = Time.Ticks;

        public void CreateDefault()
        {

            #region Magery
            var mageryCircleLength = SpellsMagery.CircleNames.Length;
            foreach (var spell in SpellsMagery.GetAllSpells.Values)
            {
                var circle = (1 + (spell.ID / mageryCircleLength));
                SpellDurations.Add(spell.ID, TimeSpan.FromSeconds((3 + circle) * CastDelaySecondsPerTick).Ticks);
            }
            #endregion

            #region Necromancy
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastAnimatedDead), TimeSpan.FromSeconds(1.75).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastBloodOath), TimeSpan.FromSeconds(1.75).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastCorpseSkin), TimeSpan.FromSeconds(1.75).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastCurseWeapon), TimeSpan.FromSeconds(1.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastEvilOmen), TimeSpan.FromSeconds(1.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastHorrificBeast), TimeSpan.FromSeconds(2.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastLichForm), TimeSpan.FromSeconds(2.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastMindRot), TimeSpan.FromSeconds(1.75).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastPainSpike), TimeSpan.FromSeconds(1.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastPoisonStrike), TimeSpan.FromSeconds(2).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastStrangle), TimeSpan.FromSeconds(2.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastSummonFamiliar), TimeSpan.FromSeconds(2.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastVampiricEmbrace), TimeSpan.FromSeconds(2.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastVangefulSpririt), TimeSpan.FromSeconds(2.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastWither), TimeSpan.FromSeconds(1.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastWraithForm), TimeSpan.FromSeconds(2.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastExorcism), TimeSpan.FromSeconds(2).Ticks);
            #endregion

            #region chivlary
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastCleanseByFire), TimeSpan.FromSeconds(1.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastCloseWounds), TimeSpan.FromSeconds(1.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastConsecrateWeapon), TimeSpan.FromSeconds(0.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastDispelEvil), TimeSpan.FromSeconds(0.25).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastDivineFury), TimeSpan.FromSeconds(1.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastEnemyOfOne), TimeSpan.FromSeconds(0.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastHolyLight), TimeSpan.FromSeconds(1.75).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastNobleSacrifice), TimeSpan.FromSeconds(1.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastRemoveCurse), TimeSpan.FromSeconds(2.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastSacredJourney), TimeSpan.FromSeconds(1.5).Ticks);
            #endregion

            #region Spellweaving
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastArcaneCircle), TimeSpan.FromSeconds(0.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastGiftOfRenewal), TimeSpan.FromSeconds(3).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastImmolatingWeapon), TimeSpan.FromSeconds(1.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastAttuneWeapon), TimeSpan.FromSeconds(1.0).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastThinderstorm), TimeSpan.FromSeconds(1.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastNaturesFury), TimeSpan.FromSeconds(1.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastSummonFey), TimeSpan.FromSeconds(1.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastSummonFiend), TimeSpan.FromSeconds(2).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastReaperForm), TimeSpan.FromSeconds(2.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastWildFire), TimeSpan.FromSeconds(2.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastEssenceOfWind), TimeSpan.FromSeconds(3).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastDryadAllure), TimeSpan.FromSeconds(3).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastEtherealVoyage), TimeSpan.FromSeconds(3.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastWordOfDeath), TimeSpan.FromSeconds(3.5).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastGiftOfLife), TimeSpan.FromSeconds(4).Ticks);
            SpellDurations.Add(SpellManager.HotKeyActionToSpellId(HotkeyAction.CastArcaneEmpowerment), TimeSpan.FromSeconds(3).Ticks);
            #endregion

            #region Mysticism
            foreach (var spell in SpellsMysticism.GetAllSpells.Values)
            {
                var circle = 1 + (((spell.ID - 78) % 100) / 2);
                SpellDurations.Add(spell.ID, TimeSpan.FromSeconds((4 + circle) * CastDelaySecondsPerTick).Ticks);
            }

            //var list = new Dictionary<int, int>(){

            //    {(int)HotkeyAction.CastNetherBolt,1 },
            //    {(int)HotkeyAction.CastHealingStone,1},
            //    {(int)HotkeyAction.CastPurgeMagic,2},
            //    {(int)HotkeyAction.CastEnchant,2},
            //    {(int)HotkeyAction.CastSleep,3},
            //    {(int)HotkeyAction.CastEagleStrike,3},
            //    {(int)HotkeyAction.CastAnimatedWeapon,4},
            //    {(int)HotkeyAction.CastStoneForm,4},
            //    {(int)HotkeyAction.CastSpellTrigger,5},
            //    {(int)HotkeyAction.CastMassSleep,5},
            //    {(int)HotkeyAction.CastCleansingWinds,6},
            //    {(int)HotkeyAction.CastBombard,6},
            //    {(int)HotkeyAction.CastSpellPlague,7},
            //    {(int)HotkeyAction.CastHailStorm,7},
            //    {(int)HotkeyAction.CastNetherCyclone,8},
            //    {(int)HotkeyAction.CastRisingColossus,8}
            //};
            #endregion


        }

        private uint GetCastDelay(int spellId, PlayerMobile caster)
        {
            // Faster casting cap of 2 (if not using the protection spell) 
            // Faster casting cap of 0 (if using the protection spell) 
            // Paladin spells are subject to a faster casting cap of 4 
            // Paladins with magery of 70.0 or above are subject to a faster casting cap of 2
            TimeSpan baseDelay = TimeSpan.Zero;
            TimeSpan fcDelay = TimeSpan.Zero;
            int multiplier = 1;
            int fcMax = 4;

            if (SpellDurations.TryGetValue(spellId, out var duration))
            {
                baseDelay = TimeSpan.FromTicks(duration);

                if (SpellsMagery.GetAllSpells.TryGetValue(spellId, out var _current))
                {
                    fcMax = 2;
                    if (spellId == 33)  //bladeSpirit
                    {
                        multiplier = 3;
                    }
                    if (spellId == 40) //Summon Creature
                    {
                        multiplier = 5;
                    }
                }
                else if (SpellsNecromancy.GetAllSpells.TryGetValue(spellId - 100, out _current))
                {
                    fcMax = 2;
                }
                else if (SpellsChivalry.GetAllSpells.TryGetValue(spellId - 200, out _current))
                {
                    var magerySkill = caster.Skills.Where(s => s.Name.Equals(Enum.GetName(typeof(SpellBookType), SpellBookType.Magery))).FirstOrDefault();
                    var mysticism = caster.Skills.Where(s => s.Name.Equals(Enum.GetName(typeof(SpellBookType), SpellBookType.Mysticism))).FirstOrDefault();
                    if ((magerySkill != null && magerySkill.Value > 70.0) || (mysticism != null && mysticism.Value > 70.0))
                    {
                        fcMax = 2;
                    }
                }
                else if (SpellsBushido.GetAllSpells.TryGetValue(spellId - 400, out _current))
                {
                    return (uint)TimeSpan.Zero.TotalMilliseconds;
                }
            }

            int fc = Math.Min(caster.FasterCasting, fcMax);
            if (caster.BuffIcons.TryGetValue(BuffIconType.Protection, out _))
            {
                fc = Math.Min(fcMax - 2, fc - 2);
            }else if (caster.BuffIcons.TryGetValue(BuffIconType.ArchProtection, out _))
            {
                fc = Math.Min(fcMax - 2, fc - 2);
            }


            fcDelay = TimeSpan.FromSeconds(-(1 * fc * CastDelaySecondsPerTick));


            TimeSpan delay = baseDelay + fcDelay;
            if (multiplier > 1)
            {
                delay += TimeSpan.FromSeconds(multiplier);
            }

            if (delay < TimeSpan.FromSeconds(CastDelaySecondsPerTick))
            {
                delay = TimeSpan.FromSeconds(CastDelaySecondsPerTick);
            }
            return (uint)delay.TotalMilliseconds;
        }
        public void DrawCasterBar(UltimaBatcher2D batcher)
        {
            if (State != SpellState.Casting || !SpellDurations.TryGetValue(Spell.ID, out var duration))
            {
                return;
            }
            else
            {
                if (TargetManager.IsTargeting)
                {
                    State = SpellState.Sequencing;
                    return;
                }
            }

            Entity entity = World.Player;
            Point p = entity.RealScreenPosition;
            p.X += (int)entity.Offset.X + 22;
            p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

            p = Client.Game.Scene.Camera.WorldToScreen(p);

            var texture = GumpsLoader.Instance.GetGumpTexture(0x805, out var bounds);

            p.X -= bounds.Width >> 1;
            p.Y -= bounds.Height >> 1;

            var castRemaining = (int)((_castEnd - Time.Ticks) * 100 / (_castEnd - _lastCreation));
            int percentage = bounds.Width * (castRemaining) / 100;

            p.Y += 22;
            float alpha = 1.0f;
            ushort hue = Notoriety.GetHue(NotorietyFlag.Innocent);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, alpha);

            const int MULTIPLER = 1;
            if (percentage < 100)
            {
                batcher.Draw
                  (
                      texture,
                      new Rectangle
                      (
                          p.X,
                          p.Y,
                          bounds.Width * MULTIPLER,
                          bounds.Height * MULTIPLER
                      ),
                      bounds,
                      hueVec
                  );
            }         

            //hueVec.X = 0x21;

            if (percentage < 100)
            {
                hueVec.X = hue;
                texture = GumpsLoader.Instance.GetGumpTexture(0x806, out bounds);
                batcher.DrawTiled
                (
                    texture,
                    new Rectangle
                    (
                        p.X,
                        p.Y,
                        percentage * MULTIPLER,
                        bounds.Height * MULTIPLER
                    ),
                    bounds,
                    hueVec
                );
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
