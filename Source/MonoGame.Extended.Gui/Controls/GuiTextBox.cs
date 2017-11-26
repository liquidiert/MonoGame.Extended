using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;
using System.Collections.Generic;
using MonoGame.Extended.BitmapFonts;

namespace MonoGame.Extended.Gui.Controls
{
    public class GuiTextBox : GuiControl
    {
        public GuiTextBox()
            : this(null)
        {
        }

        public GuiTextBox(GuiSkin skin, string text = null)
            : base(skin)
        {
            Text = text ?? string.Empty;
        }

        public int SelectionStart { get; set; }
        public char? PasswordCharacter { get; set; }
        public Thickness Padding { get; set; }

        protected override void OnTextChanged()
        {
            if (SelectionStart > Text.Length)
                SelectionStart = Text.Length;

            base.OnTextChanged();
        }

        protected override Size2 CalculateDesiredSize(IGuiContext context, Size2 availableSize)
        {
            var font = Font ?? context.DefaultFont;
            return new Size2(Width + Padding.Left + Padding.Right, (Height <= 0.0f ? font.LineHeight + 2 : Height) + Padding.Top + Padding.Bottom);
        }

        public override bool OnPointerDown(IGuiContext context, GuiPointerEventArgs args)
        {
            SelectionStart = FindNearestGlyphIndex(context, args.Position);
            _isCaretVisible = true;

            _selectionIndexes.Clear();
            _selectionIndexes.Push(SelectionStart);
            _startSelectionBox = Text.Length > 0;

            return base.OnPointerDown(context, args);
        }

        public override bool OnPointerMove(IGuiContext context, GuiPointerEventArgs args)
        {
            if (_startSelectionBox)
            {
                var selection = FindNearestGlyphIndex(context, args.Position);
                if (selection != _selectionIndexes.Peek())
                {
                    if (_selectionIndexes.Count == 1)
                    {
                        _selectionIndexes.Push(selection);
                    }
                    else if (_selectionIndexes.Last() < _selectionIndexes.Peek())
                    {
                        if (selection > _selectionIndexes.Peek()) _selectionIndexes.Pop();
                        else _selectionIndexes.Push(selection);
                    }
                    else
                    {
                        if (selection < _selectionIndexes.Peek()) _selectionIndexes.Pop();
                        else _selectionIndexes.Push(selection);
                    }
                    SelectionStart = selection;
                }
            }

            return base.OnPointerMove(context, args);
        }

        public override bool OnPointerLeave(IGuiContext context, GuiPointerEventArgs args)
        {
            _startSelectionBox = false;

            return base.OnPointerLeave(context, args);
        }

        public override bool OnPointerUp(IGuiContext context, GuiPointerEventArgs args)
        {
            _startSelectionBox = false;

            return base.OnPointerUp(context, args);
        }

        private int FindNearestGlyphIndex(IGuiContext context, Point position)
        {
            var font = Font ?? context.DefaultFont;
            var textInfo = GetTextInfo(context, Text, BoundingRectangle, HorizontalAlignment.Centre, VerticalAlignment.Centre);
            var i = 0;

            foreach (var glyph in font.GetGlyphs(textInfo.Text, textInfo.Position))
            {
                var fontRegionWidth = glyph.FontRegion?.Width ?? 0;
                var glyphMiddle = (int)(glyph.Position.X + fontRegionWidth * 0.5f);

                if (position.X >= glyphMiddle)
                {
                    i++;
                    continue;
                }

                return i;
            }

            return i;
        }

        public override bool OnKeyPressed(IGuiContext context, KeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case Keys.Tab:
                case Keys.Enter:
                    return true;
                case Keys.Back:
                    if (Text.Length > 0)
                    {
                        if (SelectionStart > 0 && _selectionIndexes.Count <= 1)
                        {
                            SelectionStart--;
                            Text = Text.Remove(SelectionStart, 1);
                        }
                        else
                        {
                            var start = MathHelper.Min(_selectionIndexes.Last(), _selectionIndexes.Peek());
                            var end = MathHelper.Max(_selectionIndexes.Last(), _selectionIndexes.Peek());
                            Text = Text.Remove(start, end - start);

                            _selectionIndexes.Clear();
                        }
                    }
                    break;
                case Keys.Delete:
                    if (SelectionStart < Text.Length && _selectionIndexes.Count <= 1)
                    {
                        Text = Text.Remove(SelectionStart, 1);
                    }
                    else if (_selectionIndexes.Count > 1)
                    {
                        var start = MathHelper.Min(_selectionIndexes.Last(), _selectionIndexes.Peek());
                        var end = MathHelper.Max(_selectionIndexes.Last(), _selectionIndexes.Peek());
                        Text = Text.Remove(start, end - start);

                        _selectionIndexes.Clear();
                    }
                    break;
                case Keys.Left:
                    if (SelectionStart > 0)
                    {
                        if (_selectionIndexes.Count > 1)
                        {
                            if (_selectionIndexes.Last() < SelectionStart) SelectionStart = _selectionIndexes.Last();
                            _selectionIndexes.Clear();
                        }
                        else
                        {
                            SelectionStart--;
                        }
                    }
                    break;
                case Keys.Right:
                    if (SelectionStart < Text.Length)
                    {
                        if (_selectionIndexes.Count > 1)
                        {
                            if (_selectionIndexes.Last() > SelectionStart) SelectionStart = _selectionIndexes.Last();
                            _selectionIndexes.Clear();
                        }
                        else
                        {
                            SelectionStart++;
                        }
                    }
                    break;
                case Keys.Home:
                    SelectionStart = 0;
                    _selectionIndexes.Clear();
                    break;
                case Keys.End:
                    SelectionStart = Text.Length;
                    _selectionIndexes.Clear();
                    break;
                default:
                    if (args.Character != null)
                    {
                        if (_selectionIndexes.Count > 1)
                        {
                            var start = MathHelper.Min(_selectionIndexes.Last(), _selectionIndexes.Peek());
                            var end = MathHelper.Max(_selectionIndexes.Last(), _selectionIndexes.Peek());
                            Text = Text.Remove(start, end - start);

                            _selectionIndexes.Clear();
                        }

                        Text = Text.Insert(SelectionStart, args.Character.ToString());
                        SelectionStart++;
                    }
                    break;
            }

            _isCaretVisible = true;
            return base.OnKeyPressed(context, args);
        }

        private const float _caretBlinkRate = 0.53f;
        private float _nextCaretBlink = _caretBlinkRate;
        private bool _isCaretVisible = true;

        private bool _startSelectionBox = false;
        private Stack<int> _selectionIndexes = new Stack<int>();

        protected override void DrawForeground(IGuiContext context, IGuiRenderer renderer, float deltaSeconds, TextInfo textInfo)
        {
            if (PasswordCharacter.HasValue)
                textInfo = GetTextInfo(context, new string(PasswordCharacter.Value, textInfo.Text.Length), BoundingRectangle, HorizontalAlignment.Centre, VerticalAlignment.Centre);

            base.DrawForeground(context, renderer, deltaSeconds, textInfo);

            if (IsFocused)
            {
                var caretRectangle = GetCaretRectangle(textInfo, SelectionStart);

                if (_isCaretVisible)
                    renderer.DrawRectangle((Rectangle)caretRectangle, TextColor);

                _nextCaretBlink -= deltaSeconds;

                if (_nextCaretBlink <= 0)
                {
                    _isCaretVisible = !_isCaretVisible;
                    _nextCaretBlink = _caretBlinkRate;
                }

                if (_selectionIndexes.Count > 1)
                {
                    var start = 0;
                    var end = 0;
                    var point = Point2.Zero;
                    if (_selectionIndexes.Last() > _selectionIndexes.Peek())
                    {
                        start = _selectionIndexes.Peek();
                        end = _selectionIndexes.Last();
                        point = caretRectangle.Position;
                    }
                    else
                    {
                        start = _selectionIndexes.Last();
                        end = _selectionIndexes.Peek();
                        point = GetCaretRectangle(textInfo, start).Position;
                    }
                    var selectionRectangle = textInfo.Font.GetStringRectangle(textInfo.Text.Substring(start, end - start), point);

                    renderer.FillRectangle((Rectangle)selectionRectangle, Color.Black * 0.25f);
                }
            }
        }

        protected override string CreateBoxText(string text, BitmapFont font, float width)
        {
            return text;
        }

        private RectangleF GetCaretRectangle(TextInfo textInfo, int index)
        {
            var caretRectangle = textInfo.Font.GetStringRectangle(textInfo.Text.Substring(0, index), textInfo.Position);

            // TODO: Finish the caret position stuff when it's outside the clipping rectangle
            if (caretRectangle.Right > ClippingRectangle.Right)
                textInfo.Position.X -= caretRectangle.Right - ClippingRectangle.Right;

            caretRectangle.X = caretRectangle.Right < ClippingRectangle.Right ? caretRectangle.Right : ClippingRectangle.Right;
            caretRectangle.Width = 1;

            if (caretRectangle.Left < ClippingRectangle.Left)
            {
                textInfo.Position.X += ClippingRectangle.Left - caretRectangle.Left;
                caretRectangle.X = ClippingRectangle.Left;
            }
            return caretRectangle;
        }
    }
}