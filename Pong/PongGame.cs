using System.Numerics;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Graphics;
using NiziKit.UI;
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

    private int _topScore;
    private int _bottomScore;

    protected override void Load(Game game)
    {
        _camera = Camera.Orthographic(orthographicSize: Window.Height / 2f, aspectRatio: Window.Width / (float)Window.Height);
        _camera.Position = new Vector3(Window.Width / 2f, Window.Height / 2f, 10f);

        _renderer = new QuadRenderer(_scene);
        _renderer.DrawUi = DrawUi;
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
        switch (_pongPhysics.Update(dt))
        {
            case PongScore.Top:
                _topScore++;
                break;
            case PongScore.Bottom:
                _bottomScore++;
                break;
        }

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

    private static readonly UiColor TableBackground = UiColor.Rgba(20, 20, 30, 140);
    private static readonly UiColor TableBorder = UiColor.Rgba(110, 110, 130, 180);
    private static readonly UiColor HeaderText = UiColor.Rgb(180, 180, 200);
    private static readonly UiColor CellText = UiColor.Rgb(255, 255, 255);

    private const float LabelCellWidth = 70;
    private const float ScoreCellWidth = 40;
    private const float CellHeight = 26;

    void DrawUi()
    {
        using (Ui.Column().Padding(12).AlignChildren(ClayAlignmentX.Right, ClayAlignmentY.Center).Open())
        {
            using (Ui.Column()
                       .Fit()
                       .Background(TableBackground)
                       .Border(TableBorder, 1, betweenChildren: 1)
                       .CornerRadius(4)
                       .Open())
            {
                ScoreRow("Top", _topScore);
                ScoreRow("Bottom", _bottomScore);
            }
        }
    }

    private static void ScoreRow(string label, int score)
    {
        using (Ui.Row().Fit().Border(TableBorder, 0, betweenChildren: 1).Open())
        {
            TableCell(label, LabelCellWidth, HeaderText);
            TableCell($"{score}", ScoreCellWidth, CellText);
        }
    }

    private static void TableCell(string text, float width, UiColor color)
    {
        using (Ui.Element().Fixed(width, CellHeight).CenterChildren().Open())
        {
            Ui.Text(text, 14, color);
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

    protected override void OnEvent(ref Event ev)
    {
        Ui.HandleEvent(ref ev);
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