using System.Collections.Generic;
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;
using static ClassicUO.Game.UI.Gumps.ModernOptionsGump;

namespace ClassicUO.LegionScripting
{
    internal class ScriptEditor : ResizableGump
    {
        private NiceButton save;
        private List<InputField> inputFields = new List<InputField>();
        private AlphaBlendControl background;
        private ScrollArea scrollArea;
        private TextBox title;
        private bool built;
        public ScriptEditor(ScriptFile scriptFile) : base(600, 400, 600, 400, 0, 0)
        {
            ScriptFile = scriptFile;
            AcceptMouseInput = true;
            CanMove = true;

            Add(background = new AlphaBlendControl());

            Add(title = new TextBox(scriptFile.FileName, TrueTypeLoader.EMBEDDED_FONT, 24, Width - 100, color: Color.White, strokeEffect: false));

            Add(save = new NiceButton(Width - 50, 0, 50, 50, ButtonAction.Default, "Save"));
            save.MouseUp += Save_MouseUp;

            scrollArea = new ScrollArea(0, 50, Width, Height - 50, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(scrollArea);

            int width = Width - scrollArea.ScrollBarWidth();
            int y = 0;
            InputField f = new InputField(width - 4, 20, text: string.Empty) { Y = y, X = 4 };
            inputFields.Add(f);
            scrollArea.Add(f);
            y += 22;

            foreach (string l in File.ReadAllLines(scriptFile.FullPath))
            {
                f = new InputField(width - 4, 20, text: l) { Y = y, X = 4 };
                inputFields.Add(f);
                scrollArea.Add(f);
                y += 22;

                f = new InputField(width - 4, 20, text: string.Empty) { Y = y, X = 4 };
                inputFields.Add(f);
                scrollArea.Add(f);
                y += 22;
            }
            built = true;
            OnResize();
        }

        public override void OnResize()
        {
            base.OnResize();
            if (!built) return;

            background.Width = Width - (BorderControl.BorderSize * 2);
            background.Height = Height - (BorderControl.BorderSize * 2);
            background.X = BorderControl.BorderSize;
            background.Y = BorderControl.BorderSize;

            title.Width = Width - 100;
            title.X = BorderControl.BorderSize;
            title.Y = BorderControl.BorderSize;

            save.X = Width - 50 - BorderControl.BorderSize;
            save.Y = BorderControl.BorderSize;

            scrollArea.Width = Width - (BorderControl.BorderSize * 2);
            scrollArea.Height = Height - (BorderControl.BorderSize * 2) - 50;
            scrollArea.UpdateScrollbarPosition();

            foreach (InputField f in inputFields)
            {                
                f.Width = scrollArea.Width - scrollArea.ScrollBarWidth() - 4;
                f.UpdateBackground();
            }
        }

        private void Save_MouseUp(object sender, Input.MouseEventArgs e)
        {
            GameActions.Print("Save not implemented yet..");
        }

        public ScriptFile ScriptFile { get; }
    }
}
