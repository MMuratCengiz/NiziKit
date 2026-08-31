using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public sealed class MenuItem : Widget
{
    private readonly Menu _menu;

    internal MenuItem(Menu menu, string text)
    {
        _menu = menu;
        Text = text;
        Width = UiSize.Grow;
        Height = menu.ItemHeight;
        Padding = new UiThickness(8, 0);
        CornerRadius = 4;
    }

    public string Text { get; set; }
    public string? Shortcut { get; init; }
    public bool IsCheckable { get; init; }
    public bool Checked { get; set; }
    public Menu? Submenu { get; init; }
    public Action? Action { get; init; }
    public Action<bool>? CheckChanged { get; init; }

    protected override bool TracksPointer => true;

    internal void OpenSubmenu()
    {
        if (Submenu == null || Submenu.IsOpen)
        {
            return;
        }

        Submenu.Owner = this;
        Submenu.Open(UiFloating.RightOf(this, 2));
    }

    internal void CloseSubmenu()
    {
        Submenu?.Close();
    }

    protected override void OnClick(UiMouseButton button)
    {
        if (button != UiMouseButton.Left || !IsEnabled)
        {
            return;
        }

        if (Submenu != null)
        {
            OpenSubmenu();
            return;
        }

        if (IsCheckable)
        {
            Checked = !Checked;
            CheckChanged?.Invoke(Checked);
        }

        Action?.Invoke();
        _menu.Dismiss();
    }

    protected override void OnPoll()
    {
        if (IsHovered && IsEnabled && Submenu != null)
        {
            OpenSubmenu();
        }
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        decl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        decl.Layout.ChildGap = 16;
        decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        var highlighted = IsEnabled && (IsHovered || Submenu is { IsOpen: true });
        if (highlighted)
        {
            decl.BackgroundColor = _menu.HighlightBackground.ToClay();
        }
    }

    protected override void BuildContent()
    {
        var textColor = (IsEnabled ? _menu.TextColor : _menu.DisabledTextColor).ToClay();
        var mutedColor = (IsEnabled ? _menu.ShortcutColor : _menu.DisabledTextColor).ToClay();
        var fontSize = (ushort)_menu.FontSize;

        if (_menu.HasCheckItems)
        {
            var box = ClayElementDeclaration.Default();
            box.Layout.Sizing.Width = ClaySizingAxis.Fixed(12);
            box.Layout.Sizing.Height = ClaySizingAxis.Fixed(12);
            box.BorderRadius = ClayBorderRadius.CreateUniform(3);
            if (IsCheckable)
            {
                box.BackgroundColor = (Checked ? _menu.CheckColor : _menu.CheckBoxBackground).ToClay();
                box.Border.Color = (Checked ? _menu.CheckColor : _menu.CheckBoxBorder).ToClay();
                box.Border.Width = ClayBorderWidth.CreateUniform(1);
            }

            Ui.Clay.OpenElement(in box);
            Ui.Clay.CloseElement();
        }

        var desc = ClayTextDesc.Default();
        desc.TextColor = textColor;
        desc.FontId = _menu.FontId;
        desc.FontSize = fontSize;
        desc.WrapMode = ClayTextWrapMode.None;
        Ui.Clay.Text(Text, in desc);

        var spacer = ClayElementDeclaration.Default();
        spacer.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        Ui.Clay.OpenElement(in spacer);
        Ui.Clay.CloseElement();

        if (Shortcut != null)
        {
            desc.TextColor = mutedColor;
            Ui.Clay.Text(Shortcut, in desc);
        }

        if (Submenu != null)
        {
            desc.TextColor = mutedColor;
            desc.FontId = FontAwesome.FontId;
            Ui.Clay.Text(FontAwesome.CaretRight, in desc);
        }
    }
}

public class Menu : Popup
{
    public Menu()
    {
        Padding = 4;
        Gap = 1;
        Width = UiSize.Fit.WithMin(160);
    }

    public int FontSize { get; set; } = 14;
    public ushort FontId { get; set; } = UiFonts.DefaultFontId;
    public float ItemHeight { get; set; } = 26;
    public UiColor TextColor { get; set; } = UiColor.Rgb(235, 235, 240);
    public UiColor DisabledTextColor { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor ShortcutColor { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor HighlightBackground { get; set; } = UiColor.Rgb(110, 150, 250);
    public UiColor CheckColor { get; set; } = UiColor.Rgb(88, 130, 240);
    public UiColor CheckBoxBackground { get; set; } = UiColor.Rgb(28, 30, 38);
    public UiColor CheckBoxBorder { get; set; } = UiColor.Rgb(70, 76, 94);
    public bool HasCheckItems { get; private set; }

    public MenuItem AddItem(string text, Action onClick, string? shortcut = null, bool enabled = true)
    {
        var item = new MenuItem(this, text) { Action = onClick, Shortcut = shortcut, Enabled = enabled };
        Children.Add(item);
        return item;
    }

    public MenuItem AddCheckItem(string text, bool isChecked, Action<bool> onChanged, bool enabled = true)
    {
        var item = new MenuItem(this, text) { IsCheckable = true, Checked = isChecked, CheckChanged = onChanged, Enabled = enabled };
        HasCheckItems = true;
        Children.Add(item);
        return item;
    }

    public MenuItem AddSubmenu(string text, Menu submenu, bool enabled = true)
    {
        var item = new MenuItem(this, text) { Submenu = submenu, Enabled = enabled };
        submenu.Owner = item;
        Children.Add(item);
        return item;
    }

    public void AddSeparator()
    {
        Children.Add(new Divider { Margin = new UiThickness(4, 3) });
    }

    public override void Clear()
    {
        base.Clear();
        HasCheckItems = false;
    }

    public void ShowAt(Vector2 pixelPosition)
    {
        Open(UiFloating.AtRoot(new Vector2(Ui.Clay.PixelsToPoints(pixelPosition.X), Ui.Clay.PixelsToPoints(pixelPosition.Y))));
    }

    public void ShowFor(Widget anchor)
    {
        Owner = anchor;
        Open(anchor);
    }

    private void ClampToViewport()
    {
        if (Floating is not { AttachTo: UiAttachTo.Root } placement)
        {
            return;
        }

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var viewport = Ui.Clay.GetViewportSize();
        var offset = placement.Offset;
        var overflowX = bounds.X + bounds.Width - viewport.Width;
        if (overflowX > 0)
        {
            offset.X -= Ui.Clay.PixelsToPoints(overflowX);
        }

        var overflowY = bounds.Y + bounds.Height - viewport.Height;
        if (overflowY > 0)
        {
            offset.Y -= Ui.Clay.PixelsToPoints(overflowY);
        }

        placement.Offset = new Vector2(MathF.Max(0, offset.X), MathF.Max(0, offset.Y));
    }

    protected override void OnPoll()
    {
        base.OnPoll();
        if (!IsOpen)
        {
            return;
        }

        ClampToViewport();

        MenuItem? hovered = null;
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i] is MenuItem { IsHovered: true } item)
            {
                hovered = item;
                break;
            }
        }

        if (hovered == null)
        {
            return;
        }

        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i] is MenuItem item && item != hovered && item.Submenu is { IsOpen: true })
            {
                item.CloseSubmenu();
            }
        }
    }
}

public static class ContextMenu
{
    public static void Attach(Widget target, Func<Menu> build)
    {
        Menu? current = null;
        target.RightClicked += _ =>
        {
            current?.Close();
            current = build();
            current.ShowAt(Ui.PointerPosition);
        };
    }

    public static void Attach(Widget target, Menu menu)
    {
        target.RightClicked += _ => menu.ShowAt(Ui.PointerPosition);
    }
}
