using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

public class Desktop : Panel
{
    private readonly List<FloatingWindow> _windows = new();
    private readonly List<FloatingWindow> _drawList = new();

    public Desktop() : base(UIDirection.Column)
    {
        Clip = true;
    }

    public IReadOnlyList<FloatingWindow> Windows
    {
        get => _windows;
        init
        {
            foreach (var window in value)
            {
                Add(window);
            }
        }
    }

    public Vector2 Area { get; private set; }

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

    public int OpenCount
    {
        get
        {
            var count = 0;
            foreach (var window in _windows)
            {
                count += window.IsOpen ? 1 : 0;
            }

            return count;
        }
    }

    public void Add(FloatingWindow window)
    {
        if (_windows.Contains(window))
        {
            return;
        }

        if (window.Parent != null && window.Parent != this)
        {
            throw new InvalidOperationException($"{window} already belongs to {window.Parent}.");
        }

        window.Parent = this;
        _windows.Add(window);
    }

    public void Remove(FloatingWindow window)
    {
        if (_windows.Remove(window))
        {
            window.Parent = null;
        }
    }

    public void BringToFront(FloatingWindow window)
    {
        if (_windows.Count > 0 && ReferenceEquals(_windows[^1], window))
        {
            return;
        }

        if (_windows.Remove(window))
        {
            _windows.Add(window);
        }
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
            window.Rect = new Rectangle(position, window.Rect.Size);
            position += new Vector2(30, 30);
        }
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        if (Ui.TryGetRect(Id, out var rect))
        {
            Area = new Vector2(rect.Width, rect.Height);
        }
    }

    private protected override void AfterChildren()
    {
        var active = Active;
        _drawList.Clear();
        _drawList.AddRange(_windows);
        foreach (var window in _drawList)
        {
            if (!window.IsOpen)
            {
                continue;
            }

            window.IsActive = ReferenceEquals(window, active);
            window.BuildFrame();
        }
    }
}

public class FloatingWindow : Widget
{
    private const float TitleHeight = 36;
    private const float GripSize = 16;
    private const float AttachDistance = 24;
    private const float KeepVisible = 90;

    private readonly uint _titleId;
    private readonly uint _bodyId;
    private readonly uint _gripId;
    private readonly Button _collapse;
    private readonly Button _close;
    private Rectangle _rect = new(40, 40, 420, 360);
    private Vector2 _attachArea;
    private bool _attachLeft;
    private bool _attachRight;
    private bool _attachTop;
    private bool _attachBottom;
    private bool _parked;
    private Rectangle _shown;
    private Rectangle _pressRect;
    private bool _moving;
    private bool _resizing;
    private UIInteraction _title;
    private UIInteraction _grip;

    public FloatingWindow(string title, Widget content)
    {
        Title = title;
        Content = content;
        content.Parent = this;
        _titleId = Ui.Id(Id, 1);
        _bodyId = Ui.Id(Id, 2);
        _gripId = Ui.Id(Id, 3);
        _collapse = new Button { Tone = Tone.Ghost, Small = true, OnClick = () => Collapsed = !Collapsed };
        _collapse.Parent = this;
        _close = new Button { Icon = FontAwesome.Xmark, Tone = Tone.Ghost, Small = true, Tooltip = "Close", OnClick = Close };
        _close.Parent = this;
    }

    public string Title { get; set; }
    public string? Icon { get; set; }
    public Color Accent { get; set; }
    public Widget Content { get; }
    public Vector2 MinSize { get; set; } = new(240, 160);
    public bool IsOpen { get; set; } = true;
    public bool Collapsed { get; set; }
    public bool Resizable { get; set; } = true;
    public bool Closable { get; set; } = true;
    public Action? OnClose { get; set; }

    public Rectangle Rect
    {
        get => _rect;
        set
        {
            _rect = value;
            _attachArea = default;
            _parked = false;
        }
    }

    public Rectangle ShownRect => _shown;

    internal bool IsActive { get; set; }

    internal override bool Interactive => true;

    private Desktop? Desktop => Parent as Desktop;

    public void Show()
    {
        IsOpen = true;
        Collapsed = false;
        Desktop?.BringToFront(this);
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Show();
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        OnClose?.Invoke();
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        _title = Ui.Interaction(_titleId);
        _grip = Ui.Interaction(_gripId);
        if (State.PressStarted)
        {
            Desktop?.BringToFront(this);
        }

        var area = Desktop?.Area ?? Vector2.Zero;
        var onButton = _collapse.IsHovered || _close.IsHovered;
        if (_title.PressStarted)
        {
            _moving = !onButton;
            _pressRect = _shown;
        }

        if (_moving && _title.DragStarted)
        {
            _rect = _shown;
        }

        if (_moving && _title.Dragging)
        {
            _rect = new Rectangle(_pressRect.Position + _title.DragDelta, _rect.Size);
            _parked = !Inside(_rect, area);
        }

        if (_moving && !_title.Pressed)
        {
            _moving = false;
            Attach(area);
        }

        if (_title.DoubleClicked && !onButton)
        {
            Collapsed = !Collapsed;
        }

        if (_grip.PressStarted)
        {
            _resizing = true;
            _pressRect = _shown;
            _rect = _shown;
        }

        if (_resizing && _grip.Dragging)
        {
            var max = area.X > 0 ? area - _rect.Position : new Vector2(float.MaxValue);
            var size = Vector2.Clamp(_pressRect.Size + _grip.DragDelta, MinSize, Vector2.Max(MinSize, max));
            _rect = new Rectangle(_rect.Position, size);
            _parked = !Inside(_rect, area);
        }

        if (_resizing && !_grip.Pressed)
        {
            _resizing = false;
            Attach(area);
        }
    }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var area = Desktop?.Area ?? Vector2.Zero;
        if (_attachArea == default && area.X > 0)
        {
            Attach(area);
        }

        _shown = Fit(area);
        var desc = Desc();
        ref var layout = ref desc.Layout;
        layout.Direction = UIDirection.Column;
        layout.Position = UIPosition.Absolute;
        layout.Insets = new UIInsets(_shown.X, _shown.Y, UIValue.Auto, UIValue.Auto);
        layout.Width = _shown.Width;
        layout.Height = Collapsed ? UIValue.Auto : _shown.Height;
        layout.Shrink = 0;
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

        Ui.Open(in desc);
        using (Element.Row(_titleId)
                   .Height(TitleHeight)
                   .Padding(12, 0, 6, 0)
                   .Gap(8)
                   .AlignItems(UIAlign.Center)
                   .Background(IsActive ? theme.SurfaceAlt : theme.Surface)
                   .Border(theme.Border, new UIEdges(0, 0, 0, Collapsed ? 0 : 1))
                   .Cursor(_moving ? SystemCursor.Move : SystemCursor.Default)
                   .Animate(UITransitionPropertyFlagBits.BackgroundColor)
                   .Open())
        {
            if (Accent.IsSet)
            {
                Element.Box().Size(4, 16).CornerRadius(2).Background(Accent.Resolve(theme)).Leaf();
            }

            var titleColor = IsActive ? theme.Text : theme.TextMuted;
            if (Icon != null)
            {
                Ui.Glyph(Icon, 13, titleColor);
            }

            Ui.Line(Title, 14, titleColor);
            using (Element.Row().Margin(left: UILength.Auto).Gap(2).Open())
            {
                Ui.PushLayout(true, false);
                _collapse.BuildFrame();
                if (Closable)
                {
                    _close.BuildFrame();
                }

                Ui.PopLayout();
            }
        }

        if (!Collapsed)
        {
            using (Element.Column(_bodyId).Fill().Open())
            {
                Ui.PushLayout(false, false);
                Content.BuildFrame();
                Ui.PopLayout();
            }

            if (Resizable)
            {
                using (Element.Row(_gripId)
                           .Absolute(right: 0, bottom: 0)
                           .Size(GripSize, GripSize)
                           .Padding(0, 0, 3, 3)
                           .Justify(UIJustify.End)
                           .AlignItems(UIAlign.End)
                           .Pointer(UIPointerMode.Block)
                           .Cursor(SystemCursor.NwseResize)
                           .Open())
                {
                    Element.Box().Size(7, 7)
                        .Border(_grip.Hovered || _resizing ? theme.Accent : theme.BorderStrong, new UIEdges(0, 0, 2, 2))
                        .CornerRadius(new UICorners(0, 0, 2, 0))
                        .Leaf();
                }
            }
        }

        Ui.Close();
    }

    private void Attach(Vector2 area)
    {
        if (area.X <= 0 || area.Y <= 0)
        {
            return;
        }

        _attachArea = area;
        _attachLeft = _rect.X <= AttachDistance;
        _attachTop = _rect.Y <= AttachDistance;
        _attachRight = area.X - _rect.Right <= AttachDistance;
        _attachBottom = area.Y - _rect.Bottom <= AttachDistance;
    }

    private Rectangle Fit(Vector2 area)
    {
        if (area.X <= 0 || area.Y <= 0)
        {
            return _rect;
        }

        var position = _rect.Position;
        var size = _rect.Size;
        if (_attachArea.X > 0)
        {
            var delta = area - _attachArea;
            if (_attachRight)
            {
                if (_attachLeft)
                {
                    size.X += delta.X;
                }
                else
                {
                    position.X += delta.X;
                }
            }

            if (_attachBottom && !Collapsed)
            {
                if (_attachTop)
                {
                    size.Y += delta.Y;
                }
                else
                {
                    position.Y += delta.Y;
                }
            }
        }

        size = Vector2.Max(MinSize, size);
        if (_parked)
        {
            position = new Vector2(
                Math.Clamp(position.X, KeepVisible - size.X, Math.Max(0, area.X - KeepVisible)),
                Math.Clamp(position.Y, 0, Math.Max(0, area.Y - TitleHeight)));
            return new Rectangle(position, size);
        }

        size = Vector2.Clamp(size, MinSize, Vector2.Max(MinSize, area));
        var footprint = Collapsed ? new Vector2(size.X, TitleHeight) : size;
        position = Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, area - footprint));
        return new Rectangle(position, size);
    }

    private static bool Inside(Rectangle rect, Vector2 area)
    {
        return area.X <= 0 || (rect.X >= 0 && rect.Y >= 0 && rect.Right <= area.X + 0.5f && rect.Bottom <= area.Y + 0.5f);
    }
}
