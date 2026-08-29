using System;
using System.Numerics;
using Pong.Entities;

namespace Pong.Movement;

public enum PongScore
{
    None,
    Top,
    Bottom
}

public sealed class PongPhysics(
    SceneObject boardTop,
    SceneObject boardBottom,
    SceneObject ball,
    float width,
    float height)
{
    private const float InitialSpeed = 250f;
    private const float SpeedIncrease = 15f;
    private const float MaxSpeed = 900f;
    private const float MaxBounceAngle = 60f * MathF.PI / 180f;

    private readonly Random _random = new();

    private Vector2 _direction = Vector2.Zero;
    private float _speed = InitialSpeed;

    public PongScore Update(float deltaTime)
    {
        if (_direction == Vector2.Zero)
        {
            Reset();
        }

        const float maxStepDistance = 5f;
        var totalDistance = _speed * deltaTime;
        var steps = Math.Max(1, (int)MathF.Ceiling(totalDistance / maxStepDistance));
        var stepDelta = deltaTime / steps;

        var ballHalfHeight = ball.Size.Y / 2f;

        for (var i = 0; i < steps; i++)
        {
            ball.Position += _direction * _speed * stepDelta;

            HandleSideWalls();

            if (_direction.Y > 0f && CheckPaddleCollision(boardTop, isTop: true))
            {
                Bounce(boardTop, isTop: true);
            }
            else if (_direction.Y < 0f && CheckPaddleCollision(boardBottom, isTop: false))
            {
                Bounce(boardBottom, isTop: false);
            }

            if (ball.Position.Y + ballHalfHeight < 0f)
            {
                Reset();
                return PongScore.Top;
            }

            if (ball.Position.Y - ballHalfHeight > height)
            {
                Reset();
                return PongScore.Bottom;
            }
        }

        return PongScore.None;
    }

    public void Reset()
    {
        ball.Position = new Vector2(width / 2f, height / 2f);
        _speed = InitialSpeed;

        var horizontal = RandomRange(-0.8f, 0.8f);

        if (MathF.Abs(horizontal) < 0.2f)
        {
            horizontal = horizontal < 0f ? -0.2f : 0.2f;
        }

        var vertical = _random.Next(2) == 0 ? -1f : 1f;

        _direction = Vector2.Normalize(
            new Vector2(horizontal, vertical)
        );
    }

    private void HandleSideWalls()
    {
        var halfBallWidth = ball.Size.X / 2f;

        if (ball.Position.X - halfBallWidth < 0f)
        {
            ball.Position.X = halfBallWidth;
            _direction.X = MathF.Abs(_direction.X);
        }
        else if (ball.Position.X + halfBallWidth > width)
        {
            ball.Position.X = width - halfBallWidth;
            _direction.X = -MathF.Abs(_direction.X);
        }
    }

    private bool CheckPaddleCollision(SceneObject paddle, bool isTop)
    {
        var paddleHalfWidth = paddle.Size.X / 2f;
        var paddleHalfHeight = paddle.Size.Y / 2f;

        var ballHalfWidth = ball.Size.X / 2f;
        var ballHalfHeight = ball.Size.Y / 2f;

        var overlapsX =
            ball.Position.X + ballHalfWidth >= paddle.Position.X - paddleHalfWidth &&
            ball.Position.X - ballHalfWidth <= paddle.Position.X + paddleHalfWidth;

        if (!overlapsX)
        {
            return false;
        }

        if (isTop)
        {
            var paddleBottomFace = paddle.Position.Y - paddleHalfHeight;
            var paddleTopFace = paddle.Position.Y + paddleHalfHeight;
            var ballTop = ball.Position.Y + ballHalfHeight;
            var ballBottom = ball.Position.Y - ballHalfHeight;

            return ballTop >= paddleBottomFace && ballBottom <= paddleTopFace;
        }
        else
        {
            var paddleTopFace = paddle.Position.Y + paddleHalfHeight;
            var paddleBottomFace = paddle.Position.Y - paddleHalfHeight;
            var ballBottom = ball.Position.Y - ballHalfHeight;
            var ballTop = ball.Position.Y + ballHalfHeight;

            return ballBottom <= paddleTopFace && ballTop >= paddleBottomFace;
        }
    }

    private void Bounce(SceneObject paddle, bool isTop)
    {
        var paddleHalfWidth = paddle.Size.X / 2f;
        var paddleHalfHeight = paddle.Size.Y / 2f;
        var ballHalfHeight = ball.Size.Y / 2f;

        if (isTop)
        {
            ball.Position.Y = paddle.Position.Y - paddleHalfHeight - ballHalfHeight;
        }
        else
        {
            ball.Position.Y = paddle.Position.Y + paddleHalfHeight + ballHalfHeight;
        }

        var hitOffset = (ball.Position.X - paddle.Position.X) / paddleHalfWidth;
        hitOffset = Math.Clamp(hitOffset, -1f, 1f);

        var bounceAngle = hitOffset * MaxBounceAngle;
        var verticalDir = isTop ? -1f : 1f;

        _direction = Vector2.Normalize(
            new Vector2(
                MathF.Sin(bounceAngle),
                verticalDir * MathF.Cos(bounceAngle)
            )
        );

        _speed = MathF.Min(
            _speed + SpeedIncrease,
            MaxSpeed
        );
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}