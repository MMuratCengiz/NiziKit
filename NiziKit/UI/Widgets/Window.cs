using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.UI.Widgets;

public class Window : VStack
{
    private const float MinVisible = 40;

    private readonly UiFloating _floating = new() { AttachTo = UiAttachTo.Root, CapturePointer = true };
    private readonly WindowTitleBar _titleBar;
    private readonly Label _title;
    private readonly WindowButton _collapseButton;
    private readonly WindowButton _closeButton;
    private readonly List<WindowResizeEdge> _edges = new();
    private bool _resizing;
    private Vector2 _size = new(320, 240);
    private Vector2 _minSize = new(120, 60);
    private bool _collapsed;
    private bool _resizable = true;

    public Window()
    {
        Width = _size.X;
        Height = _size.Y;
        Background = UiTheme.Surface;
        BorderColor = UiTheme.Border;
        CornerRadius = 8;
        ClipChildren = true;
        Floating = _floating;

        _title = new Label { Wrap = false };
        _collapseButton = new WindowButton(FontAwesome.Minus, false);
        _collapseButton.Clicked += _ => IsCollapsed = !IsCollapsed;
        _closeButton = new WindowButton(FontAwesome.Xmark, true);
        _closeButton.Clicked += _ => Close();

        _titleBar = new WindowTitleBar(this);
        _titleBar.Children.Add(_title);
        _titleBar.Children.Add(new Spacer());
        _titleBar.Children.Add(_collapseButton);
        _titleBar.Children.Add(_closeButton);

        Content = new VStack
        {
            Width = UiSize.Grow,
            Height = UiSize.Grow,
            Padding = 10,
            Gap = 8,
            ScrollVertical = true,
            ClipChildren = true
        };
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx != 0 || dy != 0)
                {
                    _edges.Add(new WindowResizeEdge(this, dx, dy));
                }
            }
        }

        Children.Add(_titleBar);
        Children.Add(Content);
        foreach (var edge in _edges)
        {
            Children.Add(edge);
        }

        Pressed += (_, _) => BringToFront();
    }

    public Window(string title) : this()
    {
        Title = title;
    }

    public VStack Content { get; }

    public string Title
    {
        get => _title.Text;
        init => _title.Text = value;
    }

    public Vector2 Position
    {
        get => _floating.Offset;
        set
        {
            if (_floating.Offset == value)
            {
                return;
            }

            _floating.Offset = value;
            Moved?.Invoke(this);
        }
    }

    public Vector2 Size
    {
        get => _size;
        set
        {
            var clamped = Vector2.Max(value, _minSize);
            if (clamped == _size)
            {
                return;
            }

            _size = clamped;
            Width = _size.X;
            if (!_collapsed)
            {
                Height = _size.Y;
            }

            Resized?.Invoke(this);
        }
    }

    public Vector2 MinSize
    {
        get => _minSize;
        set
        {
            _minSize = value;
            Size = _size;
        }
    }

    public bool Resizable
    {
        get => _resizable;
        set
        {
            _resizable = value;
            UpdateGrip();
        }
    }

    public bool Movable
    {
        get => _titleBar.Draggable;
        set => _titleBar.Draggable = value;
    }

    public bool Closable
    {
        get => _closeButton.Visible;
        set => _closeButton.Visible = value;
    }

    public bool Collapsible
    {
        get => _collapseButton.Visible;
        set => _collapseButton.Visible = value;
    }

    public bool ShowTitleBar
    {
        get => _titleBar.Visible;
        set => _titleBar.Visible = value;
    }

    public float TitleBarHeight
    {
        get => _titleBar.Height.Value;
        set => _titleBar.Height = value;
    }

    public bool IsCollapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed == value)
            {
                return;
            }

            _collapsed = value;
            Content.Visible = !value;
            Height = value ? UiSize.Fit : _size.Y;
            _collapseButton.Icon = value ? FontAwesome.Plus : FontAwesome.Minus;
            UpdateGrip();
        }
    }

    public bool IsOpen { get; private set; }
    public bool IsActive => ActiveWindow == this;
    public float ZIndex => _floating.ZIndex;

    public event Action<Window>? Closed;
    public event Action<Window>? Moved;
    public event Action<Window>? Resized;
    public event Action<Window>? Activated;

    protected override bool TracksPointer => true;

    public override void Add(Widget widget)
    {
        Content.Add(widget);
    }

    public void Show()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        CancelExit();
        SlideIn(new Vector2(0, -24), 0.25f);
        if (Transition is { } transition)
        {
            transition.PropertyFlags = ClayTransitionPropertyFlagBits.Dimensions | ClayTransitionPropertyFlagBits.BackgroundColor |
                                       ClayTransitionPropertyFlagBits.OverlayColor;
            transition.InteractionHandling = ClayTransitionInteractionHandling.AllowWhileMoving;
            Transition = transition;
        }

        Ui.Overlays.Add(this);
        Register(this);
        BringToFront();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Unregister(this);
        BeginExit(0.2f, new Vector2(0, -24), null, () => Ui.Overlays.Remove(this));
        Closed?.Invoke(this);
    }

    public void BringToFront()
    {
        if (!IsOpen || ActiveWindow == this)
        {
            return;
        }

        Raise();
        SetActive(this);
        Activated?.Invoke(this);
    }

    private void UpdateGrip()
    {
        foreach (var edge in _edges)
        {
            edge.Visible = _resizable && !_collapsed;
        }
    }

    private static Vector2 ToPoints(Vector2 pixels)
    {
        return new Vector2(Ui.Clay.PixelsToPoints(pixels.X), Ui.Clay.PixelsToPoints(pixels.Y));
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        var viewportWidth = Ui.Clay.PixelsToPoints(GraphicsContext.Width);
        var viewportHeight = Ui.Clay.PixelsToPoints(GraphicsContext.Height);
        var minX = MinVisible - _size.X;
        var maxX = MathF.Max(minX, viewportWidth - MinVisible);
        var maxY = MathF.Max(0, viewportHeight - MinVisible);
        return new Vector2(Math.Clamp(position.X, minX, maxX), Math.Clamp(position.Y, 0, maxY));
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        if (_resizing)
        {
            decl.Transition.Properties &= ~(uint)ClayTransitionPropertyFlagBits.BoundingBox;
        }
        _title.Color = IsActive ? UiTheme.Text : UiTheme.TextMuted;
    }

    private static readonly List<Window> OpenWindows = new();

    public static IReadOnlyList<Window> Windows => OpenWindows;
    public static Window? ActiveWindow { get; private set; }
    public static Vector2 CascadeOrigin { get; set; } = new(40, 40);
    public static float CascadeStep { get; set; } = 32;
    public static float TileGap { get; set; } = 8;

    public static void Open(Window window)
    {
        window.Show();
    }

    public static void CloseAll()
    {
        for (var i = OpenWindows.Count - 1; i >= 0; i--)
        {
            OpenWindows[i].Close();
        }
    }

    public static void BringToFront(Window window)
    {
        window.BringToFront();
    }

    public static void Cascade()
    {
        for (var i = 0; i < OpenWindows.Count; i++)
        {
            var window = OpenWindows[i];
            window.IsCollapsed = false;
            window.Position = CascadeOrigin + new Vector2(CascadeStep * i, CascadeStep * i);
            window.BringToFront();
        }
    }

    public static void Tile()
    {
        var count = OpenWindows.Count;
        if (count == 0)
        {
            return;
        }

        var columns = (int)MathF.Ceiling(MathF.Sqrt(count));
        var rows = (count + columns - 1) / columns;
        var viewportWidth = Ui.Clay.PixelsToPoints(GraphicsContext.Width);
        var viewportHeight = Ui.Clay.PixelsToPoints(GraphicsContext.Height);
        var cellWidth = (viewportWidth - TileGap * (columns + 1)) / columns;
        var cellHeight = (viewportHeight - TileGap * (rows + 1)) / rows;

        for (var i = 0; i < count; i++)
        {
            var window = OpenWindows[i];
            var column = i % columns;
            var row = i / columns;
            window.IsCollapsed = false;
            window.Position = new Vector2(TileGap + column * (cellWidth + TileGap), TileGap + row * (cellHeight + TileGap));
            window.Size = new Vector2(cellWidth, cellHeight);
        }
    }

    internal static void Register(Window window)
    {
        if (!OpenWindows.Contains(window))
        {
            OpenWindows.Add(window);
        }
    }

    internal static void Unregister(Window window)
    {
        OpenWindows.Remove(window);
        if (ActiveWindow != window)
        {
            return;
        }

        ActiveWindow = null;
        Window? top = null;
        for (var i = 0; i < OpenWindows.Count; i++)
        {
            var candidate = OpenWindows[i];
            if (top == null || candidate.ZIndex > top.ZIndex)
            {
                top = candidate;
            }
        }

        top?.BringToFront();
    }

    internal static void SetActive(Window window)
    {
        ActiveWindow = window;
    }

    private sealed class WindowTitleBar : HStack
    {
        private readonly Window _owner;
        private Vector2 _startPosition;

        public WindowTitleBar(Window owner)
        {
            _owner = owner;
            Width = UiSize.Grow;
            Height = 30;
            Padding = new UiThickness(10, 4, 0, 0);
            Gap = 2;
            Background = UiTheme.SurfaceRaised;
            Draggable = true;
            Pressed += (_, _) => owner.BringToFront();
            DragStarted += OnDragStarted;
            Dragging += OnDragging;
        }

        protected override bool TracksPointer => true;

        private void OnDragStarted(Widget sender, UiDragEvent e)
        {
            _startPosition = _owner.Position;
        }

        private void OnDragging(Widget sender, UiDragEvent e)
        {
            _owner.Position = _owner.ClampPosition(_startPosition + ToPoints(e.TotalDelta));
        }

        protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
        {
            base.ApplyDeclaration(ref decl);
            var radius = _owner.CornerRadius;
            var bottom = _owner.IsCollapsed || !_owner.Content.IsVisible ? radius : 0;
            decl.BorderRadius = ClayBorderRadius.Create(radius, radius, bottom, bottom);
        }
    }

    private sealed class WindowButton : Button
    {
        public WindowButton(string glyph, bool danger)
        {
            Icon = glyph;
            Width = 26;
            Height = 22;
            Padding = new UiThickness(0, 0);
            CornerRadius = 4;
            Focusable = false;
            FontSize = 20;
            NormalBackground = UiColor.Rgba(0, 0, 0, 0);
            if (danger)
            {
                HoverBackground = UiTheme.Danger;
                PressedBackground = UiTheme.Danger.Darken(0.2f);
            }
        }
    }

    private sealed class WindowResizeEdge : Widget
    {
        private const float Thickness = 6;
        private const float CornerSize = 14;

        private readonly Window _owner;
        private readonly UiFloating _edgeFloating;
        private readonly int _dx;
        private readonly int _dy;
        private readonly SystemCursor _cursor;
        private readonly bool _corner;
        private Vector2 _startSize;
        private Vector2 _startPosition;

        public WindowResizeEdge(Window owner, int dx, int dy)
        {
            _owner = owner;
            _dx = dx;
            _dy = dy;
            var corner = dx != 0 && dy != 0;
            _corner = corner;
            var main = corner ? CornerSize : Thickness;
            Width = dx == 0 ? UiSize.Grow : main;
            Height = dy == 0 ? UiSize.Grow : main;
            _cursor = corner
                ? dx == dy ? SystemCursor.NwseResize : SystemCursor.NeswResize
                : dx != 0 ? SystemCursor.EwResize : SystemCursor.NsResize;
            var anchor = AnchorFor(dx, dy);
            _edgeFloating = new UiFloating
            {
                AttachTo = UiAttachTo.Parent,
                ElementAnchor = anchor,
                ParentAnchor = anchor,
                Offset = new Vector2(dx * (main * 0.5f + (corner ? 3 : 1)), dy * (main * 0.5f + (corner ? 3 : 1))),
                CapturePointer = true
            };
            Floating = _edgeFloating;
            Draggable = true;
            Pressed += (_, _) => owner.BringToFront();
            PointerEntered += _ => InputSystem.SetCursor(_cursor);
            PointerExited += _ =>
            {
                if (!IsDragging)
                {
                    InputSystem.ResetCursor();
                }
            };
            DragStarted += OnDragStarted;
            Dragging += OnDragging;
            DragEnded += OnDragEnded;
        }

        protected override bool TracksPointer => true;

        private static UiAnchor AnchorFor(int dx, int dy)
        {
            return (dx, dy) switch
            {
                (-1, -1) => UiAnchor.TopLeft,
                (0, -1) => UiAnchor.TopCenter,
                (1, -1) => UiAnchor.TopRight,
                (-1, 0) => UiAnchor.CenterLeft,
                (1, 0) => UiAnchor.CenterRight,
                (-1, 1) => UiAnchor.BottomLeft,
                (0, 1) => UiAnchor.BottomCenter,
                _ => UiAnchor.BottomRight
            };
        }

        private void OnDragStarted(Widget sender, UiDragEvent e)
        {
            _startSize = _owner.Size;
            _startPosition = _owner.Position;
            _owner._resizing = true;
        }

        private void OnDragging(Widget sender, UiDragEvent e)
        {
            var delta = ToPoints(e.TotalDelta);
            _owner.Size = new Vector2(_startSize.X + _dx * delta.X, _startSize.Y + _dy * delta.Y);
            var applied = _owner.Size;
            var position = _startPosition;
            if (_dx < 0)
            {
                position.X = _startPosition.X + (_startSize.X - applied.X);
            }

            if (_dy < 0)
            {
                position.Y = _startPosition.Y + (_startSize.Y - applied.Y);
            }

            _owner.Position = position;
        }

        private void OnDragEnded(Widget sender, UiDragEvent e)
        {
            _owner._resizing = false;
            if (!IsHovered)
            {
                InputSystem.ResetCursor();
            }
        }

        protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
        {
            _edgeFloating.ZIndex = _owner._floating.ZIndex + (_corner ? 0.6f : 0.5f);
            base.ApplyDeclaration(ref decl);
        }
    }
}
