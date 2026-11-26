namespace UIGallery;

internal sealed partial class Gallery
{
    private const string LiveMarkup =
        """
        <h2>Rich text</h2>
        <p>Markup is parsed into paragraphs of <b>styled runs</b>. Each paragraph is <i>one</i> text leaf, so wrapping,
        shaping and direction work across styles: قرأ <b>Thorin</b> الرسالة.</p>
        <p>Inline <code>code</code>, <mark>highlights</mark>, <s>strikethrough</s>, <u>underline</u>, <small>small</small>,
        H<sub>2</sub>O and E = mc<sup>2</sup>.</p>
        <p>Colours come from CSS or the theme: <span style="color:#e5534b">red</span>,
        <span style="color: hsl(140 55% 50%)">green</span>, <span style="background: var(--accent-soft)">soft accent</span>,
        <span style="text-decoration: underline wavy; text-decoration-color: #e5534b">a red underline</span>.</p>
        <p>Links raise <code>OnLink</code>: <a href="npc:goblin-2" title="Goblin · 7 HP">Goblin</a> or <a href="https://example.com">a web page</a>.</p>
        <ul>
          <li>Lists nest
            <ul><li>with their own markers</li></ul></li>
          <li>and count:
            <ol><li>first</li><li>second</li></ol></li>
        </ul>
        <blockquote>Quotes get a bar and muted text.</blockquote>
        <pre>pre keeps   its spacing
        and its line breaks</pre>
        """;

    private const string ChatMarkup =
        """
        <p><b style="color:#d4a24c">Thorin</b> rolls <img src="Assets/d20.png" height="1.15em" style="vertical-align: middle"> <b>2d6 + 3</b> =
        <b style="color:#e5534b; font-size: 1.2em">14</b> against <a href="npc:goblin-2">Goblin</a>.</p>
        <p><b style="color:#7cc4ff">GM</b>: The goblin <s>dodges</s> <i>staggers</i> back. 🎲</p>
        <p><small style="color: var(--muted)">Whisper from <b>Lyra</b>: check the <a href="map:north-door" title="Opens the map">north door</a>.</small></p>
        """;

    private const string ItemMarkup =
        """
        <h3 style="color:#a78bfa; margin-top: 0">Moonblade</h3>
        <p style="margin-top: 0"><small style="color: var(--muted)">Longsword · <i>legendary</i></small></p>
        <p><b>+3</b> to hit, <b>1d8 + 3</b> slashing.<br>Glows <span style="color:#9bd">silver</span> near undead.</p>
        <hr>
        <p style="color: var(--muted)"><i>“Forged under a waning moon.”</i></p>
        """;

    private const string NotesMarkup =
        """
        <h3 style="margin-top: 0">Patch 1.2.1</h3>
        <ul>
          <li><b>New:</b> <code>RichText</code> draws HTML</li>
          <li><b>Fixed:</b> <s>tooltips flicker</s></li>
          <li>Icons <img src="Assets/d20.png" height="1em"> sized in <code>em</code></li>
        </ul>
        <table>
          <tr><th>Stat</th><th>Before</th><th>After</th></tr>
          <tr><td>Load</td><td>2.1 s</td><td><b style="color:#4caf50">0.8 s</b></td></tr>
        </table>
        """;

    private const string ImageMarkup =
        """
        <p>A local file <img src="Assets/d20.png" height="28" style="vertical-align: middle"> inline next to text, and at
        <img src="Assets/d20.png" height="1em"> one em.</p>
        <img src="Assets/banner.png" style="border-radius: 10px">
        <p>A remote image, fetched once and cached; it scales down with its column:</p>
        <img src="https://picsum.photos/id/1015/480/270" width="480" style="border-radius: 10px">
        """;

    private string _markup = LiveMarkup;
    private string _lastLink = "none yet";

    public ScrollView RichTextScroll { get; private set; } = null!;
    public RichText ChatText { get; private set; } = null!;
    public RichText LiveText { get; private set; } = null!;

    private Widget RichTextPage()
    {
        ChatText = new RichText(ChatMarkup) { OnLink = Link };
        LiveText = new RichText(() => _markup) { OnLink = Link };
        return RichTextScroll = Page(
            Card("Live markup", "Edit the HTML on the left. Unknown tags render their text and unknown CSS is ignored.", 0,
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children =
                    [
                        new TextArea { Text = Bind(() => _markup, text => _markup = text), Flex = 1, Height = 460 },
                        new ScrollView
                        {
                            Flex = 1,
                            Height = 460,
                            Padding = 16,
                            Background = Color.SurfaceAlt,
                            Radius = 8,
                            Children = [LiveText]
                        }
                    ]
                },
                new Paragraph(() => $"last link: {_lastLink}", TextStyle.Small)),
            Grid(
                Card("Chat log", "Inline icons, colours and links in one wrapped paragraph.", 300, ChatText),
                Card("Item tooltip", "Headings, a line break, a rule and theme colours.", 300, new RichText(ItemMarkup)),
                Card("Patch notes", "A list with an inline image and a simple table.", 300, new RichText(NotesMarkup))),
            Card("Images", "Local paths and URLs load in the background; no resolver needed.", 0, new RichText(ImageMarkup)));
    }

    private void Link(string href)
    {
        _lastLink = href;
        Ui.Notify($"Link: {href}");
    }
}
