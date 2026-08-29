using System.Numerics;

namespace Pong.Renderer;

public struct Camera
{
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 ViewProjectionMatrix;
}