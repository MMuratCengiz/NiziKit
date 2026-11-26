using System.Reflection;
using DenOfIz;

namespace NiziKit.UI;

public enum FontWeight
{
    Regular,
    Medium,
    Bold
}

public static class Fonts
{
    private static readonly Dictionary<ushort, Font> Entries = new();
    private static readonly List<Font> Removed = new();
    private static readonly Dictionary<(ushort, FontWeight), ushort> Variants = new();
    private static readonly Dictionary<(ushort, FontWeight, bool), (ushort Id, ulong Base)> Synthesized = new();
    private static ushort _nextSynthesizedId = 63;

    public const ushort DefaultFontId = 0;

    public static bool IsRegistered(ushort fontId)
    {
        return Entries.ContainsKey(fontId);
    }

    public static bool TryGet(ushort fontId, out Font font)
    {
        return Entries.TryGetValue(fontId, out font!);
    }

    public static Font Load(string path, ushort fontId, uint faceIndex = 0)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Font file not found: {path}", path);
        }

        using var pinned = new StringView.Pinned(Path.GetFullPath(path));
        return RegisterLoaded(Font.CreateFromPath(pinned, faceIndex), fontId, path);
    }

    public static Font Load(ReadOnlySpan<byte> data, ushort fontId, uint faceIndex = 0)
    {
        return RegisterLoaded(Font.Create(data, faceIndex), fontId, "font data");
    }

    public static Font LoadEmbedded(Assembly assembly, string resourceName, ushort fontId)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in {assembly.GetName().Name}");
        var data = new byte[stream.Length];
        stream.ReadExactly(data);
        return RegisterLoaded(Font.Create(data), fontId, resourceName);
    }

    public static void Register(ushort fontId, Font font)
    {
        Ui.Native.AddFont(fontId, font);
        if (Entries.TryGetValue(fontId, out var existing) && !ReferenceEquals(existing, font))
        {
            Release(existing);
        }

        Entries[fontId] = font;
    }

    public static void Unload(ushort fontId)
    {
        if (!Entries.Remove(fontId, out var font))
        {
            return;
        }

        if (Ui.IsInitialized)
        {
            Ui.Native.RemoveFont(fontId);
        }

        Release(font);
    }

    /// <summary>
    /// Draws <paramref name="fontId"/> at <paramref name="weight"/> with the font registered as <paramref name="variantId"/>, e.g. the bold face of
    /// a family loaded from its own file. Applies to <see cref="TextStyle.Weight"/> and RichText <c>b</c> and <c>strong</c>; without a variant
    /// those are synthesized. The mapping is between ids, so it survives loading another font into either id.
    /// </summary>
    public static void SetVariant(ushort fontId, FontWeight weight, ushort variantId)
    {
        if (weight == FontWeight.Regular)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "The regular face is the font itself");
        }

        Variants[(fontId, weight)] = variantId;
    }

    // Faces drawn from a regular one, given the top font ids so they stay clear of the ones apps pick.
    internal static ushort Styled(ushort fontId, FontWeight weight, bool italic)
    {
        if (weight != FontWeight.Regular && Variants.TryGetValue((fontId, weight), out var variant))
        {
            fontId = variant;
            weight = FontWeight.Regular;
        }

        if (weight == FontWeight.Regular && !italic)
        {
            return fontId;
        }

        var registered = Entries.GetValueOrDefault(fontId);
        var handle = registered != null ? (ulong)registered : 0;
        var key = (fontId, weight, italic);
        if (Synthesized.TryGetValue(key, out var entry) && entry.Base == handle)
        {
            return entry.Id;
        }

        var id = entry.Id != 0 ? entry.Id : _nextSynthesizedId--;
        var bold = weight switch { FontWeight.Medium => 0.018f, FontWeight.Bold => 0.035f, _ => 0 };
        var font = Font.CreateSynthetic(registered ?? Font.GetDefault(), new FontSynthesisDesc { Bold = bold, Slant = italic ? 0.2f : 0 });
        Register(id, font);
        Synthesized[key] = (id, handle);
        return id;
    }

    private static Font RegisterLoaded(Font font, ushort fontId, string source)
    {
        if ((ulong)font == 0)
        {
            throw new InvalidOperationException($"Failed to load font: {source}");
        }

        Register(fontId, font);
        return font;
    }

    // The UI borrows fonts; a replaced or removed font stays alive until the UI applies the change in its next BeginFrame.
    internal static void ReleaseRemoved()
    {
        foreach (var font in Removed)
        {
            font.Dispose();
        }

        Removed.Clear();
    }

    private static void Release(Font font)
    {
        if (Ui.IsInitialized)
        {
            Removed.Add(font);
        }
        else
        {
            font.Dispose();
        }
    }

    internal static void Shutdown()
    {
        foreach (var font in Entries.Values)
        {
            font.Dispose();
        }

        ReleaseRemoved();
        Entries.Clear();
        Variants.Clear();
        Synthesized.Clear();
        _nextSynthesizedId = 63;
    }
}
