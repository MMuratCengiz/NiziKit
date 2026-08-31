using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public enum ImageFit
{
    Stretch,
    Contain,
    Cover
}

public class Image : Widget
{
    public Image()
    {
    }

    public Image(Texture? texture, float width, float height)
    {
        Texture = texture;
        Width = width;
        Height = height;
    }

    public Image(Texture? texture, Vector2 sourceSize)
    {
        Texture = texture;
        SourceSize = sourceSize;
    }

    public Texture? Texture { get; set; }
    public Func<Texture?>? TextureBinding { get; init; }
    public Vector2 SourceSize { get; set; }
    public ImageFit Fit { get; init; } = ImageFit.Stretch;
    public Color? Tint { get; init; }

    public bool HasSourceSize => SourceSize.X > 0 && SourceSize.Y > 0;

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        if (Tint is { } tint)
        {
            decl.OverlayColor = tint.ToClay();
        }

        if (Fit == ImageFit.Cover && !ClipChildren)
        {
            decl.Clip = ClayClipDesc.Create(true, true);
        }

        if (AspectRatio <= 0 && HasSourceSize && Width.IsFit != Height.IsFit)
        {
            decl.AspectRatio = SourceSize.X / SourceSize.Y;
        }
    }

    protected override void BuildContent()
    {
        if (TextureBinding != null)
        {
            Texture = TextureBinding();
        }

        if (Texture == null)
        {
            return;
        }

        var width = ResolveAxis(Width, Padding.Horizontal, true);
        var height = ResolveAxis(Height, Padding.Vertical, false);
        if (float.IsNaN(width) && float.IsNaN(height))
        {
            if (HasSourceSize)
            {
                width = Math.Clamp(SourceSize.X, Width.Min, Width.Max) - Padding.Horizontal;
                height = Math.Clamp(SourceSize.Y, Height.Min, Height.Max) - Padding.Vertical;
            }
            else
            {
                var bounds = Bounds;
                width = Ui.Clay.PixelsToPoints(bounds.Width) - Padding.Horizontal;
                height = Ui.Clay.PixelsToPoints(bounds.Height) - Padding.Vertical;
            }
        }
        else if (float.IsNaN(width))
        {
            width = HasSourceSize
                ? (height + Padding.Vertical) * SourceSize.X / SourceSize.Y - Padding.Horizontal
                : Ui.Clay.PixelsToPoints(Bounds.Width) - Padding.Horizontal;
        }
        else if (float.IsNaN(height))
        {
            height = HasSourceSize
                ? (width + Padding.Horizontal) * SourceSize.Y / SourceSize.X - Padding.Vertical
                : Ui.Clay.PixelsToPoints(Bounds.Height) - Padding.Vertical;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (Fit != ImageFit.Stretch && HasSourceSize)
        {
            var scaleX = width / SourceSize.X;
            var scaleY = height / SourceSize.Y;
            var scale = Fit == ImageFit.Contain ? MathF.Min(scaleX, scaleY) : MathF.Max(scaleX, scaleY);
            width = SourceSize.X * scale;
            height = SourceSize.Y * scale;
        }

        Ui.Clay.Texture(Texture, width, height);
    }

    private float ResolveAxis(Sizing size, float padding, bool horizontal)
    {
        return size.Kind switch
        {
            SizingKind.Fixed => size.Value - padding,
            SizingKind.Fit => float.NaN,
            _ => Ui.Clay.PixelsToPoints(horizontal ? Bounds.Width : Bounds.Height) - padding
        };
    }
}
