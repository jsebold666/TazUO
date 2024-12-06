using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        private NiceButton refresh, add, browser;

        public ScriptManagerGump() : base(300, 400, 200, 300, 0, 0)
        {
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            AnchorType = ANCHOR_TYPE.DISABLED;

            LegionScripting.LoadScriptsFromFile();

            Add(background = new AlphaBlendControl(0.77f) { X = BorderControl.BorderSize, Y = BorderControl.BorderSize });

            Add(refresh = new NiceButton(BorderControl.BorderSize, BorderControl.BorderSize, 100, 50, ButtonAction.Default, "Refresh") { IsSelectable = false });

            refresh.MouseDown += (s, e) =>
            {
                Dispose();
                ScriptManagerGump g = new ScriptManagerGump() { X = X, Y = Y };
                g.ResizeWindow(new Point(Width, Height));
                UIManager.Add(g);
            };

            Add(add = new NiceButton(0, BorderControl.BorderSize, 100, 50, ButtonAction.Default, "New") { IsSelectable = false });

            add.MouseDown += (s, e) =>
            {
                InputRequest r = new InputRequest("Enter a name for this script. Do not include any file extensions.", "Create", "Cancel", (r, s) =>
                {
                    if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(s))
                    {
                        int p = s.IndexOf('.');
                        if (p != -1)
                            s = s.Substring(0, p);

                        try
                        {
                            if (!File.Exists(Path.Combine(LegionScripting.ScriptPath, s + ".lscript")))
                            {
                                File.WriteAllText(Path.Combine(LegionScripting.ScriptPath, s + ".lscript"), "// My script");
                                refresh.InvokeMouseDown(Point.Zero, MouseButtonType.Left);
                            }
                        }
                        catch (Exception e) { Console.WriteLine(e.ToString()); }
                    }
                });
                r.CenterXInScreen();
                r.CenterYInScreen();
                UIManager.Add(r);
            };

            Add(browser = new NiceButton(0, BorderControl.BorderSize, 100, 50, ButtonAction.Default, "Public Scripts") { IsSelectable = false });

            browser.MouseDown += (s, e) => { UIManager.Add(new ScriptBrowser()); };

            Add(scrollArea = new ScrollArea(BorderControl.BorderSize, BorderControl.BorderSize + 50, Width - (BorderControl.BorderSize * 2), Height - (BorderControl.BorderSize * 2) - 50, true));
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

                refresh.Width = (Width - (BorderControl.BorderSize * 2)) / 3;
                add.Width = (Width - (BorderControl.BorderSize * 2)) / 3;
                add.X = refresh.X + refresh.Width;
                browser.Width = (Width - (BorderControl.BorderSize * 2)) / 3;
                browser.X = add.X + add.Width;

                scrollArea.Width = Width - (BorderControl.BorderSize * 2);
                scrollArea.Height = Height - (BorderControl.BorderSize * 2) - 50;
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
                Script = script;

                Add(background = new AlphaBlendControl(0.35f) { Height = Height, Width = Width });

                Add(label = new TextBox(script.FileName.Substring(0, script.FileName.IndexOf('.')), TrueTypeLoader.EMBEDDED_FONT, 18, w - 130, Color.White, strokeEffect: false));
                label.Y = (Height - label.MeasuredSize.Y) / 2;
                label.X = 5;

                Add(play = new NiceButton(w - 125, 0, 50, Height, ButtonAction.Default, "Play"));
                play.MouseUp += Play_MouseUp;

                Add(stop = new NiceButton(w - 75, 0, 50, Height, ButtonAction.Default, "Stop"));
                stop.MouseUp += Stop_MouseUp;

                Add(menu = new NiceButton(w - 25, 0, 25, Height, ButtonAction.Default, "+"));
                menu.MouseDown += (s, e) => { ContextMenu?.Show(); };

                SetMenuColor();

                UpdateSize(w);
                SlowUpdate(); //Set background colors

                ContextMenu = new ContextMenuControl();

                ContextMenu.Add(new ContextMenuItemEntry("Edit", () => { UIManager.Add(new ScriptEditor(Script)); }));
                ContextMenu.Add(new ContextMenuItemEntry("Edit Externally", () => { OpenFileWithDefaultApp(Script.FullPath); }));
                ContextMenu.Add(new ContextMenuItemEntry("Autostart", () => { GenAutostartContext().Show(); }));
                ContextMenu.Add(new ContextMenuItemEntry("Delete", () =>
                {
                    QuestionGump g = new QuestionGump("Are you sure?", (r) =>
                    {
                        if (r)
                        {
                            try
                            {
                                File.Delete(Script.FullPath);
                                LegionScripting.LoadedScripts.Remove(Script);
                                Dispose();
                            }
                            catch (Exception) { }
                        }
                    });
                    UIManager.Add(g);
                }));
            }

            private void SetMenuColor()
            {
                bool global = LegionScripting.AutoLoadEnabled(Script, true);
                bool chara = LegionScripting.AutoLoadEnabled(Script, false);

                if (global || chara)
                    menu.TextLabel.Hue = 1970;
                else
                    menu.TextLabel.Hue = ushort.MaxValue;
            }

            private static void OpenFileWithDefaultApp(string filePath)
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", filePath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", filePath);
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            private ContextMenuControl GenAutostartContext()
            {
                ContextMenuControl context = new ContextMenuControl();
                bool global = LegionScripting.AutoLoadEnabled(Script, true);
                bool chara = LegionScripting.AutoLoadEnabled(Script, false);

                context.Add(new ContextMenuItemEntry("All characters", () => { LegionScripting.SetAutoPlay(Script, true, !global); SetMenuColor(); }, true, global));
                context.Add(new ContextMenuItemEntry("This character", () => { LegionScripting.SetAutoPlay(Script, false, !chara); SetMenuColor(); }, true, chara));

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
                if (Script.GetScript != null)
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
