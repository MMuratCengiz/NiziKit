using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

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
        ref var layout = ref Desc.Layout;
        layout.Padding = UILengthEdges.All(6);
        layout.Gap = 2;
        Desc.Scroll.OverflowX = UIOverflow.Clip;
        Desc.Scroll.OverflowY = UIOverflow.Clip;
    }

    public bool IsOpen { get; private set; }

    public bool Modal { get; set; }

    public bool CloseOnOutsideClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;

    public event Action<Popup>? Opened;
    public event Action<Popup>? Closed;

    internal Popup? ParentPopup { get; private set; }

    internal int ZIndex { get; set; }

    public void Open(Widget anchor, UIAnchor targetAnchor = UIAnchor.BottomLeft, UIAnchor anchorPoint = UIAnchor.TopLeft, Vector2 offset = default)
    {
        Show(Placement.Anchored, anchor.Id, targetAnchor, anchorPoint, offset == default ? new Vector2(0, 4) : offset, anchor.EnclosingPopup);
    }

    public void Open(uint targetId, UIAnchor targetAnchor = UIAnchor.BottomLeft, UIAnchor anchorPoint = UIAnchor.TopLeft, Vector2 offset = default)
    {
        Show(Placement.Anchored, targetId, targetAnchor, anchorPoint, offset == default ? new Vector2(0, 4) : offset, Ui.BuildingPopup);
    }

    public void ShowAt(Vector2 point)
    {
        Show(Placement.Point, 0, UIAnchor.TopLeft, UIAnchor.TopLeft, point, Ui.BuildingPopup);
    }

    public void ShowCentered()
    {
        Show(Placement.Centered, 0, UIAnchor.Center, UIAnchor.Center, Vector2.Zero, Ui.BuildingPopup);
    }

    public void Toggle(Widget anchor)
    {
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
        OnClosed();
        Closed?.Invoke(this);
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnClosed()
    {
    }

    protected override void OnBuild()
    {
        if (!IsOpen)
        {
            return;
        }

        if ((CloseOnOutsideClick && Interaction.PressedOutside) || (_placement == Placement.Anchored && !Ui.TryGetRect(_targetId, out _)))
        {
            Close();
            return;
        }

        var theme = Ui.Theme;
        ref var desc = ref Desc;
        desc.Style.BackgroundColor = theme.Surface;
        desc.Style.BorderColor = theme.Border;
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius + 2);

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
        desc.Transition.Enter = _placement == Placement.Anchored
            ? UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, -6))
            : UITransitionState.FromOpacity(0).WithScale(0.96f).WithOffset(new Vector2(0, 12));
        desc.Transition.Exit = _placement == Placement.Anchored ? UITransitionState.FromOpacity(0) : UITransitionState.FromOpacity(0).WithScale(0.96f);

        var previous = Ui.BuildingPopup;
        Ui.BuildingPopup = this;
        if (Modal)
        {
            OpenBackdrop(theme.Backdrop);
        }

        using (OpenSelf())
        {
            BuildChildren();
            Ui.DeclarePopups(this);
        }

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

        Ui.Column(_backdropId)
            .Size(UIValue.Percent(1), UIValue.Percent(1))
            .Center()
            .Background(color)
            .Pointer(UIPointerMode.Block)
            .FloatOnViewport()
            .ZIndex(ZIndex)
            .Animate(UITransitionPropertyFlagBits.None, 0.2f)
            .Enter(UITransitionState.FromOpacity(0))
            .Exit(UITransitionState.FromOpacity(0))
            .Open();
    }

    private void Show(Placement placement, uint targetId, UIAnchor targetAnchor, UIAnchor anchor, Vector2 offset, Popup? layer)
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
        OnOpened();
        Opened?.Invoke(this);
    }
}
