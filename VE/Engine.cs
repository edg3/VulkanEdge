using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace VE;

public class Engine
{
    private IWindow _window;
    public Engine()
    {
        WindowOptions options = WindowOptions.DefaultVulkan;
        options.Title = "Game";
        options.Size = new Vector2D<int>(1280, 720);
        _window = Window.Create(options);

        _window.Load += InitialLoad;
        _window.Update += Update;
        _window.Render += Render;

        _window.Run();
    }

    private void Render(double d)
    {

    }

    private void Update(double d)
    {

    }

    private void InitialLoad()
    {

    }
}