using DenOfIz;

namespace NiziKit.UI.Widgets;

public class TextArea : TextInput
{
    public TextArea() : base(true)
    {
        ref var layout = ref Desc.Layout;
        layout.Direction = UIDirection.Column;
        layout.Height = 120;
        layout.Padding = UILengthEdges.Symmetric(10, 8);
        Desc.Scroll.OverflowX = UIOverflow.Clip;
        Desc.Scroll.OverflowY = UIOverflow.Scroll;
    }

    public TextArea(string text) : this()
    {
        Text = text;
    }

    public UITextWrap Wrap { get; set; } = UITextWrap.Words;

    public int LineCount => (int)Ui.Native.GetTextLineCount(TextId);

    protected override void OnBuild()
    {
        HandlePointer();
        ApplyChrome();
        using (OpenSelf())
        {
            if (ShowsPlaceholder)
            {
                var placeholder = PlaceholderStyle(Wrap);
                Ui.Text(Placeholder, in placeholder);
                return;
            }

            var editor = Editor;
            var desc = EditorStyle(Wrap, (uint)editor.Caret, (uint)editor.SelectionStart, (uint)editor.SelectionEnd);
            Ui.Text(editor.Bytes, in desc);
            if (editor.Reveal && IsFocused)
            {
                editor.Reveal = false;
                Ui.Native.ScrollCaretIntoView(TextId, (uint)editor.Caret);
            }
        }
    }
}
