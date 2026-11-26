using System.Collections.Concurrent;
using System.Numerics;

namespace UIGallery;

internal sealed partial class Gallery
{
    private readonly ConcurrentQueue<string> _jobUpdates = new();
    private string _action = "none";
    private string _job = "not started";
    private bool _jobRunning;

    public Menu ContextMenu { get; private set; } = null!;
    public Dialog Dialog { get; private set; } = null!;
    public Popup InfoPopup { get; private set; } = null!;
    public Button InfoButton { get; private set; } = null!;

    public void OpenContextMenuAt(Vector2 point)
    {
        ContextMenu.ShowAt(point);
    }

    private Widget OverlaysPage()
    {
        ContextMenu = new Menu
        {
            Items =
            [
                new MenuItem("Cut", FontAwesome.Cut, "⌘X") { OnClick = () => _action = "Cut" },
                new MenuItem("Copy", FontAwesome.Copy, "⌘C") { OnClick = () => _action = "Copy" },
                new MenuItem("Paste", FontAwesome.Paste, "⌘V") { OnClick = () => _action = "Paste" },
                new MenuSeparator(),
                new MenuItem("Rename", FontAwesome.Edit) { OnClick = () => _action = "Rename" },
                new MenuItem("Delete", FontAwesome.Trash) { Enabled = false, OnClick = () => _action = "Delete" }
            ]
        };

        var fileMenu = new Menu
        {
            Items =
            [
                new MenuItem("New scene", FontAwesome.File, "⌘N") { OnClick = () => _action = "New scene" },
                new MenuItem("Open…", FontAwesome.FolderOpen, "⌘O") { OnClick = () => _action = "Open" },
                new MenuItem("Save", FontAwesome.Save, "⌘S") { OnClick = () => _action = "Save" }
            ]
        };

        InfoButton = new Button("Anchored popup", FontAwesome.CircleInfo);
        InfoPopup = new Popup { Width = 280, Padding = 14, Gap = 10 };
        InfoPopup.Children =
        [
            new Label("Anchored popup", TextStyle.Sized(15)),
            new Paragraph("Floating on the button's bottom left, kept in the viewport. Pressing outside closes it, pressing the button toggles it.", TextStyle.Muted with { Size = 13 }),
            new Button("Got it") { Tone = Tone.Primary, AlignSelf = UIAlignSelf.End, OnClick = InfoPopup.Close }
        ];
        InfoButton.OnClick = () => InfoPopup.Toggle(InfoButton);
        Dialog = BuildDialog();

        return Page(
            Card("Popups and menus", "Menus and popups are objects you show; menu items are widgets with OnClick, and a popup opened from inside another keeps its parent open.", 0,
                Line(8,
                    InfoButton,
                    new MenuButton("File", fileMenu) { Tone = Tone.Secondary, Icon = FontAwesome.Bars },
                    new Button("Open dialog", FontAwesome.Message) { Tone = Tone.Primary, OnClick = Dialog.Show },
                    new Button("Confirm…") { OnClick = () => Dialogs.Confirm("Delete the scene?", "This removes it from the project. You can't undo this.", "Delete", Tone.Danger, () => _action = "deleted") }),
                new Column
                {
                    Height = 110,
                    Justify = UIJustify.Center,
                    Align = UIAlign.Center,
                    Gap = 4,
                    Background = Color.SurfaceAlt,
                    HoverBackground = Color.Hover,
                    Border = Color.BorderStrong,
                    Radius = 8,
                    Cursor = SystemCursor.Crosshair,
                    ContextMenu = () => ContextMenu,
                    Children =
                    [
                        new Label("Right-click anywhere in this area"),
                        new Label("for a context menu at the pointer", TextStyle.Small)
                    ]
                },
                new Label(() => $"last action: {_action}", TextStyle.Small)),
            Card("Tooltips and notifications", "Any widget takes a Tooltip; Ui.Notify shows a transient message over everything.", 0,
                Line(8,
                    new Button("Hover me") { Tooltip = "A tooltip floats under its target, passes the pointer through and fades in after a delay" },
                    new Button("Me too") { Tone = Tone.Ghost, Tooltip = "It flips above when there is no room below" },
                    new Row
                    {
                        Padding = (10, 7),
                        Gap = 6,
                        Background = Color.SurfaceAlt,
                        Radius = 6,
                        Tooltip = "Panels take tooltips too",
                        Children = [new Icon(FontAwesome.CircleQuestion) { Color = Color.TextMuted }, new Label("A plain row")]
                    },
                    new Button("Notify", FontAwesome.Bell) { OnClick = () => Ui.Notify($"Saved at {DateTime.Now:HH:mm:ss}") },
                    new Button("Notify for 8 s") { OnClick = () => Ui.Notify("This one stays for 8 seconds", 8, Color.Warning) },
                    new Button("Background job", FontAwesome.Play) { Name = "overlays.job", OnClick = StartJob, Enabled = Bind(() => !_jobRunning) }),
                new Label(() => $"job: {_job}", TextStyle.Small) { Name = "overlays.job.status" },
                Caption("The job posts progress from another thread; the game's Update, or UiDebug.OnFrame when headless, drains it.")));
    }

    public void Pump()
    {
        while (_jobUpdates.TryDequeue(out var update))
        {
            _job = update;
        }

        if (_jobRunning && _job == "done")
        {
            _jobRunning = false;
            Ui.Notify("Background job finished", 6, Color.Success);
        }
    }

    private void StartJob()
    {
        _jobRunning = true;
        Task.Run(() =>
        {
            for (var step = 1; step <= 5; step++)
            {
                Thread.Sleep(40);
                _jobUpdates.Enqueue($"step {step} of 5");
                Game.RequestFrame();
            }

            _jobUpdates.Enqueue("done");
            Game.RequestFrame();
        });
    }

    private Dialog BuildDialog()
    {
        var name = new TextField { Placeholder = "Scene name", Width = UIValue.Percent(1) };
        var template = new Dropdown(["Empty", "Platformer", "Top down", "Puzzle"]) { Width = UIValue.Percent(1) };
        var dialog = new Dialog { Title = "New scene", Gap = 12, OnClose = name.Clear };
        dialog.Children =
        [
            new Paragraph("A modal dialog: the backdrop blocks the page and fades on its own, the panel scales in and out. The dropdown's list opens inside the dialog.",
                TextStyle.Muted with { Size = 13 }),
            name,
            template,
            new Row
            {
                Justify = UIJustify.End,
                Gap = 8,
                Children =
                [
                    new Button("Cancel")
                    {
                        OnClick = () =>
                        {
                            _action = "dialog cancelled";
                            dialog.Close();
                        }
                    },
                    new Button("Create")
                    {
                        Tone = Tone.Primary,
                        OnClick = () =>
                        {
                            _action = $"created \"{name.Text.Value}\" from {template.Items[template.Selected.Value]}";
                            dialog.Close();
                        }
                    }
                ]
            }
        ];
        return dialog;
    }
}
