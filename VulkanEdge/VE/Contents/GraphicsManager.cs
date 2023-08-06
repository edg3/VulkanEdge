using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace VE.Contents;

public enum Renderer
{
    Vulkan,
    Default
}

public class GraphicsManager
{
    #region GLOBALS
    public Renderer Renderer { get; private set; } = Renderer.Vulkan;
    private WindowOptions _windowOptions;
    private static IWindow _window;
    public GraphicsManager()
    {
        // Create Window
        _windowOptions = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "My first Silk.Net application",
            API = GraphicsAPI.DefaultVulkan
        };

        try
        {
            _window = Window.Create(_windowOptions);
        }
        catch
        {
            Renderer = Renderer.Default;
            _windowOptions.API = GraphicsAPI.Default;
            _window = Window.Create(_windowOptions);
        }

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

        InitiateRenderer();

        _window.Run();
    }

    private void InitiateRenderer()
    {
        // Vulkan
        if (Renderer == Renderer.Vulkan)
        {
            InitiateVulkan();
        }
        // Default
        else
        {
            InitiateDefault();
        }
    }

    internal void Close()
    {
        _window.Close();
    }

    public void SetInputContext()
    {
        Game.InputContext = _window.CreateInput();
    }
    #endregion

    #region VULKAN
    public void InitiateVulkan()
    {

    }
    #endregion

    #region DEFAULT
    public void InitiateDefault()
    {

    }
    #endregion
}
