namespace UIGallery;

internal sealed class FileEntry(int index)
{
    private static readonly string[] Kinds = ["Texture", "Mesh", "Material", "Shader", "Audio", "Scene", "Script"];

    public int Index { get; } = index;
    public string Name { get; } = $"Row {index:0000}";
    public string Kind => Kinds[Index % Kinds.Length];
    public string Size { get; } = $"{index * 37 % 1000}.{index % 10} KB";
}

internal sealed partial class Gallery
{
    private readonly FileEntry[] _files = Enumerable.Range(0, 1000).Select(i => new FileEntry(i)).ToArray();
    private FileEntry? _selectedFile;
    private int _opened = -1;

    public ListView<FileEntry> Files { get; private set; } = null!;

    public void SelectFile(int index)
    {
        _selectedFile = _files[index];
        Files.Reveal(_selectedFile);
        Files.Focus();
    }

    private Widget ListsPage()
    {
        Files = new ListView<FileEntry>(() => _files, FileRow)
        {
            Height = 360,
            Padding = 1,
            Striped = true,
            Background = Color.Surface,
            Border = Color.Border,
            Radius = 8,
            Selected = Bind(() => _selectedFile, file => _selectedFile = file),
            OnActivate = file => _opened = file.Index
        };

        var strip = new ScrollView
        {
            Horizontal = true,
            Gap = 12,
            Padding = 12,
            Align = UIAlign.Start,
            Background = Color.SurfaceAlt,
            Radius = 14,
            Children = [.. Enumerable.Range(0, 24).Select(StripCard)]
        };

        return Page(
            Card("1,000 rows", "Each row is a widget keyed by its item. Click selects, double click or Enter opens; Up, Down, Page Up, Page Down, Home and End move the selection, which is scrolled into view. The rows are clipped to the list's rounded corners.", 0,
                new Row
                {
                    Gap = 8,
                    Children =
                    [
                        new Button("Scroll to top", FontAwesome.ArrowUp) { OnClick = Files.ScrollToTop },
                        new Button("Scroll to bottom", FontAwesome.ArrowDown) { OnClick = Files.ScrollToEnd },
                        new Button("Select row 500") { OnClick = () => SelectFile(500) },
                        new Spacer(),
                        new Label(() => $"offset {Files.ScrollOffset.Y:0}, selected {(_selectedFile?.Index ?? -1)}, opened {_opened}", TextStyle.Small)
                    ]
                },
                Files),
            Card("Horizontal strip", "A horizontal ScrollView with fixed-size cards; scroll with a horizontal wheel or trackpad, or drag the scrollbar.", 0,
                Line(8,
                    new Button("Strip start") { Tone = Tone.Ghost, OnClick = strip.ScrollToTop },
                    new Button("Strip end") { Tone = Tone.Ghost, OnClick = strip.ScrollToEnd }),
                strip));
    }

    private Widget FileRow(FileEntry file)
    {
        return new Row
        {
            Gap = 12,
            Flex = 1,
            Children =
            [
                new Label(file.Name, TextStyle.Sized(13)) { Color = Bind(() => _selectedFile == file ? Color.Accent : Color.Text) },
                new Label(file.Kind, TextStyle.Small),
                new Spacer(),
                new Label(file.Size, TextStyle.Small)
            ]
        };
    }

    private static Widget StripCard(int i)
    {
        var hue = (byte)(i * 10);
        return new Column
        {
            Width = 150,
            Height = 96,
            Padding = 12,
            Justify = UIJustify.SpaceBetween,
            Background = Color.Rgba((byte)(60 + hue), (byte)(110 + hue / 2), (byte)(230 - hue)),
            Radius = 10,
            Children =
            [
                new Label($"Card {i + 1}", TextStyle.Sized(16, Color.OnAccent)),
                new Label("Horizontal scroll", TextStyle.Sized(11, Color.OnAccent))
            ]
        };
    }
}
