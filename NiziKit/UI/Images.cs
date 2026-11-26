using System.Collections.Concurrent;
using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Core;
using NiziKit.Graphics;

namespace NiziKit.UI;

// Images referenced by source: a file path, an http(s) URL or a data: URI. Loading starts on first use; bytes are fetched and decoded off the
// main thread and uploaded at the start of the next UI frame.
internal static class Images
{
    internal sealed class Entry
    {
        public Texture? Texture;
        public float Width;
        public float Height;
        public bool Failed;
    }

    private static readonly Dictionary<string, Entry> Entries = new();
    private static readonly ConcurrentQueue<(Entry Entry, TextureData Data)> Decoded = new();
    private static readonly Lazy<HttpClient> Http = new(() => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });

    internal static bool Loading => !Decoded.IsEmpty || Entries.Values.Any(e => e.Texture == null && !e.Failed);

    internal static Entry Get(string source)
    {
        if (!Entries.TryGetValue(source, out var entry))
        {
            entry = new Entry();
            Entries[source] = entry;
            _ = Task.Run(() => Load(source, entry));
        }

        return entry;
    }

    private static async Task Load(string source, Entry entry)
    {
        try
        {
            var bytes = await Read(source);
            var data = Decode(bytes);
            if (data == null)
            {
                entry.Failed = true;
                return;
            }

            Decoded.Enqueue((entry, data));
        }
        catch (Exception e)
        {
            Log.Get("NiziKit.UI").LogWarning("Image {Source} could not be loaded: {Message}", Shorten(source), e.Message);
            entry.Failed = true;
        }
    }

    private static async Task<byte[]> Read(string source)
    {
        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = source.IndexOf(',');
            if (comma < 0 || !source.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("only base64 data URIs are supported");
            }

            return Convert.FromBase64String(source[(comma + 1)..]);
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return await Http.Value.GetByteArrayAsync(uri);
        }

        var path = uri is { IsFile: true } ? uri.LocalPath : source;
        if (!Path.IsPathRooted(path) && !File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        return await File.ReadAllBytesAsync(path);
    }

    private static unsafe TextureData? Decode(byte[] bytes)
    {
        fixed (byte* data = bytes)
        {
            var view = new ByteArrayView { Elements = (IntPtr)data, NumElements = (ulong)bytes.Length };
            var desc = new TextureCreateFromDataDesc { Data = view, Extension = TextureData.IdentifyTextureFormat(in view) };
            var texture = TextureData.CreateFromData(in desc);
            if ((ulong)texture != 0 && texture.GetWidth() > 0 && texture.GetHeight() > 0)
            {
                return texture;
            }

            texture.Dispose();
            return null;
        }
    }

    internal static void Upload()
    {
        if (Decoded.IsEmpty)
        {
            return;
        }

        var loaded = new List<(Entry Entry, TextureData Data, Texture Texture)>();
        using (var copy = new BatchResourceCopy(new BatchResourceCopyDesc { Device = GraphicsContext.Device, IssueBarriers = true }))
        {
            copy.Begin();
            while (Decoded.TryDequeue(out var item))
            {
                var data = item.Data;
                var texture = GraphicsContext.Device.CreateTexture(new TextureDesc
                {
                    HeapType = HeapType.Gpu,
                    Usage = (uint)(TextureUsageFlagBits.TextureBinding | TextureUsageFlagBits.CopyDst),
                    Width = data.GetWidth(),
                    Height = data.GetHeight(),
                    Depth = Math.Max(1u, data.GetDepth()),
                    ArraySize = Math.Max(1u, data.GetArraySize()),
                    MipLevels = Math.Max(1u, data.GetMipLevels()),
                    Format = data.GetFormat(),
                    DebugName = StringView.Intern("RichTextImage")
                });
                copy.LoadTextureFromData(new LoadTextureFromDataDesc { TextureData = data, DstTexture = texture });
                loaded.Add((item.Entry, data, texture));
            }

            copy.Submit(null);
            GraphicsContext.WaitIdle();
        }

        foreach (var (entry, data, texture) in loaded)
        {
            GraphicsContext.ResourceTracking.TrackTexture(texture, QueueType.Graphics);
            entry.Width = data.GetWidth();
            entry.Height = data.GetHeight();
            entry.Texture = texture;
            data.Dispose();
        }
    }

    internal static void Shutdown()
    {
        while (Decoded.TryDequeue(out var item))
        {
            item.Data.Dispose();
        }

        foreach (var entry in Entries.Values)
        {
            if (entry.Texture is { } texture)
            {
                GraphicsContext.ResourceTracking.UntrackTexture(texture);
                texture.Dispose();
            }
        }

        Entries.Clear();
    }

    private static string Shorten(string source)
    {
        return source.Length > 80 ? source[..77] + "..." : source;
    }
}
