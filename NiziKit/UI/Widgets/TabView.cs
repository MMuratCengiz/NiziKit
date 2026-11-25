using DenOfIz;

namespace NiziKit.UI.Widgets;

public sealed class Tab(string title, Widget page)
{
    public string Title { get; set; } = title;

    public string? Icon { get; set; }

    public Widget Page { get; } = page;
}

public class TabView : Widget
{
    private readonly List<Tab> _tabs = new();
    private readonly List<uint> _ids = new();
    private uint _stripId;
    private uint _pagesId;
    private int _selectedIndex;

    public TabView()
    {
        Desc.Layout.Direction = UIDirection.Column;
    }

    public IReadOnlyList<Tab> Tabs => _tabs;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = _tabs.Count == 0 ? 0 : Math.Clamp(value, 0, _tabs.Count - 1);
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            SelectionChanged?.Invoke(this);
        }
    }

    public Widget? SelectedPage => _selectedIndex < _tabs.Count ? _tabs[_selectedIndex].Page : null;

    public event Action<TabView>? SelectionChanged;

    public Tab Add(string title, Widget page, string? icon = null)
    {
        var tab = new Tab(title, page) { Icon = icon };
        var index = (uint)_tabs.Count;
        page.Parent = this;
        _tabs.Add(tab);
        _ids.Add(Ui.Id(Id, index * 2 + 1));
        _ids.Add(Ui.Id(Id, index * 2 + 2));
        return tab;
    }

    public override T? Find<T>(string name) where T : class
    {
        if (base.Find<T>(name) is { } self)
        {
            return self;
        }

        foreach (var tab in _tabs)
        {
            if (tab.Page.Find<T>(name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        switch (key.KeyCode)
        {
            case KeyCode.Left:
                SelectedIndex = _selectedIndex - 1;
                return true;
            case KeyCode.Right:
                SelectedIndex = _selectedIndex + 1;
                return true;
            default:
                return false;
        }
    }

    protected override void OnBuild()
    {
        if (_stripId == 0)
        {
            _stripId = Ui.Id(Id, 0x5791);
            _pagesId = Ui.Id(Id, 0x9A6E);
        }

        var theme = Ui.Theme;
        var strip = Ui.Interaction(_stripId);
        TrackFocus(in strip);
        var focusRing = strip.Focused && Ui.FocusVisible;
        using (OpenSelf())
        {
            using (Ui.Row(_stripId)
                       .Gap(2)
                       .Padding(8, 0)
                       .Shrink(0)
                       .Focusable()
                       .Border(theme.Border, new UIEdges(0, 0, 0, 1))
                       .Open())
            {
                for (var i = 0; i < _tabs.Count; i++)
                {
                    var tab = _tabs[i];
                    var id = _ids[i * 2];
                    var interaction = Ui.Interaction(id);
                    if (interaction.Clicked)
                    {
                        SelectedIndex = i;
                    }

                    var selected = i == _selectedIndex;
                    var color = selected ? theme.Accent : interaction.Hovered ? theme.Text : theme.TextMuted;
                    using (Ui.Row(id)
                               .AlignItems(UIAlign.Center)
                               .Gap(8)
                               .Padding(14, 10, 14, 8)
                               .Margin(0, 0, 0, -1)
                               .Background(interaction.Hovered && !selected ? theme.Hover : default)
                               .Border(selected ? focusRing ? theme.FocusRing : theme.Accent : default, new UIEdges(0, 0, 0, 2))
                               .CornerRadius(new UICorners(theme.CornerRadius, theme.CornerRadius, 0, 0))
                               .Cursor(SystemCursor.Pointer)
                               .Animate()
                               .Open())
                    {
                        if (tab.Icon != null && FontAwesome.IsInitialized)
                        {
                            Ui.Text(tab.Icon, theme.FontSize, color, FontAwesome.FontId);
                        }

                        Ui.Text(tab.Title, theme.FontSize, color);
                    }
                }
            }

            using (Ui.Column(_pagesId).Fill().Open())
            {
                for (var i = 0; i < _tabs.Count; i++)
                {
                    using (Ui.Column(_ids[i * 2 + 1]).Fill().Hidden(i != _selectedIndex).Open())
                    {
                        _tabs[i].Page.Build();
                    }
                }
            }
        }
    }
}
