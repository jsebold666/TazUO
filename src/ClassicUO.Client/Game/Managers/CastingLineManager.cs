using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace ClassicUO.Game.Managers
{
    internal class CastingLineManager
    {
        const ushort BACKGROUND_GRAPHIC = 0x805;
        const ushort CAST_GRAPHIC = 0x806;
        public static int[] _stopCastAtClilocs = new int[]
        {
            // procurar clilocs de disturb de magia
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            502632,     // The spell fizzles
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            1061628,    // polymorphed
            1061631,    // disquised
            502644,     // You have not yet recovered from casting a spell.
            1072060,    // You cannot cast a spell while calmed.
            1072112,    // You must have GM Spirit Speaking
        };
        private readonly Dictionary<uint, SpellState> _mobileSpells = new Dictionary<uint, SpellState>();
        public bool IsEnabled => ProfileManager.CurrentProfile != null;

        public void Draw(UltimaBatcher2D batcher)
        {
            Draw(batcher, World.Player.Serial);
        }
        public void Draw(UltimaBatcher2D batcher, uint serial)
        {
            var camera = Client.Game.Scene.Camera;
            if (!IsEnabled)
            {
                return;
            }

            if (_mobileSpells.TryGetValue(serial, out var spell))
            {
                Entity entity = World.Get(serial);
                if (!IsCasting(serial))
                {
                    return;
                }
                Point p = entity.RealScreenPosition;
                p.X += (int)entity.Offset.X + 22;
                p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

                p = Client.Game.Scene.Camera.WorldToScreen(p);

                var texture = GumpsLoader.Instance.GetGumpTexture(BACKGROUND_GRAPHIC, out var bounds);

                p.X -= bounds.Width >> 1;
                p.Y -= bounds.Height >> 1;

                var castRemaining = (int)((spell.CastEndTime - Time.Ticks) * 100 / (spell.CastEndTime - spell.Created));
                int per = bounds.Width * (castRemaining) / 100;

                DrawCastLine
                (
                    batcher,
                    p.X,
                    p.Y,
                    per
                );
            }            
        }      
        private void DrawCastLine(UltimaBatcher2D batcher, int x, int y, int percentage)
        {
            y += 22;
            float alpha = 1.0f;
            ushort hue = Notoriety.GetHue(NotorietyFlag.Innocent);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, alpha);


            const int MULTIPLER = 1;

            var texture = GumpsLoader.Instance.GetGumpTexture(BACKGROUND_GRAPHIC, out var bounds);
            batcher.Draw
            (
                texture,
                new Rectangle
                (
                    x,
                    y,
                    bounds.Width * MULTIPLER,
                    bounds.Height * MULTIPLER
                ),
                bounds,
                hueVec
            );           

            //hueVec.X = 0x21;

            if (percentage < 100)
            {
                hueVec.X = hue;
                texture = GumpsLoader.Instance.GetGumpTexture(CAST_GRAPHIC, out bounds);
                batcher.DrawTiled
                (
                    texture,
                    new Rectangle
                    (
                        x,
                        y,
                        percentage * MULTIPLER,
                        bounds.Height * MULTIPLER
                    ),
                    bounds,
                    hueVec
                );
            }
        }

        public void RegisterCasting(uint serial, int spellId)
        {
            if (IsCasting(serial) || !CanCast(serial))
            {
                return;
            }
            SpellState spell = default;
            if (!_mobileSpells.TryGetValue(serial, out spell))
            {
                _mobileSpells.Add(serial, spell = new SpellState(serial));
            }
            //spell.Value = (SpellAction)spellId;            
        }
        public void StartCasting(uint serial, string spellText)
        {
            if (SpellDefinition.WordToTargettype.TryGetValue(spellText, out SpellDefinition def))
            {
                if (_mobileSpells.TryGetValue(serial, out var spell))
                {
                    spell.Start(def);
                }
            }            
        }
        public void StopCasting(uint serial, uint? ticks = null)
        {
            if (_mobileSpells.TryGetValue(serial, out var spell))
            {
                if (ticks.HasValue && spell.CastEndTime < ticks.Value)
                {
                    _mobileSpells.Remove(serial);
                    return;
                }
                _mobileSpells.Remove(serial);
            }
        }
        public bool IsCasting(uint serial)
        {
            if (_mobileSpells.TryGetValue(serial, out var spell))
            {
                return Time.Ticks < spell.CastEndTime;
            }
            return false;
        }
        public bool CanCast(uint serial)
        {
            if (World.Mobiles.TryGetValue(serial,out var mobile))
            {
                if (mobile.IsParalyzed || mobile.IsDead || mobile.IsDestroyed)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public void HandleSpellCliloc(uint cliloc, uint serial)
        {
            SpellDefinition spell = default;
            SpellState spellState = default;
            if (_mobileSpells.TryGetValue(serial, out spellState))
            {
                if (_stopCastAtClilocs.Contains((int)cliloc))
                {
                    StopCasting(serial);
                    return;
                }
                if (ClilocToSpell(cliloc, out spell))
                {
                    spellState.Start(spell);
                }
                return;
            }
            if (ClilocToSpell(cliloc, out spell))
            {
                RegisterCasting(serial, spell.ID);
                if (_mobileSpells.TryGetValue(serial, out spellState))
                {
                    spellState.Start(spell);
                }
            }
        }

        private bool ClilocToSpell(uint cliloc, out SpellDefinition spell)
        {
            spell = SpellDefinition.EmptySpell;
            string text = ClilocLoader.Instance.Translate((int)cliloc);
            if (string.IsNullOrWhiteSpace(text))
            {                
                return false;
            }
            if (SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition def))
            {
                spell = def;
                return true;    
            }
            return false;
        }

        public static uint GetCastDelay(int spellId, PlayerMobile caster)
        {
            // Faster casting cap of 2 (if not using the protection spell) 
            // Faster casting cap of 0 (if using the protection spell) 
            // Paladin spells are subject to a faster casting cap of 4 
            // Paladins with magery of 70.0 or above are subject to a faster casting cap of 2
            TimeSpan baseDelay = TimeSpan.Zero;
            TimeSpan fcDelay = TimeSpan.Zero; // TimeSpan.FromSeconds(-(1 * 4 * 0.25));
            int multiplier = 1;
            int fcMax = 4;
        

            SpellDefinition spell = default;
            if (SpellsMagery.GetAllSpells.TryGetValue(spellId, out spell)){
                baseDelay = spell.CastDelayBase;
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
            else if (SpellsNecromancy.GetAllSpells.TryGetValue(spellId - 100, out spell))
            {
                baseDelay = spell.CastDelayBase;
                fcMax = 2;
            }
            else if (SpellsChivalry.GetAllSpells.TryGetValue(spellId - 200, out spell))
            {
                baseDelay = spell.CastDelayBase;
                var magerySkill = caster.Skills.Where(s => s.Name.Equals(Enum.GetName(typeof(SpellBookType), SpellBookType.Magery))).FirstOrDefault();
                var mysticism = caster.Skills.Where(s => s.Name.Equals(Enum.GetName(typeof(SpellBookType), SpellBookType.Mysticism))).FirstOrDefault();
                if ((magerySkill != null && magerySkill.Value > 70.0) || (mysticism != null && mysticism.Value > 70.0))
                {
                    fcMax = 2;
                }

            }
            else if (SpellsBushido.GetAllSpells.TryGetValue(spellId - 400, out spell))
            {
                return (uint)TimeSpan.Zero.TotalMilliseconds;
            }

            
            int fc = Math.Min(caster.FasterCasting, fcMax);
            if (caster.BuffIcons.TryGetValue(BuffIconType.Protection, out _))
            {
                fc = Math.Min(fcMax - 2, fc - 2);
            }

            fcDelay = TimeSpan.FromSeconds(-(1 * fc * 0.25));


            TimeSpan delay = baseDelay + fcDelay;
            if (multiplier > 1)
            {
                delay += TimeSpan.FromSeconds(multiplier);
            }

            if (delay < TimeSpan.FromSeconds(0.25))
            {
                delay = TimeSpan.FromSeconds(0.25);
            }
            return (uint)delay.TotalMilliseconds;
        }
            }

    internal class SpellState
    {
        public SpellState(uint serial) {
            if (World.Mobiles.TryGetValue(serial, out var m)){
                _caster = m;
            }
        }
        private Mobile _caster;
        private uint _lastCreation = Time.Ticks;
        private uint _castEnd = Time.Ticks;

        public uint Created { get { return _lastCreation; } }

        public uint CastEndTime
        {
            get
            {
                return _castEnd;
            }
        }

        public void Start(SpellDefinition spell)
        {
            _lastCreation = Time.Ticks;
            _castEnd = _lastCreation + CastingLineManager.GetCastDelay(spell.ID, (PlayerMobile)_caster);
        }
    }
}
