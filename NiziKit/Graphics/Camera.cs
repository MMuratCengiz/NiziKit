using System.Numerics;

namespace NiziKit.Graphics;

public enum ProjectionType
{
    Perspective,
    Orthographic
}

public sealed class Camera
{
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.Identity;
    private ProjectionType _projection = ProjectionType.Perspective;
    private float _fieldOfView = MathF.PI / 3f;
    private float _orthographicSize = 1f;
    private float _aspectRatio = 16f / 9f;
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000f;

    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;
    private Matrix4x4 _viewProjectionMatrix;
    private Matrix4x4 _inverseViewProjectionMatrix;
    private bool _dirty = true;

    public static Camera Perspective(float fieldOfView, float aspectRatio, float nearPlane = 0.1f,
        float farPlane = 1000f) =>
        new()
        {
            Projection = ProjectionType.Perspective,
            FieldOfView = fieldOfView,
            AspectRatio = aspectRatio,
            NearPlane = nearPlane,
            FarPlane = farPlane
        };

    /// <param name="orthographicSize">Half of the visible height in world units.</param>
    public static Camera Orthographic(float orthographicSize, float aspectRatio, float nearPlane = 0.1f,
        float farPlane = 1000f) =>
        new()
        {
            Projection = ProjectionType.Orthographic,
            OrthographicSize = orthographicSize,
            AspectRatio = aspectRatio,
            NearPlane = nearPlane,
            FarPlane = farPlane
        };

    public Vector3 Position
    {
        get => _position;
        set => Set(ref _position, value);
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set => Set(ref _rotation, value);
    }

    public ProjectionType Projection
    {
        get => _projection;
        set => Set(ref _projection, value);
    }

    /// <summary>Vertical field of view in radians. Perspective only.</summary>
    public float FieldOfView
    {
        get => _fieldOfView;
        set => Set(ref _fieldOfView, value);
    }

    /// <summary>Half of the visible height in world units. Orthographic only.</summary>
    public float OrthographicSize
    {
        get => _orthographicSize;
        set => Set(ref _orthographicSize, value);
    }

    /// <summary>Width / height. Update this from your resize handler.</summary>
    public float AspectRatio
    {
        get => _aspectRatio;
        set => Set(ref _aspectRatio, value);
    }

    public float NearPlane
    {
        get => _nearPlane;
        set => Set(ref _nearPlane, value);
    }

    public float FarPlane
    {
        get => _farPlane;
        set => Set(ref _farPlane, value);
    }

    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, _rotation);
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, _rotation);
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, _rotation);

    public Matrix4x4 ViewMatrix
    {
        get
        {
            Recompute();
            return _viewMatrix;
        }
    }

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            Recompute();
            return _projectionMatrix;
        }
    }

    public Matrix4x4 ViewProjectionMatrix
    {
        get
        {
            Recompute();
            return _viewProjectionMatrix;
        }
    }

    public Matrix4x4 InverseViewProjectionMatrix
    {
        get
        {
            Recompute();
            return _inverseViewProjectionMatrix;
        }
    }

    /// <summary>Rotates the camera so its forward axis points at <paramref name="target"/>.</summary>
    public void LookAt(Vector3 target, Vector3? up = null)
    {
        var forward = target - _position;
        if (forward.LengthSquared() < float.Epsilon)
        {
            return;
        }

        var world = Matrix4x4.CreateWorld(Vector3.Zero, Vector3.Normalize(forward), up ?? Vector3.UnitY);
        Rotation = Quaternion.CreateFromRotationMatrix(world);
    }

    /// <summary>World position to normalized device coordinates (x, y in [-1, 1], z in [0, 1]).</summary>
    public Vector3 Project(Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), ViewProjectionMatrix);
        return new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    }

    /// <summary>Normalized device coordinates back to a world position. Useful for mouse picking.</summary>
    public Vector3 Unproject(Vector3 ndc)
    {
        var world = Vector4.Transform(new Vector4(ndc, 1f), InverseViewProjectionMatrix);
        return new Vector3(world.X, world.Y, world.Z) / world.W;
    }

    private void Set<T>(ref T field, T value) where T : struct
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        _dirty = true;
    }

    private void Recompute()
    {
        if (!_dirty)
        {
            return;
        }

        _viewMatrix = Matrix4x4.CreateTranslation(-_position) *
                      Matrix4x4.CreateFromQuaternion(Quaternion.Conjugate(_rotation));

        _projectionMatrix = _projection == ProjectionType.Perspective
            ? Matrix4x4.CreatePerspectiveFieldOfView(_fieldOfView, _aspectRatio, _nearPlane, _farPlane)
            : Matrix4x4.CreateOrthographic(2f * _orthographicSize * _aspectRatio, 2f * _orthographicSize, _nearPlane,
                _farPlane);

        _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
        Matrix4x4.Invert(_viewProjectionMatrix, out _inverseViewProjectionMatrix);
        _dirty = false;
    }
}