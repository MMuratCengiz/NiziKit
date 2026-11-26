using System.Collections;
using System.Numerics;
using DenOfIz;
using DzUI = DenOfIz.UI;

namespace NiziKit.UI;

public abstract class Widget
{
    private static uint _next;
    private int _consumedFrame = -1;
    private int _builtFrame = -1;

    internal UIInteraction State;
    internal bool EnabledNow = true;

    protected Widget()
    {
        Id = DzUI.CombineId(0x4E5A4B57u, ++_next);
    }

    internal uint Id { get; }

    public Widget? Parent { get; internal set; }
    public string? Name { get; set; }

    public UIValue Width { get; set; }
    public UIValue Height { get; set; }
    public UIValue MinWidth { get; set; }
    public UIValue MinHeight { get; set; }
    public UIValue MaxWidth { get; set; }
    public UIValue MaxHeight { get; set; }
    public float Flex { get; set; }
    public UIValue Basis { get; set; }
    public Edges Margin { get; set; }
    public UIAlignSelf AlignSelf { get; set; }
    public UIAnchor? Float { get; set; }
    public Vector2 Offset { get; set; }

    public Prop<bool> Visible { get; set; } = true;
    public Prop<bool> Enabled { get; set; } = true;
    public Prop<string?> Tooltip { get; set; }
    public Func<Menu>? ContextMenu { get; set; }

    public Action? OnClick { get; set; }
    public Action? OnDoubleClick { get; set; }
    public Action? OnRightClick { get; set; }
    public Action<Vector2>? OnDrag { get; set; }
    public Action? OnDragEnd { get; set; }
    public Action? OnUpdate { get; set; }

    public bool IsHovered => State.Hovered;
    public bool IsPressed => State.Pressed;
    public bool IsFocused => State.Focused;
    public bool IsShown => _builtFrame >= Ui.FrameNumber - 1;

    public Rectangle Bounds => Ui.TryGetRect(Id, out var rect) ? Rectangle.From(rect) : default;

    public void Focus()
    {
        Ui.Focus = Id;
    }

    protected virtual bool Clickable => OnClick != null || OnDoubleClick != null;
    protected virtual bool Focusable => Clickable;
    protected virtual bool KeepsSize => false;

    internal virtual bool Interactive => Clickable || Focusable || OnRightClick != null || ContextMenu != null || OnDrag != null || Tooltip.IsBound ||
                                         Tooltip.Value != null;

    internal virtual bool AcceptsTextInput => false;

    internal bool Consumed => _consumedFrame == Ui.FrameNumber;

    internal bool BuiltLastFrame { get; private set; }

    internal void BuildFrame()
    {
        if (!Visible.Value)
        {
            return;
        }

        EnabledNow = Ui.ParentEnabled && Enabled.Value;
        BuiltLastFrame = _builtFrame == Ui.FrameNumber - 1;
        _builtFrame = Ui.FrameNumber;
        Ui.Built.Add(this);
        Ui.PushEnabled(EnabledNow);
        Render();
        Ui.PopEnabled();
    }

    internal abstract void Render();

    internal virtual void Dispatch()
    {
        ref readonly var state = ref State;
        if (state.FocusGained)
        {
            OnFocusChanged(true);
        }

        if (state.FocusLost)
        {
            OnFocusChanged(false);
        }

        if (!EnabledNow)
        {
            return;
        }

        if (Ui.Activated(Id, in state) && Clickable && !Consumed)
        {
            ConsumeAncestors();
            OnActivate();
        }

        if (state.DoubleClicked && OnDoubleClick != null && !Consumed)
        {
            ConsumeAncestors();
            OnDoubleClick();
        }

        if (state.RightClicked && !Consumed && (ContextMenu != null || OnRightClick != null))
        {
            ConsumeAncestors();
            OnRightClick?.Invoke();
            ContextMenu?.Invoke().ShowAt(Ui.PointerPosition);
        }

        if (OnDrag != null && state.Dragging)
        {
            OnDrag(state.DragDelta);
        }

        if (state.DragEnded)
        {
            OnDragEnd?.Invoke();
        }
    }

    protected virtual void OnActivate()
    {
        OnClick?.Invoke();
    }

    internal virtual void OnFocusChanged(bool focused)
    {
    }

    protected internal virtual bool OnKeyDown(in KeyboardEventData key)
    {
        return false;
    }

    internal virtual void OnTextInput(ReadOnlySpan<byte> utf8)
    {
    }

    internal void ConsumeAncestors()
    {
        var frame = Ui.FrameNumber;
        for (var parent = Parent; parent != null; parent = parent.Parent)
        {
            parent._consumedFrame = frame;
        }
    }

    internal UIElementDesc Desc()
    {
        var desc = new UIElementDesc { Id = Id };
        ref var layout = ref desc.Layout;
        layout.Width = Width;
        layout.Height = Height;
        layout.MinWidth = MinWidth;
        layout.MinHeight = MinHeight;
        layout.MaxWidth = MaxWidth;
        layout.MaxHeight = MaxHeight;
        layout.Margin = Margin.ToLength();
        layout.AlignSelf = AlignSelf;
        if (Flex > 0)
        {
            layout.Grow = Flex;
            layout.Shrink = 1;
            layout.Basis = Basis.Type == UIValueType.Auto ? UIValue.Points(0) : Basis;
        }
        else if (Ui.ParentFills && Width.Type == UIValueType.Auto)
        {
            layout.Grow = 1;
            layout.Shrink = 1;
        }
        else
        {
            layout.Shrink = Ui.ShrinkFor(KeepsSize, Width, Height);
        }

        if (Float is { } anchor)
        {
            desc.Floating.Target = UIFloatingTarget.Parent;
            desc.Floating.TargetAnchor = anchor;
            desc.Floating.Anchor = anchor;
            desc.Floating.Offset = Offset;
        }

        var clickable = Clickable;
        if (Focusable && EnabledNow)
        {
            desc.Flags |= (uint)UIElementFlagBits.Focusable;
        }

        if (!EnabledNow && (clickable || Focusable))
        {
            desc.Flags |= (uint)UIElementFlagBits.Disabled;
        }

        if (clickable)
        {
            desc.Cursor = EnabledNow ? SystemCursor.Pointer : SystemCursor.NotAllowed;
            desc.Pointer = UIPointerMode.Block;
        }

        return desc;
    }

    internal void OfferTooltip()
    {
        if (!State.Hovered || State.Pressed)
        {
            return;
        }

        if (Tooltip.Value is { Length: > 0 } text)
        {
            Ui.OfferTooltip(Id, text);
        }
    }

    public override string ToString()
    {
        return Name != null ? $"{GetType().Name} \"{Name}\"" : GetType().Name;
    }
}

public abstract class Component : Widget
{
    private Widget? _content;

    protected abstract Widget Build();

    internal override void Render()
    {
        if (_content == null)
        {
            _content = Build();
            _content.Parent = this;
        }

        _content.BuildFrame();
    }
}

public sealed class WidgetList : IList<Widget>
{
    private readonly List<Widget> _items = new();
    private Widget? _owner;

    public int Count => _items.Count;
    public bool IsReadOnly => false;

    public Widget this[int index]
    {
        get => _items[index];
        set
        {
            Release(_items[index]);
            Adopt(value);
            _items[index] = value;
        }
    }

    public void Add(Widget item)
    {
        Adopt(item);
        _items.Add(item);
    }

    public void Insert(int index, Widget item)
    {
        Adopt(item);
        _items.Insert(index, item);
    }

    public bool Remove(Widget item)
    {
        if (!_items.Remove(item))
        {
            return false;
        }

        Release(item);
        return true;
    }

    public void RemoveAt(int index)
    {
        Release(_items[index]);
        _items.RemoveAt(index);
    }

    public void Clear()
    {
        foreach (var item in _items)
        {
            Release(item);
        }

        _items.Clear();
    }

    public bool Contains(Widget item) => _items.Contains(item);
    public int IndexOf(Widget item) => _items.IndexOf(item);
    public void CopyTo(Widget[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    public List<Widget>.Enumerator GetEnumerator() => _items.GetEnumerator();
    IEnumerator<Widget> IEnumerable<Widget>.GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    internal void AttachTo(Widget owner)
    {
        if (_owner != null && _owner != owner)
        {
            throw new InvalidOperationException($"This widget list already belongs to {_owner}.");
        }

        _owner = owner;
        foreach (var item in _items)
        {
            Adopt(item);
        }
    }

    internal void Detach()
    {
        foreach (var item in _items)
        {
            if (item.Parent == _owner)
            {
                item.Parent = null;
            }
        }

        _owner = null;
    }

    internal void BuildAll()
    {
        var items = _items;
        for (var i = 0; i < items.Count; i++)
        {
            items[i].BuildFrame();
        }
    }

    private void Adopt(Widget item)
    {
        if (_owner == null)
        {
            return;
        }

        if (item.Parent != null && item.Parent != _owner)
        {
            throw new InvalidOperationException($"{item} already belongs to {item.Parent}; a widget can only be in one place. Create a second widget for {_owner}.");
        }

        item.Parent = _owner;
    }

    private void Release(Widget item)
    {
        if (_owner != null && item.Parent == _owner)
        {
            item.Parent = null;
        }
    }
}
