using System.Numerics;
using DenOfIz;
using NiziKit.UI;
using NiziKit.UI.Widgets;

namespace VttDemo.UiKit;

/// <summary>Transient notifications floating over everything, with enter and exit transitions.</summary>
public sealed class Toasts
{
    private const float Lifetime = 3.5f;

    private sealed class Toast
    {
        public uint Id;
        public required string Text;
        public UIColor Color;
        public float Age;
    }

    private readonly List<Toast> _toasts = new();
    private readonly uint _id = Ui.Id("VttDemo.Toasts");
    private uint _next;

    public void Show(string text, UIColor color)
    {
        _toasts.Add(new Toast { Id = ++_next, Text = text, Color = color });
        if (_toasts.Count > 4)
        {
            _toasts.RemoveAt(0);
        }
    }

    public void Draw(float dt)
    {
        _toasts.RemoveAll(t => (t.Age += dt) > Lifetime);
        var theme = Ui.Theme;
        using (Ui.Column(_id)
                   .FloatOnViewport(UIAnchor.BottomCenter, UIAnchor.BottomCenter, new Vector2(0, -44))
                   .ZIndex(5000, absolute: true)
                   .Gap(8)
                   .AlignItems(UIAlign.Center)
                   .Passthrough()
                   .Open())
        {
            foreach (var toast in _toasts)
            {
                using (Ui.Row(Ui.Id(_id, toast.Id))
                           .AlignItems(UIAlign.Center)
                           .Gap(10)
                           .Padding(16, 10)
                           .Background(theme.TooltipBackground)
                           .Border(toast.Color, 1)
                           .CornerRadius(10)
                           .Animate(UITransitionPropertyFlagBits.Position, 0.25f, UIEasing.EaseOutBack)
                           .Enter(UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, 16)).WithScale(0.94f))
                           .Exit(UITransitionState.FromOpacity(0).WithScale(0.96f))
                           .Open())
                {
                    Ui.Element().Size(8, 8).CornerRadius(4).Background(toast.Color).Leaf();
                    Im.Line(toast.Text, 14, theme.TooltipText);
                }
            }
        }
    }
}

/// <summary>A modal yes/no dialog built from retained widgets inside a <see cref="Popup"/>.</summary>
public sealed class ConfirmDialog : Popup
{
    private readonly Label _title = new() { FontSize = 18 };
    private readonly Label _message = new();
    private readonly Button _confirm = new() { Kind = ButtonKind.Danger };
    private Action? _action;

    public ConfirmDialog()
    {
        Modal = true;
        Layout.Width = 420;
        Layout.Padding = UILengthEdges.All(20);
        Layout.Gap = 14;

        var cancel = new Button("Cancel");
        cancel.Clicked += _ => Close();
        _confirm.Clicked += _ =>
        {
            Close();
            _action?.Invoke();
        };

        var buttons = new HStack { Justify = UIJustify.End, Gap = 8 };
        buttons.Add(cancel, _confirm);
        Add(_title, _message, buttons);
    }

    public void Show(string title, string message, string confirm, Action action, bool danger = true)
    {
        _title.Text = title;
        _message.Text = message;
        _confirm.Text = confirm;
        _confirm.Kind = danger ? ButtonKind.Danger : ButtonKind.Primary;
        _action = action;
        ShowCentered();
        _confirm.Focus();
    }
}
