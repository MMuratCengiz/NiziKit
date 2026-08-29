using DenOfIz;

namespace Pong.Renderer;

public class TextureStore : IDisposable
{
    private readonly IDictionary<string, Texture> _textures = new Dictionary<string, Texture>();
    private readonly List<string> _pending = [];

    public void Add(string path)
    {
        _pending.Add(path);
    }
    
    public void Load(BatchResourceCopy batchCopy)
    {
        foreach (var path in _pending)
        {
            _textures[path] = batchCopy.CreateAndLoadTexture(StringView.Create(path));
        }
    }
    
    public Texture GetTexture(string path)
    {
        return _textures[path];
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }
        _textures.Clear();
    }
}