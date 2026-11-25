using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Image : Widget
{
    public Image()
    {
    }

    public Image(Texture? texture, UIImageFit fit = UIImageFit.Fill)
    {
        Texture = texture;
        Fit = fit;
    }

    public Texture? Texture { get; set; }

    public Func<Texture?>? Source { get; set; }

    public UIImageFit Fit { get; set; }

    public UIColor Tint { get; set; }

    public Vector4 Uv { get; set; }

    public UIColor Background { get; set; }

    public float CornerRadius { get; set; }

    public UIColor Overlay { get; set; }

    protected override void OnBuild()
    {
        ref var desc = ref Desc;
        desc.Image.Texture = Source?.Invoke() ?? Texture;
        desc.Image.Fit = Fit;
        desc.Image.Tint = Tint;
        desc.Image.UV = Uv;
        desc.Style.BackgroundColor = Background;
        desc.Style.CornerRadius = UICorners.All(CornerRadius);
        desc.Style.OverlayColor = Overlay;
        Ui.Open(in desc);
        Ui.Close();
    }
}
