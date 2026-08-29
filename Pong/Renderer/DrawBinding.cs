using System.Numerics;
using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using Pong.Entities;

namespace Pong.Renderer;

public class DrawBinding(BindGroupLayout layout) : ShaderBinding<SceneObject>(layout)
{
    private readonly ConstantBuffer<Matrix4x4> _dataBuffer = new();

    public override bool RequiresCycling => true;

    protected override void OnCreated()
    {
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

    protected override void OnUpdate(SceneObject target)
    {
        var model = Matrix4x4.CreateScale(target.Size.X, target.Size.Y, 1)
                    * Matrix4x4.CreateFromQuaternion(target.Rotation)
                    * Matrix4x4.CreateTranslation(target.Position.X, target.Position.Y, 1);

        _dataBuffer.Write(model);
    }
}
