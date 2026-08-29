using System.Numerics;
using NiziKit.Application;
using Pong.Renderer;

namespace Pong;

public class PongGame : Game
{
    private QuadRenderer _renderer = null!;
    
    public PongGame(GameDesc? desc) : base(desc) {}
    
    protected override void Load(Game game)
    {
        _renderer = new QuadRenderer();
        
        _renderer.AddTexture("Assets/Textures/Fighter.png");
        _renderer.Load();
        
        _renderer.AddRenderObject(new RenderObject()
        {
            AssetPath = "Assets/Textures/Fighter.png",
            Position = new Vector2(0.5f, 0.5f),
        });
    }

    protected override void Update(float dt)
    {
        base.Update(dt);
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