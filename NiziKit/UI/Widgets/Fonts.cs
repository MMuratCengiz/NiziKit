using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public static class Fonts
{
    private sealed class FontEntry : IDisposable
    {
        public required Font Font;
        public FontAsset? Asset;
        public FontAssetReader? AssetReader;
        public DenOfIz.BinaryReader? Reader;
        public GCHandle Data;

        public void Dispose()
        {
            Font.Dispose();
            Asset?.Dispose();
            AssetReader?.Dispose();
            Reader?.Dispose();
            if (Data.IsAllocated)
            {
                Data.Free();
            }
        }
    }

    private static FontLibrary? _library;
    private static readonly Dictionary<ushort, FontEntry> Entries = new();

    public const ushort DefaultFontId = 0;

    public static FontLibrary Library => _library ??= new FontLibrary();

    public static bool IsRegistered(ushort fontId)
    {
        return Entries.ContainsKey(fontId);
    }

    public static bool TryGet(ushort fontId, out Font font)
    {
        if (Entries.TryGetValue(fontId, out var entry))
        {
            font = entry.Font;
            return true;
        }

        font = null!;
        return false;
    }

    public static Font Load(string path, ushort fontId)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Font file not found: {path}", path);
        }

        if (path.EndsWith(".dzfont", StringComparison.OrdinalIgnoreCase))
        {
            return LoadAsset(File.ReadAllBytes(path), fontId);
        }

        if (path.EndsWith(".dzfont.br", StringComparison.OrdinalIgnoreCase))
        {
            return LoadCompressedAsset(File.OpenRead(path), fontId);
        }

        Font font;
        using (var pinned = new StringView.Pinned(Path.GetFullPath(path)))
        {
            font = Library.LoadFontFromPath(pinned);
        }

        if ((ulong)font == 0)
        {
            throw new InvalidOperationException($"Failed to import font: {path}");
        }

        Register(fontId, new FontEntry { Font = font });
        return font;
    }

    public static Font LoadEmbedded(Assembly assembly, string resourceName, ushort fontId)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in {assembly.GetName().Name}");
        }

        if (resourceName.EndsWith(".dzfont.br", StringComparison.OrdinalIgnoreCase))
        {
            return LoadCompressedAsset(stream, fontId);
        }

        if (resourceName.EndsWith(".dzfont", StringComparison.OrdinalIgnoreCase))
        {
            var data = new byte[stream.Length];
            stream.ReadExactly(data);
            return LoadAsset(data, fontId);
        }

        throw new InvalidOperationException($"Unsupported embedded font format '{resourceName}'. Embed a .dzfont or .dzfont.br produced by Fonts.Export.");
    }

    public static Font LoadCompressedAsset(Stream compressed, ushort fontId)
    {
        using var owned = compressed;
        using var brotli = new BrotliStream(owned, CompressionMode.Decompress);
        using var buffer = new MemoryStream();
        brotli.CopyTo(buffer);
        return LoadAsset(buffer.ToArray(), fontId);
    }

    public static Font LoadAsset(byte[] assetData, ushort fontId)
    {
        var handle = GCHandle.Alloc(assetData, GCHandleType.Pinned);
        var view = new ByteArrayView { Elements = handle.AddrOfPinnedObject(), NumElements = (ulong)assetData.Length };
        var reader = DenOfIz.BinaryReader.CreateFromData(view, new BinaryReaderDesc { NumBytes = 0 });
        var assetReader = new FontAssetReader(new FontAssetReaderDesc { Reader = reader });
        var asset = assetReader.Read();
        var font = Library.LoadFontFromDesc(new FontDesc { FontAsset = asset });
        if ((ulong)font == 0)
        {
            assetReader.Dispose();
            reader.Dispose();
            handle.Free();
            throw new InvalidOperationException("Failed to load font asset");
        }

        Register(fontId, new FontEntry { Font = font, Asset = asset, AssetReader = assetReader, Reader = reader, Data = handle });
        return font;
    }

    public static Font ImportTtf(string ttfPath, ushort fontId, params UnicodeRange[] ranges)
    {
        return LoadAsset(ImportTtfToAsset(ttfPath, ranges), fontId);
    }

    public static void Export(string ttfPath, string outputPath, params UnicodeRange[] ranges)
    {
        var asset = ImportTtfToAsset(ttfPath, ranges);
        using var output = File.Create(outputPath);
        if (outputPath.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
        {
            using var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, true);
            brotli.Write(asset);
        }
        else
        {
            output.Write(asset);
        }
    }

    public static byte[] ImportTtfToAsset(string ttfPath, params UnicodeRange[] ranges)
    {
        if (!File.Exists(ttfPath))
        {
            throw new FileNotFoundException($"Font file not found: {ttfPath}", ttfPath);
        }

        var handle = ranges.Length > 0 ? GCHandle.Alloc(ranges, GCHandleType.Pinned) : default;
        using var container = new BinaryContainer();
        using var source = new StringView.Pinned(Path.GetFullPath(ttfPath));
        using var importer = new FontImporter();
        try
        {
            var desc = new FontImportDesc
            {
                SourceFilePath = source,
                InitialFontSize = 32,
                AtlasWidth = 2048,
                AtlasHeight = 2048,
                TargetContainer = container
            };
            if (ranges.Length > 0)
            {
                desc.CustomRanges = UnicodeRangeArray.FromPinned(handle, ranges.Length);
            }

            var result = importer.Import(desc);
            if (result.ResultCode != ImporterResultCode.Success)
            {
                throw new InvalidOperationException($"Font import failed for {ttfPath}: {result.ErrorMessage}");
            }
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        var view = container.GetData();
        var data = new byte[view.NumElements];
        Marshal.Copy(view.Elements, data, 0, data.Length);
        return data;
    }

    public static void Register(ushort fontId, Font font)
    {
        Register(fontId, new FontEntry { Font = font });
    }

    private static void Register(ushort fontId, FontEntry entry)
    {
        if (Entries.Remove(fontId, out var existing))
        {
            Ui.Clay.RemoveFont(fontId);
            if (!ReferenceEquals(existing.Font, entry.Font))
            {
                existing.Dispose();
            }
        }

        Ui.Clay.AddFont(fontId, entry.Font);
        Entries[fontId] = entry;
    }

    public static void Unload(ushort fontId)
    {
        if (!Entries.Remove(fontId, out var entry))
        {
            return;
        }

        Ui.Clay.RemoveFont(fontId);
        entry.Dispose();
    }

    public static void Shutdown()
    {
        foreach (var entry in Entries.Values)
        {
            entry.Dispose();
        }

        Entries.Clear();
        _library?.Dispose();
        _library = null;
    }
}
