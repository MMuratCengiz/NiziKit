using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

public class Popup : Panel
{
    private enum Placement
    {
        Anchored,
        Point,
        Centered
    }

    private Placement _placement;
    private uint _targetId;
    private UIAnchor _targetAnchor;
    private UIAnchor _anchor;
    private Vector2 _offset;
    private uint _backdropId;

    public Popup() : base(UIDirection.Column)
    {
        Padding = 6;
        Gap = 2;
        Clip = true;
    }

    public bool IsOpen { get; private set; }
    public bool Modal { get; set; }
    public bool CloseOnOutsideClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public Action? OnOpen { get; set; }
    public Action? OnClose { get; set; }

    internal Popup? ParentPopup { get; private set; }
    internal int ZIndex { get; set; }

    protected internal override void Describe(UiWidgetInfo info)
    {
        info.Set("open", IsOpen);
        if (Modal)
        {
            info.Set("modal", true);
        }
    }

    internal override bool Interactive => true;

    public void Show(Widget anchor, UIAnchor targetAnchor = UIAnchor.BottomLeft, UIAnchor anchorPoint = UIAnchor.TopLeft, Vector2 offset = default)
    {
        Open(Placement.Anchored, anchor.Id, targetAnchor, anchorPoint, offset == default ? new Vector2(0, 4) : offset, EnclosingPopup(anchor));
    }

    public void ShowAt(Vector2 point)
    {
        Open(Placement.Point, 0, UIAnchor.TopLeft, UIAnchor.TopLeft, point, null);
    }

    public void ShowCentered()
    {
        Open(Placement.Centered, 0, UIAnchor.Center, UIAnchor.Center, Vector2.Zero, null);
    }

    public void Toggle(Widget anchor)
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Show(anchor);
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        var popups = Ui.OpenPopups;
        for (var i = popups.Count - 1; i >= 0; i--)
        {
            if (i < popups.Count && popups[i].ParentPopup == this)
            {
                popups[i].Close();
            }
        }

        popups.Remove(this);
        ParentPopup = null;
        Closed();
        OnClose?.Invoke();
    }

    private protected virtual void Opened()
    {
    }

    private protected virtual void Closed()
    {
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        if (IsOpen && CloseOnOutsideClick && State.PressedOutside)
        {
            Close();
        }
    }

    internal override void Render()
    {
        if (!IsOpen)
        {
            return;
        }

        if (_placement == Placement.Anchored && !Ui.TryGetRect(_targetId, out _))
        {
            Close();
            return;
        }

        var theme = Ui.Theme;
        var desc = PanelDesc();
        desc.Style.BackgroundColor = theme.Surface;
        desc.Style.BorderColor = theme.Border;
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius + 2);
        desc.Layout.Shrink = 0;

        var inBackdrop = Modal && _placement == Placement.Centered;
        desc.Floating = default;
        if (!inBackdrop)
        {
            desc.Floating.Target = _placement == Placement.Anchored ? UIFloatingTarget.Element : UIFloatingTarget.Viewport;
            desc.Floating.TargetId = _targetId;
            desc.Floating.TargetAnchor = _targetAnchor;
            desc.Floating.Anchor = _anchor;
            desc.Floating.Offset = _offset;
            desc.Floating.ZIndex = Modal ? 1 : ZIndex;
            desc.Floating.Flags = (uint)UIFloatingFlagBits.KeepInViewport;
        }

        desc.Transition.Duration = 0.14f;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.None;
        desc.Transition.Flags = (uint)(UITransitionFlagBits.EnterWithParent | UITransitionFlagBits.ExitWithParent);
        desc.Transition.Enter = _placement == Placement.Anchored ? Motion.SlideDown : UITransitionState.FromOpacity(0).WithScale(0.96f).WithOffset(new Vector2(0, 12));
        desc.Transition.Exit = _placement == Placement.Anchored ? Motion.Fade : Motion.Pop;

        var previous = Ui.BuildingPopup;
        Ui.BuildingPopup = this;
        if (Modal)
        {
            OpenBackdrop(theme.Backdrop);
        }

        Ui.Open(in desc);
        Ui.PushLayout(false, false);
        BuildChildren();
        Ui.DeclarePopups(this);
        Ui.PopLayout();
        Ui.Close();
        if (Modal)
        {
            Ui.Close();
        }

        Ui.BuildingPopup = previous;
    }

    private void OpenBackdrop(UIColor color)
    {
        if (_backdropId == 0)
        {
            _backdropId = Ui.Id(Id, 0xBAC0);
        }

        var backdrop = Element.Column(_backdropId)
            .Size(UIValue.Percent(1), UIValue.Percent(1))
            .Center()
            .Background(color)
            .Pointer(UIPointerMode.Block)
            .FloatOnViewport()
            .ZIndex(ZIndex)
            .Animate(UITransitionPropertyFlagBits.None, 0.2f)
            .Enter(UITransitionState.FromOpacity(0))
            .Exit(UITransitionState.FromOpacity(0));
        Ui.Open(in backdrop.Desc);
    }

    private void Open(Placement placement, uint targetId, UIAnchor targetAnchor, UIAnchor anchor, Vector2 offset, Popup? layer)
    {
        _placement = placement;
        _targetId = targetId;
        _targetAnchor = targetAnchor;
        _anchor = anchor;
        _offset = offset;
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        ParentPopup = layer is { IsOpen: true } && layer != this ? layer : null;
        Ui.OpenPopups.Add(this);
        Opened();
        OnOpen?.Invoke();
    }

    private static Popup? EnclosingPopup(Widget widget)
    {
        for (var current = widget.Parent; current != null; current = current.Parent)
        {
            if (current is Popup popup)
            {
                return popup;
            }
        }

        return Ui.BuildingPopup;
    }
}

public class Menu : Popup
{
    private readonly List<MenuItem> _items = new();
    private MenuItem? _highlight;
    private MenuItem? _activate;

    public Menu()
    {
        MinWidth = 180;
        Padding = 4;
        Gap = 0;
    }

    public WidgetList Items
    {
        get => Children;
        set => Children = value;
    }

    internal static Menu? Building { get; private set; }

    internal MenuItem? Highlight
    {
        get => _highlight;
        set => _highlight = value;
    }

    internal bool TakeActivation(MenuItem item)
    {
        if (!ReferenceEquals(_activate, item))
        {
            return false;
        }

        _activate = null;
        return true;
    }

    private protected override void Opened()
    {
        _highlight = null;
        _activate = null;
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        switch (key.KeyCode)
        {
            case KeyCode.Down:
                Step(1);
                return true;
            case KeyCode.Up:
                Step(-1);
                return true;
            case KeyCode.Home:
                _highlight = null;
                Step(1);
                return true;
            case KeyCode.End:
                _highlight = null;
                Step(-1);
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

    private protected override void BuildChildren()
    {
        var previous = Building;
        Building = this;
        _items.Clear();
        base.BuildChildren();
        Building = previous;
    }

    internal void Register(MenuItem item)
    {
        _items.Add(item);
    }

    private void Step(int direction)
    {
        var count = _items.Count;
        if (count == 0)
        {
            return;
        }

        var start = _highlight != null ? _items.IndexOf(_highlight) : direction > 0 ? -1 : count;
        for (var i = start + direction; i >= 0 && i < count; i += direction)
        {
            if (_items[i].EnabledNow)
            {
                _highlight = _items[i];
                return;
            }
        }
    }
}

public class MenuItem : Widget
{
    private Menu? _menu;

    public MenuItem(string text, string? icon = null, string? shortcut = null)
    {
        Text = text;
        Icon = icon;
        Shortcut = shortcut;
    }

    public MenuItem(TextSource text, string? icon = null, string? shortcut = null)
    {
        Text = new TextValue(text);
        Icon = icon;
        Shortcut = shortcut;
    }

    public TextValue Text { get; set; }
    public string? Icon { get; set; }
    public string? Shortcut { get; set; }
    public Prop<bool> Checked { get; set; }

    protected internal override void Describe(UiWidgetInfo info)
    {
        info.Text = Text.ToString();
        if (Checked.IsBound || Checked.Value)
        {
            info.Value = Checked.Value;
        }

        if (Shortcut != null)
        {
            info.Set("shortcut", Shortcut);
        }
    }

    protected override bool Clickable => true;

    internal override void Dispatch()
    {
        if (_menu != null && (State.HoverStarted || (State.Hovered && _menu.Highlight == null)))
        {
            _menu.Highlight = this;
        }

        base.Dispatch();
        if (_menu != null && EnabledNow && _menu.TakeActivation(this))
        {
            OnActivate();
        }
    }

    protected override void OnActivate()
    {
        _menu?.Close();
        base.OnActivate();
    }

    internal override void Render()
    {
        _menu = Menu.Building;
        _menu?.Register(this);
        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var highlighted = enabled && _menu?.Highlight == this;
        var text = enabled ? theme.Text : theme.TextDisabled;
        var desc = Desc();
        desc.Layout.AlignItems = UIAlign.Center;
        desc.Layout.Gap = 10;
        desc.Layout.Padding = new Edges(10, 6).ToLength();
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius - 2);
        desc.Style.BackgroundColor = highlighted ? theme.AccentSoft : default;
        Ui.Open(in desc);
        OfferTooltip();
        if (FontAwesome.IsInitialized)
        {
            using (Element.Row().Width(16).Justify(UIJustify.Center).Open())
            {
                var glyph = Checked.Value ? FontAwesome.Check : Icon;
                if (glyph != null)
                {
                    Ui.Glyph(glyph, theme.FontSize, highlighted ? theme.Accent : theme.TextMuted);
                }
            }
        }

        Ui.Line(Text.Read(), 0, text);
        if (Shortcut != null)
        {
            using (Element.Row().Margin(left: UILength.Auto).Padding(16, 0, 0, 0).Open())
            {
                Ui.Line(Shortcut, theme.SmallFontSize, theme.TextMuted);
            }
        }

        Ui.Close();
    }
}

public sealed class MenuSeparator : Widget
{
    internal override void Render()
    {
        var desc = Desc();
        desc.Layout.Height = 1;
        desc.Layout.Margin = new Edges(0, 4).ToLength();
        desc.Style.BackgroundColor = Ui.Theme.Border;
        Ui.Open(in desc);
        Ui.Close();
    }
}

public class MenuButton : Button
{
    private static MenuButton? _open;

    public MenuButton(string text, Menu menu) : base(text)
    {
        Menu = menu;
        Tone = Tone.Ghost;
        menu.OnClose += () =>
        {
            if (_open == this)
            {
                _open = null;
            }
        };
    }

    public Menu Menu { get; }

    protected override void OnActivate()
    {
        if (Menu.IsOpen)
        {
            Menu.Close();
        }
        else
        {
            OpenMenu();
        }

        base.OnActivate();
    }

    internal override void Dispatch()
    {
        if (State.HoverStarted && _open != null && _open != this && _open.Parent == Parent && _open.Menu.IsOpen)
        {
            _open.Menu.Close();
            OpenMenu();
        }

        base.Dispatch();
    }

    internal override void Render()
    {
        var open = Menu.IsOpen;
        var checkedState = Checked;
        Checked = open ? true : null;
        base.Render();
        Checked = checkedState;
    }

    private void OpenMenu()
    {
        Menu.Show(this);
        _open = this;
    }
}

public class Dialog : Popup
{
    private Label? _title;

    public Dialog()
    {
        Modal = true;
        Width = 420;
        Padding = 20;
        Gap = 14;
    }

    public string? Title { get; set; }

    public void Show()
    {
        ShowCentered();
    }

    private protected override void BuildChildren()
    {
        if (Title != null)
        {
            _title ??= new Label("", TextStyle.Title);
            _title.Text = Title;
            _title.BuildFrame();
        }

        base.BuildChildren();
    }
}

public static class Dialogs
{
    public static Dialog Confirm(string title, string message, string confirm, Tone tone, Action onConfirm, string cancel = "Cancel")
    {
        var dialog = new Dialog { Title = title };
        var confirmButton = new Button(confirm) { Tone = tone };
        confirmButton.OnClick = () =>
        {
            dialog.Close();
            onConfirm();
        };
        dialog.Children =
        [
            new Paragraph(message, TextStyle.Muted),
            new Row
            {
                Justify = UIJustify.End,
                Gap = 8,
                Children = [new Button(cancel) { OnClick = dialog.Close }, confirmButton]
            }
        ];
        dialog.Show();
        confirmButton.Focus();
        return dialog;
    }
}
