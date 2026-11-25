using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public abstract class Widget
{
    private UIElementDesc _desc;
    private UIInteraction _interaction;
    private bool _customLayout;

    protected Widget()
    {
        Id = Ui.NewWidgetId();
        _desc.Id = Id;
    }

    public uint Id { get; }
    public string? Name { get; set; }
    public object? Tag { get; set; }
    public Widget? Parent { get; internal set; }

    public bool Visible { get; set; } = true;

    public bool Enabled { get; set; } = true;

    public string? Tooltip { get; set; }

    public ref UILayoutDesc Layout
    {
        get
        {
            _customLayout = true;
            return ref _desc.Layout;
        }
    }

    public UIValue Width
    {
        get => _desc.Layout.Width;
        set => Layout.Width = value;
    }

    public UIValue Height
    {
        get => _desc.Layout.Height;
        set => Layout.Height = value;
    }

    public UIValue MinWidth
    {
        get => _desc.Layout.MinWidth;
        set => Layout.MinWidth = value;
    }

    public UIValue MinHeight
    {
        get => _desc.Layout.MinHeight;
        set => Layout.MinHeight = value;
    }

    public UIValue MaxWidth
    {
        get => _desc.Layout.MaxWidth;
        set => Layout.MaxWidth = value;
    }

    public UIValue MaxHeight
    {
        get => _desc.Layout.MaxHeight;
        set => Layout.MaxHeight = value;
    }

    public float Grow
    {
        get => _desc.Layout.Grow;
        set => Layout.Grow = value;
    }

    public float Shrink
    {
        get => _desc.Layout.Shrink;
        set => Layout.Shrink = value;
    }

    public UIValue Basis
    {
        get => _desc.Layout.Basis;
        set => Layout.Basis = value;
    }

    public UILengthEdges Margin
    {
        get => _desc.Layout.Margin;
        set => Layout.Margin = value;
    }

    public UILengthEdges Padding
    {
        get => _desc.Layout.Padding;
        set => Layout.Padding = value;
    }

    public UIAlignSelf AlignSelf
    {
        get => _desc.Layout.AlignSelf;
        set => Layout.AlignSelf = value;
    }

    public bool Fill
    {
        get => _desc.Layout is { Grow: 1, Shrink: 1, Basis.Type: UIValueType.Points };
        set
        {
            Layout.Grow = value ? 1 : 0;
            Layout.Shrink = value ? 1 : 0;
            Layout.Basis = value ? UIValue.Points(0) : UIValue.Auto;
        }
    }

    public bool IsHovered => _interaction.Hovered;
    public bool IsPressed => _interaction.Pressed;
    public bool IsFocused => _interaction.Focused;

    public ref readonly UIInteraction Interaction => ref _interaction;

    public event Action<Widget>? Clicked;
    public event Action<Widget>? DoubleClicked;
    public event Action<Widget>? RightClicked;
    public event Action<Widget>? MiddleClicked;
    public event Action<Widget>? PointerEntered;
    public event Action<Widget>? PointerExited;
    public event Action<Widget>? FocusGained;
    public event Action<Widget>? FocusLost;
    public event Action<Widget>? DragStarted;

    public event Action<Widget, Vector2>? Dragged;

    public event Action<Widget>? DragEnded;

    public bool TryGetBounds(out UIRect bounds)
    {
        return Ui.TryGetRect(Id, out bounds);
    }

    public void Focus()
    {
        Ui.Focus = Id;
    }

    public virtual T? Find<T>(string name) where T : Widget
    {
        return Name == name ? this as T : null;
    }

    protected ref UIElementDesc Desc => ref _desc;

    protected bool HasCustomLayout => _customLayout;

    protected virtual bool Focusable => false;

    internal virtual bool AcceptsTextInput => false;

    internal Popup? EnclosingPopup { get; private set; }

    internal void Build()
    {
        _interaction = Ui.Interaction(Id);
        EnclosingPopup = Ui.BuildingPopup;
        if (Visible)
        {
            TrackFocus(in _interaction);
            RaiseEvents();
        }

        _desc.Flags = (Focusable && Enabled ? (uint)UIElementFlagBits.Focusable : 0) | (Enabled ? 0 : (uint)UIElementFlagBits.Disabled);
        if (!Visible)
        {
            _desc.Layout.Display = UIDisplay.None;
        }
        else if (_desc.Layout.Display == UIDisplay.None)
        {
            _desc.Layout.Display = UIDisplay.Flex;
        }

        OnBuild();
    }

    protected abstract void OnBuild();

    protected Scope OpenSelf()
    {
        return new Scope(Ui.Open(in _desc));
    }

    protected void TrackFocus(in UIInteraction interaction)
    {
        if (interaction.Focused)
        {
            Ui.TrackFocused(this);
        }
    }

    protected virtual void OnClick()
    {
    }

    protected virtual void OnFocusChanged(bool focused)
    {
    }

    protected internal virtual bool OnKeyDown(in KeyboardEventData key)
    {
        return false;
    }

    internal virtual void OnTextInput(ReadOnlySpan<byte> utf8)
    {
    }

    private void RaiseEvents()
    {
        ref readonly var interaction = ref _interaction;
        if (interaction.HoverStarted)
        {
            PointerEntered?.Invoke(this);
        }

        if (interaction.HoverEnded)
        {
            PointerExited?.Invoke(this);
        }

        if (interaction.FocusGained)
        {
            OnFocusChanged(true);
            FocusGained?.Invoke(this);
        }

        if (interaction.FocusLost)
        {
            OnFocusChanged(false);
            FocusLost?.Invoke(this);
        }

        if (interaction.DragStarted)
        {
            DragStarted?.Invoke(this);
        }

        if (interaction.Dragging)
        {
            Dragged?.Invoke(this, interaction.DragDelta);
        }

        if (interaction.DragEnded)
        {
            DragEnded?.Invoke(this);
        }

        if (Enabled && Ui.Activated(Id, in interaction))
        {
            OnClick();
            Clicked?.Invoke(this);
        }

        if (interaction.DoubleClicked)
        {
            DoubleClicked?.Invoke(this);
        }

        if (interaction.RightClicked)
        {
            RightClicked?.Invoke(this);
        }

        if (interaction.MiddleClicked)
        {
            MiddleClicked?.Invoke(this);
        }

        if (Tooltip != null && interaction.Hovered && !interaction.Pressed)
        {
            Ui.Tooltip(Id, Tooltip);
        }
    }
}
