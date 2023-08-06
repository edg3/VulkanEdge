using Silk.NET.Input;
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
    public static GraphicsManager Graphics => _game.EGraphics;
    public static void Exit() => _game.EExit();
    public static IInputContext? InputContext { get; set; } = null;

    // Public properties
    public EventsManager EEvents { get; init; } = new();
    public InputManager EInput { get; init; } = new();
    public AudioManager EAudio { get; init; } = new();
    public AssetManager EAssets { get; init; } = new();
    public GraphicsManager EGraphics { get; init; } = new();

    // Game States
    private List<GameState> _gameStates = new();

    // Game Window Creation
    public Game()
    {
        if (null != _game) throw new Exception("Game() - can't create second Game instance.");
        _game = this;
    }

    // Future: Make the Silk.Net renderer onto a surface inside a Xamarin form - the game editor. Future step only in the stage 'thinking about it a lot' for now
    public Game(object surface)
    {
        throw new NotImplementedException();
    }

    // Game starting with the first state
    public void EStart(GameState gameState)
    {
        _gameStates.Add(gameState);

        EGraphics.Load += Load;
        EGraphics.Update += Update;
        EGraphics.Draw += Draw;

        // TODO: make it add all events to the queue for the base engine; such as key press, key release, mouse move, clicks, and so on
        // TODO: make it render 3D first, then 2D in order it was sent in
        Graphics.Run();
    }

    // Clean up as needed
    ~Game()
    {

    }

    private void Load()
    {
        Game.Input.Register();
        // TODO: Load_Imports; load screen, etc - will currently stick to just load and draw without the threading for now
        _gameStates[0].Load();
    }

    private void Update(double obj)
    {
        if (_gameStates.Count > 0)
        {
            _gameStates.Last().Update();
        }
        else
        {
            Graphics.Close();
        }
    }

    private void Draw(double obj)
    {
        if (_gameStates.Count > 0)
        {
            _gameStates.Last().Draw();
        }
    }

    private void EExit()
    {
        Graphics.Close();
    }
}