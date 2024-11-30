using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
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

            Add(background = new AlphaBlendControl(0.77f) { X = BorderControl.BorderSize, Y = BorderControl.BorderSize});

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
            private NiceButton play, stop;

            public ScriptFile Script { get; }

            public ScriptControl(int w, ScriptFile script)
            {
                Width = w;
                Height = 50;

                Add(background = new AlphaBlendControl(0.35f) {  Height = Height, Width = Width });

                Add(label = new TextBox(script.FileName, TrueTypeLoader.EMBEDDED_FONT, 18, w - 105, Color.White, strokeEffect: false));
                label.Y = (Height - label.MeasuredSize.Y) / 2;
                label.X = 5;

                Add(play = new NiceButton(w - 100, 0, 50, Height, ButtonAction.Default, "Play"));
                play.MouseUp += Play_MouseUp;

                Add(stop = new NiceButton(w - 50, 0, 50, Height, ButtonAction.Default, "Stop"));
                stop.MouseUp += Stop_MouseUp;

                UpdateSize(w);
                Script = script;
            }

            private void Stop_MouseUp(object sender, Input.MouseEventArgs e)
            {
                LegionScripting.StopScript(Script.FileAsScript);
                stop.IsSelected = true;
                play.IsSelected = false;
            }

            private void Play_MouseUp(object sender, Input.MouseEventArgs e)
            {
                LegionScripting.PlayScript(Script.FileAsScript);
                play.IsSelected = true;
                stop.IsSelected = false;
            }

            public override void SlowUpdate()
            {
                base.SlowUpdate();
                if (Script.FileAsScript.IsPlaying)
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
                label.Width = w - 105;
                play.X = w - 100;
                stop.X = w - 50;
            }
        }
    }
}
