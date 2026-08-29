using NiziKit.Application;
using Pong.Renderer;

namespace Pong;

public class PongGame : Game
{
    private QuadRenderer _renderer = null!;
    
    public PongGame(GameDesc? desc) {}
    
    protected override void Load(Game game)
    {
        _renderer = new QuadRenderer();
    }

    protected override void Update(float dt)
    {
        base.Update(dt);
    }

    protected override void Render(float dt)
    {
        _renderer.Render(dt);
    }
}