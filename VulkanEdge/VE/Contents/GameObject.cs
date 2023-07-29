namespace VE.Contents;

public enum GameObjectType
{
    _2D,
    _3D
}

public abstract class GameObject
{
    public GameObjectType ObjectType { get; init; }
    public GameObject(GameObjectType objectType)
    {
        ObjectType = objectType;
    }

    public abstract void Load();
    public abstract void Update();
    public abstract void Draw();


    // Event Queue
    private Dictionary<string, Action> _events = new();

    // Add this game object to the queue
    public void Register(string key, Action value)
    {
        Game.Events.Register(key, this);
        _events.Add(key, value);
    }

    // This is called by the QueueManager if a registered event came through
    public void Trigger(string key)
    {
        if (_events.ContainsKey(key))
        {
            _events[key].Invoke();
        }
    }

    // This intends to remove this GameObject from all queues it was registered to
    ~GameObject()
    {
        if (_events.Count > 0)
            Game.Events.Deregister(this);
    }
}
