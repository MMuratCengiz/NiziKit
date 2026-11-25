using NiziKit.UI.Widgets;

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
        PrimaryButton = new Button("Primary") { Kind = ButtonKind.Primary, Tooltip = "Kind = Primary, focusable, Enter or Space click it" };
        var toggle = new Button("Toggle") { Icon = FontAwesome.Star, Tooltip = "Checked flips on every click" };
        toggle.Clicked += _ => toggle.Checked = !toggle.Checked;
        var reset = new Button("Reset") { Kind = ButtonKind.Ghost, Icon = FontAwesome.RotateLeft };
        reset.Clicked += _ => _clicks = 0;

        Button[] buttons =
        [
            PrimaryButton,
            new("Secondary"),
            new("Danger") { Kind = ButtonKind.Danger },
            new("Save") { Icon = FontAwesome.Save },
            toggle,
            new("Disabled") { Enabled = false, Tooltip = "Disabled elements are still hovered, so they keep their tooltip" }
        ];
        foreach (var button in buttons)
        {
            button.Clicked += b =>
            {
                _clicks++;
                _lastButton = ((Button)b).Text;
            };
        }

        var buttonRow = Row(8, buttons);
        buttonRow.Add(reset);

        var notifications = new Checkbox("Notifications", true);
        var sounds = new Checkbox("Sounds");
        var locked = new Checkbox("Locked option", true) { Enabled = false };
        var wifi = new Switch("Wi-Fi", true);
        var airplane = new Switch("Airplane mode");
        var lockedSwitch = new Switch("Locked switch") { Enabled = false };

        var volume = new Slider(0, 100, 65) { ShowValue = true, ValueFormat = "0" };
        var opacity = new Slider(0, 1, 0.5f) { ShowValue = true, Step = 0.25f, ValueFormat = "0.00" };
        var disabledSlider = new Slider(0, 1, 0.3f) { Enabled = false };
        var bass = new Slider(0, 1, 0.7f) { Vertical = true, Fill = false, Height = 140 };
        var treble = new Slider(0, 1, 0.35f) { Vertical = true, Fill = false, Height = 140 };

        QualityDropdown = new Dropdown(Qualities, 2) { Tooltip = "Up and Down change it while focused" };
        var country = new Dropdown(Countries, -1) { Placeholder = "Pick a country", MaxListHeight = 220 };
        var disabledDropdown = new Dropdown(Qualities, 1) { Enabled = false };

        return Page(
            Grid(
                new Card("Buttons", "Hover, press, focus and disabled states, color transitions and tooltips.", 360)
                {
                    buttonRow,
                    new Immediate(() => Ui.Text($"Clicked {_clicks} times, last \"{_lastButton}\"", 13, Ui.Theme.TextMuted))
                },
                new Card("Toggles", "Checkboxes and switches; the switch knob slides with a position transition.", 360)
                {
                    Row(16, notifications, sounds, locked),
                    Row(16, wifi, airplane, lockedSwitch),
                    new Immediate(() => Ui.Text($"notifications {notifications.Checked}, sounds {sounds.Checked}, wi-fi {wifi.On}, airplane {airplane.On}",
                        12, Ui.Theme.TextMuted))
                },
                new Card("Sliders", "A percent fill and an absolutely positioned knob. Drag, click the track, or use the arrow keys.", 360)
                {
                    SliderRow("Volume", volume),
                    SliderRow("Opacity", opacity),
                    SliderRow("Disabled", disabledSlider),
                    Row(24, bass, treble, new Immediate(() => Ui.Text($"bass {bass.Value:0.00}\ntreble {treble.Value:0.00}", 12, Ui.Theme.TextMuted)))
                },
                new Card("Dropdowns", "The list floats under its button at the button's width, scrolls past its maximum height and flips when it would leave the window.", 360)
                {
                    Row(12, QualityDropdown, country),
                    disabledDropdown,
                    new Immediate(() => Ui.Text($"quality {QualityDropdown.SelectedItem}, country {country.SelectedItem ?? "none"}", 12, Ui.Theme.TextMuted))
                }));
    }

    private static HStack SliderRow(string label, Slider slider)
    {
        var row = new HStack { Gap = 12 };
        row.Add(new Label(label) { Width = 70, Wrap = UITextWrap.None });
        row.Add(slider);
        return row;
    }
}
