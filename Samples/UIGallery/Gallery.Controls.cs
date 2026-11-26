namespace UIGallery;

internal sealed partial class Gallery
{
    private static readonly string[] Qualities = ["Low", "Medium", "High", "Ultra"];

    private static readonly string[] Countries =
    [
        "Argentina", "Australia", "Austria", "Belgium", "Brazil", "Canada", "Chile", "China", "Colombia", "Croatia", "Czechia", "Denmark",
        "Egypt", "Estonia", "Finland", "France", "Germany", "Greece", "Hungary", "Iceland", "India", "Indonesia", "Ireland", "Italy",
        "Japan", "Kenya", "Mexico", "Morocco", "Netherlands", "New Zealand", "Norway", "Peru", "Poland", "Portugal", "Spain", "Sweden",
        "Switzerland", "Turkey", "Ukraine", "Uruguay"
    ];

    private int _clicks;
    private string _lastButton = "none";

    public Button PrimaryButton { get; private set; } = null!;
    public Dropdown QualityDropdown { get; private set; } = null!;

    private Widget ControlsPage()
    {
        PrimaryButton = Counted(new Button("Primary") { Tone = Tone.Primary, Tooltip = "Tone.Primary, focusable, Enter or Space click it" });
        var notifications = new Checkbox("Notifications") { Checked = true };
        var sounds = new Checkbox("Sounds");
        var wifi = new Switch("Wi-Fi") { On = true };
        var airplane = new Switch("Airplane mode");
        var bass = new Slider(0, 1) { Value = 0.7f, Vertical = true, Height = 140 };
        var treble = new Slider(0, 1) { Value = 0.35f, Vertical = true, Height = 140 };
        QualityDropdown = new Dropdown(Qualities) { Selected = 2, Tooltip = "Up and Down change it while focused" };
        var country = new Dropdown(Countries) { Selected = -1, Placeholder = "Pick a country", MaxListHeight = 220 };

        return Page(
            Grid(
                Card("Buttons", "Hover, press, focus and disabled states, color transitions and tooltips.", 360,
                    Line(8,
                        PrimaryButton,
                        Counted(new Button("Secondary")),
                        Counted(new Button("Danger") { Tone = Tone.Danger }),
                        Counted(new Button("Save", FontAwesome.Save)),
                        Counted(new Button("Toggle", FontAwesome.Star) { Checked = false, Tooltip = "Checked flips on every click" }),
                        Counted(new Button("Disabled") { Enabled = false, Tooltip = "Disabled widgets are still hovered, so they keep their tooltip" }),
                        new Button("Reset", FontAwesome.RotateLeft) { Tone = Tone.Ghost, OnClick = () => _clicks = 0 }),
                    new Label(() => $"Clicked {_clicks} times, last \"{_lastButton}\"", TextStyle.Muted with { Size = 13 })),
                Card("Toggles", "Checkboxes and switches; the switch knob slides with a position transition.", 360,
                    Line(16, notifications, sounds, new Checkbox("Locked option") { Checked = true, Enabled = false }),
                    Line(16, wifi, airplane, new Switch("Locked switch") { Enabled = false }),
                    new Paragraph(() => $"notifications {notifications.Checked.Value}, sounds {sounds.Checked.Value}, wi-fi {wifi.On.Value}, airplane {airplane.On.Value}",
                        TextStyle.Small)),
                Card("Sliders", "A percent fill and an absolutely positioned knob. Drag, click the track, or use the arrow keys.", 360,
                    SliderRow("Volume", new Slider(0, 100) { Value = 65, Format = "0" }),
                    SliderRow("Opacity", new Slider(0, 1) { Value = 0.5f, Step = 0.25f, Format = "0.00" }),
                    SliderRow("Disabled", new Slider(0, 1) { Value = 0.3f, Enabled = false }),
                    Line(24, bass, treble, new Paragraph(() => $"bass {bass.Current:0.00}\ntreble {treble.Current:0.00}", TextStyle.Small))),
                Card("Dropdowns", "The list floats under its button at the button's width, scrolls past its maximum height and flips when it would leave the window.", 360,
                    Line(12, QualityDropdown, country),
                    new Dropdown(Qualities) { Selected = 1, Enabled = false },
                    new Label(() => $"quality {Qualities[QualityDropdown.Selected.Value]}, country {(country.Selected.Value >= 0 ? Countries[country.Selected.Value] : "none")}",
                        TextStyle.Small))));
    }

    private Button Counted(Button button)
    {
        var name = button.Text.ToString();
        button.OnClick += () =>
        {
            _clicks++;
            _lastButton = name;
        };
        return button;
    }

    private static Row SliderRow(string label, Slider slider)
    {
        return new Row { Gap = 12, Children = [new Label(label) { Width = 70 }, slider] };
    }
}
