﻿#region license

// Copyright (c) 2021, andreakarasho
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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
// ## BEGIN - END ## // OVERHEAD / UNDERCHAR
using ClassicUO.Dust765.Dust765;
// ## BEGIN - END ## // OVERHEAD / UNDERCHAR
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class BaseHealthBarGump : AnchorableGump
    {
        private bool _targetBroke;
        public static readonly MemoryCache entityCache = new MemoryCache(new MemoryCacheOptions());

        public bool IsLastAttackBar { get; set; } = false;
        public static BaseHealthBarGump LastAttackBar { get; set; }
        protected bool HasBeenBuilt { get; set; } = false;

        protected BaseHealthBarGump(Entity entity) : this(0, 0)
        {
            if (entity == null || entity.IsDestroyed)
            {
                Dispose();

                return;
            }

            GameActions.RequestMobileStatus(entity.Serial, true);
            LocalSerial = entity.Serial;
            CanCloseWithRightClick = true;
            _name = entity.Name;
            _isDead = entity is Mobile mm && mm.IsDead;

            // ## BEGIN - END ## // MISC
            LocalEntity = entity;
            // ## BEGIN - END ## // MISC


            if (!entityCache.TryGetValue(entity.Serial, out Entity cachedEntity))
            {
                // A entidade não foi encontrada no cache, então a adicionamos
                entityCache.Set(entity.Serial, entity, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(2),

                    Priority = CacheItemPriority.Normal
                });
            }


            BuildGump();
            HasBeenBuilt = true;
        }

        public virtual void SetNewMobile(uint serial)
        {
            if (World.Mobiles.TryGetValue(serial, out Mobile m))
            {
                LocalSerial = serial;
                _name = m.Name;
                _isDead = m.IsDead;

                Children.Clear();
                BuildGump();
                HasBeenBuilt = true;
            }
        }

        protected BaseHealthBarGump(uint serial) : this(World.Get(serial))
        {
        }

        protected BaseHealthBarGump(uint local, uint server) : base(local, server)
        {
            CanMove = true;
            AnchorType = ANCHOR_TYPE.HEALTHBAR;
        }

        public override int GroupMatrixWidth
        {
            get => Width;
            protected set { }
        }

        public override int GroupMatrixHeight
        {
            get => Height;
            protected set { }
        }

        public override GumpType GumpType => GumpType.HealthBar;
        internal bool IsInactive => (_isDead || _outOfRange) && !_canChangeName;
        protected bool _canChangeName;
        protected bool _isDead;
        protected string _name;
        protected bool _outOfRange;
        protected StbTextBox _textBox;

        public bool IsLastTarget { get; set; } = false;

        private bool _locked = false;
        private new bool IsLocked
        {
            get { return _locked; }
            set
            {
                _locked = value;
                if (_locked)
                {
                    CanMove = false;
                    CanCloseWithRightClick = false;
                }
                else
                {
                    CanMove = true;
                    CanCloseWithRightClick = true;
                }
            }
        }

        protected abstract void BuildGump();

        //public override void AfterDispose()
        //{
        //    base.AfterDispose();

        //    _textBox?.Dispose();
        //    _textBox = null;
        //}

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            if (IsLastTarget && ProfileManager.CurrentProfile != null)
                ProfileManager.CurrentProfile.LastTargetHealthBarPos = Location;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            if (ProfileManager.CurrentProfile.SaveHealthbars)
            {
                writer.WriteAttributeString("name", _name);
                writer.WriteAttributeString("locked", IsLocked.ToString());
                writer.WriteAttributeString("lastAttackSingle", IsLastAttackBar.ToString());
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if (LocalSerial == World.Player)
            {
                _name = World.Player.Name;
                BuildGump();
                HasBeenBuilt = true;
            }
            else if (ProfileManager.CurrentProfile.SaveHealthbars)
            {
                _name = xml.GetAttribute("name");
                if (bool.TryParse(xml.GetAttribute("locked"), out bool locked))
                {
                    IsLocked = locked;
                }

                if (bool.TryParse(xml.GetAttribute("lastAttackSingle"), out bool lastAttack))
                {
                    if (lastAttack)
                    {
                        LastAttackBar = this;
                        IsLastAttackBar = lastAttack;
                    }
                }

                _outOfRange = true;
                BuildGump();
                HasBeenBuilt = true;
            }
            else
            {
                Dispose();
            }
        }

        protected void TextBoxOnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
            {
                return;
            }

            if (World.Get(LocalSerial) == null)
            {
                return;
            }

            Point p = Mouse.LDragOffset;

            if (Math.Max(Math.Abs(p.X), Math.Abs(p.Y)) >= 1)
            {
                return;
            }

            if (TargetManager.IsTargeting)
            {
                TargetManager.Target(LocalSerial);
                Mouse.LastLeftButtonClickTime = 0;
            }
            // ## BEGIN - END ## // MISC
            //else if (_canChangeName && !_targetBroke)
            // ## BEGIN - END ## // MISC
            else if (_canChangeName)
            // ## BEGIN - END ## // MISC
            {
                _textBox.IsEditable = true;
                _textBox.SetKeyboardFocus();
            }

            // ## BEGIN - END ## // MISC
            //_targetBroke = false;
            // ## BEGIN - END ## // MISC
        }

        protected static int CalculatePercents(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                {
                    max = 100;
                }

                if (max > 1)
                {
                    max = maxValue * max / 100;
                }
            }

            return max;
        }

        protected override void OnDragEnd(int x, int y)
        {
            // when dragging an healthbar with target on, we have to reset the dclick timer 
            if (TargetManager.IsTargeting)
            {
                Mouse.LastLeftButtonClickTime = 0;
                Mouse.CancelDoubleClick = true;
            }

            base.OnDragEnd(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left && Keyboard.Alt)
            {
                if (UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
                {
                    ref readonly var texture = ref Client.Game.Gumps.GetGump(0x82C);
                    if (texture.Texture != null)
                    {
                        if (x >= 0 && x < texture.UV.Width && y >= 0 && y <= texture.UV.Height)
                        {
                            IsLocked = !IsLocked;
                            return;
                        }
                    }
                }

                if (!ProfileManager.CurrentProfile.DisableAutoFollowAlt)
                {
                    MessageManager.HandleMessage
                    (
                        World.Player,
                        ResGeneral.NowFollowing,
                        string.Empty,
                        0,
                        MessageType.Regular,
                        3,
                        TextType.CLIENT
                    );
                    ProfileManager.CurrentProfile.FollowingMode = true;
                    ProfileManager.CurrentProfile.FollowingTarget = LocalSerial;
                }
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            if (TargetManager.IsTargeting)
            {
                // ## BEGIN - END ## // MISC
                // ## BEGIN - END ## // MISC
                //_targetBroke = true;
                // ## BEGIN - END ## // MISC
                TargetManager.Target(LocalSerial);
                // ## BEGIN - END ## // MISC
                Entity ent = World.Get(LocalSerial);

                if (ProfileManager.CurrentProfile.SetTargetOut) {
                    if (ent == null)
                    {

                        if (LocalSerial != null)
                        {

                            GameActions.Print(World.Player, $"Target OutRange: {_name}");
                            TargetManager.LastTargetInfo.Serial = LocalSerial;
                            TargetManager.TargetFromHealthBar(LocalSerial);
                        }
                        else
                        {

                            Entity cachedEntity;
                            if (entityCache.TryGetValue(LocalSerial, out cachedEntity))
                            {

                                GameActions.Print(World.Player, $"Target OutRange : {cachedEntity.Name}");
                                TargetManager.LastTargetInfo.Serial = cachedEntity.Serial;
                                TargetManager.TargetFromHealthBar(cachedEntity.Serial);

                            }
                            else
                            {
                                GameActions.Print($"No has info for Target, need see for updates infos.", 88);
                            }


                        }
                    }
                } else {
                    if (ent == null)
                    {
                        TargetManager.LastTargetInfo.Serial = LocalSerial;
                        TargetManager.CancelTarget();
                    }
                
                    else
                    {
                        if (LocalEntity != null && LocalEntity.Serial != default)
                        {
                            TargetManager.LastTargetInfo.Serial = LocalEntity.Serial;
                        }
                        
                    }
                }
                // ## BEGIN - END ## // MISC
                Mouse.LastLeftButtonClickTime = 0;
            }
            else if (_canChangeName)
            {
                if (_textBox != null)
                {
                    _textBox.IsEditable = false;
                }

                UIManager.KeyboardFocusControl = null;
                UIManager.SystemChat?.SetFocus();
            }

            base.OnMouseDown(x, y, button);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return false;
            }

            if (_canChangeName)
            {
                if (_textBox != null)
                {
                    _textBox.IsEditable = false;
                }

                UIManager.KeyboardFocusControl = null;
                UIManager.SystemChat?.SetFocus();
            }

            Entity entity = World.Get(LocalSerial);

            if (entity != null)
            {
                if (entity != World.Player)
                {
                    if (World.Player.InWarMode)
                    {
                        GameActions.Attack(entity);
                    }
                    else if (!GameActions.OpenCorpse(entity))
                    {
                        GameActions.DoubleClick(entity);
                    }
                }
                else
                {
                    if (StatusGumpBase.GetStatusGump() == null)
                        UIManager.Add(StatusGumpBase.AddStatusGump(ProfileManager.CurrentProfile.StatusGumpPosition.X, ProfileManager.CurrentProfile.StatusGumpPosition.Y));
                }
            }

            return true;
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            Entity entity = World.Get(LocalSerial);

            if (entity == null || SerialHelper.IsItem(entity.Serial))
            {
                return;
            }

            if ((key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER) && _textBox != null && _textBox.IsEditable)
            {
                GameActions.Rename(entity, _textBox.Text);
                UIManager.KeyboardFocusControl = null;
                UIManager.SystemChat?.SetFocus();
                _textBox.IsEditable = false;
            }
        }

        protected override void OnMouseOver(int x, int y)
        {
            Entity entity = World.Get(LocalSerial);

            if (entity != null)
            {
                SelectedObject.HealthbarObject = entity;
                SelectedObject.Object = entity;
            }

            base.OnMouseOver(x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            base.Draw(batcher, x, y);

            if (Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                ref readonly var texture = ref Client.Game.Gumps.GetGump(0x82C);

                if (texture.Texture != null)
                {
                    if (IsLocked)
                    {
                        hueVector.X = 34;
                        hueVector.Y = 1;
                    }
                    batcher.Draw
                    (
                        texture.Texture,
                        new Vector2(x, y),
                        texture.UV,
                        hueVector
                    );
                }
            }

            return true;
        }

        protected bool CheckIfAnchoredElseDispose()
        {
            if (IsLocked || IsLastAttackBar)
            {
                return false;
            }
            if ((UIManager.AnchorManager[this] == null || ProfileManager.CurrentProfile.CloseHealthBarIfAnchored) && LocalSerial != World.Player)
            {
                Dispose();

                return true;
            }

            return false;
        }
    }

    internal class HealthBarGumpCustom : BaseHealthBarGump
    {
        private static Settings _settings;
        private static Settings settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = (Settings)Settings.Load<Settings>(typeof(HealthBarGumpCustom).ToString());
                    if (_settings == null)
                    {
                        _settings = new Settings();
                        Settings.Save<Settings>(typeof(HealthBarGumpCustom).ToString(), _settings);
                    }
                    return _settings;
                }
                else
                {
                    return _settings;
                }
            }
        }

        internal static int HPB_WIDTH = settings.Width;
        internal static int HPB_HEIGHT_MULTILINE = settings.Height_Multiline;
        internal static int HPB_HEIGHT_SINGLELINE = settings.Height_Singleline;
        private static int HPB_BORDERSIZE = settings.BorderSize;
        private static int HPB_OUTLINESIZE = settings.OutlineSize;


        internal static int HPB_BAR_WIDTH = settings.HP_Bar_Width;
        private static int HPB_BAR_HEIGHT = settings.HP_Bar_Height;
        private static int HPB_BAR_SPACELEFT = (HPB_WIDTH - HPB_BAR_WIDTH) / 2;


        private static Color HPB_COLOR_DRAW_RED = settings.Hue_HP_Lines_Red;
        private static Color HPB_COLOR_DRAW_BLUE = settings.Hue_HP_Lines_Blue;
        private static Color HPB_COLOR_DRAW_BLACK = settings.Hue_HP_Lines_Black;

        private static readonly Texture2D HPB_COLOR_BLUE = SolidColorTextureCache.GetTexture(settings.Hue_HP_Blue);
        private static readonly Texture2D HPB_COLOR_GRAY = SolidColorTextureCache.GetTexture(settings.Hue_HP_Gray);
        private static readonly Texture2D HPB_COLOR_RED = SolidColorTextureCache.GetTexture(settings.Hue_HP_Red);
        private static readonly Texture2D HPB_COLOR_YELLOW = SolidColorTextureCache.GetTexture(settings.Hue_HP_Yellow);
        private static readonly Texture2D HPB_COLOR_POISON = SolidColorTextureCache.GetTexture(settings.Hue_HP_Poison);
        private static readonly Texture2D HPB_COLOR_BLACK = SolidColorTextureCache.GetTexture(settings.Hue_HP_Black);

        private readonly LineCHB[] _bars = new LineCHB[3];
        private readonly LineCHB[] _border = new LineCHB[4];

        // ## BEGIN - END ## // HEALTHBAR
        private readonly LineCHB[] _flash = new LineCHB[4];
        private static readonly Texture2D HPB_COLOR_PARA = SolidColorTextureCache.GetTexture(Color.MediumPurple);
        // ## BEGIN - END ## // HEALTHBAR

        private LineCHB _hpLineRed, _manaLineRed, _stamLineRed, _outline;


        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;

        public HealthBarGumpCustom(Entity entity) : base(entity)
        {
        }

        public HealthBarGumpCustom(uint serial) : base(serial)
        {
        }

        public HealthBarGumpCustom() : base(0, 0)
        {
        }

        protected AlphaBlendControl _background;

        protected override void UpdateContents()
        {
            Clear();
            Children.Clear();

            _background = null;
            _hpLineRed = _manaLineRed = _stamLineRed = null;

            if (_textBox != null)
            {
                _textBox.MouseUp -= TextBoxOnMouseUp;
                _textBox.Dispose();
            }

            _textBox = null;

            BuildGump();
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed || !HasBeenBuilt)
            {
                return;
            }

            bool inparty = World.Party.Contains(LocalSerial);


            ushort textColor = 0x0386;

            Entity entity = World.Get(LocalSerial);

            if (entity is Item it && it.Layer == 0 && it.Container == World.Player)
            {
                entity = null;
            }

            if (entity == null || entity.IsDestroyed)
            {
                bool hasCorpse = World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000);
                if (LocalSerial != World.Player && (ProfileManager.CurrentProfile.CloseHealthBarType == 1 || ProfileManager.CurrentProfile.CloseHealthBarType == 3) || ((ProfileManager.CurrentProfile.CloseHealthBarType == 2 || ProfileManager.CurrentProfile.CloseHealthBarType == 3) && hasCorpse))
                {
                    //### KEEPS PARTY BAR ACTIVE WHEN PARTY MEMBER DIES & MOBILEBAR CLOSE SELECTED ###//
                    if (!inparty && CheckIfAnchoredElseDispose())
                    {
                        return;
                    }

                    //### KEEPS PARTY BAR ACTIVE WHEN PARTY MEMBER DIES & MOBILEBAR CLOSE SELECTED ###//
                }

                if (_isDead)
                {
                    _isDead = false;
                }

                if (!_outOfRange)
                {
                    _outOfRange = true;
                    textColor = 912;

                    if (TargetManager.LastAttack != LocalSerial)
                    {
                        GameActions.SendCloseStatus(LocalSerial);
                    }

                    if (inparty)
                    {
                        if (_textBox != null && _textBox.Hue != textColor)
                        {
                            _textBox.Hue = textColor;
                        }

                        if (_bars.Length >= 2 && _bars[1] != null)
                        {
                            _bars[1].IsVisible = false;

                            _bars[2].IsVisible = false;
                        }
                    }
                    else
                    {
                        if (_textBox != null)
                        {
                            if (_textBox.Hue != textColor)
                            {
                                _textBox.Hue = textColor;
                            }

                            _textBox.IsEditable = false;
                        }
                    }

                    if (_background != null && _background.Hue != 912)
                    {
                        _background.Hue = 912;
                    }

                    if (_hpLineRed != null && _hpLineRed.LineColor != HPB_COLOR_GRAY)
                    {
                        _hpLineRed.LineColor = HPB_COLOR_GRAY;

                        _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;

                        // ## BEGIN - END ## // HEALTHBAR
                        _flash[0].LineColor = _flash[1].LineColor = _flash[2].LineColor = _flash[3].LineColor = HPB_COLOR_BLACK;
                        _flash[0].IsVisible = _flash[1].IsVisible = _flash[2].IsVisible = _flash[3].IsVisible = false;
                        // ## BEGIN - END ## // HEALTHBAR

                        if (_manaLineRed != null && _stamLineRed != null)
                        {
                            _manaLineRed.LineColor = _stamLineRed.LineColor = HPB_COLOR_GRAY;
                        }
                    }

                    if (_bars[0] != null)
                    {
                        _bars[0].IsVisible = false;
                    }
                }
            }

            if (entity != null && !entity.IsDestroyed)
            {
                _hpLineRed.IsVisible = entity.HitsMax > 0;

                Mobile mobile = entity as Mobile;

                if (!_isDead && entity != World.Player && mobile != null && mobile.IsDead && (ProfileManager.CurrentProfile.CloseHealthBarType == 2 || ProfileManager.CurrentProfile.CloseHealthBarType == 3)) // is dead
                {
                    if (!inparty && CheckIfAnchoredElseDispose())
                    {
                        return;
                    }
                }

                if (entity is Mobile mm && _canChangeName != mm.IsRenamable)
                {
                    _canChangeName = mm.IsRenamable;

                    if (_textBox != null)
                    {
                        _textBox.AcceptMouseInput = _canChangeName;
                        _textBox.AcceptKeyboardInput = _canChangeName;

                        if (!_canChangeName)
                        {
                            _textBox.IsEditable = false;
                        }
                    }
                }

                if (!(mobile != null && mobile.IsDead) && _isDead)
                {
                    _isDead = false;
                }

                if (!string.IsNullOrEmpty(entity.Name) && _name != entity.Name)
                {
                    _name = entity.Name;

                    if (_textBox != null)
                    {
                        _textBox.SetText(_name);
                    }
                }

                if (_outOfRange)
                {
                    if (entity.HitsMax == 0)
                    {
                        GameActions.RequestMobileStatus(entity);
                    }

                    _outOfRange = false;


                    _canChangeName = mobile != null && mobile.IsRenamable;

                    if (_canChangeName)
                    {
                        textColor = 0x000E;
                    }

                    if (inparty && _bars.Length >= 2 && _bars[1] != null)
                    {
                        _bars[1].IsVisible = true;

                        _bars[2].IsVisible = true;
                    }

                    if (_hpLineRed.LineColor != HPB_COLOR_RED)
                    {
                        _hpLineRed.LineColor = HPB_COLOR_RED;

                        _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;

                        // ## BEGIN - END ## // HEALTHBAR
                        _flash[0].LineColor = _flash[1].LineColor = _flash[2].LineColor = _flash[3].LineColor = HPB_COLOR_BLACK;
                        _flash[0].IsVisible = _flash[1].IsVisible = _flash[2].IsVisible = _flash[3].IsVisible = false;
                        // ## BEGIN - END ## // HEALTHBAR

                        if (_manaLineRed != null && _stamLineRed != null)
                        {
                            _manaLineRed.LineColor = _stamLineRed.LineColor = HPB_COLOR_RED;
                        }
                    }

                    _bars[0].IsVisible = true;
                }

                // ## BEGIN - END ## // HEALTHBAR
                var LTBorder = ProfileManager.CurrentProfile.HighlightLastTargetHealthBarOutline == true;
                var HLBorder = ProfileManager.CurrentProfile.HighlightHealthBarByState == true;
                // ## BEGIN - END ## // HEALTHBAR

                if (mobile != null && mobile != World.Player)
                    if (TargetManager.LastTargetInfo.Serial != World.Player && !_outOfRange)
                    {
                        // ## BEGIN - END ## // HEALTHBAR
                        //int tDistance = mobile.Distance;
                        //if (mobile == TargetManager.LastTargetInfo.Serial)
                        //{
                        //    _border[0].LineColor = HPB_COLOR_RED;
                        //    _border[0].Hue = 0;
                        //    if (tDistance < 15 && mobile != World.Player)
                        //    {
                        //        _border[0].LineColor = HPB_COLOR_YELLOW;
                        //        _border[0].Hue = 42;
                        //        if (tDistance < 10)
                        //        {
                        //            _border[0].Hue = 52;
                        //            if (tDistance < 5)
                        //            {
                        //                _border[0].Hue = 67;
                        //            }
                        //        }
                        //    }

                        //    if (_border.Length >= 3)
                        //    {
                        //        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_RED;
                        //    }
                        //}
                        //else if (mobile != TargetManager.LastTargetInfo.Serial)
                        //{
                        //    _border[0].LineColor = HPB_COLOR_BLACK;
                        //    _border[0].Hue = 0;
                        //    if (tDistance < 15 && mobile != World.Player)
                        //    {
                        //        _border[0].LineColor = HPB_COLOR_YELLOW;
                        //        _border[0].Hue = 42;
                        //        if (tDistance < 10)
                        //        {
                        //            _border[0].Hue = 52;
                        //            if (tDistance < 5)
                        //            {
                        //                _border[0].Hue = 67;
                        //            }
                        //        }
                        //    }

                        //    if (_border.Length >= 3)
                        //    {
                        //        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;
                        //    }
                        //}
                        // ## BEGIN - END ## // HEALTHBAR
                        // ## BEGIN - END ## // HEALTHBAR
                        if (LTBorder || HLBorder) //##FOR BORDER COLORS
                        {
                            if ((mobile == TargetManager.LastTargetInfo.Serial && LTBorder) || HLBorder && (mobile.IsPoisoned || mobile.IsParalyzed || mobile.IsYellowHits || mobile.IsRenamable))
                            {
                                //not in an else if as we want it blue but also overwritten when poisoned
                                if (mobile.IsRenamable)
                                {
                                    _border[0].LineColor = HPB_COLOR_BLUE;
                                    if (_border.Length >= 3)
                                    {
                                        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLUE;
                                    }
                                }

                                if (mobile.IsPoisoned)
                                {
                                    _border[0].LineColor = HPB_COLOR_POISON;
                                    if (_border.Length >= 3)
                                    {
                                        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_POISON;
                                    }
                                }
                                else if (mobile.IsParalyzed)
                                {
                                    _border[0].LineColor = HPB_COLOR_PARA;
                                    if (_border.Length >= 3)
                                    {
                                        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_PARA;
                                    }
                                }
                                else if (mobile.IsYellowHits)
                                {
                                    _border[0].LineColor = HPB_COLOR_YELLOW;
                                    if (_border.Length >= 3)
                                    {
                                        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_YELLOW;
                                    }
                                }
                                else //its last target
                                {
                                    _border[0].LineColor = HPB_COLOR_RED;

                                    if (_border.Length >= 3)
                                    {
                                        _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_RED;
                                    }
                                }
                            }
                            else if (mobile == World.Player && World.Player.InWarMode)
                            {
                                _border[0].LineColor = HPB_COLOR_RED;

                                if (_border.Length >= 3)
                                {
                                    _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_RED;
                                }
                            }
                            else if (mobile != TargetManager.LastTargetInfo.Serial) //&& mobile != World.Player)
                            {
                                _border[0].LineColor = HPB_COLOR_BLACK;

                                if (_border.Length >= 3)
                                {
                                    _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;
                                }
                            }
                        }
                        // ## BEGIN - END ## // HEALTHBAR
                    }

                if (mobile != null)
                {
                    textColor = Notoriety.GetHue(mobile.NotorietyFlag);
                }

                if (_textBox != null && _textBox.Hue != textColor)
                {
                    _textBox.Hue = textColor;
                }

                ushort barColor = mobile != null ? Notoriety.GetHue(mobile.NotorietyFlag) : (ushort)912;

                if (_background.Hue != barColor)
                {
                    if (mobile != null && mobile.IsDead)
                    {
                        _background.Hue = 912;
                    }
                    else if (!ProfileManager.CurrentProfile.CBBlackBGToggled)
                    {
                        _background.Hue = barColor;
                    }
                }

                if (mobile != null && mobile.IsDead || ProfileManager.CurrentProfile.CBBlackBGToggled)
                {
                    if (_background.Hue != 912)
                    {
                        _background.Hue = 912;
                    }
                }


                if (mobile != null)
                {
                    if (mobile.IsPoisoned && !_poisoned)
                    {
                        _bars[0].LineColor = HPB_COLOR_POISON;

                        _poisoned = true;
                        _normalHits = false;
                    }
                    else if (mobile.IsYellowHits && !_yellowHits)
                    {
                        _bars[0].LineColor = HPB_COLOR_YELLOW;

                        _yellowHits = true;
                        _normalHits = false;
                    }
                    else if (!_normalHits && !mobile.IsPoisoned && !mobile.IsYellowHits && (_poisoned || _yellowHits))
                    {
                        _bars[0].LineColor = HPB_COLOR_BLUE;

                        _poisoned = false;
                        _yellowHits = false;
                        _normalHits = true;
                    }
                }

                // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
                CombatCollection.UpdateOverheads(mobile);
                // ## BEGIN - END ## // OVERHEAD / UNDERCHAR

                // ## BEGIN - END ## // HEALTHBAR
                if (ProfileManager.CurrentProfile != null)
                {
                    //SET COLOR ON HP CHANGE
                    if (mobile.Hits != mobile.OldHits)
                    {
                        if ((mobile.OldHits - mobile.Hits) >= ProfileManager.CurrentProfile.FlashingHealthbarTreshold || (mobile.Hits - mobile.OldHits) >= ProfileManager.CurrentProfile.FlashingHealthbarTreshold)
                        {
                            if (mobile.Hits > mobile.OldHits && !ProfileManager.CurrentProfile.FlashingHealthbarNegativeOnly)
                                _flash[0].LineColor = _flash[1].LineColor = _flash[2].LineColor = _flash[3].LineColor = HPB_COLOR_POISON;
                            else
                                _flash[0].LineColor = _flash[1].LineColor = _flash[2].LineColor = _flash[3].LineColor = HPB_COLOR_RED;

                            mobile.OldHits = mobile.Hits;
                            mobile.FlashTimeTick = Time.Ticks;
                        }
                    }

                    //MAKE IT FLASH
                    if (ProfileManager.CurrentProfile.FlashingHealthbarOutlineAll || ProfileManager.CurrentProfile.FlashingHealthbarOutlineSelf && LocalSerial == World.Player || ProfileManager.CurrentProfile.FlashingHealthbarOutlineParty && inparty || ProfileManager.CurrentProfile.FlashingHealthbarOutlineGreen && mobile.NotorietyFlag == NotorietyFlag.Ally || ProfileManager.CurrentProfile.FlashingHealthbarOutlineOrange && mobile.NotorietyFlag == NotorietyFlag.Enemy)
                    {
                        if (mobile.FlashTimeTick > Time.Ticks - 500)
                            _flash[0].IsVisible = _flash[1].IsVisible = _flash[2].IsVisible = _flash[3].IsVisible = true;
                        else
                            _flash[0].IsVisible = _flash[1].IsVisible = _flash[2].IsVisible = _flash[3].IsVisible = false;
                    }
                    else
                        _flash[0].IsVisible = _flash[1].IsVisible = _flash[2].IsVisible = _flash[3].IsVisible = false;
                }
                // ## BEGIN - END ## // HEALTHBAR

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, HPB_BAR_WIDTH);

                if (hits != _bars[0].LineWidth)
                {
                    _bars[0].LineWidth = hits;
                }

                if ((inparty || LocalSerial == World.Player) && mobile != null && _bars != null)
                {
                    int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, HPB_BAR_WIDTH);
                    int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, HPB_BAR_WIDTH);

                    if (_bars.Length >= 2 && _bars[1] != null && mana != _bars[1].LineWidth)
                    {
                        _bars[1].LineWidth = mana;
                    }

                    if (_bars.Length >= 2 && _bars[2] != null && stam != _bars[2].LineWidth)
                    {
                        _bars[2].LineWidth = stam;
                    }
                }

                if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.RootParent == this)
                {
                    SelectedObject.HealthbarObject = entity;
                    SelectedObject.Object = entity;
                }
            }

            if (LocalSerial == World.Player)
            {
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;

                    if (World.Player.InWarMode)
                    {
                        _border[0].LineColor = HPB_COLOR_RED;

                        if (_border.Length >= 3)
                        {
                            _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_RED;
                        }
                    }
                    else
                    {
                        _border[0].LineColor = HPB_COLOR_BLACK;

                        if (_border.Length >= 3)
                        {
                            _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;
                        }
                    }
                }
            }
        }

        protected override void BuildGump()
        {
            WantUpdateSize = false;

            Entity entity = World.Get(LocalSerial);


            if (World.Party.Contains(LocalSerial))
            {
                Height = HPB_HEIGHT_MULTILINE;
                Width = HPB_WIDTH;

                Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });


                if (LocalSerial == World.Player)
                {
                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            1,
                            32,
                            HPB_WIDTH,
                            true,
                            FontStyle.Cropped | FontStyle.BlackBorder,
                            Notoriety.GetHue(World.Player.NotorietyFlag),
                            TEXT_ALIGN_TYPE.TS_CENTER
                        )
                        {
                            X = 0,
                            Y = 3,
                            Width = HPB_BAR_WIDTH,
                            IsEditable = false,
                            CanMove = true
                        }
                    );
                }
                else
                {
                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            1,
                            32,
                            HPB_WIDTH,
                            true,
                            FontStyle.Cropped | FontStyle.BlackBorder,
                            Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray),
                            TEXT_ALIGN_TYPE.TS_CENTER
                        )
                        {
                            X = 0,
                            Y = 3,
                            Width = HPB_BAR_WIDTH,
                            IsEditable = false,
                            CanMove = true
                        }
                    );
                }

                Add
                (
                    _outline = new LineCHB
                    (
                        HPB_BAR_SPACELEFT - HPB_OUTLINESIZE,
                        27 - HPB_OUTLINESIZE,
                        HPB_BAR_WIDTH + HPB_OUTLINESIZE * 2,
                        HPB_BAR_HEIGHT * 3 + 2 + HPB_OUTLINESIZE * 2,
                        HPB_COLOR_DRAW_BLACK.PackedValue
                    )
                );

                Add
                (
                    _hpLineRed = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        27,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_RED.PackedValue
                    )
                );

                Add
                (
                    _manaLineRed = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        36,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_RED.PackedValue
                    )
                );

                Add
                (
                    _stamLineRed = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        45,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_RED.PackedValue
                    )
                );

                Add
                (
                    _bars[0] = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        27,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_BLUE.PackedValue
                    )
                    { LineWidth = 0 }
                );

                Add
                (
                    _bars[1] = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        36,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_BLUE.PackedValue
                    )
                    { LineWidth = 0 }
                );

                Add
                (
                    _bars[2] = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        45,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_BLUE.PackedValue
                    )
                    { LineWidth = 0 }
                );

                Add
                (
                    _border[0] = new LineCHB
                    (
                        0,
                        0,
                        HPB_WIDTH,
                        HPB_BORDERSIZE,
                        HPB_COLOR_DRAW_BLACK.PackedValue
                    )
                );

                Add
                (
                    _border[1] = new LineCHB
                    (
                        0,
                        HPB_HEIGHT_MULTILINE - HPB_BORDERSIZE,
                        HPB_WIDTH,
                        HPB_BORDERSIZE,
                        HPB_COLOR_DRAW_BLACK.PackedValue
                    )
                );

                Add
                (
                    _border[2] = new LineCHB
                    (
                        0,
                        0,
                        HPB_BORDERSIZE,
                        HPB_HEIGHT_MULTILINE,
                        HPB_COLOR_DRAW_BLACK.PackedValue
                    )
                );

                Add
                (
                    _border[3] = new LineCHB
                    (
                        HPB_WIDTH - HPB_BORDERSIZE,
                        0,
                        HPB_BORDERSIZE,
                        HPB_HEIGHT_MULTILINE,
                        HPB_COLOR_DRAW_BLACK.PackedValue
                    )
                );

                // ## BEGIN - END ## // HEALTHBAR
                Add(_flash[0] = new LineCHB(-HPB_BORDERSIZE, -HPB_BORDERSIZE, HPB_WIDTH + HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue)); //top
                Add(_flash[1] = new LineCHB(-HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_WIDTH + HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue)); //bottom
                Add(_flash[2] = new LineCHB(-HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));//left
                Add(_flash[3] = new LineCHB(HPB_WIDTH, -HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE + HPB_BORDERSIZE * 2, HPB_COLOR_DRAW_BLACK.PackedValue));//right
                // ## BEGIN - END ## // HEALTHBAR
            }
            else
            {
                if (LocalSerial == World.Player)
                {
                    _oldWarMode = World.Player.InWarMode;
                    Height = HPB_HEIGHT_MULTILINE;
                    Width = HPB_WIDTH;

                    Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });

                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            1,
                            32,
                            isunicode: true,
                            style: FontStyle.Cropped | FontStyle.BlackBorder,
                            hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray),
                            maxWidth: Width,
                            align: TEXT_ALIGN_TYPE.TS_CENTER
                        )
                        {
                            X = 0,
                            Y = 3,
                            Width = HPB_BAR_WIDTH,
                            IsEditable = false,
                            CanMove = true
                        }
                    );

                    Add
                    (
                        _outline = new LineCHB
                        (
                            HPB_BAR_SPACELEFT - HPB_OUTLINESIZE,
                            _textBox.Height - HPB_OUTLINESIZE,
                            HPB_BAR_WIDTH + HPB_OUTLINESIZE * 2,
                            HPB_BAR_HEIGHT * 3 + 2 + HPB_OUTLINESIZE * 3,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _hpLineRed = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            _outline.Y + HPB_OUTLINESIZE,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_RED.PackedValue
                        )
                    );
                    Control m, s;
                    Add
                    (m =
                        new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            _hpLineRed.Height + _hpLineRed.Y + HPB_OUTLINESIZE,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_RED.PackedValue
                        )
                    );

                    Add
                    (s =
                        new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            m.Height + m.Y + HPB_OUTLINESIZE,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_RED.PackedValue
                        )
                    );

                    Add
                    (
                        _bars[0] = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            _hpLineRed.Y,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_BLUE.PackedValue
                        )
                        { LineWidth = 0 }
                    );

                    Add
                    (
                        _bars[1] = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            m.Y,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_BLUE.PackedValue
                        )
                        { LineWidth = 0 }
                    );

                    Add
                    (
                        _bars[2] = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            s.Y,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_BLUE.PackedValue
                        )
                        { LineWidth = 0 }
                    );

                    Add
                    (
                        _border[0] = new LineCHB
                        (
                            0,
                            0,
                            HPB_WIDTH,
                            HPB_BORDERSIZE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _border[1] = new LineCHB
                        (
                            0,
                            HPB_HEIGHT_MULTILINE - HPB_BORDERSIZE,
                            HPB_WIDTH,
                            HPB_BORDERSIZE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _border[2] = new LineCHB
                        (
                            0,
                            0,
                            HPB_BORDERSIZE,
                            HPB_HEIGHT_MULTILINE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _border[3] = new LineCHB
                        (
                            HPB_WIDTH - HPB_BORDERSIZE,
                            0,
                            HPB_BORDERSIZE,
                            HPB_HEIGHT_MULTILINE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    // ## BEGIN - END ## // HEALTHBAR
                    Add(_flash[0] = new LineCHB(-HPB_BORDERSIZE, -HPB_BORDERSIZE, HPB_WIDTH + HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue)); //top
                    Add(_flash[1] = new LineCHB(-HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_WIDTH + HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue)); //bottom
                    Add(_flash[2] = new LineCHB(-HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE, HPB_COLOR_DRAW_BLACK.PackedValue));//left
                    Add(_flash[3] = new LineCHB(HPB_WIDTH, -HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_HEIGHT_MULTILINE + HPB_BORDERSIZE * 2, HPB_COLOR_DRAW_BLACK.PackedValue));//right
                    // ## BEGIN - END ## // HEALTHBAR

                    _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = _oldWarMode ? HPB_COLOR_RED : HPB_COLOR_BLACK;
                }
                else
                {
                    Mobile mobile = entity as Mobile;

                    if (entity != null)
                    {
                        _canChangeName = mobile != null && mobile.IsRenamable;
                    }

                    Height = HPB_HEIGHT_SINGLELINE;
                    Width = HPB_WIDTH;

                    Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });

                    Add
                    (
                        _outline = new LineCHB
                        (
                            HPB_BAR_SPACELEFT - HPB_OUTLINESIZE,
                            21 - HPB_OUTLINESIZE,
                            HPB_BAR_WIDTH + HPB_OUTLINESIZE * 2,
                            HPB_BAR_HEIGHT + HPB_OUTLINESIZE * 2,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _hpLineRed = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            21,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_RED.PackedValue
                        )
                    );

                    Add
                    (
                        _bars[0] = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            21,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_BLUE.PackedValue
                        )
                        { LineWidth = 0 }
                    );

                    Add
                    (
                        _border[0] = new LineCHB
                        (
                            0,
                            0,
                            HPB_WIDTH,
                            HPB_BORDERSIZE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _border[1] = new LineCHB
                        (
                            0,
                            HPB_HEIGHT_SINGLELINE - HPB_BORDERSIZE,
                            HPB_WIDTH,
                            HPB_BORDERSIZE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _border[2] = new LineCHB
                        (
                            0,
                            0,
                            HPB_BORDERSIZE,
                            HPB_HEIGHT_SINGLELINE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    Add
                    (
                        _border[3] = new LineCHB
                        (
                            HPB_WIDTH - HPB_BORDERSIZE,
                            0,
                            HPB_BORDERSIZE,
                            HPB_HEIGHT_SINGLELINE,
                            HPB_COLOR_DRAW_BLACK.PackedValue
                        )
                    );

                    // ## BEGIN - END ## // HEALTHBAR
                    Add(_flash[0] = new LineCHB(-HPB_BORDERSIZE, -HPB_BORDERSIZE, HPB_WIDTH + HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue)); //top
                    Add(_flash[1] = new LineCHB(-HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE, HPB_WIDTH + HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_COLOR_DRAW_BLACK.PackedValue)); //bottom
                    Add(_flash[2] = new LineCHB(-HPB_BORDERSIZE, 0, HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE, HPB_COLOR_DRAW_BLACK.PackedValue));//left
                    Add(_flash[3] = new LineCHB(HPB_WIDTH, -HPB_BORDERSIZE, HPB_BORDERSIZE, HPB_HEIGHT_SINGLELINE + HPB_BORDERSIZE * 2, HPB_COLOR_DRAW_BLACK.PackedValue));//right
                    // ## BEGIN - END ## // HEALTHBAR

                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            1,
                            32,
                            HPB_WIDTH,
                            true,
                            hue: Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray),
                            style: FontStyle.Cropped | FontStyle.BlackBorder,
                            align: TEXT_ALIGN_TYPE.TS_CENTER
                        )
                        {
                            X = 0,
                            Y = 0,
                            Width = HPB_WIDTH,
                            Height = 15,
                            IsEditable = false,
                            AcceptMouseInput = _canChangeName,
                            AcceptKeyboardInput = _canChangeName,
                            WantUpdateSize = false,
                            CanMove = true
                        }
                    );
                }
            }

            _textBox.MouseUp += TextBoxOnMouseUp;
            _textBox.SetText(_name);

            if (entity == null)
            {
                _textBox.Hue = _background.Hue = 912;

                if (_hpLineRed.LineColor != HPB_COLOR_GRAY)
                {
                    _hpLineRed.LineColor = HPB_COLOR_GRAY;

                    _border[0].LineColor = _border[1].LineColor = _border[2].LineColor = _border[3].LineColor = HPB_COLOR_BLACK;

                    // ## BEGIN - END ## // HEALTHBAR
                    _flash[0].LineColor = _flash[1].LineColor = _flash[2].LineColor = _flash[3].LineColor = HPB_COLOR_BLACK;
                    _flash[0].IsVisible = _flash[1].IsVisible = _flash[2].IsVisible = _flash[3].IsVisible = false;
                    // ## BEGIN - END ## // HEALTHBAR

                    if (_manaLineRed != null && _stamLineRed != null)
                    {
                        _manaLineRed.LineColor = _stamLineRed.LineColor = HPB_COLOR_GRAY;
                    }
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            return true;
        }

        private class LineCHB : Line
        {
            public int Hue = 0;
            public LineCHB(int x, int y, int w, int h, uint color) : base
            (
                x,
                y,
                w,
                h,
                color
            )
            {
                LineWidth = w;

                LineColor = SolidColorTextureCache.GetTexture(new Color { PackedValue = color });

                CanMove = true;
            }

            public int LineWidth { get; set; }
            public Texture2D LineColor { get; set; }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);

                batcher.Draw
                (
                    LineColor,
                    new Rectangle
                    (
                        x,
                        y,
                        LineWidth,
                        Height
                    ),
                    hueVector
                );

                return true;
            }
        }

        #region Health Bar Gump Custom

        // Health Bar Gump Custom v.1c by Syrupz(Alan)
        //
        // The goal of this was to simply modernize the Health Bar Gumps while still giving people
        // an option to continue using the classic Health Bar Gumps. The option to overide bar types
        // be it (straight line(custom) or graphic(classic) is directly included in this version
        // with no need to change art files in UO directory.
        //
        // Please report any problems with this to Alan#0084 on Discord and I will promptly work on fixing said issues.
        //
        // Lastly, I want to give a special thanks to Gaechti for helping me stress test this
        // and helping me work and organizing this in a timely fashion to get this released.
        // I would like to also thank KaRaShO, Roxya, Stalli, and Link for their input, tips, 
        // and advice to approach certain challenges that arose throughout development.
        // in different manners to get these Health Bars to function per my own vision; gratitude.
        //
        // Health Bar Gump Custom v.1c by Syrupz(Alan)

        #endregion

        private class Settings : UISettings
        {
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Blue { get; set; } = Color.DodgerBlue;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Gray { get; set; } = Color.Gray;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Red { get; set; } = Color.Red;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Yellow { get; set; } = Color.Orange;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Poison { get; set; } = Color.LimeGreen;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Black { get; set; } = Color.Black;

            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Lines_Red { get; set; } = Color.Red;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Lines_Blue { get; set; } = Color.DodgerBlue;
            [JsonConverter(typeof(ColorJsonConverter))]
            public Color Hue_HP_Lines_Black { get; set; } = Color.Black;

            public int Width { get; set; } = 120;
            public int Height_Multiline { get; set; } = 60;
            public int Height_Singleline { get; set; } = 36;
            public int BorderSize { get; set; } = 1;
            public int OutlineSize { get; set; } = 1;
            public int HP_Bar_Width { get; set; } = 100;
            public int HP_Bar_Height { get; set; } = 8;
        }
    }

    internal class HealthBarGump : BaseHealthBarGump
    {
        private static Settings _settings;
        private static Settings settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = (Settings)Settings.Load<Settings>(typeof(HealthBarGump).ToString());
                    if (_settings == null)
                    {
                        _settings = new Settings();
                        Settings.Save<Settings>(typeof(HealthBarGump).ToString(), _settings);
                    }
                    return _settings;
                }
                else
                {
                    return _settings;
                }
            }
        }

        private GumpPic _background, _hpLineRed, _manaLineRed, _stamLineRed;

        private readonly GumpPicWithWidth[] _bars = new GumpPicWithWidth[3];

        private Button _buttonHeal1, _buttonHeal2;
        private int _oldHits, _oldStam, _oldMana;

        private bool _oldWarMode, _normalHits, _poisoned, _yellowHits;


        public HealthBarGump(Entity entity) : base(entity)
        {
        }

        public HealthBarGump(uint serial) : base(serial)
        {
        }

        public HealthBarGump() : base(0, 0)
        {
        }

        public override int GroupMatrixWidth
        {
            get => Width;
            protected set { }
        }

        public override int GroupMatrixHeight
        {
            get => Height;
            protected set { }
        }

        protected override void UpdateContents()
        {
            Clear();
            Children.Clear();

            _background = _hpLineRed = _manaLineRed = _stamLineRed = null;
            _buttonHeal1 = _buttonHeal2 = null;

            if (_textBox != null)
            {
                _textBox.MouseUp -= TextBoxOnMouseUp;
                _textBox.Dispose();
            }

            _textBox = null;

            BuildGump();
        }

        protected override void BuildGump()
        {
            WantUpdateSize = false;

            Entity entity = World.Get(LocalSerial);

            if (World.Party.Contains(LocalSerial))
            {
                Add
                (
                    _background = new GumpPic(0, 0, settings.Background_Normal, settings.Hue_Background)
                    {
                        ContainsByBounds = true,
                        Alpha = 0
                    }
                );

                Width = 115;
                Height = 55;

                if (LocalSerial == World.Player)
                {
                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            3,
                            32,
                            120,
                            false,
                            FontStyle.Fixed,
                            Notoriety.GetHue(World.Player.NotorietyFlag)
                        )
                        {
                            X = 0,
                            Y = -2,
                            Width = 120,
                            Height = 50,
                            IsEditable = false,
                            CanMove = true
                        }
                    );

                    _name = ResGumps.Self;
                }
                else
                {
                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            3,
                            32,
                            109,
                            false,
                            FontStyle.Fixed | FontStyle.BlackBorder,
                            Notoriety.GetHue((entity as Mobile)?.NotorietyFlag ?? NotorietyFlag.Gray)
                        )
                        {
                            X = 0,
                            Y = -2,
                            Width = 109,
                            Height = 50,
                            IsEditable = false,
                            CanMove = true
                        }
                    );
                }

                Add(_buttonHeal1 = new Button((int)ButtonParty.Heal1, 0x0938, 0x093A, 0x0938) { ButtonAction = ButtonAction.Activate, X = 0, Y = 20 });

                Add(_buttonHeal2 = new Button((int)ButtonParty.Heal2, 0x0939, 0x093A, 0x0939) { ButtonAction = ButtonAction.Activate, X = 0, Y = 33 });

                Add(_hpLineRed = new GumpPic(18, 20, settings.Line_Red_Party, 0));
                Add(_manaLineRed = new GumpPic(18, 33, settings.Line_Red_Party, 0));
                Add(_stamLineRed = new GumpPic(18, 45, settings.Line_Red_Party, 0));

                Add
                (
                    _bars[0] = new GumpPicWithWidth
                    (
                        18,
                        20,
                        settings.Line_Blue_Party,
                        settings.Hue_Bar_HP_Foreground_Normal,
                        96
                    )
                );

                Add
                (
                    _bars[1] = new GumpPicWithWidth
                    (
                        18,
                        33,
                        settings.Line_Blue_Party,
                        settings.Hue_Bar_Mana_Foreground_Normal,
                        96
                    )
                );

                Add
                (
                    _bars[2] = new GumpPicWithWidth
                    (
                        18,
                        45,
                        settings.Line_Blue_Party,
                        settings.Hue_Bar_Stam_Foreground_Normal,
                        96
                    )
                );
            }
            else
            {
                if (LocalSerial == World.Player)
                {
                    _oldWarMode = World.Player.InWarMode;

                    Add(_background = new GumpPic(0, 0, _oldWarMode ? settings.Background_War : settings.Background_Normal, 0) { ContainsByBounds = true });

                    Width = _background.Width;
                    Height = _background.Height;

                    // add backgrounds
                    Add(_hpLineRed = new GumpPic(34, 12, settings.Line_Red, 0));
                    Add(new GumpPic(34, 25, settings.Line_Red, 0));
                    Add(new GumpPic(34, 38, settings.Line_Red, 0));

                    // add over
                    Add
                    (
                        _bars[0] = new GumpPicWithWidth
                        (
                            34,
                            12,
                            settings.Line_Blue,
                            settings.Hue_Bar_HP_Foreground_Normal,
                            0
                        )
                    );

                    Add
                    (
                        _bars[1] = new GumpPicWithWidth
                        (
                            34,
                            25,
                            settings.Line_Blue,
                            settings.Hue_Bar_Mana_Foreground_Normal,
                            0
                        )
                    );

                    Add
                    (
                        _bars[2] = new GumpPicWithWidth
                        (
                            34,
                            38,
                            settings.Line_Blue,
                            settings.Hue_Bar_Stam_Foreground_Normal,
                            0
                        )
                    );
                }
                else
                {
                    ushort textColor = 0x0386;
                    ushort hitsColor = 0x0386;

                    Mobile mobile = entity as Mobile;

                    if (entity != null)
                    {
                        hitsColor = 0;
                        _canChangeName = mobile != null && mobile.IsRenamable;

                        if (_canChangeName)
                        {
                            textColor = 0x000E;
                        }
                    }

                    ushort barColor = entity == null || entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (ushort)0 : Notoriety.GetHue(mobile.NotorietyFlag);

                    Add(_background = new GumpPic(0, 0, 0x0804, barColor) { ContainsByBounds = true });
                    Add(_hpLineRed = new GumpPic(34, 38, settings.Line_Red, hitsColor));

                    Add
                    (
                        _bars[0] = new GumpPicWithWidth
                        (
                            34,
                            38,
                            settings.Line_Blue,
                            0,
                            0
                        )
                    );

                    Width = _background.Width;
                    Height = _background.Height;

                    Add
                    (
                        _textBox = new StbTextBox
                        (
                            1,
                            32,
                            120,
                            false,
                            hue: textColor,
                            style: FontStyle.Fixed
                        )
                        {
                            X = 16,
                            Y = 14,
                            Width = 120,
                            Height = 15,
                            IsEditable = false,
                            AcceptMouseInput = _canChangeName,
                            AcceptKeyboardInput = _canChangeName,
                            WantUpdateSize = false,
                            CanMove = true
                        }
                    );
                }
            }


            if (_textBox != null)
            {
                _textBox.MouseUp += TextBoxOnMouseUp;
                _textBox.SetText(_name);
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed || !HasBeenBuilt)
            {
                return;
            }

            bool inparty = World.Party.Contains(LocalSerial);


            ushort textColor = settings.Hue_Text;
            ushort hitsColor = settings.Hue_HitsBar_Background;
            ushort backgroundHue = settings.Background_Normal;
            ushort hpForegroundHue = settings.Hue_Bar_HP_Foreground_Normal;

            Entity entity = World.Get(LocalSerial);

            if (entity is Item it && it.Layer == 0 && it.Container == World.Player)
            {
                entity = null;
            }

            if (entity == null || entity.IsDestroyed)
            {
                bool hasCorpse = World.CorpseManager.Exists(0, LocalSerial | 0x8000_0000);
                if (LocalSerial != World.Player && (ProfileManager.CurrentProfile.CloseHealthBarType == 1 || ProfileManager.CurrentProfile.CloseHealthBarType == 3) || ((ProfileManager.CurrentProfile.CloseHealthBarType == 2 || ProfileManager.CurrentProfile.CloseHealthBarType == 3) && hasCorpse))
                {
                    if (CheckIfAnchoredElseDispose())
                    {
                        return;
                    }
                }

                if (_isDead)
                {
                    _isDead = false;
                }

                if (!_outOfRange)
                {
                    _outOfRange = true;

                    if (TargetManager.LastAttack != LocalSerial)
                    {
                        GameActions.SendCloseStatus(LocalSerial);
                    }

                    if (inparty)
                    {
                        hitsColor = settings.Hue_HitsBarInParty;
                        textColor = settings.Hue_TextInParty;

                        if (_textBox != null && _textBox.Hue != textColor)
                        {
                            _textBox.Hue = textColor;
                        }

                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = false;

                        if (_bars.Length >= 2 && _bars[1] != null)
                        {
                            _bars[1].IsVisible = false;

                            _bars[2].IsVisible = false;
                        }
                    }
                    else
                    {
                        if (_textBox != null)
                        {
                            if (_textBox.Hue != textColor)
                            {
                                _textBox.Hue = textColor;
                            }

                            _textBox.IsEditable = false;
                        }
                    }

                    if (_bars[0] != null)
                    {
                        _bars[0].IsVisible = false;
                    }
                }
            }

            if (entity != null && !entity.IsDestroyed)
            {
                _hpLineRed.IsVisible = entity.HitsMax > 0;

                Mobile mobile = entity as Mobile;

                if (!_isDead && entity != World.Player && mobile != null && mobile.IsDead && !inparty && (ProfileManager.CurrentProfile.CloseHealthBarType == 2 || ProfileManager.CurrentProfile.CloseHealthBarType == 3)) // is dead
                {
                    if (CheckIfAnchoredElseDispose())
                    {
                        return;
                    }
                }

                if (entity is Mobile mm && _canChangeName != mm.IsRenamable)
                {
                    _canChangeName = mm.IsRenamable;

                    if (_textBox != null)
                    {
                        _textBox.AcceptMouseInput = _canChangeName;
                        _textBox.AcceptKeyboardInput = _canChangeName;

                        if (!_canChangeName)
                        {
                            _textBox.IsEditable = false;
                        }
                    }
                }

                if (!(mobile != null && mobile.IsDead) && _isDead)
                {
                    _isDead = false;
                }

                if (!string.IsNullOrEmpty(entity.Name) && !(inparty && LocalSerial == World.Player.Serial) && _name != entity.Name)
                {
                    _name = entity.Name;

                    if (_textBox != null)
                    {
                        _textBox.SetText(_name);
                    }
                }

                if (_outOfRange)
                {
                    if (entity.HitsMax == 0)
                    {
                        GameActions.RequestMobileStatus(entity);
                    }

                    _outOfRange = false;

                    _canChangeName = !inparty && mobile != null && mobile.IsRenamable;

                    hitsColor = settings.Hue_HitsBar_OutOfRange;

                    if (inparty)
                    {
                        _buttonHeal1.IsVisible = _buttonHeal2.IsVisible = true;

                        if (_bars.Length >= 2 && _bars[1] != null)
                        {
                            _bars[1].IsVisible = true;

                            _bars[2].IsVisible = true;
                        }
                    }
                    _bars[0].IsVisible = true;
                }

                if (inparty && mobile != null)
                {
                    textColor = Notoriety.GetHue(mobile.NotorietyFlag);
                }
                else
                {
                    if (_canChangeName)
                    {
                        textColor = settings.Hue_Text_Renamable;
                    }
                }

                backgroundHue = entity == World.Player || mobile == null || mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray ? (ushort)settings.Hue_Background : Notoriety.GetHue(mobile.NotorietyFlag);

                if (mobile != null && mobile.IsPoisoned && !_poisoned)
                {
                    if (inparty)
                    {
                        hpForegroundHue = settings.Hue_Bar_Poisoned_InParty;
                    }
                    else
                    {
                        hpForegroundHue = settings.Hue_Bar_Poisoned;
                        _bars[0].Graphic = settings.Line_Poisoned;
                    }

                    _poisoned = true;
                    _normalHits = false;
                }
                else if (mobile != null && mobile.IsYellowHits && !_yellowHits)
                {
                    if (inparty)
                    {
                        hpForegroundHue = settings.Hue_Bar_Yellow_InParty;
                    }
                    else
                    {
                        hpForegroundHue = settings.Hue_Bar_Yellow;
                        _bars[0].Graphic = settings.Line_Yellowhits;
                    }

                    _yellowHits = true;
                    _normalHits = false;
                }
                else if (!_normalHits && mobile != null && !mobile.IsPoisoned && !mobile.IsYellowHits && (_poisoned || _yellowHits))
                {
                    if (inparty)
                    {
                        hpForegroundHue = settings.Hue_HitsBarInParty;
                    }
                    else
                    {
                        hpForegroundHue = settings.Hue_Bar_HP_Foreground_Normal;
                        _bars[0].Graphic = settings.Line_Blue;
                    }

                    _poisoned = false;
                    _yellowHits = false;
                    _normalHits = true;
                }

                int barW = inparty ? 96 : 109;

                // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
                CombatCollection.UpdateOverheads(mobile);
                // ## BEGIN - END ## // OVERHEAD / UNDERCHAR

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, barW);


                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;

                    _oldHits = hits;
                }


                if ((inparty || LocalSerial == World.Player) && mobile != null)
                {
                    int mana = CalculatePercents(mobile.ManaMax, mobile.Mana, barW);
                    int stam = CalculatePercents(mobile.StaminaMax, mobile.Stamina, barW);

                    if (mana != _oldMana && _bars.Length >= 2 && _bars[1] != null)
                    {
                        _bars[1].Percent = mana;

                        _oldMana = mana;
                    }

                    if (stam != _oldStam && _bars.Length >= 2 && _bars[2] != null)
                    {
                        _bars[2].Percent = stam;

                        _oldStam = stam;
                    }
                }

                if (UIManager.MouseOverControl != null && UIManager.MouseOverControl.RootParent == this)
                {
                    SelectedObject.HealthbarObject = entity;
                    SelectedObject.Object = entity;
                }
            }

            if (LocalSerial == World.Player)
            {
                if (World.Player.InWarMode != _oldWarMode)
                {
                    _oldWarMode = !_oldWarMode;

                    _background.Graphic = World.Player.InWarMode ? settings.Background_War : settings.Background_Normal;
                }
            }

            if (_bars.Length > 0 && _bars[0].Hue != hpForegroundHue) //HP Foreground
            {
                _bars[0].Hue = hpForegroundHue;
            }
            if (_textBox != null && _textBox.Hue != textColor)
            {
                _textBox.Hue = textColor;
            }
            if (_background.Hue != backgroundHue)
            {
                _background.Hue = backgroundHue;
            }
            if (_hpLineRed.Hue != hitsColor)
            {
                _hpLineRed.Hue = hitsColor;

                if (_manaLineRed != null && _stamLineRed != null)
                {
                    _manaLineRed.Hue = _stamLineRed.Hue = hitsColor;
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonParty)buttonID)
            {
                case ButtonParty.Heal1:
                    GameActions.CastSpell(29);
                    World.Party.PartyHealTimer = Time.Ticks + 50;
                    World.Party.PartyHealTarget = LocalSerial;

                    break;

                case ButtonParty.Heal2:
                    GameActions.CastSpell(11);
                    World.Party.PartyHealTimer = Time.Ticks + 50;
                    World.Party.PartyHealTarget = LocalSerial;

                    break;
            }

            Mouse.CancelDoubleClick = true;
            Mouse.LastLeftButtonClickTime = 0;
        }

        private enum ButtonParty
        {
            Heal1,
            Heal2
        }

        private class Settings : UISettings
        {
            public ushort Background_Normal { get; set; } = 0x0803;
            public ushort Background_War { get; set; } = 0x0807;
            public ushort Line_Red { get; set; } = 0x0805;
            public ushort Line_Blue { get; set; } = 0x0806;
            public ushort Line_Poisoned { get; set; } = 0x0808;
            public ushort Line_Yellowhits { get; set; } = 0x0809;
            public ushort Line_Red_Party { get; set; } = 0x0028;
            public ushort Line_Blue_Party { get; set; } = 0x0029;

            public ushort Hue_Text { get; set; } = 0x0386;
            public ushort Hue_Text_Renamable { get; set; } = 0x000E;
            public ushort Hue_HitsBar_Background { get; set; } = 0;
            public ushort Hue_TextInParty { get; set; } = 912;
            public ushort Hue_HitsBarInParty { get; set; } = 912;
            public ushort Hue_HitsBar_OutOfRange { get; set; } = 0;
            public ushort Hue_Background { get; set; } = 0;
            public ushort Hue_Bar_Poisoned_InParty { get; set; } = 63;
            public ushort Hue_Bar_Poisoned { get; set; } = 63;
            public ushort Hue_Bar_Yellow_InParty { get; set; } = 353;
            public ushort Hue_Bar_Yellow { get; set; } = 353;
            public ushort Hue_Bar_HP_Foreground_Normal { get; set; } = 0;
            public ushort Hue_Bar_Mana_Foreground_Normal { get; set; } = 0;
            public ushort Hue_Bar_Stam_Foreground_Normal { get; set; } = 0;
        }

    }
}