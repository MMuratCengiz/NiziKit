using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

/// <summary>
/// Floating panel. While open it lives in <see cref="Ui.Overlays"/>, owns a z-index above everything
/// declared before it, clamps itself into the viewport and dismisses on click-outside or Escape.
/// Anchor it to a widget (<see cref="Open(Widget)"/>), to a screen position (<see cref="ShowAt"/>)
/// or to the viewport centre (<see cref="ShowCentered"/>); give it a <see cref="Backdrop"/> to make
/// it modal. Menus, tooltips, dropdown lists and dialogs are all this type plus content.
/// </summary>
public class Popup : StackPanel
{
    internal static readonly Color Transparent = Color.Rgba(0, 0, 0, 0);
    internal static readonly Vector2 SlideOffset = new(0, -8);
    internal const float ExitDuration = 0.15f;

    private static readonly List<Popup> OpenPopups = new();

    private PopupBackdrop? _backdrop;
    private bool _armed;

    public Popup() : base(Orientation.Vertical)
    {
        Background = Color.Rgb(44, 48, 60);
        BorderColor = Color.Rgb(70, 76, 94);
        CornerRadius = 6;
        Padding = 6;
        SlideIn(SlideOffset);
    }

    public bool IsOpen { get; private set; }
    public bool CloseOnClickOutside { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Keeps the panel inside the viewport when it is anchored to a screen position.</summary>
    public bool ClampToViewport { get; set; } = true;

    /// <summary>
    /// False lets the pointer fall through to whatever is underneath, so the panel neither blocks
    /// clicks nor counts as hovered. Set it for pointer-following overlays such as tooltips.
    /// </summary>
    public bool CapturePointer { get; set; } = true;

    /// <summary>
    /// Fills the viewport behind the panel while it is open, blocking input to everything below.
    /// Null (the default) leaves the rest of the UI live.
    /// </summary>
    public Color? Backdrop { get; set; }

    /// <summary>
    /// Widget this panel belongs to. Popups form a chain through their owners: closing one closes
    /// everything it owns, and a click inside a child counts as a click inside its parent.
    /// </summary>
    public Widget? Owner { get; set; }

    public event Action<Popup>? Opened;
    public event Action<Popup>? Closed;

    public static Popup? Topmost => OpenPopups.Count > 0 ? OpenPopups[^1] : null;
    public static bool AnyOpen => OpenPopups.Count > 0;

    public static void CloseAll()
    {
        for (var i = OpenPopups.Count - 1; i >= 0; i--)
        {
            if (i < OpenPopups.Count)
            {
                OpenPopups[i].Close();
            }
        }
    }

    public void Open(Floating placement)
    {
        placement.CapturePointer = CapturePointer;
        if (Backdrop is { } backdrop)
        {
            _backdrop ??= new PopupBackdrop(this);
            _backdrop.CancelExit();
            _backdrop.Background = backdrop;
            _backdrop.Floating = new Floating { AttachTo = AttachTo.Root, ZIndex = Ui.NextZIndex() };
            if (_backdrop.Parent != Ui.Overlays)
            {
                Ui.Overlays.Add(_backdrop);
            }
        }

        placement.ZIndex = Ui.NextZIndex();
        Floating = placement;
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        _armed = false;
        CancelExit();
        Visible = true;
        if (Parent != Ui.Overlays)
        {
            Ui.Overlays.Add(this);
        }

        OpenPopups.Add(this);
        Ui.UnhandledKeyDown += HandleKey;
        OnOpened();
        Opened?.Invoke(this);
    }

    public void Open(Widget anchor)
    {
        Open(Floating.Below(anchor));
    }

    /// <summary>Opens the panel at a position in pixels, such as <see cref="Ui.PointerPosition"/>.</summary>
    public void ShowAt(Vector2 pixelPosition)
    {
        Open(Floating.AtRoot(new Vector2(Ui.Clay.PixelsToPoints(pixelPosition.X), Ui.Clay.PixelsToPoints(pixelPosition.Y))));
    }

    public void ShowCentered()
    {
        Open(Floating.Centered());
    }

    public void Toggle(Widget anchor)
    {
        Owner = anchor;
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open(anchor);
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        CloseOwned();
        BeginExit(ExitDuration, SlideOffset, null, () => Ui.Overlays.Remove(this));
        if (_backdrop != null)
        {
            _backdrop.Background = _backdrop.Background?.WithAlpha(0);
            _backdrop.BeginExit(ExitDuration, Vector2.Zero, null, () => Ui.Overlays.Remove(_backdrop));
        }

        OpenPopups.Remove(this);
        Ui.UnhandledKeyDown -= HandleKey;
        OnClosed();
        Closed?.Invoke(this);
    }

    /// <summary>Closes the root of this panel's owner chain, tearing down the whole stack at once.</summary>
    public void Dismiss()
    {
        var root = this;
        var current = Owner;
        while (current != null)
        {
            if (current is Popup popup)
            {
                root = popup;
                current = popup.Owner;
            }
            else
            {
                current = current.Parent;
            }
        }

        root.Close();
    }

    public bool IsOwnedBy(Widget widget)
    {
        var current = Owner;
        while (current != null)
        {
            if (current == widget)
            {
                return true;
            }

            current = current is Popup popup ? popup.Owner : current.Parent;
        }

        return false;
    }

    public bool ContainsPointer()
    {
        if (Ui.Clay.PointerOver(Id) || FloatingDescendantHit(this))
        {
            return true;
        }

        for (var i = 0; i < OpenPopups.Count; i++)
        {
            var other = OpenPopups[i];
            if (other != this && other.IsOwnedBy(this) && other.ContainsPointer())
            {
                return true;
            }
        }

        return false;
    }

    private static bool FloatingDescendantHit(Container container)
    {
        var children = container.Children;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child.Floating != null && Ui.Clay.PointerOver(child.Id))
            {
                return true;
            }

            if (child is Container nested && FloatingDescendantHit(nested))
            {
                return true;
            }
        }

        return false;
    }

    private void CloseOwned()
    {
        for (var i = OpenPopups.Count - 1; i >= 0; i--)
        {
            if (i >= OpenPopups.Count)
            {
                continue;
            }

            var other = OpenPopups[i];
            if (other != this && other.IsOwnedBy(this))
            {
                other.Close();
            }
        }
    }

    private bool PointerOverOwner()
    {
        return Owner != null && Ui.Clay.PointerOver(Owner.Id);
    }

    private static bool AnyButtonPressed()
    {
        return Ui.WasPressed(MouseButton.Left) || Ui.WasPressed(MouseButton.Right) || Ui.WasPressed(MouseButton.Middle);
    }

    private void HandleKey(KeyboardEventData key)
    {
        if (key.KeyCode == KeyCode.Escape && CloseOnEscape && Topmost == this)
        {
            Close();
        }
    }

    private void ClampIntoViewport()
    {
        if (Floating is not { AttachTo: AttachTo.Root } placement || !ClampToViewport)
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

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnClosed()
    {
    }

    protected override void OnPoll()
    {
        if (!IsOpen)
        {
            return;
        }

        if (!_armed)
        {
            _armed = true;
            return;
        }

        if (Floating is { AttachTo: AttachTo.Element } anchored && (anchored.Anchor == null || !anchored.Anchor.BuiltLastFrame || !anchored.Anchor.IsVisible))
        {
            Close();
            return;
        }

        ClampIntoViewport();

        if (CloseOnClickOutside && AnyButtonPressed() && !ContainsPointer() && !PointerOverOwner())
        {
            Close();
        }
    }

    private sealed class PopupBackdrop : Widget
    {
        private readonly Popup _owner;

        public PopupBackdrop(Popup owner)
        {
            _owner = owner;
            Width = Sizing.Grow;
            Height = Sizing.Grow;
            Animate(0.2f);
            EnterFrom(new ClayTransitionStateDesc().WithBackgroundColor(Transparent.ToClay()));
        }

        protected override bool TracksPointer => true;

        protected override void OnClick(MouseButton button)
        {
            if (button == MouseButton.Left && _owner.CloseOnClickOutside && Topmost == _owner)
            {
                _owner.Close();
            }
        }
    }
}
