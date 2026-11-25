using DenOfIz;

namespace NiziKit.UI.Widgets;

public sealed class MenuItem
{
    public string Text { get; set; } = "";

    public string? Icon { get; set; }

    public string? Shortcut { get; set; }

    public bool Enabled { get; set; } = true;
    public bool IsSeparator { get; init; }
    public Action? Action { get; set; }
}

public class Menu : Popup
{
    private readonly List<MenuItem> _items = new();
    private readonly List<uint> _itemIds = new();
    private int _highlight = -1;
    private int _activate = -1;

    public Menu()
    {
        Layout.MinWidth = 180;
        Layout.Padding = UILengthEdges.All(4);
        Layout.Gap = 0;
    }

    public IReadOnlyList<MenuItem> Items => _items;

    public MenuItem Add(string text, Action? action = null, string? icon = null, string? shortcut = null)
    {
        var item = new MenuItem { Text = text, Action = action, Icon = icon, Shortcut = shortcut };
        Add(item);
        return item;
    }

    public void Add(MenuItem item)
    {
        _items.Add(item);
        _itemIds.Add(Ui.Id(Id, (uint)_items.Count));
    }

    public void AddSeparator()
    {
        Add(new MenuItem { IsSeparator = true });
    }

    public void ClearItems()
    {
        _items.Clear();
        _itemIds.Clear();
        _highlight = -1;
    }

    protected override void OnOpened()
    {
        _highlight = -1;
        _activate = -1;
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        switch (key.KeyCode)
        {
            case KeyCode.Down:
                _highlight = Step(_highlight, 1);
                return true;
            case KeyCode.Up:
                _highlight = Step(_highlight < 0 ? _items.Count : _highlight, -1);
                return true;
            case KeyCode.Home:
                _highlight = Step(-1, 1);
                return true;
            case KeyCode.End:
                _highlight = Step(_items.Count, -1);
                return true;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Space:
                _activate = _highlight;
                return true;
            default:
                return false;
        }
    }

    protected override void BuildChildren()
    {
        var theme = Ui.Theme;
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.IsSeparator)
            {
                Ui.Element().Height(1).Margin(0, 4).Background(theme.Border).Leaf();
                continue;
            }

            var id = _itemIds[i];
            var interaction = Ui.Interaction(id);
            if (interaction.HoverStarted || (interaction.Hovered && _highlight < 0))
            {
                _highlight = i;
            }

            if (item.Enabled && (interaction.Clicked || _activate == i))
            {
                _activate = -1;
                Run(item);
            }

            var highlighted = _highlight == i && item.Enabled;
            var text = item.Enabled ? theme.Text : theme.TextDisabled;
            using (Ui.Row(id)
                       .AlignItems(UIAlign.Center)
                       .Gap(10)
                       .Padding(10, 6)
                       .CornerRadius(theme.CornerRadius - 2)
                       .Background(highlighted ? theme.AccentSoft : default)
                       .Disabled(!item.Enabled)
                       .Cursor(item.Enabled ? SystemCursor.Pointer : SystemCursor.NotAllowed)
                       .Open())
            {
                if (FontAwesome.IsInitialized)
                {
                    using (Ui.Row().Width(16).Shrink(0).Justify(UIJustify.Center).Open())
                    {
                        if (item.Icon != null)
                        {
                            Ui.Text(item.Icon, theme.FontSize, highlighted ? theme.Accent : theme.TextMuted, FontAwesome.FontId);
                        }
                    }
                }

                Ui.Text(item.Text, theme.FontSize, text);
                if (item.Shortcut != null)
                {
                    using (Ui.Row().Margin(left: UILength.Auto).Padding(16, 0, 0, 0).Open())
                    {
                        Ui.Text(item.Shortcut, theme.SmallFontSize, theme.TextMuted);
                    }
                }
            }
        }

        base.BuildChildren();
    }

    private void Run(MenuItem item)
    {
        Close();
        item.Action?.Invoke();
    }

    private int Step(int from, int direction)
    {
        for (var i = from + direction; i >= 0 && i < _items.Count; i += direction)
        {
            if (_items[i] is { IsSeparator: false, Enabled: true })
            {
                return i;
            }
        }

        return _highlight;
    }
}
