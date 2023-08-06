using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace VE.Contents;

public enum Renderer
{
    Vulkan,
    OpenGL
}

public class GraphicsManager
{
    public Renderer Renderer { get; private set; } = Renderer.Vulkan;
    private WindowOptions _windowOptions;
    private static IWindow _window;
    public GraphicsManager()
    {
        // Create Window
        _windowOptions = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "My first SIlk.Net application"
        };
        _window = Window.Create(_windowOptions);
    }

    public event Action? Load;
    public event Action<double>? Update;
    public event Action<double>? Draw;

    public void Run()
    {
        _window.Load += Load;
        _window.Update += Update;
        _window.Render += Draw;

        // TODO: See if Vulkan or OpenGL

        _window.Run();
    }

    internal void Close()
    {
        _window.Close();
    }

    public void SetInputContext()
    {
        Game.InputContext = _window.CreateInput();
    }
}
