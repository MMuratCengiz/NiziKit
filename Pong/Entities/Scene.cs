using System.Collections;

namespace Pong.Entities;

public class Scene : IEnumerable<SceneObject>
{
    private readonly Lock _lock = new Lock();
    private readonly SceneObject[] _objects = new SceneObject[1024];
    private int _nextId = 0;

    public SceneObject NewSceneObject()
    {
        lock (_lock)
        {
            var sceneObject = new SceneObject(_nextId++);
            _objects[sceneObject.Id] = sceneObject;
            return this[sceneObject.Id];
        }
    }

    public ref SceneObject this[int index] => ref _objects[index];

    public IEnumerator<SceneObject> GetEnumerator()
    {
        return _objects.AsEnumerable().Take(_nextId).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}