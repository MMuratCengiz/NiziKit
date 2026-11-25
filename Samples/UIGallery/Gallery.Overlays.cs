using System.Numerics;
using NiziKit.UI.Widgets;

namespace UIGallery;

internal sealed partial class Gallery
{
    private readonly uint _contextAreaId = Ui.Id("Gallery.ContextArea");
    private readonly uint _hintId = Ui.Id("Gallery.Hint");
    private string _action = "none";

    public Menu ContextMenu { get; private set; } = null!;
    public Popup Dialog { get; private set; } = null!;
    public Popup InfoPopup { get; private set; } = null!;
    public Button InfoButton { get; private set; } = null!;

    private Widget OverlaysPage()
    {
        ContextMenu = new Menu();
        ContextMenu.Add("Cut", () => _action = "Cut", FontAwesome.Cut, "⌘X");
        ContextMenu.Add("Copy", () => _action = "Copy", FontAwesome.Copy, "⌘C");
        ContextMenu.Add("Paste", () => _action = "Paste", FontAwesome.Paste, "⌘V");
        ContextMenu.AddSeparator();
        ContextMenu.Add("Rename", () => _action = "Rename", FontAwesome.Edit);
        ContextMenu.Add("Delete", () => _action = "Delete", FontAwesome.Trash).Enabled = false;

        var fileMenu = new Menu();
        fileMenu.Add("New scene", () => _action = "New scene", FontAwesome.File, "⌘N");
        fileMenu.Add("Open…", () => _action = "Open", FontAwesome.FolderOpen, "⌘O");
        fileMenu.Add("Save", () => _action = "Save", FontAwesome.Save, "⌘S");
        var fileButton = new Button("File") { Icon = FontAwesome.Bars };
        fileButton.Clicked += b => fileMenu.Toggle(b);

        InfoButton = new Button("Anchored popup") { Icon = FontAwesome.CircleInfo };
        InfoPopup = new Popup { Width = 280, Padding = UILengthEdges.All(14), Gap = 10 };
        var gotIt = new Button("Got it") { Kind = ButtonKind.Primary, AlignSelf = UIAlignSelf.End };
        gotIt.Clicked += _ => InfoPopup.Close();
        InfoPopup.Add(
            new Label("Anchored popup") { FontSize = 15 },
            new Label("Floating on the button's bottom left, kept in the viewport. Pressing outside closes it, pressing the button toggles it.") { Muted = true, FontSize = 13 },
            gotIt);
        InfoButton.Clicked += b => InfoPopup.Toggle(b);

        var dialogButton = new Button("Open dialog") { Kind = ButtonKind.Primary, Icon = FontAwesome.Message };
        Dialog = BuildDialog();
        dialogButton.Clicked += _ => Dialog.ShowCentered();

        return Page(
            new Card("Popups and menus", "Popups are declared at the root by Ui.EndFrame; a popup opened from inside another is declared inside it, so pressing it does not dismiss its parent.")
            {
                Row(8, InfoButton, fileButton, dialogButton),
                new Immediate(DrawContextArea),
                new Immediate(() => Ui.Text($"last action: {_action}", 12, Ui.Theme.TextMuted))
            },
            new Card("Tooltips", "Widgets take a Tooltip string; immediate code offers one with Ui.Tooltip(id, text) while the element is hovered.")
            {
                Row(8,
                    new Button("Hover me") { Tooltip = "A tooltip floats under its target, passes the pointer through and fades in after a delay" },
                    new Button("Me too") { Kind = ButtonKind.Ghost, Tooltip = "It flips above when there is no room below" },
                    new Immediate(DrawHint))
            });
    }

    private Popup BuildDialog()
    {
        var dialog = new Popup { Modal = true, Width = 420, Padding = UILengthEdges.All(20), Gap = 12 };
        var name = new TextBox { Placeholder = "Scene name", Width = UIValue.Percent(1) };
        var template = new Dropdown(["Empty", "Platformer", "Top down", "Puzzle"], 0) { Width = UIValue.Percent(1) };
        var cancel = new Button("Cancel");
        cancel.Clicked += _ =>
        {
            _action = "dialog cancelled";
            dialog.Close();
        };
        var create = new Button("Create") { Kind = ButtonKind.Primary };
        create.Clicked += _ =>
        {
            _action = $"created \"{name.Text}\" from {template.SelectedItem}";
            dialog.Close();
        };
        var actions = new HStack { Gap = 8, Justify = UIJustify.End };
        actions.Add(cancel, create);
        dialog.Add(
            new Label("New scene") { FontSize = 18 },
            new Label("A modal popup: the backdrop blocks the page and fades on its own, the panel scales in and out. The dropdown's list is declared inside the dialog.") { Muted = true, FontSize = 13 },
            name,
            template,
            actions);
        dialog.Closed += _ => name.Clear();
        return dialog;
    }

    private void DrawContextArea()
    {
        var theme = Ui.Theme;
        var interaction = Ui.Interaction(_contextAreaId);
        if (interaction.RightClicked)
        {
            ContextMenu.ShowAt(Ui.PointerPosition);
        }

        using (Ui.Column(_contextAreaId)
                   .Height(110)
                   .Center()
                   .Gap(4)
                   .Background(interaction.Hovered ? theme.Hover : theme.SurfaceAlt)
                   .Border(theme.BorderStrong, 1)
                   .CornerRadius(8)
                   .Cursor(SystemCursor.Crosshair)
                   .Animate()
                   .Open())
        {
            Ui.Text("Right-click anywhere in this area", 14, theme.Text);
            Ui.Text("for a context menu at the pointer", 12, theme.TextMuted);
        }
    }

    private void DrawHint()
    {
        var theme = Ui.Theme;
        if (Ui.Interaction(_hintId).Hovered)
        {
            Ui.Tooltip(_hintId, "Offered from immediate code");
        }

        using (Ui.Row(_hintId).Padding(10, 7).AlignItems(UIAlign.Center).Gap(6).Background(theme.SurfaceAlt).CornerRadius(6).Open())
        {
            Ui.Text(FontAwesome.CircleQuestion, 14, theme.TextMuted, FontAwesome.FontId);
            Ui.Text("Immediate element", 14, theme.Text);
        }
    }

    public void OpenContextMenuAt(Vector2 point)
    {
        ContextMenu.ShowAt(point);
    }
}
