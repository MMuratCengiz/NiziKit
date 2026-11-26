using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

public abstract class TextInput : Widget
{
    private uint _textId;
    private string? _shown;

    private protected TextInput(bool multiline)
    {
        Editor.Multiline = multiline;
    }

    public Bound<string> Text { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public Action<string>? OnChange { get; set; }
    public Action<string>? OnSubmit { get; set; }
    public float FontSize { get; set; }

    public bool ReadOnly
    {
        get => Editor.ReadOnly;
        set => Editor.ReadOnly = value;
    }

    public int MaxLength
    {
        get => Editor.MaxLength;
        set => Editor.MaxLength = Math.Max(0, value);
    }

    public int Caret => Editor.Caret;
    public bool HasSelection => Editor.HasSelection;
    public int SelectionStart => Editor.SelectionStart;
    public int SelectionEnd => Editor.SelectionEnd;
    public string SelectedText => Editor.SelectedText;
    public int ByteLength => Editor.Length;

    public void SelectAll()
    {
        Editor.SelectAll();
    }

    public void Select(int start, int end)
    {
        Editor.Select(start, end);
    }

    public void Clear()
    {
        Editor.SetText("");
        Changed();
    }

    internal TextEditor Editor { get; } = new();

    private protected uint TextId => _textId != 0 ? _textId : _textId = Ui.Id(Id, 2);

    protected override bool Focusable => true;
    protected override bool KeepsSize => true;
    internal override bool Interactive => true;
    internal override bool AcceptsTextInput => !Editor.ReadOnly;

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        var result = Editor.HandleKey(in key, TextId);
        if (result == true)
        {
            Changed();
            return true;
        }

        if (result == false)
        {
            return true;
        }

        if (key.KeyCode is KeyCode.Return or KeyCode.KeypadEnter)
        {
            OnSubmit?.Invoke(Editor.Text);
            return true;
        }

        return key.KeyCode is not (KeyCode.Tab or KeyCode.Escape) and not (>= KeyCode.F1 and <= KeyCode.F12);
    }

    internal override void OnTextInput(ReadOnlySpan<byte> utf8)
    {
        if (Editor.Insert(utf8))
        {
            Changed();
        }
    }

    internal override void OnFocusChanged(bool focused)
    {
        Editor.Touch();
        if (!focused)
        {
            Editor.EndPointer();
        }
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        ref readonly var state = ref State;
        if (!EnabledNow || !(state.PressStarted || state.Pressed))
        {
            Editor.EndPointer();
            return;
        }

        var offset = (int)Ui.Native.GetTextOffsetAtPoint(TextId, Ui.PointerPosition);
        Editor.HandlePointer(in state, TextOffset(offset));
    }

    private protected virtual int TextOffset(int displayOffset)
    {
        return displayOffset;
    }

    private protected void SyncFromBinding()
    {
        var value = Text.Value ?? "";
        if (ReferenceEquals(value, _shown))
        {
            return;
        }

        _shown = value;
        if (!string.Equals(Editor.Text, value, StringComparison.Ordinal))
        {
            Editor.SetText(value);
        }
    }

    private void Changed()
    {
        var text = Editor.Text;
        _shown = text;
        var bound = Text;
        bound.Set(text);
        Text = bound;
        OnChange?.Invoke(text);
    }

    private protected UIElementDesc Chrome()
    {
        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var desc = Desc();
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.BackgroundColor = enabled ? theme.Surface : theme.Disabled;
        desc.Style.BorderColor = !enabled ? theme.Disabled : IsFocused ? theme.Accent : IsHovered ? theme.BorderStrong : theme.Border;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);
        desc.Transition.Duration = theme.TransitionDuration;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.BorderColor;
        desc.Cursor = enabled ? SystemCursor.Text : SystemCursor.NotAllowed;
        return desc;
    }

    private protected UITextDesc EditorStyle(UITextWrap wrap, uint caret, uint selectionStart, uint selectionEnd)
    {
        var theme = Ui.Theme;
        var focused = IsFocused;
        var desc = Ui.TextStyleOf(FontSize, EnabledNow ? theme.Text : theme.TextDisabled);
        desc.Id = TextId;
        desc.Wrap = wrap;
        desc.Selection.Start = selectionStart;
        desc.Selection.End = selectionEnd;
        desc.Selection.Color = focused ? theme.Selection : theme.InactiveSelection;
        desc.Caret = caret;
        desc.CaretColor = focused && !ReadOnly && Editor.CaretVisible ? theme.Text : default;
        return desc;
    }

    private protected UITextDesc PlaceholderStyle(UITextWrap wrap)
    {
        var desc = Ui.TextStyleOf(FontSize, Ui.Theme.TextMuted);
        desc.Wrap = wrap;
        desc.Overflow = wrap == UITextWrap.None ? UITextOverflow.Ellipsis : UITextOverflow.Clip;
        return desc;
    }

    private protected bool ShowsPlaceholder => Editor.Length == 0 && !IsFocused && Placeholder.Length > 0;
}

public class TextField : TextInput
{
    private byte[] _masked = [];
    private byte[] _mask = "•"u8.ToArray();
    private char _maskChar = '•';
    private uint _viewportId;

    public TextField() : base(false)
    {
        Width = 200;
    }

    public bool IsPassword { get; set; }

    public char PasswordChar
    {
        get => _maskChar;
        set
        {
            _maskChar = value;
            _mask = Encoding.UTF8.GetBytes([value]);
        }
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        if (IsPassword && Keys.Shortcut(key.Mod) && key.KeyCode is KeyCode.C or KeyCode.X)
        {
            return true;
        }

        return base.OnKeyDown(in key);
    }

    internal override void OnFocusChanged(bool focused)
    {
        base.OnFocusChanged(focused);
        if (!focused && _viewportId != 0)
        {
            Ui.ScrollTo(_viewportId, Vector2.Zero);
        }
    }

    private protected override int TextOffset(int displayOffset)
    {
        if (!IsPassword)
        {
            return displayOffset;
        }

        var bytes = Editor.Bytes;
        var offset = 0;
        for (var i = displayOffset / _mask.Length; i > 0 && offset < bytes.Length; i--)
        {
            offset = TextEditor.NextCodepoint(bytes, offset);
        }

        return offset;
    }

    internal override void Render()
    {
        if (_viewportId == 0)
        {
            _viewportId = Ui.Id(Id, 1);
        }

        SyncFromBinding();
        var desc = Chrome();
        desc.Layout.AlignItems = UIAlign.Center;
        desc.Layout.Padding = new Edges(10, 7).ToLength();
        Ui.Open(in desc);
        OfferTooltip();
        using (Element.Row(_viewportId)
                   .Fill()
                   .AlignItems(UIAlign.Center)
                   .Padding(0, 0, 2, 0)
                   .Overflow(UIOverflow.Scroll, UIOverflow.Clip)
                   .HideScrollbar()
                   .Open())
        {
            if (ShowsPlaceholder)
            {
                var placeholder = PlaceholderStyle(UITextWrap.None);
                Ui.Text(Placeholder, in placeholder);
            }
            else
            {
                var editor = Editor;
                var text = IsPassword ? Mask() : editor.Bytes;
                var caret = DisplayOffset(editor.Caret);
                var style = EditorStyle(UITextWrap.None, caret, DisplayOffset(editor.SelectionStart), DisplayOffset(editor.SelectionEnd));
                Ui.Text(text, in style);
                if (editor.Reveal && IsFocused)
                {
                    editor.Reveal = false;
                    Ui.Native.ScrollCaretIntoView(TextId, caret);
                }
            }
        }

        Ui.Close();
    }

    private uint DisplayOffset(int offset)
    {
        return IsPassword ? (uint)(TextEditor.CodepointCount(Editor.Bytes[..offset]) * _mask.Length) : (uint)offset;
    }

    private ReadOnlySpan<byte> Mask()
    {
        var count = TextEditor.CodepointCount(Editor.Bytes);
        var length = count * _mask.Length;
        if (_masked.Length < length)
        {
            _masked = new byte[Math.Max(length, _masked.Length * 2)];
        }

        for (var i = 0; i < count; i++)
        {
            _mask.CopyTo(_masked, i * _mask.Length);
        }

        return _masked.AsSpan(0, length);
    }
}

public class TextArea : TextInput
{
    public TextArea() : base(true)
    {
        Height = 120;
    }

    public UITextWrap Wrap { get; set; } = UITextWrap.Words;

    public int LineCount => (int)Ui.Native.GetTextLineCount(TextId);

    internal override void Render()
    {
        SyncFromBinding();
        var desc = Chrome();
        desc.Layout.Direction = UIDirection.Column;
        desc.Layout.Padding = new Edges(10, 8).ToLength();
        desc.Scroll.OverflowX = UIOverflow.Clip;
        desc.Scroll.OverflowY = UIOverflow.Scroll;
        if (Width.Type == UIValueType.Auto && Flex <= 0)
        {
            desc.Layout.AlignSelf = UIAlignSelf.Stretch;
        }

        Ui.Open(in desc);
        OfferTooltip();
        if (ShowsPlaceholder)
        {
            var placeholder = PlaceholderStyle(Wrap);
            Ui.Text(Placeholder, in placeholder);
        }
        else
        {
            var editor = Editor;
            var style = EditorStyle(Wrap, (uint)editor.Caret, (uint)editor.SelectionStart, (uint)editor.SelectionEnd);
            Ui.Text(editor.Bytes, in style);
            if (editor.Reveal && IsFocused)
            {
                editor.Reveal = false;
                Ui.Native.ScrollCaretIntoView(TextId, (uint)editor.Caret);
            }
        }

        Ui.Close();
    }
}
