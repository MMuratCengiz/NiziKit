using DenOfIz;

namespace NiziKit.UI.Widgets;

public abstract class TextInput : Widget
{
    private uint _textId;

    private protected TextInput(bool multiline)
    {
        Editor.Multiline = multiline;
        Desc.Cursor = SystemCursor.Text;
        Desc.Style.BorderWidth = UIEdges.All(1);
    }

    public string Text
    {
        get => Editor.Text;
        set
        {
            if (string.Equals(Editor.Text, value, StringComparison.Ordinal))
            {
                return;
            }

            Editor.SetText(value);
            TextChanged?.Invoke(this);
        }
    }

    public string Placeholder { get; set; } = "";

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

    public float FontSize { get; set; }

    public int Caret => Editor.Caret;

    public bool HasSelection => Editor.HasSelection;
    public int SelectionStart => Editor.SelectionStart;
    public int SelectionEnd => Editor.SelectionEnd;
    public string SelectedText => Editor.SelectedText;

    public ReadOnlySpan<byte> Utf8 => Editor.Bytes;

    public event Action<TextInput>? TextChanged;

    public event Action<TextInput>? Submitted;

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
        Text = "";
    }

    internal TextEditor Editor { get; } = new();

    private protected uint TextId => _textId != 0 ? _textId : _textId = Ui.Id(Id, 2);

    protected override bool Focusable => true;

    internal override bool AcceptsTextInput => !Editor.ReadOnly;

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        var result = Editor.HandleKey(in key, TextId);
        if (result == true)
        {
            TextChanged?.Invoke(this);
            return true;
        }

        if (result == false)
        {
            return true;
        }

        if (key.KeyCode is KeyCode.Return or KeyCode.KeypadEnter)
        {
            Submitted?.Invoke(this);
            return true;
        }

        return key.KeyCode is not (KeyCode.Tab or KeyCode.Escape) and not (>= KeyCode.F1 and <= KeyCode.F12);
    }

    internal override void OnTextInput(ReadOnlySpan<byte> utf8)
    {
        if (Editor.Insert(utf8))
        {
            TextChanged?.Invoke(this);
        }
    }

    protected override void OnFocusChanged(bool focused)
    {
        Editor.Touch();
        if (!focused)
        {
            Editor.EndPointer();
        }
    }

    private protected virtual int TextOffset(int displayOffset)
    {
        return displayOffset;
    }

    private protected void HandlePointer()
    {
        ref readonly var interaction = ref Interaction;
        if (!Enabled || !(interaction.PressStarted || interaction.Pressed))
        {
            Editor.EndPointer();
            return;
        }

        var offset = (int)Ui.Native.GetTextOffsetAtPoint(TextId, Ui.PointerPosition);
        Editor.HandlePointer(in interaction, TextOffset(offset));
    }

    private protected void ApplyChrome()
    {
        var theme = Ui.Theme;
        ref var desc = ref Desc;
        desc.Style.BackgroundColor = Enabled ? theme.Surface : theme.Disabled;
        desc.Style.BorderColor = !Enabled ? theme.Disabled : IsFocused ? theme.Accent : IsHovered ? theme.BorderStrong : theme.Border;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);
        desc.Transition.Duration = theme.TransitionDuration;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.BorderColor;
        desc.Cursor = Enabled ? SystemCursor.Text : SystemCursor.NotAllowed;
    }

    private protected UITextDesc EditorStyle(UITextWrap wrap, uint caret, uint selectionStart, uint selectionEnd)
    {
        var theme = Ui.Theme;
        var focused = IsFocused;
        var desc = Ui.TextStyle(FontSize, Enabled ? theme.Text : theme.TextDisabled);
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
        var desc = Ui.TextStyle(FontSize, Ui.Theme.TextMuted);
        desc.Wrap = wrap;
        desc.Overflow = wrap == UITextWrap.None ? UITextOverflow.Ellipsis : UITextOverflow.Clip;
        return desc;
    }

    private protected bool ShowsPlaceholder => Editor.Length == 0 && !IsFocused && Placeholder.Length > 0;
}
