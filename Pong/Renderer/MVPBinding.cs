using System.Numerics;
using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;

namespace Pong.Renderer;

public class MvpBinding : ShaderBinding<MvpBinding.Data>
{
    public struct Data
    {
        public Matrix4x4 Model;
        public Matrix4x4 ViewProjection;
    }

    private readonly UniformBuffer<Data> _dataBuffer = new();

    public override BindGroupLayout Layout { get; }

    public override bool RequiresCycling => true;


    public MvpBinding(BindGroupLayout layout)
    {
        Layout = layout;
        for (var i = 0; i < NumBindGroups; i++)
        {
            var dataView = _dataBuffer[i];

            var bg = BindGroups[i];
            bg.BeginUpdate();
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = 0,
                Resource = dataView.Buffer,
                ResourceOffset = dataView.Offset
            });
            bg.EndUpdate();
        }
    }

    protected override void OnUpdate(Data target)
    {
        throw new NotImplementedException();
    }
}