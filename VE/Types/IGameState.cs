namespace VE.Types;

public abstract class IGameState
{
    private bool _loaded { get; set; }
    public bool Loaded => _loaded;

    public IGameState()
    {
        _loaded = false;
    }

    public void ThreadedLoad()
    {
        Thread thread = new Thread(() =>
        {
            Load();
            _loaded = true;
        });
    }

    public abstract void Load();
    public abstract void Update();
    public abstract void Draw();
}
