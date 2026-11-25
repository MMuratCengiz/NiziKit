using System.Numerics;
using DenOfIz;
using NiziKit.UI;
using NiziKit.UI.Widgets;

namespace VttDemo.UiKit;

/// <summary>
/// A movable, resizable window built from plain NiziKit.UI parts: an absolutely positioned column with a title bar
/// (drag to move, double-click to collapse), a content widget and a resize grip. A <see cref="Desktop"/> owns the z-order.
/// </summary>
public class FloatingWindow : Widget
{
    private const float TitleHeight = 36;
    private const float GripSize = 16;

    private readonly uint _titleId;
    private readonly uint _bodyId;
    private readonly uint _gripId;
    private readonly Button _collapse;
    private readonly Button _close;
    private Vector2 _pressPosition;
    private Vector2 _pressSize;
    private bool _moving;
    private bool _resizing;

    public FloatingWindow(string title, Widget content)
    {
        Title = title;
        Content = content;
        content.Fill = true;
        _titleId = Ui.Id(Id, 1);
        _bodyId = Ui.Id(Id, 2);
        _gripId = Ui.Id(Id, 3);

        _collapse = TitleButton(FontAwesome.Minus, "Collapse");
        _collapse.Clicked += _ => Collapsed = !Collapsed;
        _close = TitleButton(FontAwesome.Xmark, "Close");
        _close.Clicked += _ => Close();
    }

    public string Title { get; set; }

    /// <summary>A <see cref="FontAwesome"/> glyph drawn before the title.</summary>
    public string? Icon { get; set; }

    /// <summary>A colored stripe before the title, e.g. the character's token color.</summary>
    public UIColor? Accent { get; set; }

    public Widget Content { get; }

    /// <summary>Top left in points, relative to the desktop area.</summary>
    public Vector2 Position { get; set; }

    public Vector2 Size { get; set; } = new(420, 360);
    public Vector2 MinSize { get; set; } = new(240, 160);
    public bool IsOpen { get; set; } = true;
    public bool Collapsed { get; set; }
    public bool Resizable { get; set; } = true;
    public bool Closable { get; set; } = true;

    public event Action<FloatingWindow>? Closed;

    internal Vector2 AreaSize { get; set; }
    internal bool IsActive { get; set; }

    /// <summary>Set while built when the window was pressed this frame; the desktop raises it.</summary>
    internal bool WantsFront { get; private set; }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Closed?.Invoke(this);
    }

    protected override void OnBuild()
    {
        var theme = Ui.Theme;
        WantsFront = Interaction.PressStarted;
        HandleMove();
        HandleResize();
        KeepInArea();

        ref var desc = ref Desc;
        desc.Layout.Direction = UIDirection.Column;
        desc.Layout.Position = UIPosition.Absolute;
        desc.Layout.Insets = new UIInsets(Position.X, Position.Y, UIValue.Auto, UIValue.Auto);
        desc.Layout.Width = Size.X;
        desc.Layout.Height = Collapsed ? UIValue.Auto : Size.Y;
        desc.Style.BackgroundColor = theme.Surface;
        desc.Style.BorderColor = IsActive ? theme.BorderStrong : theme.Border;
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.CornerRadius = UICorners.All(10);
        desc.Scroll.OverflowX = UIOverflow.Clip;
        desc.Scroll.OverflowY = UIOverflow.Clip;
        desc.Pointer = UIPointerMode.Block;
        desc.Transition.Duration = 0.16f;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.BorderColor;
        desc.Transition.Enter = UITransitionState.FromOpacity(0).WithScale(0.97f).WithOffset(new Vector2(0, 8));
        desc.Transition.Exit = UITransitionState.FromOpacity(0).WithScale(0.97f);

        _collapse.Icon = Collapsed ? FontAwesome.WindowRestore : FontAwesome.Minus;
        _collapse.Tooltip = Collapsed ? "Expand" : "Collapse";

        using (OpenSelf())
        {
            using (Ui.Row(_titleId)
                       .Height(TitleHeight)
                       .Shrink(0)
                       .Padding(12, 0, 6, 0)
                       .Gap(8)
                       .AlignItems(UIAlign.Center)
                       .Background(IsActive ? theme.SurfaceAlt : theme.Surface)
                       .Border(theme.Border, new UIEdges(0, 0, 0, Collapsed ? 0 : 1))
                       .Cursor(_moving ? SystemCursor.Move : SystemCursor.Default)
                       .Animate(UITransitionPropertyFlagBits.BackgroundColor)
                       .Open())
            {
                if (Accent is { } accent)
                {
                    Ui.Element().Size(4, 16).Shrink(0).CornerRadius(2).Background(accent).Leaf();
                }

                if (Icon != null)
                {
                    Im.Icon(Icon, 13, IsActive ? theme.Text : theme.TextMuted);
                }

                Im.Line(Title, 14, IsActive ? theme.Text : theme.TextMuted);
                using (Ui.Row().Margin(left: UILength.Auto).Gap(2).Shrink(0).Open())
                {
                    Ui.Draw(_collapse);
                    if (Closable)
                    {
                        Ui.Draw(_close);
                    }
                }
            }

            using (Ui.Column(_bodyId).Fill().Hidden(Collapsed).Open())
            {
                Ui.Draw(Content);
            }

            if (Resizable && !Collapsed)
            {
                var grip = Ui.Interaction(_gripId);
                using (Ui.Row(_gripId)
                           .Absolute(right: 0, bottom: 0)
                           .Size(GripSize, GripSize)
                           .Padding(0, 0, 3, 3)
                           .Justify(UIJustify.End)
                           .AlignItems(UIAlign.End)
                           .Pointer(UIPointerMode.Block)
                           .Cursor(SystemCursor.NwseResize)
                           .Open())
                {
                    Ui.Element().Size(7, 7)
                        .Border(grip.Hovered || _resizing ? theme.Accent : theme.BorderStrong, new UIEdges(0, 0, 2, 2))
                        .CornerRadius(new UICorners(0, 0, 2, 0))
                        .Leaf();
                }
            }
        }
    }

    private void HandleMove()
    {
        var title = Ui.Interaction(_titleId);
        var onButton = _collapse.IsHovered || _close.IsHovered;
        if (title.PressStarted)
        {
            _moving = !onButton;
            _pressPosition = Position;
        }

        if (_moving && title.Dragging)
        {
            Position = _pressPosition + title.DragDelta;
        }

        if (!title.Pressed)
        {
            _moving = false;
        }

        if (title.DoubleClicked && !onButton)
        {
            Collapsed = !Collapsed;
        }
    }

    private void HandleResize()
    {
        var grip = Ui.Interaction(_gripId);
        if (grip.PressStarted)
        {
            _resizing = true;
            _pressSize = Size;
        }

        if (_resizing && grip.Dragging)
        {
            var max = AreaSize.X > 0 ? AreaSize - Position : new Vector2(float.MaxValue);
            Size = Vector2.Clamp(_pressSize + grip.DragDelta, MinSize, Vector2.Max(MinSize, max));
        }

        if (!grip.Pressed)
        {
            _resizing = false;
        }
    }

    /// <summary>Keeps the title bar reachable when the window or the desktop area changes size.</summary>
    private void KeepInArea()
    {
        if (AreaSize.X <= 0 || AreaSize.Y <= 0)
        {
            return;
        }

        const float keepVisible = 90;
        var x = Math.Clamp(Position.X, keepVisible - Size.X, Math.Max(0, AreaSize.X - keepVisible));
        var y = Math.Clamp(Position.Y, 0, Math.Max(0, AreaSize.Y - TitleHeight));
        Position = new Vector2(x, y);
    }

    private static Button TitleButton(string icon, string tooltip)
    {
        return new Button
        {
            Icon = icon,
            Kind = ButtonKind.Ghost,
            Tooltip = tooltip,
            FontSize = 12,
            Padding = UILengthEdges.Symmetric(7, 5)
        };
    }
}

/// <summary>Owns the windows of a desktop area: draws them in z-order and raises the one pressed.</summary>
public sealed class Desktop
{
    private readonly List<FloatingWindow> _windows = new();
    private readonly List<FloatingWindow> _drawList = new();

    public IReadOnlyList<FloatingWindow> Windows => _windows;

    /// <summary>Size of the area the windows live in, from the last frame.</summary>
    public Vector2 AreaSize { get; set; }

    public FloatingWindow? Active
    {
        get
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                if (_windows[i].IsOpen)
                {
                    return _windows[i];
                }
            }

            return null;
        }
    }

    public int OpenCount => _windows.Count(w => w.IsOpen);

    public T Add<T>(T window) where T : FloatingWindow
    {
        _windows.Add(window);
        return window;
    }

    public void Show(FloatingWindow window)
    {
        window.IsOpen = true;
        window.Collapsed = false;
        BringToFront(window);
    }

    public void Toggle(FloatingWindow window)
    {
        if (window.IsOpen)
        {
            window.Close();
        }
        else
        {
            Show(window);
        }
    }

    public void BringToFront(FloatingWindow window)
    {
        if (_windows.Count > 0 && ReferenceEquals(_windows[^1], window))
        {
            return;
        }

        _windows.Remove(window);
        _windows.Add(window);
    }

    public void Cascade()
    {
        var position = new Vector2(24, 24);
        foreach (var window in _windows)
        {
            if (!window.IsOpen)
            {
                continue;
            }

            window.Collapsed = false;
            window.Position = position;
            position += new Vector2(30, 30);
        }
    }

    /// <summary>Declares the open windows, bottom to top, at the current position of the tree (inside the area element).</summary>
    public void Draw()
    {
        var active = Active;
        FloatingWindow? raise = null;
        _drawList.Clear();
        _drawList.AddRange(_windows);
        foreach (var window in _drawList)
        {
            if (!window.IsOpen)
            {
                continue;
            }

            window.AreaSize = AreaSize;
            window.IsActive = ReferenceEquals(window, active);
            Ui.Draw(window);
            if (window.WantsFront)
            {
                raise = window;
            }
        }

        if (raise != null)
        {
            BringToFront(raise);
        }
    }
}
