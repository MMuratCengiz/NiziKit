using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using NiziKit.Graphics.Resources;

namespace Pong.Renderer;

public class AlbedoBinding(BindGroupLayout layout) : ShaderBinding<AlbedoBinding.Data>
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


    protected override void OnUpdate(Data target)
    {
        var hash = HashCode.Combine(
            target.Albedo,
            target.Sampler);

        if (hash == _lastHash)
        {
            return;
        }
        _lastHash = hash;
        
        var bg = BindGroups[0];
        bg.BeginUpdate();
        bg.SrvTexture(0, target.Albedo);
        bg.Sampler(1, target.Sampler);
        bg.EndUpdate();
    }
}