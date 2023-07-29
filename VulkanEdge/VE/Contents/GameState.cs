namespace VE.Contents;

public abstract class GameState
{
    /// <summary>
    /// Used to get the tiny assets you want to show on the load screen
    /// </summary>
    public abstract void Load_Imports();
    /// <summary>
    /// The update mechanism for the load screen, so you can do what you want here
    /// </summary>
    public abstract void Load_Update();
    /// <summary>
    /// The draw mechanism for the load screen, so you can render what you loaded
    /// </summary>
    public abstract void Load_Draw();
    /// <summary>
    /// The place to load all the assets you initially need for this game state
    /// </summary>
    public abstract void Load();
    /// <summary>
    /// What you need specific to this game state, remember the plan is to have GameObjects have their own management for ease
    ///  - This occurs BEFORE the game objects update; just remember the events trigger BEFORE the update calls
    /// </summary>
    public abstract void Update();
    /// <summary>
    /// Render the game state's private needs
    ///  - This occurs AFTER the game objects have all drawn; so you can put UI here if need be, but gameobjects 3D is first, then 2D, then the game state
    /// </summary>
    public abstract void Draw();

    internal bool Loaded = false;
    private Thread? _loadThread;

    public GameState()
    {
        // TODO: think of how to manage if physics is turned on for 2D or 3D, perhaps?
        Load_Imports();
        _loadThread = new Thread(Load);
        _loadThread.Start();
    }

    public List<GameObject> GameObjects = new();

    internal void RunUpdate()
    {
        if (_loadThread.IsAlive)
        {
            Load_Update();
        }
        else
        {
            Update();
            foreach (var gameObject in GameObjects)
            {
                gameObject.Update();
            }
        }
    }

    internal void RunDraw()
    {
        if (_loadThread.IsAlive)
        {
            Load_Draw();
        }
        else
        {
            // TODO: reconsider this a little when I get to the rendering steps
            foreach (var gameObject in GameObjects)
            {
                if (gameObject.ObjectType == GameObjectType._3D)
                {
                    gameObject.Draw();
                }
            }
            foreach (var gameObject in GameObjects)
            {
                if (gameObject.ObjectType == GameObjectType._2D)
                {
                    gameObject.Draw();
                }
            }
            Draw();
        }
    }
}
