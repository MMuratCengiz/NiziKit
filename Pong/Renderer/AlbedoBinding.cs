using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using NiziKit.Graphics.Resources;

namespace Pong.Renderer;

public class AlbedoBinding(BindGroupLayout layout, TextureStore textureStore) : ShaderBinding<RenderObject>
{
    public struct Data
    {
        public Texture Albedo;
        public Sampler Sampler;
    }

    private readonly Sampler _sampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
    {
        AddressModeU = SamplerAddressMode.Repeat,
        AddressModeV = SamplerAddressMode.Repeat,
        AddressModeW = SamplerAddressMode.Repeat,
        MinFilter = Filter.Linear,
        MagFilter = Filter.Linear,
        MipmapMode = MipmapMode.Linear
    });

    private int _lastHash;

    public override BindGroupLayout Layout { get; } = layout;


    protected override void OnUpdate(RenderObject target)
    {
        var hash = HashCode.Combine(target.AssetPath, target.Position);

        if (hash == _lastHash)
        {
            return;
        }
        _lastHash = hash;
        
        var bg = BindGroups[0];
        bg.BeginUpdate();
        bg.SrvTexture(0, textureStore.GetTexture(target.AssetPath));
        bg.Sampler(1, _sampler);
        bg.EndUpdate();
    }
}