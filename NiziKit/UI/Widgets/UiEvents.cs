using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public enum UiMouseButton
{
    Left = 0,
    Middle = 1,
    Right = 2
}

public readonly struct UiPointerEvent(UiMouseButton button, Vector2 position, int clicks)
{
    public readonly UiMouseButton Button = button;
    public readonly Vector2 Position = position;
    public readonly int Clicks = clicks;
}

public readonly struct UiDragEvent(UiMouseButton button, Vector2 start, Vector2 position, Vector2 delta)
{
    public readonly UiMouseButton Button = button;
    public readonly Vector2 Start = start;
    public readonly Vector2 Position = position;
    public readonly Vector2 Delta = delta;
    public Vector2 TotalDelta => Position - Start;
}

public readonly struct UiDropEvent(Widget source, object? payload, Vector2 position)
{
    public readonly Widget Source = source;
    public readonly object? Payload = payload;
    public readonly Vector2 Position = position;
}

public enum UiAttachTo
{
    Parent,
    Root,
    Element
}

public enum UiAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public sealed class UiFloating
{
    public UiAttachTo AttachTo { get; init; } = UiAttachTo.Parent;
    public Widget? Anchor { get; init; }
    public Vector2 Offset { get; set; }
    public Vector2 Expand { get; set; }
    public float ZIndex { get; set; }
    public UiAnchor ElementAnchor { get; init; } = UiAnchor.TopLeft;
    public UiAnchor ParentAnchor { get; init; } = UiAnchor.TopLeft;
    public bool CapturePointer { get; set; } = true;
    public bool ClipToParent { get; set; }

    public static UiFloating AtRoot(Vector2 offset, float zIndex = 0)
    {
        return new UiFloating { AttachTo = UiAttachTo.Root, Offset = offset, ZIndex = zIndex };
    }

    public static UiFloating Below(Widget anchor, float gap = 4)
    {
        return new UiFloating
        {
            AttachTo = UiAttachTo.Element,
            Anchor = anchor,
            ElementAnchor = UiAnchor.TopLeft,
            ParentAnchor = UiAnchor.BottomLeft,
            Offset = new Vector2(0, gap)
        };
    }

    public static UiFloating Above(Widget anchor, float gap = 4)
    {
        return new UiFloating
        {
            AttachTo = UiAttachTo.Element,
            Anchor = anchor,
            ElementAnchor = UiAnchor.BottomLeft,
            ParentAnchor = UiAnchor.TopLeft,
            Offset = new Vector2(0, -gap)
        };
    }

    public static UiFloating RightOf(Widget anchor, float gap = 4)
    {
        return new UiFloating
        {
            AttachTo = UiAttachTo.Element,
            Anchor = anchor,
            ElementAnchor = UiAnchor.TopLeft,
            ParentAnchor = UiAnchor.TopRight,
            Offset = new Vector2(gap, 0)
        };
    }

    public static UiFloating Centered(float zIndex = 0)
    {
        return new UiFloating
        {
            AttachTo = UiAttachTo.Root,
            ElementAnchor = UiAnchor.Center,
            ParentAnchor = UiAnchor.Center,
            ZIndex = zIndex
        };
    }

    internal void Apply(ref ClayElementDeclaration decl)
    {
        decl.Floating.AttachTo = AttachTo switch
        {
            UiAttachTo.Root => ClayFloatingAttachTo.Root,
            UiAttachTo.Element => ClayFloatingAttachTo.ElementWithId,
            _ => ClayFloatingAttachTo.Parent
        };
        decl.Floating.ParentId = AttachTo == UiAttachTo.Element && Anchor != null ? Anchor.Id : 0;
        decl.Floating.Offset = Offset;
        decl.Floating.Expand = new ClayDimensions { Width = Expand.X, Height = Expand.Y };
        decl.Floating.ZIndex = ZIndex;
        decl.Floating.ElementAttachPoint = ToClay(ElementAnchor);
        decl.Floating.ParentAttachPoint = ToClay(ParentAnchor);
        decl.Floating.PointerCaptureMode = CapturePointer ? ClayPointerCaptureMode.Capture : ClayPointerCaptureMode.Passthrough;
        decl.Floating.ClipTo = ClipToParent ? ClayFloatingClipTo.AttachedParent : ClayFloatingClipTo.None;
    }

    private static ClayFloatingAttachPoint ToClay(UiAnchor anchor)
    {
        return anchor switch
        {
            UiAnchor.TopCenter => ClayFloatingAttachPoint.CenterTop,
            UiAnchor.TopRight => ClayFloatingAttachPoint.RightTop,
            UiAnchor.CenterLeft => ClayFloatingAttachPoint.LeftCenter,
            UiAnchor.Center => ClayFloatingAttachPoint.CenterCenter,
            UiAnchor.CenterRight => ClayFloatingAttachPoint.RightCenter,
            UiAnchor.BottomLeft => ClayFloatingAttachPoint.LeftBottom,
            UiAnchor.BottomCenter => ClayFloatingAttachPoint.CenterBottom,
            UiAnchor.BottomRight => ClayFloatingAttachPoint.RightBottom,
            _ => ClayFloatingAttachPoint.LeftTop
        };
    }
}
