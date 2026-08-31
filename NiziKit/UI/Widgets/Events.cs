using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public readonly struct PointerEvent(MouseButton button, Vector2 position, int clicks)
{
    public readonly MouseButton Button = button;
    public readonly Vector2 Position = position;
    public readonly int Clicks = clicks;
}

public readonly struct DragEvent(MouseButton button, Vector2 start, Vector2 position, Vector2 delta)
{
    public readonly MouseButton Button = button;
    public readonly Vector2 Start = start;
    public readonly Vector2 Position = position;
    public readonly Vector2 Delta = delta;
    public Vector2 TotalDelta => Position - Start;
}

public readonly struct DropEvent(Widget source, object? payload, Vector2 position)
{
    public readonly Widget Source = source;
    public readonly object? Payload = payload;
    public readonly Vector2 Position = position;
}

public enum AttachTo
{
    Parent,
    Root,
    Element
}

public enum AnchorPoint
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

public sealed class Floating
{
    public AttachTo AttachTo { get; init; } = AttachTo.Parent;
    public Widget? Anchor { get; init; }
    public Vector2 Offset { get; set; }
    public Vector2 Expand { get; set; }
    public float ZIndex { get; set; }
    public AnchorPoint ElementAnchor { get; init; } = AnchorPoint.TopLeft;
    public AnchorPoint ParentAnchor { get; init; } = AnchorPoint.TopLeft;
    public bool CapturePointer { get; set; } = true;
    public bool ClipToParent { get; set; }

    public static Floating AtRoot(Vector2 offset, float zIndex = 0)
    {
        return new Floating { AttachTo = AttachTo.Root, Offset = offset, ZIndex = zIndex };
    }

    public static Floating Below(Widget anchor, float gap = 4)
    {
        return new Floating
        {
            AttachTo = AttachTo.Element,
            Anchor = anchor,
            ElementAnchor = AnchorPoint.TopLeft,
            ParentAnchor = AnchorPoint.BottomLeft,
            Offset = new Vector2(0, gap)
        };
    }

    public static Floating Above(Widget anchor, float gap = 4)
    {
        return new Floating
        {
            AttachTo = AttachTo.Element,
            Anchor = anchor,
            ElementAnchor = AnchorPoint.BottomLeft,
            ParentAnchor = AnchorPoint.TopLeft,
            Offset = new Vector2(0, -gap)
        };
    }

    public static Floating RightOf(Widget anchor, float gap = 4)
    {
        return new Floating
        {
            AttachTo = AttachTo.Element,
            Anchor = anchor,
            ElementAnchor = AnchorPoint.TopLeft,
            ParentAnchor = AnchorPoint.TopRight,
            Offset = new Vector2(gap, 0)
        };
    }

    public static Floating Centered(float zIndex = 0)
    {
        return new Floating
        {
            AttachTo = AttachTo.Root,
            ElementAnchor = AnchorPoint.Center,
            ParentAnchor = AnchorPoint.Center,
            ZIndex = zIndex
        };
    }

    internal void Apply(ref ClayElementDeclaration decl)
    {
        decl.Floating.AttachTo = AttachTo switch
        {
            AttachTo.Root => ClayFloatingAttachTo.Root,
            AttachTo.Element => ClayFloatingAttachTo.ElementWithId,
            _ => ClayFloatingAttachTo.Parent
        };
        decl.Floating.ParentId = AttachTo == AttachTo.Element && Anchor != null ? Anchor.Id : 0;
        decl.Floating.Offset = Offset;
        decl.Floating.Expand = new ClayDimensions { Width = Expand.X, Height = Expand.Y };
        decl.Floating.ZIndex = ZIndex;
        decl.Floating.ElementAttachPoint = ToClay(ElementAnchor);
        decl.Floating.ParentAttachPoint = ToClay(ParentAnchor);
        decl.Floating.PointerCaptureMode = CapturePointer ? ClayPointerCaptureMode.Capture : ClayPointerCaptureMode.Passthrough;
        decl.Floating.ClipTo = ClipToParent ? ClayFloatingClipTo.AttachedParent : ClayFloatingClipTo.None;
    }

    private static ClayFloatingAttachPoint ToClay(AnchorPoint anchor)
    {
        return anchor switch
        {
            AnchorPoint.TopCenter => ClayFloatingAttachPoint.CenterTop,
            AnchorPoint.TopRight => ClayFloatingAttachPoint.RightTop,
            AnchorPoint.CenterLeft => ClayFloatingAttachPoint.LeftCenter,
            AnchorPoint.Center => ClayFloatingAttachPoint.CenterCenter,
            AnchorPoint.CenterRight => ClayFloatingAttachPoint.RightCenter,
            AnchorPoint.BottomLeft => ClayFloatingAttachPoint.LeftBottom,
            AnchorPoint.BottomCenter => ClayFloatingAttachPoint.CenterBottom,
            AnchorPoint.BottomRight => ClayFloatingAttachPoint.RightBottom,
            _ => ClayFloatingAttachPoint.LeftTop
        };
    }
}
