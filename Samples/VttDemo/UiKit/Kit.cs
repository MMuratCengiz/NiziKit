using VttDemo.Model;

namespace VttDemo.UiKit;

public static class Palette
{
    public static readonly Color White = Color.Hex(0xFFFFFFFF);
    public static readonly Color Good = Color.Hex(0x4CB860FF);
    public static readonly Color Warn = Color.Hex(0xE0A33AFF);
    public static readonly Color Bad = Color.Hex(0xD9484BFF);
    public static readonly Color TempHp = Color.Hex(0x5AA9E6FF);

    public static Color Hp(float fraction)
    {
        return fraction > 0.5f ? Good : fraction > 0.25f ? Warn : Bad;
    }
}

// App-level building blocks shared by the views. Each returns a widget built once; anything that changes is bound.
public static class Kit
{
    private static readonly Style Tile = new()
    {
        Padding = (6, 7),
        Gap = 2,
        Align = UIAlign.Center,
        Background = Color.SurfaceAlt,
        Border = Color.Border,
        Radius = 6
    };

    private static readonly Style RollTile = Tile with { HoverBackground = Color.Hover, HoverBorder = Color.Accent };

    public static readonly Style Card = new()
    {
        Padding = 12,
        Gap = 8,
        Background = Color.SurfaceAlt,
        Border = Color.Border,
        Radius = 8
    };

    public static Button IconButton(string icon, string? tooltip, Action onClick)
    {
        return new Button { Icon = icon, Tone = Tone.Ghost, Tooltip = tooltip, OnClick = onClick };
    }

    public static Widget SectionHeader(string text)
    {
        return new Row
        {
            Gap = 10,
            Margin = (0, 6, 0, 0),
            Children = [new Label(text, TextStyle.Caption), new Row { Flex = 1, Height = 1, Background = Color.Border }]
        };
    }

    public static Row Badge(string text, Color background, Color foreground)
    {
        return new Row { Padding = (7, 2), Background = background, Radius = 4, Children = [new Label(text, TextStyle.Sized(11, foreground))] };
    }

    public static Row Badge(TextSource text, Color background, Color foreground)
    {
        return new Row { Padding = (7, 2), Background = background, Radius = 4, Children = [new Label(text, TextStyle.Sized(11, foreground))] };
    }

    public static Meter HpBar(Character character, float height)
    {
        return new Meter
        {
            Height = height,
            Value = Bind(() => character.MaxHp > 0 ? character.Hp / (float)character.MaxHp : 0),
            Color = Bind(() => Palette.Hp(character.MaxHp > 0 ? character.Hp / (float)character.MaxHp : 0)),
            Secondary = Bind(() => character.MaxHp > 0 ? character.TempHp / (float)character.MaxHp : 0),
            SecondaryColor = Palette.TempHp
        };
    }

    public static Widget Portrait(Character character, float size)
    {
        return new Row
        {
            Width = size,
            Height = size,
            Justify = UIJustify.Center,
            Background = character.Color,
            Border = new Border(Palette.White.WithAlpha(70), Math.Max(1, size / 24)),
            Radius = size * 0.5f,
            Overlay = Bind(() => character.IsDown ? Color.Hex(0x000000A0) : Color.Transparent),
            Children = [new Label(character.Initials, TextStyle.Sized(size * 0.38f, Palette.White))]
        };
    }

    public static Widget StatTile(string label, TextSource value, string caption, float width, Action? roll = null, string? tooltip = null)
    {
        var tile = new Column { Style = roll != null ? RollTile : Tile, Width = width, Tooltip = tooltip, OnClick = roll };
        tile.Children =
        [
            new Label(label, TextStyle.Caption),
            new Label(value, TextStyle.Heading),
            new Label(caption, TextStyle.Small)
            {
                Visible = caption.Length > 0,
                Color = Bind(() => roll != null && tile.IsHovered ? Color.Accent : Color.TextMuted)
            }
        ];
        return tile;
    }

    public static Widget KeyValue(string key, string value)
    {
        return new Column { Gap = 1, Children = [new Label(key, TextStyle.Caption), new Paragraph(value, TextStyle.Sized(12))] };
    }

    public static Widget Empty(string icon, string title, TextSource detail)
    {
        return new Column
        {
            Flex = 1,
            Justify = UIJustify.Center,
            Align = UIAlign.Center,
            Gap = 8,
            Padding = 24,
            Children =
            [
                new Icon(icon) { Size = 28, Color = Color.TextDisabled },
                new Label(title, TextStyle.Muted with { Size = 15 }),
                new Paragraph(detail, TextStyle.Hint) { Align = UITextAlign.Center }
            ]
        };
    }
}

// Cached number strings so bound labels don't allocate.
public static class Fmt
{
    private const int SignedMin = -50;
    private static readonly string[] SignedCache = Enumerable.Range(SignedMin, 151).Select(i => i >= 0 ? $"+{i}" : $"-{-i}").ToArray();
    private static readonly string[] IntCache = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArray();

    public static string Sign(int value)
    {
        var index = value - SignedMin;
        return index >= 0 && index < SignedCache.Length ? SignedCache[index] : value >= 0 ? $"+{value}" : $"-{-value}";
    }

    public static string Int(int value)
    {
        return value >= 0 && value < IntCache.Length ? IntCache[value] : value.ToString();
    }
}
