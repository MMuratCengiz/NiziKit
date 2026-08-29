using System.Numerics;

namespace Pong.Entities;

public class SceneObject(int id)
{
    public readonly int Id = id;

    public string AssetPath = "";

    public Vector4 Color = Vector4.Zero;

    public Vector2 Position = default;

    public Vector2 Size = Vector2.One;

    public Quaternion Rotation = Quaternion.Identity;
}