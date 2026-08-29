using System.Numerics;

namespace Pong.Entities;

public struct RenderObject()
{
    private static int _uid = 0;

    // Simple game so we are getting away with it, do not do this in high object count scenarios or you will burn ids
    public readonly int Id = _uid++;

    public string AssetPath = "";

    public Vector4 Color = Vector4.Zero;

    public Vector2 Position = default;

    public Vector2 Size = Vector2.One;

    public Quaternion Rotation = Quaternion.Identity;
}