using VE.Contents;

namespace VE;

internal enum Renderer
{
    Vulkan,
    OpenGL
}

public class Game
{
    // Internals
    internal static Renderer Renderer { get; private set; }
    internal static Game _game { get; private set; }

    // Global 'Game.' references for ease
    public static void Start(GameState gameState) => _game.EStart(gameState);
    public static EventsManager Events => _game.EEvents;
    public static InputManager Input => _game.EInput;
    public static AudioManager Audio => _game.EAudio;
    public static AssetManager Assets => _game.EAssets;

    // Public properties
    public EventsManager EEvents { get; init; } = new();
    public InputManager EInput { get; init; } = new();
    public AudioManager EAudio { get; init; } = new();
    public AssetManager EAssets { get; init; } = new();

    // Game Window Creation
    public Game()
    {
        // TODO: Create window
        // TODO: Check if Vulkan supported - set Renderer
        // TODO: if yes -> load vulkan stuff
        // TODO: if no -> load open gl stuff
        throw new NotImplementedException();
    }

    // Future: Make the Silk.Net renderer onto a surface inside a Xamarin form - the game editor. Future step only in the stage 'thinking about it a lot' for now
    public Game(object surface)
    {
        throw new NotImplementedException();
    }

    // Game starting with the first state
    public void EStart(GameState gameState)
    {
        // TODO: make it add all events to the queue for the base engine; such as key press, key release, mouse move, clicks, and so on
        // TODO: make it render 3D first, then 2D in order it was sent in
    }

    // Clean up as needed
    ~Game()
    {
        
    }
}