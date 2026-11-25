namespace UIGallery;

internal static class PatternTexture
{
    public const uint Width = 320;
    public const uint Height = 180;

    public static unsafe Texture Create(bool autoAlign)
    {
        var texture = GraphicsContext.Device.CreateTexture(new TextureDesc
        {
            Width = Width,
            Height = Height,
            Depth = 1,
            ArraySize = 1,
            MipLevels = 1,
            Format = Format.R8G8B8A8Unorm,
            Usage = (uint)(TextureUsageFlagBits.TextureBinding | TextureUsageFlagBits.CopyDst),
            DebugName = StringView.Intern("GalleryPattern")
        });

        var pixels = new byte[Width * Height * 4];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var i = (y * (int)Width + x) * 4;
                var checker = ((x / 20) + (y / 20)) % 2 == 0;
                byte r = checker ? (byte)236 : (byte)60;
                byte g = checker ? (byte)110 : (byte)70;
                byte b = checker ? (byte)80 : (byte)140;
                if (y is > 70 and < 110)
                {
                    var t = x / (float)Width;
                    r = (byte)(255 * t);
                    g = (byte)(200 * (1 - t) + 40);
                    b = 230;
                }

                var dx = x - Width * 0.5f;
                var dy = y - Height * 0.5f;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance is > 54 and < 66)
                {
                    r = g = b = 255;
                }

                if (x < 4 || y < 4 || x >= Width - 4 || y >= Height - 4)
                {
                    r = 255;
                    g = 220;
                    b = 0;
                }

                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = 255;
            }
        }

        using var copy = new BatchResourceCopy(new BatchResourceCopyDesc { Device = GraphicsContext.Device, IssueBarriers = true });
        copy.Begin();
        fixed (byte* data = pixels)
        {
            copy.CopyDataToTexture(new CopyDataToTextureDesc
            {
                DstTexture = texture,
                Data = new ByteArrayView { Elements = (IntPtr)data, NumElements = (ulong)pixels.Length },
                AutoAlign = autoAlign,
                Width = Width,
                Height = Height
            });
        }

        copy.Submit(null);
        GraphicsContext.WaitIdle();
        GraphicsContext.ResourceTracking.TrackTexture(texture, QueueType.Graphics);
        return texture;
    }
}
