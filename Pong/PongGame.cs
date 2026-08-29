using System.Numerics;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Graphics;
using NiziKit.Inputs;
using Pong.Entities;
using Pong.Movement;
using Pong.Renderer;

namespace Pong;

public class PongGame(GameDesc? desc) : Game(desc)
{
    private QuadRenderer _renderer = null!;
    private Camera _camera = null!;
    private Scene _scene = new();

    private SceneObject _boardTop;
    private SceneObject _boardBottom;
    private SceneObject _ball;
    private PongPhysics _pongPhysics;

    protected override void Load(Game game)
    {
        _camera = Camera.Orthographic(orthographicSize: Window.Height / 2f, aspectRatio: Window.Width / (float)Window.Height);
        _camera.Position = new Vector3(Window.Width / 2f, Window.Height / 2f, 10f);

        _renderer = new QuadRenderer(_scene);
        _renderer.SetCamera(_camera);
        _renderer.AddTexture("Assets/Textures/Fighter.png");
        _renderer.Load();

        const float boardWidth = 200.0f;
        const float boardHeight = 40.0f;

        {
            _boardTop = _scene.NewSceneObject();
            _boardTop.Size = new Vector2(boardWidth, boardHeight);
            _boardTop.Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            _boardTop.Position =
                new Vector2(Window.Width / 2f, Window.Height - _boardTop.Size.Y / 2f);
        }
        {
            _ball = _scene.NewSceneObject();
            _ball.Size = new Vector2(25, 25);
            _ball.Position = new Vector2(Window.Width / 2f, Window.Height / 2f);
            _ball.Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        }
        {
            _boardBottom = _scene.NewSceneObject();
            _boardBottom.Size = new Vector2(boardWidth, boardHeight);
            _boardBottom.Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            _boardBottom.Position =
                new Vector2(Window.Width / 2f, _boardBottom.Size.Y / 2f);
        }
        
        _pongPhysics = new PongPhysics(_boardTop, _boardBottom, _ball, Window.Width, Window.Height);
    }

    protected override void Update(float dt)
    {
        _pongPhysics.Update(dt);

        const float speed = 1000.0f; 
        if (InputSystem.GetKeyState(KeyCode.A) == KeyState.Pressed)
        {
            _boardTop.Position.X -= speed * dt;
        }
        if (InputSystem.GetKeyState(KeyCode.D) == KeyState.Pressed)
        {
            _boardTop.Position.X += speed * dt;
        }
        if (InputSystem.GetKeyState(KeyCode.Left) == KeyState.Pressed)
        {
            _boardBottom.Position.X -= speed * dt;
        }
        if (InputSystem.GetKeyState(KeyCode.Right) == KeyState.Pressed)
        {
            _boardBottom.Position.X += speed * dt;
        }
    }

    protected override void OnResize(uint width, uint height)
    {
        if (height > 0)
        {
            _camera.OrthographicSize = height / 2f;
            _camera.AspectRatio = width / (float)height;
            _camera.Position = new Vector3(width / 2f, height / 2f, 10f);
        }
    }

    protected override void Render(float dt)
    {
        _renderer.Render(dt);
    }

    protected override void OnShutdown()
    {
        _renderer.Dispose();
    }
}