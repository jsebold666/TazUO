using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class ScriptManagerGump : ResizableGump
    {
        private AlphaBlendControl background;
        private ScrollArea scrollArea;

        public ScriptManagerGump() : base(300, 400, 200, 300, 0, 0)
        {
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;

            LegionScripting.LoadScriptsFromFile();

            Add(background = new AlphaBlendControl(0.77f) { X = BorderControl.BorderSize, Y = BorderControl.BorderSize });

            Add(scrollArea = new ScrollArea(BorderControl.BorderSize, BorderControl.BorderSize, Width - (BorderControl.BorderSize * 2), Height - (BorderControl.BorderSize * 2), true));
            scrollArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int y = 0;
            foreach (ScriptFile sf in LegionScripting.LoadedScripts)
            {
                scrollArea.Add(new ScriptControl(scrollArea.Width - scrollArea.ScrollBarWidth(), sf) { Y = y });
                y += 52;
            }

            CenterXInViewPort();
            CenterYInViewPort();

            OnResize();
        }

        public override void OnResize()
        {
            base.OnResize();

            if (background != null) //Quick check to see if the gump has been built yet
            {
                background.Width = Width - (BorderControl.BorderSize * 2);
                background.Height = Height - (BorderControl.BorderSize * 2);

                scrollArea.Width = Width - (BorderControl.BorderSize * 2);
                scrollArea.Height = Height - (BorderControl.BorderSize * 2);
                scrollArea.UpdateScrollbarPosition();

                foreach (Control c in scrollArea.Children)
                {
                    if (c is ScriptControl sc)
                        sc.UpdateSize(scrollArea.Width - scrollArea.ScrollBarWidth());
                }
            }
        }

        internal class ScriptControl : Control
        {
            private AlphaBlendControl background;
            private TextBox label;
            private NiceButton play, stop, menu;

            public ScriptFile Script { get; }

            public ScriptControl(int w, ScriptFile script)
            {
                Width = w;
                Height = 50;

                Add(background = new AlphaBlendControl(0.35f) { Height = Height, Width = Width });

                Add(label = new TextBox(script.FileName, TrueTypeLoader.EMBEDDED_FONT, 18, w - 130, Color.White, strokeEffect: false));
                label.Y = (Height - label.MeasuredSize.Y) / 2;
                label.X = 5;

                Add(play = new NiceButton(w - 125, 0, 50, Height, ButtonAction.Default, "Play"));
                play.MouseUp += Play_MouseUp;

                Add(stop = new NiceButton(w - 75, 0, 50, Height, ButtonAction.Default, "Stop"));
                stop.MouseUp += Stop_MouseUp;

                Add(menu = new NiceButton(w - 25, 0, 25, Height, ButtonAction.Default, "+"));
                menu.MouseDown += (s, e) => { ContextMenu?.Show(); };

                UpdateSize(w);
                Script = script;

                ContextMenu = new ContextMenuControl();

                ContextMenu.Add(new ContextMenuItemEntry("Edit", () => { UIManager.Add(new ScriptEditor(Script)); }));
                ContextMenu.Add(new ContextMenuItemEntry("Autostart", () => { GenAutostartContext().Show(); }));
            }

            private ContextMenuControl GenAutostartContext()
            {
                ContextMenuControl context = new ContextMenuControl();
                bool global = LegionScripting.AutoLoadEnabled(Script, true);
                bool chara = LegionScripting.AutoLoadEnabled(Script, false);

                context.Add(new ContextMenuItemEntry("All characters", () => { LegionScripting.SetAutoPlay(Script, true, !global); }, true, global));
                context.Add(new ContextMenuItemEntry("This character", () => { LegionScripting.SetAutoPlay(Script, false, !chara); }, true, chara));

                return context;
            }

            private void Stop_MouseUp(object sender, Input.MouseEventArgs e)
            {
                LegionScripting.StopScript(Script);
                stop.IsSelected = true;
                play.IsSelected = false;
            }

            private void Play_MouseUp(object sender, Input.MouseEventArgs e)
            {
                LegionScripting.PlayScript(Script);
                play.IsSelected = true;
                stop.IsSelected = false;
            }

            public override void SlowUpdate()
            {
                base.SlowUpdate();
                if (Script.GetScript.IsPlaying)
                {
                    background.BaseColor = Color.DarkGreen;
                    play.IsSelected = true;
                    stop.IsSelected = false;
                }
                else
                {
                    background.BaseColor = Color.DarkRed;
                    stop.IsSelected = true;
                    play.IsSelected = false;
                }
            }

            public void UpdateSize(int w)
            {
                Width = w;
                background.Width = w;
                label.Width = w - 130;
                play.X = label.X + label.Width + 5;
                stop.X = play.X + play.Width;
                menu.X = stop.X + stop.Width;
            }
        }
    }
}
