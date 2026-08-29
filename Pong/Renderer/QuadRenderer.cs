using DenOfIz;
using NiziKit.Graphics.Recording;
using NiziKit.Graphics.Renderer;

namespace Pong;

public class QuadRenderer
{
    private CommandListAllocator _commandListAllocator = new();
    private CycledCommandList _cycledCommandList = new(QueueType.Graphics);

    public QuadRenderer()
    {
    }

    public void Render(float deltaTime)
    {
        
    }
}