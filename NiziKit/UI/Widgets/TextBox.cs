using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class TextBox : TextInput
{
    private byte[] _masked = [];
    private byte[] _mask = "•"u8.ToArray();
    private char _maskChar = '•';
    private uint _viewportId;

    public TextBox() : base(false)
    {
        ref var layout = ref Desc.Layout;
        layout.Width = 200;
        layout.AlignItems = UIAlign.Center;
        layout.Padding = UILengthEdges.Symmetric(10, 7);
    }

    public TextBox(string text) : this()
    {
        Text = text;
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

    protected override void OnFocusChanged(bool focused)
    {
        base.OnFocusChanged(focused);
        if (!focused)
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

    protected override void OnBuild()
    {
        if (_viewportId == 0)
        {
            _viewportId = Ui.Id(Id, 1);
        }

        HandlePointer();
        ApplyChrome();
        using (OpenSelf())
        {
            using (Ui.Row(_viewportId)
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
                    var desc = EditorStyle(UITextWrap.None, caret, DisplayOffset(editor.SelectionStart), DisplayOffset(editor.SelectionEnd));
                    Ui.Text(text, in desc);
                    if (editor.Reveal && IsFocused)
                    {
                        editor.Reveal = false;
                        Ui.Native.ScrollCaretIntoView(TextId, caret);
                    }
                }
            }
        }
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
