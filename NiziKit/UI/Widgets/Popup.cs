using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

internal static class OverlayMotion
{
    internal static readonly Vector2 SlideOffset = new(0, -8);
    internal static readonly UiColor Transparent = UiColor.Rgba(0, 0, 0, 0);

    internal const float ExitDuration = 0.15f;

    internal static void Slide(Widget widget)
    {
        widget.SlideIn(SlideOffset);
    }

    internal static void Fade(Widget widget)
    {
        widget.FadeIn(UiTheme.Background);
    }

    internal static void FadeBackground(Widget widget)
    {
        widget.Animate(0.2f);
        widget.EnterFrom(new ClayTransitionStateDesc().WithBackgroundColor(Transparent.ToClay()));
    }
}

public class Popup : StackPanel
{
    private static readonly List<Popup> OpenPopups = new();

    private bool _armed;

    public Popup() : base(UiOrientation.Vertical)
    {
        Background = UiTheme.SurfaceRaised;
        BorderColor = UiTheme.Border;
        CornerRadius = UiTheme.CornerRadius;
        Padding = 6;
        OverlayMotion.Slide(this);
    }

    public bool IsOpen { get; private set; }
    public bool CloseOnClickOutside { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
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

    public void Open(UiFloating placement)
    {
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
        Open(UiFloating.Below(anchor));
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
        BeginExit(OverlayMotion.ExitDuration, OverlayMotion.SlideOffset, null, () => Ui.Overlays.Remove(this));
        OpenPopups.Remove(this);
        Ui.UnhandledKeyDown -= HandleKey;
        OnClosed();
        Closed?.Invoke(this);
    }

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
        return Ui.WasPressed(UiMouseButton.Left) || Ui.WasPressed(UiMouseButton.Right) || Ui.WasPressed(UiMouseButton.Middle);
    }

    private void HandleKey(KeyboardEventData key)
    {
        if (key.KeyCode == KeyCode.Escape && CloseOnEscape && Topmost == this)
        {
            Close();
        }
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

        if (Floating is { AttachTo: UiAttachTo.Element } anchored && (anchored.Anchor == null || !anchored.Anchor.BuiltLastFrame || !anchored.Anchor.IsVisible))
        {
            Close();
            return;
        }

        if (CloseOnClickOutside && AnyButtonPressed() && !ContainsPointer() && !PointerOverOwner())
        {
            Close();
        }
    }
}
