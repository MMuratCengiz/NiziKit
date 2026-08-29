using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Buffers;

namespace Pong.Renderer;

public class QuadVertexBuffer : IDisposable
{
    private struct Vertex
    {
        public Vector2 Position;
        public Vector2 TextureCoordinate;
    }

    public const uint VertexCount = 6;
    private const ulong NumBytes = VertexCount * 4 * sizeof(float);

    public DenOfIz.Buffer Buffer { get; } = GraphicsContext.Device.CreateBuffer(
        new BufferDesc
        {
            Format = Format.B8G8R8A8Unorm,
            NumBytes = NumBytes,
            HeapType = HeapType.Gpu
        });

    public void Load(BatchResourceCopy batchCopy)
    {
        Vertex[] vertices =
        [
            new() { Position = new Vector2(-0.5f, -0.5f), TextureCoordinate = new Vector2(0f, 1f) },
            new() { Position = new Vector2(-0.5f,  0.5f), TextureCoordinate = new Vector2(0f, 0f) },
            new() { Position = new Vector2( 0.5f, -0.5f), TextureCoordinate = new Vector2(1f, 1f) },
            new() { Position = new Vector2( 0.5f, -0.5f), TextureCoordinate = new Vector2(1f, 1f) },
            new() { Position = new Vector2(-0.5f,  0.5f), TextureCoordinate = new Vector2(0f, 0f) },
            new() { Position = new Vector2( 0.5f,  0.5f), TextureCoordinate = new Vector2(1f, 0f) },
        ];

        var bytes = MemoryMarshal.AsBytes(vertices).ToArray();

        batchCopy.CopyToGPUBuffer(
            new CopyToGpuBufferDesc
            {
                DstBuffer = Buffer,
                Data = ByteArrayView.Create(bytes)
            });
    }

    public void Dispose()
    {
        Buffer.Dispose();
    }
}