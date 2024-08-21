#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class InfoBarGump : ResizableGump
    {
        private readonly AlphaBlendControl _background;

        private readonly List<InfoBarControl> _infobarControls = new List<InfoBarControl>();
        private long _refreshTime;

<<<<<<< HEAD
        public override bool IsLocked => _isLocked;

        public InfoBarGump() : base(ProfileManager.CurrentProfile.InfoBarSize.X, ProfileManager.CurrentProfile.InfoBarSize.Y, 50, 20, 0, 0)
=======
        public InfoBarGump(World world) : base(world, 0, 0)
>>>>>>> externo/main
        {
            CanBeLocked = true; //For base gump locking, resizable uses a special locking procedure
            CanMove = true;
            _prevCanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            _prevCloseWithRightClick = false;
            ShowBorder = true;
            _prevBorder = true;

            Insert(0, _background = new AlphaBlendControl(0.7f) { Width = Width - 8, Height = Height - 8, X = 4, Y = 4, Parent = this });

            ResetItems();

        }

        public override GumpType GumpType => GumpType.InfoBar;

        public void ResetItems()
        {
            foreach (InfoBarControl c in _infobarControls)
            {
                c.Dispose();
            }

            _infobarControls.Clear();

            List<InfoBarItem> infoBarItems = World.InfoBars.GetInfoBars();

            for (int i = 0; i < infoBarItems.Count; i++)
            {
                InfoBarControl info = new InfoBarControl(this, infoBarItems[i].label, infoBarItems[i].var, infoBarItems[i].hue);

                _infobarControls.Add(info);
                Add(info);
            }
        }

        public void UpdateOptions()
        {
            ResetItems();
        }

        public static void UpdateAllOptions()
        {
            foreach(InfoBarGump g in UIManager.Gumps.OfType<InfoBarGump>())
            {
                g.UpdateOptions();
            }
        }

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < Time.Ticks)
            {
                _refreshTime = (long)Time.Ticks + 250;

                int x = 6, y = 6;

                foreach (InfoBarControl c in _infobarControls)
                {
                    if (x + c.Width + 8 > Width)
                    {
                        y += c.Height;
                        x = 6;
                    }

                    c.X = x;
                    c.Y = y;

                    x += c.Width + 8;
                }
                ProfileManager.CurrentProfile.InfoBarLocked = IsLocked;
            }

            base.Update();

            _background.Width = Width - 8;
            _background.Height = Height - 8;
        }

        public override void OnResize()
        {
            base.OnResize();

            ProfileManager.CurrentProfile.InfoBarSize = new Point(Width, Height);
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            SetLockStatus(ProfileManager.CurrentProfile.InfoBarLocked);
        }
    }


    internal class InfoBarControl : Control
    {
<<<<<<< HEAD
        private readonly TextBox _data;
        private readonly TextBox _label;
        private readonly ResizableStaticPic _pic;
=======
        private readonly InfoBarGump _gump;
        private readonly Label _data;
        private readonly Label _label;
>>>>>>> externo/main
        private ushort _warningLinesHue;

        public InfoBarControl(InfoBarGump gump, string label, InfoBarVars var, ushort hue)
        {
            _gump = gump;
            AcceptMouseInput = false;
            WantUpdateSize = true;
            CanMove = false;
            Hue = hue;

            _label = new TextBox(
                label,
                ProfileManager.CurrentProfile.InfoBarFont,
                ProfileManager.CurrentProfile.InfoBarFontSize,
                null,
                hue,
                strokeEffect: false
                );
            if (label.StartsWith(@"\"))
            {
                if (ushort.TryParse(label.Substring(1), out ushort gphc))
                {
                    _label.IsVisible = false;
                    Add(_pic = new ResizableStaticPic(gphc, 20, 20) { Hue = hue });
                }
            }

            Var = var;

            _data = new TextBox(
                "",
                ProfileManager.CurrentProfile.InfoBarFont,
                ProfileManager.CurrentProfile.InfoBarFontSize,
                null,
                0x0481,
                strokeEffect: false
                )
            { X = _label.IsVisible ? _label.Width + 3 : _pic.Width };

            Add(_label);
            Add(_data);
        }

        public string Text => _label.Text;
        public InfoBarVars Var { get; }

        public ushort Hue { get; }
        protected long _refreshTime = (long)Time.Ticks - 1;

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < Time.Ticks)
            {
                _refreshTime = (long)Time.Ticks + 250;

                string newData = GetVarData(Var) ?? string.Empty;
                if (!newData.Equals(_data.Text))
                {
                    _data.UpdateText(newData);
                    _data.WantUpdateSize = true;
                    WantUpdateSize = true;
                }

                if (ProfileManager.CurrentProfile.InfoBarHighlightType == 0 || Var == InfoBarVars.NameNotoriety)
                {
                    ushort hue = GetVarHue(Var);
                    if (!hue.Equals((ushort)_data.Hue))
                    {
                        _data.Hue = hue;
                    }
                }
                else
                {
                    if ((ushort)_data.Hue != 0x0481)
                    {
                        _data.Hue = 0x0481;
                    }
                    _warningLinesHue = GetVarHue(Var);
                }
            }

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (Var != InfoBarVars.NameNotoriety && ProfileManager.CurrentProfile.InfoBarHighlightType == 1 && _warningLinesHue != 0x0481)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(_warningLinesHue);

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle
                    (
                        _data.ScreenCoordinateX,
                        _data.ScreenCoordinateY,
                        _data.Width,
                        2
                    ),
                    hueVector
                );

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle
                    (
                        _data.ScreenCoordinateX,
                        _data.ScreenCoordinateY + Parent.Height - 2,
                        _data.Width,
                        2
                    ),
                    hueVector
                );
            }

            return true;
        }

        private string GetVarData(InfoBarVars var)
        {
            switch (var)
            {
                case InfoBarVars.HP: return $"{_gump.World.Player.Hits}/{_gump.World.Player.HitsMax}";

                case InfoBarVars.Mana: return $"{_gump.World.Player.Mana}/{_gump.World.Player.ManaMax}";

                case InfoBarVars.Stamina: return $"{_gump.World.Player.Stamina}/{_gump.World.Player.StaminaMax}";

                case InfoBarVars.Weight: return $"{_gump.World.Player.Weight}/{_gump.World.Player.WeightMax}";

                case InfoBarVars.Followers: return $"{_gump.World.Player.Followers}/{_gump.World.Player.FollowersMax}";

                case InfoBarVars.Gold: return _gump.World.Player.Gold.ToString();

                case InfoBarVars.Damage: return $"{_gump.World.Player.DamageMin}-{_gump.World.Player.DamageMax}";

                case InfoBarVars.Armor: return _gump.World.Player.PhysicalResistance.ToString();

                case InfoBarVars.Luck: return _gump.World.Player.Luck.ToString();

                case InfoBarVars.FireResist: return _gump.World.Player.FireResistance.ToString();

                case InfoBarVars.ColdResist: return _gump.World.Player.ColdResistance.ToString();

                case InfoBarVars.PoisonResist: return _gump.World.Player.PoisonResistance.ToString();

                case InfoBarVars.EnergyResist: return _gump.World.Player.EnergyResistance.ToString();

                case InfoBarVars.LowerReagentCost: return _gump.World.Player.LowerReagentCost.ToString();

                case InfoBarVars.SpellDamageInc: return _gump.World.Player.SpellDamageIncrease.ToString();

                case InfoBarVars.FasterCasting: return _gump.World.Player.FasterCasting.ToString();

                case InfoBarVars.FasterCastRecovery: return _gump.World.Player.FasterCastRecovery.ToString();

                case InfoBarVars.HitChanceInc: return _gump.World.Player.HitChanceIncrease.ToString();

                case InfoBarVars.DefenseChanceInc: return _gump.World.Player.DefenseChanceIncrease.ToString();

                case InfoBarVars.LowerManaCost: return _gump.World.Player.LowerManaCost.ToString();

                case InfoBarVars.DamageChanceInc: return _gump.World.Player.DamageIncrease.ToString();

                case InfoBarVars.SwingSpeedInc: return _gump.World.Player.SwingSpeedIncrease.ToString();

                case InfoBarVars.StatsCap: return _gump.World.Player.StatsCap.ToString();

                case InfoBarVars.NameNotoriety: return _gump.World.Player.Name;

                case InfoBarVars.TithingPoints: return _gump.World.Player.TithingPoints.ToString();

                default: return "";
            }
        }

        private ushort GetVarHue(InfoBarVars var)
        {
            float percent;

            switch (var)
            {
                case InfoBarVars.HP:
<<<<<<< HEAD
                    percent = World.Player.Hits / (float)World.Player.HitsMax;
=======
                    percent = _gump.World.Player.Hits / (float) _gump.World.Player.HitsMax;
>>>>>>> externo/main

                    if (percent <= 0.25)
                    {
                        return 0x0021;
                    }
                    else if (percent <= 0.5)
                    {
                        return 0x0030;
                    }
                    else if (percent <= 0.75)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.Mana:
<<<<<<< HEAD
                    percent = World.Player.Mana / (float)World.Player.ManaMax;
=======
                    percent = _gump.World.Player.Mana / (float) _gump.World.Player.ManaMax;
>>>>>>> externo/main

                    if (percent <= 0.25)
                    {
                        return 0x0021;
                    }
                    else if (percent <= 0.5)
                    {
                        return 0x0030;
                    }
                    else if (percent <= 0.75)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.Stamina:
<<<<<<< HEAD
                    percent = World.Player.Stamina / (float)World.Player.StaminaMax;
=======
                    percent = _gump.World.Player.Stamina / (float)_gump.World.Player.StaminaMax;
>>>>>>> externo/main

                    if (percent <= 0.25)
                    {
                        return 0x0021;
                    }
                    else if (percent <= 0.5)
                    {
                        return 0x0030;
                    }
                    else if (percent <= 0.75)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.Weight:
<<<<<<< HEAD
                    percent = World.Player.Weight / (float)World.Player.WeightMax;
=======
                    percent = _gump.World.Player.Weight / (float)_gump.World.Player.WeightMax;
>>>>>>> externo/main

                    if (percent >= 1)
                    {
                        return 0x0021;
                    }
                    else if (percent >= 0.75)
                    {
                        return 0x0030;
                    }
                    else if (percent >= 0.5)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.NameNotoriety: return Notoriety.GetHue(_gump.World.Player.NotorietyFlag);

                default: return 0x0481;
            }
        }
    }
}