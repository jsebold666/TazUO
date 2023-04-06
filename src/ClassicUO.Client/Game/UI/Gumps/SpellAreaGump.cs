#region license

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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;
using static ClassicUO.Game.UI.Gumps.OptionsGump;
using static ClassicUO.Renderer.UltimaBatcher2D;
using System.Collections;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SpellAreaGump : Gump
    {
        private const int WIDTH = 500;
        private const int HEIGHT = 500;
        private DataBox _databox;

        public SpellAreaGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            WantUpdateSize = false;

            Width = WIDTH;
            Height = HEIGHT;

            Add
            (
                new AlphaBlendControl(0.95f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 2,
                    Height = HEIGHT - 2
                }
            );

            ScrollArea area = new ScrollArea
            (
                20,
                40,
                WIDTH - 40,
                HEIGHT - 115,
                true
            )
            {
                AcceptMouseInput = true,
                AcceptKeyboardInput = true
            };

            Add(area);

            _databox = new DataBox(0, 0, 1, 1)
            {
                AcceptMouseInput = true,
                AcceptKeyboardInput = true,
                WantUpdateSize = true
            };
            

            area.Add(_databox);
                       

            Add
            (
                new NiceButton
                (
                    40,
                    10,
                    180,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Name
                )
            );

            Add
            (
                new NiceButton
                (
                    220,
                    10,
                    80,
                    25,
                    ButtonAction.Activate,
                    "Range"
                )
            );

            Add
            (
                new NiceButton
                (
                    300,
                    10,
                    80,
                    25,
                    ButtonAction.Activate,
                    "Is Linear"
                )
            );

            Add
            (
                new NiceButton
                (
                    380,
                    10,
                    80,
                    25,
                    ButtonAction.Activate,
                    "Hue"
                )        
            );

            Add
            (
                new Line
                (
                    20,
                    40,
                    435,
                    1,
                    0xFFFFFFFF
                )
            );

            Add
            (
                new Line
                (
                    20,
                    area.Y + area.Height,
                    435,
                    1,
                    0xFFFFFFFF
                )
            );

            Add
            (
                new NiceButton
                (
                    10,
                    HEIGHT - 35,
                    50,
                    25,
                    ButtonAction.Activate,
                    "Add New"
                )
                {
                    ButtonParameter = (int)Buttons.Add
                }
            ); ;


            Button apply = new Button((int)Buttons.Apply, 0x00EF, 0x00F0, 0x00EE)
            {
                ButtonAction = ButtonAction.Activate
            };

            apply.X = (WIDTH >> 1) - (apply.Width >> 1);
            apply.Y = HEIGHT - (apply.Height + 10);
            Add(apply);



            BuildGump();
        }

        public override GumpType GumpType => GumpType.SpellArea;

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == (int)Buttons.Apply)
            {
                var spMgr = Client.Game.GetScene<GameScene>()?.SpellManager;

                spMgr.ClearSpellAreas();
                foreach (SpellAreaEntry entry in _databox.Children.OfType<SpellAreaEntry>())
                {
                    var spA = entry.GetSpellArea();
                    if (spA.Id == 0)
                    {
                        continue;
                    }
                    spMgr.AddSpellArea(spA);
                }
                spMgr.Save();
            } else if (buttonID == (int)Buttons.Add)
            {
                UIManager.GetGump<SpellAreaEntryGump>()?.Dispose();
                UIManager.Add(new SpellAreaEntryGump(X,Y));
            }          
        }

        private void BuildGump()
        {
            _databox.Clear();

            //foreach (SpellArea area in Client.Game.GetScene<GameScene>()?.SpellManager.SpellAreas)
            //{
            //    entry.Clear();
            //    entry.Dispose();
            //}

            //_skillListEntries.Clear();

            //PropertyInfo pi = typeof(Skill).GetProperty(_sortField);
            //List<Skill> sortSkills = new List<Skill>(World.Player.Skills.OrderBy(x => pi.GetValue(x, null)));

            //if (_sortAsc)
            //{
            //    sortSkills.Reverse();
            //}

            foreach (SpellArea area in Client.Game.GetScene<GameScene>()?.SpellManager.SpellAreas)
            {   
                _databox.Add(new SpellAreaEntry(area));
            }

            //_databox.Add(new SpellAreaEntry(new SpellArea(0, 1, false, 0)));


           _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }

        public void AddSpellArea(SpellArea area)
        {
            _databox.Add(new SpellAreaEntry(area));
            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }

        public override void Update()
        {
            base.Update();

            //if (_updateSkillsNeeded)
            //{
            //    foreach (Label label in Children.OfType<Label>())
            //    {
            //        label.Dispose();
            //    }

                //BuildGump();

            //    _updateSkillsNeeded = false;
            //}
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }


        private enum Buttons
        {
            Apply = 1,
            Add
        }
    }

    internal class SpellAreaEntryGump: Gump
    {
        private List<SpellDefinition> _availableSpells;
        private SpellAreaEntry _entry;
        public SpellAreaEntryGump(int? x = 0, int? y = 0) : base(0, 0)
        {

            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            CanMove = true;
            Width = 500;
            Height = 150;
            X = x ?? 100;
            Y = y ?? 100;

            Add
            (
                new AlphaBlendControl(0.95f)
                {
                    X = 1,
                    Y = 1,
                    Width = Width - 2,
                    Height = Height - 2
                }
            );


            _availableSpells = new List<SpellDefinition>();
            var mageryIds = SpellsMagery.GetAllSpells.Keys.Except(Client.Game.GetScene<GameScene>()?.SpellManager.GetAllSpellAreas.Keys).ToArray();
            foreach (var spellId in mageryIds)
            {
                _availableSpells.Add(SpellsMagery.GetSpell(spellId));
            }


            Add(new Label("Spell", true, 0xFFFF)
            {
                X = 75,
                Y = 10
            });
            Add(new Label("Range", true, 0xFFFF)
            {
                X = 230,
                Y = 10
            });
            Add(new Label("Is Linear", true, 0xFFFF)
            {
                X = 305,
                Y = 10
            });
            Add(new Label("Hue", true, 0xFFFF)
            {
                X = 400,
                Y = 10
            });
            Add
            (
                new Line
                (
                    5,
                    35,
                    Width - 5,
                    1,
                    0xFFFFFFFF
                )
            );
            var entry = _entry = new SpellAreaEntry(new SpellArea(_availableSpells[0].ID, 1, false, 0), _availableSpells.ToArray());
            entry.Y = 45;
            Add(entry);


            Button apply = new Button(1, 0x00EF, 0x00F0, 0x00EE)
            {
                ButtonAction = ButtonAction.Activate
            };

            apply.X = (Width >> 1) - (apply.Width >> 1);
            apply.Y = Height - (apply.Height + 10);
            Add(apply);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 1)
            {
                UIManager.GetGump<SpellAreaGump>()?.AddSpellArea(_entry.GetSpellArea());
                this.Dispose();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }
    }

    internal class SpellAreaEntry : Control
    {
        private SpellDefinition _spell;
        private GumpPic _spellIcon;
        private Checkbox _isLinear;
        private InputField _range;
        private ModernColorPicker.HueDisplay _hue;
        private SpellDefinition[] _availableSpells;


        public SpellAreaEntry(SpellArea spellArea, SpellDefinition[] availableSpells = null)
        {
            Height = 70;
            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            _spell = SpellDefinition.FullIndexGetSpell(spellArea.Id);
            _availableSpells = availableSpells;
            
            Label name = new Label(_spell.Name, true, 1153, font: 3);

            InputField range = _range =new InputField
            (
                0x0BB8,
                0xFF,
                0xFFFF,
                true,
                50,
                25,
                25,
                -1
            )
            {
                NumbersOnly = true, 
            };
            range.SetText(spellArea.Range.ToString());
                       

            Checkbox isLinear = _isLinear = new Checkbox(0x00D2,0x00D3,string.Empty,0xFF, 0xFFFF)
            {
                IsChecked = spellArea.IsLinear
            };

            ModernColorPicker.HueDisplay hue = _hue = new ModernColorPicker.HueDisplay(spellArea.Hue, null, true);  

            _spellIcon = new GumpPic(0, 0, (ushort)_spell.GumpIconSmallID, 0) { AcceptMouseInput = false, IsVisible = false };




            //if (skill.IsClickable)
            //{
            //    Add
            //    (
            //        _activeUse = new Button((int) Buttons.ActiveSkillUse, 0x837, 0x838)
            //        {
            //            X = 0, Y = 4, ButtonAction = ButtonAction.Activate
            //        }
            //    );
            //}

            _spellIcon.X = 10;
            _spellIcon.Y = 10;
            Add(_spellIcon);

            if (_availableSpells == null || !_availableSpells.Any())
            {
                _spellIcon.IsVisible = true;
                name.X = 70;
                name.Y = name.Height >> 1;
                Add(name);
            }
            else
            {
                var options = _availableSpells.OfType<SpellDefinition>().Select(def => def.Name).ToArray();
                var selectedIndex = Array.IndexOf(options, _spell.Name);

                Combobox spellList = new Combobox(70, 0, 100, options, selectedIndex);
                spellList.OnOptionSelected += SpellList_OnOptionSelected;
                Add(spellList);

                if (selectedIndex > -1)
                {
                    SpellList_OnOptionSelected(spellList, selectedIndex);
                }
            }
                      


            range.X = 225;
            range.Y = name.Y;
            Add(range);

            isLinear.X = 320;
            isLinear.Y = name.Y;
            Add(isLinear);

            hue.X = 400;
            hue.Y = hue.Height >> 1;
            Add(hue);
            
        }

        private void SpellList_OnOptionSelected(object sender, int e)
        {
            _spell = (SpellDefinition)_availableSpells[e];
            _spellIcon.Graphic = (ushort)_spell.GumpIconSmallID;
            _spellIcon.IsVisible = true;
        }
        
        public SpellArea GetSpellArea()
        {
            return new SpellArea(_spell.ID, int.Parse(_range.Text), _isLinear.IsChecked, _hue.Hue);
        }

        private enum Buttons
        {
            ActiveSkillUse = 1
        }
    }
}