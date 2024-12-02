using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL2;
using StbTextEditSharp;

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

            int width = Width - scrollArea.ScrollBarWidth() - 4;
            int y = 0;

            foreach (string l in File.ReadAllLines(scriptFile.FullPath))
            {
                InputField f = genInputField(width, l, y);
                inputFields.Add(f);
                scrollArea.Add(f);
                y += 22;
            }
            built = true;
            OnResize();
        }

        private InputField genInputField(int width, string text, int y = 0)
        {
            InputField field = new InputField(width, 20, text: text) { Y = y, X = 4 };
            field.ScriptEditorParent = this;
            return field;
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

        private void RepositionLines()
        {
            int y = 0;
            foreach (InputField line in inputFields)
            {
                line.Y = y;
                y += 22;
            }
        }

        public void EnterPressed(InputField f)
        {
            if (inputFields.Contains(f))
            {
                int pos = inputFields.IndexOf(f);
                string prevtext = f.Text.Substring(0, f.CaretPosition);
                string newtext = f.Text.Substring(f.CaretPosition);

                f.SetText(prevtext);
                InputField newfield = genInputField(f.Width, newtext);
                inputFields.Insert(pos + 1, newfield);
                scrollArea.Add(newfield);
                newfield.SetFocus();
                RepositionLines();
            }
        }

        private void Save_MouseUp(object sender, MouseEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach(InputField f in inputFields)
            {
                sb.Append(f.Text + "\n");
            }

            try
            {
                File.WriteAllText(ScriptFile.FullPath, sb.ToString());
                GameActions.Print($"Saved {ScriptFile.FileName}.");
            }
            catch (Exception ex)
            {
                GameActions.Print(ex.ToString());
            }
        }

        public ScriptFile ScriptFile { get; }

        public class InputField : Control
        {
            private readonly StbTextBox _textbox;

            private AlphaBlendControl _background;

            public event EventHandler TextChanged { add { _textbox.TextChanged += value; } remove { _textbox.TextChanged -= value; } }
            public event EventHandler<KeyboardEventArgs> KeyDown { add { _textbox.KeyDown += value; } remove { _textbox.KeyDown -= value; } }

            public ScriptEditor ScriptEditorParent { get; set; }

            public int CaretPosition { get { return _textbox.CaretIndex; } }

            public InputField
            (
                int width,
                int height,
                int maxWidthText = 0,
                int maxCharsCount = -1,
                string text = "",
                bool numbersOnly = false,
                EventHandler onTextChanges = null
            )
            {
                WantUpdateSize = false;

                Width = width;
                Height = height;

                _textbox = new StbTextBox
                (
                    maxCharsCount,
                    maxWidthText
                )
                {
                    X = 4,
                    Width = width - 8,
                };
                _textbox.Y = (height >> 1) - (_textbox.Height >> 1);
                _textbox.Text = text;
                _textbox.NumbersOnly = numbersOnly;

                Add(_background = new AlphaBlendControl() { Width = Width, Height = Height });
                Add(_textbox);
                if (onTextChanges != null)
                {
                    TextChanged += onTextChanges;
                }
            }

            public override void OnKeyboardReturn(int textID, string text)
            {
                base.OnKeyboardReturn(textID, text);

                ScriptEditorParent?.EnterPressed(this);
            }

            public void SetFocus()
            {
                _textbox.SetKeyboardFocus();
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    base.Draw(batcher, x, y);

                    batcher.ClipEnd();
                }

                return true;
            }


            public void UpdateBackground()
            {
                _background.Width = Width;
                _background.Height = Height;
            }

            public string Text => _textbox.Text;

            public override bool AcceptKeyboardInput
            {
                get => _textbox.AcceptKeyboardInput;
                set => _textbox.AcceptKeyboardInput = value;
            }

            public bool NumbersOnly
            {
                get => _textbox.NumbersOnly;
                set => _textbox.NumbersOnly = value;
            }


            public void SetText(string text)
            {
                _textbox.SetText(text);
            }


            internal class StbTextBox : Control, ITextEditHandler
            {
                protected static readonly Color SELECTION_COLOR = new Color() { PackedValue = 0x80a06020 };
                private const int FONT_SIZE = 20;
                private readonly int _maxCharCount = -1;


                public StbTextBox
                (
                    int max_char_count = -1,
                    int maxWidth = 0
                )
                {
                    AcceptKeyboardInput = true;
                    AcceptMouseInput = true;
                    CanMove = false;
                    IsEditable = true;

                    _maxCharCount = max_char_count;

                    Stb = new TextEdit(this);
                    Stb.SingleLine = true;

                    _rendererText = new TextBox(string.Empty, TrueTypeLoader.EMBEDDED_FONT, FONT_SIZE, maxWidth > 0 ? maxWidth : null, Color.White, strokeEffect: false, supportsCommands: false, ignoreColorCommands: true, calculateGlyphs: true);
                    _rendererCaret = new TextBox("_", TrueTypeLoader.EMBEDDED_FONT, FONT_SIZE, null, Color.White, strokeEffect: false, supportsCommands: false, ignoreColorCommands: true);

                    Height = _rendererCaret.Height;
                    LoseFocusOnEscapeKey = true;
                }

                protected TextEdit Stb { get; }

                public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

                public bool AllowTAB { get; set; }
                public bool NoSelection { get; set; }

                public bool LoseFocusOnEscapeKey { get; set; }

                public int CaretIndex
                {
                    get => Stb.CursorIndex;
                    set
                    {
                        Stb.CursorIndex = value;
                        UpdateCaretScreenPosition();
                    }
                }

                public bool Multiline
                {
                    get => !Stb.SingleLine;
                    set => Stb.SingleLine = !value;
                }

                public bool NumbersOnly { get; set; }

                public int SelectionStart
                {
                    get => Stb.SelectStart;
                    set
                    {
                        if (AllowSelection)
                        {
                            Stb.SelectStart = value;
                        }
                    }
                }

                public int SelectionEnd
                {
                    get => Stb.SelectEnd;
                    set
                    {
                        if (AllowSelection)
                        {
                            Stb.SelectEnd = value;
                        }
                    }
                }

                public bool AllowSelection { get; set; } = true;

                internal int TotalHeight
                {
                    get
                    {
                        return _rendererText.Height;
                    }
                }

                public string Text
                {
                    get => _rendererText.Text;

                    set
                    {
                        if (_maxCharCount > 0)
                        {
                            if (value != null && value.Length > _maxCharCount)
                            {
                                value = value.Substring(0, _maxCharCount);
                            }
                        }

                        _rendererText.Text = value;

                        if (!_is_writing)
                        {
                            OnTextChanged();
                        }
                    }
                }

                public int Length => Text?.Length ?? 0;

                public float GetWidth(int index)
                {
                    if (Text != null)
                    {
                        if (index < _rendererText.Text.Length)
                        {
                            var glyphRender = _rendererText.RTL.GetGlyphInfoByIndex(index);
                            if (glyphRender != null)
                            {
                                return glyphRender.Value.Bounds.Width;
                            }
                        }
                    }
                    return 0;
                }

                public TextEditRow LayoutRow(int startIndex)
                {
                    TextEditRow r = new TextEditRow() { num_chars = _rendererText.Text.Length };

                    int sx = ScreenCoordinateX;
                    int sy = ScreenCoordinateY;

                    r.x0 += sx;
                    r.x1 += sx;
                    r.ymin += sy;
                    r.ymax += sy;

                    return r;
                }

                protected Point _caretScreenPosition;
                protected bool _is_writing;
                protected bool _leftWasDown, _fromServer;
                protected TextBox _rendererText, _rendererCaret;

                public event EventHandler TextChanged;

                public void SelectAll()
                {
                    if (AllowSelection)
                    {
                        Stb.SelectStart = 0;
                        Stb.SelectEnd = Length;
                    }
                }

                protected void UpdateCaretScreenPosition()
                {
                    _caretScreenPosition = GetCoordsForIndex(Stb.CursorIndex);
                }

                protected Point GetCoordsForIndex(int index)
                {
                    int x = 0, y = 0;

                    if (Text != null)
                    {
                        if (index < Text.Length)
                        {
                            var glyphRender = _rendererText.RTL.GetGlyphInfoByIndex(index);
                            if (glyphRender != null)
                            {
                                x += glyphRender.Value.Bounds.Left;
                                y += glyphRender.Value.LineTop;
                            }
                        }
                        else if (_rendererText.RTL.Lines != null && _rendererText.RTL.Lines.Count > 0)
                        {
                            // After last glyph
                            var lastLine = _rendererText.RTL.Lines[_rendererText.RTL.Lines.Count - 1];
                            if (lastLine.Count > 0)
                            {
                                var glyphRender = lastLine.GetGlyphInfoByIndex(lastLine.Count - 1);

                                x += glyphRender.Value.Bounds.Right;
                                y += glyphRender.Value.LineTop;
                            }
                            else if (_rendererText.RTL.Lines.Count > 1)
                            {
                                var previousLine = _rendererText.RTL.Lines[_rendererText.RTL.Lines.Count - 2];
                                if (previousLine.Count > 0)
                                {
                                    var glyphRender = previousLine.GetGlyphInfoByIndex(0);
                                    y += glyphRender.Value.LineTop + lastLine.Size.Y + _rendererText.RTL.VerticalSpacing;
                                }
                            }
                        }
                    }

                    return new Point(x, y);
                }

                protected int GetIndexFromCoords(Point coords)
                {
                    if (Text != null)
                    {
                        var line = _rendererText.RTL.GetLineByY(coords.Y);
                        if (line != null)
                        {
                            int? index = line.GetGlyphIndexByX(coords.X);
                            if (index != null)
                            {
                                return (int)index;
                            }
                        }
                    }
                    return 0;
                }

                protected Point GetCoordsForClick(Point clicked)
                {
                    if (Text != null)
                    {
                        var line = _rendererText.RTL.GetLineByY(clicked.Y);
                        if (line != null)
                        {
                            int? index = line.GetGlyphIndexByX(clicked.X);
                            if (index != null)
                            {
                                return GetCoordsForIndex((int)index);
                            }
                        }
                    }
                    return Point.Zero;
                }

                private ControlKeys ApplyShiftIfNecessary(ControlKeys k)
                {
                    if (Keyboard.Shift && !NoSelection)
                    {
                        k |= ControlKeys.Shift;
                    }

                    return k;
                }

                private bool IsMaxCharReached(int count)
                {
                    return _maxCharCount >= 0 && Length + count >= _maxCharCount;
                }

                protected virtual void OnTextChanged()
                {
                    TextChanged?.Raise(this);

                    UpdateCaretScreenPosition();
                }

                internal override void OnFocusEnter()
                {
                    base.OnFocusEnter();
                    CaretIndex = Text?.Length ?? 0;
                }

                internal override void OnFocusLost()
                {
                    if (Stb != null)
                    {
                        Stb.SelectStart = Stb.SelectEnd = 0;
                    }

                    base.OnFocusLost();
                }

                protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
                {
                    ControlKeys? stb_key = null;
                    bool update_caret = false;

                    switch (key)
                    {
                        case SDL.SDL_Keycode.SDLK_TAB:
                            if (AllowTAB)
                            {
                                // UO does not support '\t' char in its fonts
                                OnTextInput("   ");
                            }
                            else
                            {
                                Parent?.KeyboardTabToNextFocus(this);
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_a when Keyboard.Ctrl && !NoSelection:
                            SelectAll();

                            break;

                        case SDL.SDL_Keycode.SDLK_ESCAPE:
                            if (LoseFocusOnEscapeKey && SelectionStart == SelectionEnd)
                            {
                                UIManager.KeyboardFocusControl = null;
                            }
                            SelectionStart = 0;
                            SelectionEnd = 0;
                            break;

                        case SDL.SDL_Keycode.SDLK_INSERT when IsEditable:
                            stb_key = ControlKeys.InsertMode;

                            break;

                        case SDL.SDL_Keycode.SDLK_c when Keyboard.Ctrl && !NoSelection:
                            int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                            int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                            if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                            {
                                SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_x when Keyboard.Ctrl && !NoSelection:
                            selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                            selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                            if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                            {
                                SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));

                                if (IsEditable)
                                {
                                    Stb.Cut();
                                }
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_v when Keyboard.Ctrl && IsEditable:
                            OnTextInput(StringHelper.GetClipboardText(Multiline));

                            break;

                        case SDL.SDL_Keycode.SDLK_z when Keyboard.Ctrl && IsEditable:
                            stb_key = ControlKeys.Undo;

                            break;

                        case SDL.SDL_Keycode.SDLK_y when Keyboard.Ctrl && IsEditable:
                            stb_key = ControlKeys.Redo;

                            break;

                        case SDL.SDL_Keycode.SDLK_LEFT:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.WordLeft;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.Left;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.WordLeft;
                            }
                            else
                            {
                                stb_key = ControlKeys.Left;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_RIGHT:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.WordRight;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.Right;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.WordRight;
                            }
                            else
                            {
                                stb_key = ControlKeys.Right;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_UP:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.Up);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_DOWN:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.Down);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_BACKSPACE when IsEditable:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.BackSpace);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_DELETE when IsEditable:
                            stb_key = ApplyShiftIfNecessary(ControlKeys.Delete);
                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_HOME:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.TextStart;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.LineStart;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.TextStart;
                            }
                            else
                            {
                                stb_key = ControlKeys.LineStart;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_END:
                            if (Keyboard.Ctrl && Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.TextEnd;
                                }
                            }
                            else if (Keyboard.Shift)
                            {
                                if (!NoSelection)
                                {
                                    stb_key = ControlKeys.Shift | ControlKeys.LineEnd;
                                }
                            }
                            else if (Keyboard.Ctrl)
                            {
                                stb_key = ControlKeys.TextEnd;
                            }
                            else
                            {
                                stb_key = ControlKeys.LineEnd;
                            }

                            update_caret = true;

                            break;

                        case SDL.SDL_Keycode.SDLK_KP_ENTER:
                        case SDL.SDL_Keycode.SDLK_RETURN:
                            if (IsEditable)
                            {
                                if (Multiline)
                                {
                                    if (!_fromServer && !IsMaxCharReached(0))
                                    {
                                        OnTextInput("\n");
                                    }
                                }
                                else
                                {
                                    Parent?.OnKeyboardReturn(0, Text);

                                    if (UIManager.SystemChat != null && UIManager.SystemChat.TextBoxControl != null && IsFocused)
                                    {
                                        if (!IsFromServer || !UIManager.SystemChat.TextBoxControl.IsVisible)
                                        {
                                            OnFocusLost();
                                            OnFocusEnter();
                                        }
                                        else if (UIManager.KeyboardFocusControl == null || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl)
                                        {
                                            UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
                                        }
                                    }
                                }
                            }

                            break;
                    }

                    if (stb_key != null)
                    {
                        Stb.Key(stb_key.Value);
                    }

                    if (update_caret)
                    {
                        UpdateCaretScreenPosition();
                    }

                    base.OnKeyDown(key, mod);
                }

                public void SetText(string text)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        ClearText();
                    }
                    else
                    {
                        if (_maxCharCount > 0)
                        {
                            if (text.Length > _maxCharCount)
                            {
                                text = text.Substring(0, _maxCharCount);
                            }
                        }

                        Stb.ClearState(!Multiline);
                        Text = text;

                        Stb.CursorIndex = Length;

                        if (!_is_writing)
                        {
                            OnTextChanged();
                        }
                    }
                }

                public void ClearText()
                {
                    if (Length != 0)
                    {
                        SelectionStart = 0;
                        SelectionEnd = 0;
                        Stb.Delete(0, Length);

                        if (!_is_writing)
                        {
                            OnTextChanged();
                        }
                    }
                }

                public void AppendText(string text)
                {
                    Stb.Paste(text);
                }

                protected override void OnTextInput(string c)
                {
                    if (c == null || !IsEditable)
                    {
                        return;
                    }

                    _is_writing = true;

                    if (SelectionStart != SelectionEnd)
                    {
                        Stb.DeleteSelection();
                    }

                    int count;

                    if (_maxCharCount > 0)
                    {
                        int remains = _maxCharCount - Length;

                        if (remains <= 0)
                        {
                            _is_writing = false;

                            return;
                        }

                        count = Math.Min(remains, c.Length);

                        if (remains < c.Length && count > 0)
                        {
                            c = c.Substring(0, count);
                        }
                    }
                    else
                    {
                        count = c.Length;
                    }

                    if (count > 0)
                    {
                        if (NumbersOnly)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                if (!char.IsNumber(c[i]))
                                {
                                    _is_writing = false;

                                    return;
                                }
                            }

                            if (_maxCharCount > 0 && int.TryParse(Stb.text + c, out int val))
                            {
                                if (val > _maxCharCount)
                                {
                                    _is_writing = false;
                                    SetText(_maxCharCount.ToString());

                                    return;
                                }
                            }
                        }


                        if (count > 1)
                        {
                            Stb.Paste(c);
                            OnTextChanged();
                        }
                        else
                        {
                            Stb.InputChar(c[0]);
                            OnTextChanged();
                        }
                    }

                    _is_writing = false;
                }

                private int GetXOffset()
                {
                    if (_caretScreenPosition.X > Width)
                    {
                        return _caretScreenPosition.X - Width + 5;
                    }

                    return 0;
                }

                public void Click(Point pos)
                {
                    pos = new Point((pos.X - ScreenCoordinateX), pos.Y - ScreenCoordinateY);
                    CaretIndex = GetIndexFromCoords(pos);
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    Stb.HasPreferredX = false;
                }

                public void Drag(Point pos)
                {
                    pos = new Point((pos.X - ScreenCoordinateX), pos.Y - ScreenCoordinateY);

                    if (SelectionStart == SelectionEnd)
                    {
                        SelectionStart = CaretIndex;
                    }

                    CaretIndex = SelectionEnd = GetIndexFromCoords(pos);
                }

                private protected void DrawSelection(UltimaBatcher2D batcher, int x, int y)
                {
                    if (!AllowSelection)
                    {
                        return;
                    }

                    int selectStart = Math.Min(SelectionStart, SelectionEnd);
                    int selectEnd = Math.Max(SelectionStart, SelectionEnd);

                    if (selectStart < selectEnd)
                    { //Show selection
                        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

                        Point start = GetCoordsForIndex(selectStart);
                        Point size = GetCoordsForIndex(selectEnd);
                        size = new Point(size.X - start.X, _rendererText.Height);

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                            new Rectangle
                            (
                                x + start.X,
                                y + start.Y,
                                size.X,
                                size.Y
                            ),
                            hueVector
                        );
                    }
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    int slideX = x - GetXOffset();

                    if (batcher.ClipBegin(x, y, Width, Height))
                    {
                        base.Draw(batcher, x, y);
                        DrawSelection(batcher, slideX, y);
                        _rendererText.Draw(batcher, slideX, y);
                        DrawCaret(batcher, slideX, y);
                        batcher.ClipEnd();
                    }

                    return true;
                }

                protected virtual void DrawCaret(UltimaBatcher2D batcher, int x, int y)
                {
                    if (HasKeyboardFocus)
                    {
                        _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
                    }
                }

                protected override void OnMouseDown(int x, int y, MouseButtonType button)
                {
                    if (button == MouseButtonType.Left && IsEditable)
                    {
                        if (!NoSelection)
                        {
                            _leftWasDown = true;
                        }

                        Click(new Point(x + ScreenCoordinateX + GetXOffset(), y + ScreenCoordinateY));
                    }

                    base.OnMouseDown(x, y, button);
                }

                protected override void OnMouseUp(int x, int y, MouseButtonType button)
                {
                    if (button == MouseButtonType.Left)
                    {
                        _leftWasDown = false;
                    }

                    base.OnMouseUp(x, y, button);
                }

                protected override void OnMouseOver(int x, int y)
                {
                    base.OnMouseOver(x, y);

                    if (!_leftWasDown)
                    {
                        return;
                    }

                    Drag(new Point(x + ScreenCoordinateX + GetXOffset(), y + ScreenCoordinateY));
                }

                public override void Dispose()
                {
                    _rendererText?.Dispose();
                    _rendererCaret?.Dispose();

                    base.Dispose();
                }

                protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
                {
                    if (!NoSelection && CaretIndex < Text.Length && CaretIndex >= 0 && !char.IsWhiteSpace(Text[CaretIndex]))
                    {
                        int idx = CaretIndex;

                        if (idx - 1 >= 0 && char.IsWhiteSpace(Text[idx - 1]))
                        {
                            ++idx;
                        }

                        SelectionStart = Stb.MoveToPreviousWord(idx);
                        SelectionEnd = Stb.MoveToNextWord(idx);

                        if (SelectionEnd < Text.Length)
                        {
                            --SelectionEnd;
                        }

                        return true;
                    }

                    return base.OnMouseDoubleClick(x, y, button);
                }
            }
        }

    }
}
