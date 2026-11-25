using System.Collections;
using System.Collections.ObjectModel;
using System.Numerics;
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

public class Panel : Widget, IEnumerable<Widget>
{
    public Panel()
    {
        Children = new WidgetCollection(this);
    }

    public Panel(UIDirection direction) : this()
    {
        Desc.Layout.Direction = direction;
    }

    public WidgetCollection Children { get; }

    public UIDirection Direction
    {
        get => Desc.Layout.Direction;
        set => Layout.Direction = value;
    }

    public UILength Gap
    {
        get => Desc.Layout.Gap;
        set => Layout.Gap = value;
    }

    public UILength LineGap
    {
        get => Desc.Layout.LineGap;
        set => Layout.LineGap = value;
    }

    public UIJustify Justify
    {
        get => Desc.Layout.Justify;
        set => Layout.Justify = value;
    }

    public UIAlign AlignItems
    {
        get => Desc.Layout.AlignItems;
        set => Layout.AlignItems = value;
    }

    public UIAlignContent AlignContent
    {
        get => Desc.Layout.AlignContent;
        set => Layout.AlignContent = value;
    }

    public UIFlexWrap Wrap
    {
        get => Desc.Layout.Wrap;
        set => Layout.Wrap = value;
    }

    public ref UIStyleDesc Style => ref Desc.Style;

    public UIColor Background
    {
        get => Desc.Style.BackgroundColor;
        set => Desc.Style.BackgroundColor = value;
    }

    public UIColor BorderColor
    {
        get => Desc.Style.BorderColor;
        set => Desc.Style.BorderColor = value;
    }

    public float BorderWidth
    {
        get => Desc.Style.BorderWidth.Left;
        set => Desc.Style.BorderWidth = UIEdges.All(value);
    }

    public float CornerRadius
    {
        get => Desc.Style.CornerRadius.TopLeft;
        set => Desc.Style.CornerRadius = UICorners.All(value);
    }

    public UIColor DividerColor
    {
        get => Desc.Style.DividerColor;
        set
        {
            Desc.Style.DividerColor = value;
            if (Desc.Style.DividerWidth <= 0)
            {
                Desc.Style.DividerWidth = 1;
            }
        }
    }

    public UIOverflow OverflowX
    {
        get => Desc.Scroll.OverflowX;
        set => Desc.Scroll.OverflowX = value;
    }

    public UIOverflow OverflowY
    {
        get => Desc.Scroll.OverflowY;
        set => Desc.Scroll.OverflowY = value;
    }

    public SystemCursor Cursor
    {
        get => Desc.Cursor;
        set => Desc.Cursor = value;
    }

    public void Add(Widget widget)
    {
        Children.Add(widget);
    }

    public Panel Add(params ReadOnlySpan<Widget> widgets)
    {
        foreach (var widget in widgets)
        {
            Children.Add(widget);
        }

        return this;
    }

    public bool Remove(Widget widget)
    {
        return Children.Remove(widget);
    }

    public void Clear()
    {
        Children.Clear();
    }

    public override T? Find<T>(string name) where T : class
    {
        if (base.Find<T>(name) is { } self)
        {
            return self;
        }

        foreach (var child in Children)
        {
            if (child.Find<T>(name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    public IEnumerator<Widget> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected override void OnBuild()
    {
        using (OpenSelf())
        {
            BuildChildren();
        }
    }

    protected virtual void BuildChildren()
    {
        var children = Children;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            child.Build();
            if (i < children.Count && ReferenceEquals(children[i], child))
            {
                continue;
            }

            var index = children.IndexOf(child);
            i = index >= 0 ? index : i - 1;
        }
    }
}

public class HStack : Panel
{
    public HStack() : base(UIDirection.Row)
    {
        Desc.Layout.AlignItems = UIAlign.Center;
    }
}

public class VStack : Panel
{
    public VStack() : base(UIDirection.Column)
    {
    }
}

public class ScrollView : Panel
{
    public ScrollView() : base(UIDirection.Column)
    {
        Desc.Layout.Grow = 1;
        Desc.Layout.Shrink = 1;
        Desc.Layout.Basis = UIValue.Points(0);
        Desc.Scroll.OverflowX = UIOverflow.Clip;
        Desc.Scroll.OverflowY = UIOverflow.Scroll;
    }

    public bool TryGetScroll(out UIScrollState state)
    {
        return Ui.TryGetScroll(Id, out state);
    }

    public void ScrollTo(Vector2 offset)
    {
        Ui.ScrollTo(Id, offset);
    }

    public void ScrollToTop()
    {
        Ui.ScrollTo(Id, Vector2.Zero);
    }

    public void ScrollToEnd()
    {
        Ui.ScrollToEnd(Id);
    }

    public void ScrollIntoView(Widget widget)
    {
        Ui.ScrollIntoView(widget.Id);
    }
}

public class Divider : Widget
{
    public bool Vertical { get; set; }
    public float Thickness { get; set; } = 1;
    public UIColor? Color { get; set; }

    protected override void OnBuild()
    {
        ref var desc = ref Desc;
        desc.Layout.AlignSelf = UIAlignSelf.Stretch;
        desc.Layout.Width = Vertical ? UIValue.Points(Thickness) : UIValue.Auto;
        desc.Layout.Height = Vertical ? UIValue.Auto : UIValue.Points(Thickness);
        desc.Layout.Shrink = 0;
        desc.Style.BackgroundColor = Color ?? Ui.Theme.Border;
        Ui.Open(in desc);
        Ui.Close();
    }
}
