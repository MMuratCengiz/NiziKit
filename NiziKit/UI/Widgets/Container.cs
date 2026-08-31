using System.Collections;
using System.Numerics;
using System.Collections.ObjectModel;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public sealed class WidgetCollection(Widget owner) : Collection<Widget>
{
    protected override void InsertItem(int index, Widget item)
    {
        item.Parent = owner;
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Widget item)
    {
        this[index].Parent = null;
        item.Parent = owner;
        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        this[index].Parent = null;
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        foreach (var child in this)
        {
            child.Parent = null;
        }

        base.ClearItems();
    }
}

public abstract class Container : Widget, IEnumerable<Widget>
{
    protected Container()
    {
        Children = new WidgetCollection(this);
    }

    public WidgetCollection Children { get; }
    public Orientation Orientation { get; init; } = Orientation.Horizontal;
    public float Gap { get; set; }
    public Align AlignX { get; init; } = Align.Start;
    public Align AlignY { get; set; } = Align.Start;
    public bool ScrollVertical { get; set; }
    public bool ScrollHorizontal { get; set; }
    public bool ShowScrollbar { get; set; } = true;
    public float ScrollbarWidth { get; set; } = 6;
 
    public Style ScrollbarStyle { get; set; } = new()
    {
        Normal = new StyleState { Background = Color.Rgb(70, 76, 94) },
        Hover = new StyleState { Background = Color.Rgb(160, 165, 180) }
    };

    private ScrollThumb? _thumb;

    public ClayScrollContainerData ScrollData => Ui.Clay.GetScrollContainerData(Id);

    public void ScrollTo(float offsetPoints)
    {
        var data = ScrollData;
        var max = MathF.Max(0, data.ContentSize.Height - data.ContainerSize.Height);
        var y = Math.Clamp(Ui.Clay.PointsToPixels(offsetPoints), 0, max);
        Ui.Clay.SetScrollPosition(Id, new Vector2(data.ScrollPosition.X, -y));
    }

    public void ScrollToEnd()
    {
        var data = ScrollData;
        var max = MathF.Max(0, data.ContentSize.Height - data.ContainerSize.Height);
        Ui.Clay.SetScrollPosition(Id, new Vector2(data.ScrollPosition.X, -max));
    }

    public virtual void Add(Widget widget)
    {
        Children.Add(widget);
    }

    public Container Add(params Widget[] widgets)
    {
        foreach (var widget in widgets)
        {
            Add(widget);
        }

        return this;
    }

    public virtual bool Remove(Widget widget)
    {
        return Children.Remove(widget);
    }

    public virtual void Clear()
    {
        Children.Clear();
    }

    public override T? Find<T>(string name) where T : class
    {
        if (Name == name && this is T self)
        {
            return self;
        }

        foreach (var child in Children)
        {
            var found = child.Find<T>(name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    protected override void CollectChildren(List<Widget> frame)
    {
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.CollectFrame(frame);
            if (i < Children.Count && !ReferenceEquals(Children[i], child))
            {
                i--;
            }
        }

        if (ScrollVertical && ShowScrollbar)
        {
            _thumb ??= new ScrollThumb(this) { Parent = this };
            _thumb.CollectFrame(frame);
        }
    }

    protected void BuildScrollbar()
    {
        if (ScrollVertical && ShowScrollbar)
        {
            _thumb?.Build();
        }
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        decl.Layout.LayoutDirection = Orientation == Orientation.Vertical ? ClayLayoutDirection.TopToBottom : ClayLayoutDirection.LeftToRight;
        decl.Layout.ChildGap = (ushort)Gap;
        decl.Layout.ChildAlignment.X = AlignX.ToClayX();
        decl.Layout.ChildAlignment.Y = AlignY.ToClayY();
        decl.Scroll.Vertical = ScrollVertical;
        decl.Scroll.Horizontal = ScrollHorizontal;
        if (ScrollVertical || ScrollHorizontal)
        {
            decl.Clip = ClayClipDesc.Create(ScrollHorizontal, ScrollVertical);
        }
    }

    protected override void BuildContent()
    {
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Build();
        }

        BuildScrollbar();
    }

    public IEnumerator<Widget> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal sealed class ScrollThumb : Widget
{
    private readonly Container _owner;
    private readonly Floating _floating = new()
    {
        AttachTo = AttachTo.Parent,
        ElementAnchor = AnchorPoint.TopRight,
        ParentAnchor = AnchorPoint.TopRight,
        CapturePointer = true
    };

    private float _dragStartScroll;
    private float _ratio;

    public ScrollThumb(Container owner)
    {
        _owner = owner;
        Floating = _floating;
        Draggable = true;
        CornerRadius = 3;
        Visible = false;
        DragStarted += (_, _) => _dragStartScroll = -_owner.ScrollData.ScrollPosition.Y;
        Dragging += (_, e) =>
        {
            var data = _owner.ScrollData;
            var max = MathF.Max(0, data.ContentSize.Height - data.ContainerSize.Height);
            var y = Math.Clamp(_dragStartScroll + e.TotalDelta.Y * _ratio, 0, max);
            Ui.Clay.SetScrollPosition(_owner.Id, new Vector2(data.ScrollPosition.X, -y));
        };
    }

    protected override bool TracksPointer => true;

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        var data = _owner.ScrollData;
        var container = data.ContainerSize.Height;
        var content = data.ContentSize.Height;
        var overflow = content - container;
        Visible = data.Found && overflow > 1 && container > 0;
        const float margin = 2f;
        var trackPoints = Ui.Clay.PixelsToPoints(container) - margin * 2;
        var thumbPoints = MathF.Min(MathF.Max(24, trackPoints * container / MathF.Max(content, 1)), trackPoints);
        var travel = MathF.Max(0, trackPoints - thumbPoints);
        var t = overflow > 0 ? Math.Clamp(-data.ScrollPosition.Y / overflow, 0, 1) : 0;
        _ratio = travel > 0 ? overflow / Ui.Clay.PointsToPixels(travel) : 0;

        Width = _owner.ScrollbarWidth;
        Height = thumbPoints;
        _floating.Offset = new Vector2(-margin, margin + travel * t);
        Style = _owner.ScrollbarStyle;
        base.ApplyDeclaration(ref decl);
        if (IsDragging)
        {
            _owner.ScrollbarStyle.Hover.Apply(ref decl);
        }
    }
}

public class StackPanel : Container
{
    public StackPanel()
    {
    }

    public StackPanel(Orientation orientation)
    {
        Orientation = orientation;
    }
}

public class HStack : StackPanel
{
    public HStack() : base(Orientation.Horizontal)
    {
        AlignY = Align.Center;
    }
}

public class VStack() : StackPanel(Orientation.Vertical);

public class ScrollView : StackPanel
{
    public ScrollView() : base(Orientation.Vertical)
    {
        ScrollVertical = true;
        ClipChildren = true;
        Width = Sizing.Grow;
        Height = Sizing.Grow;
    }
}

public class Spacer : Widget
{
    public Spacer()
    {
        Width = Sizing.Grow;
        Height = Sizing.Grow;
    }

    public Spacer(float size)
    {
        Width = size;
        Height = size;
    }
}

public class Divider : Widget
{
    public Divider()
    {
        Width = Sizing.Grow;
        Height = 1;
        Background = Color.Rgb(70, 76, 94);
    }
}
