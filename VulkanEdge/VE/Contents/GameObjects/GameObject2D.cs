using Silk.NET.Vulkan;

namespace VE.Contents.GameObjects;

public class GameObject2D : GameObject
{
    private Silk.NET.Vulkan.Image _textureImage;
    private DeviceMemory _deviceImageMemory;
    public GameObject2D(string file) : base(GameObjectType._2D)
    {
        (_textureImage, _deviceImageMemory) = Game.Graphics.OpenImage(file);
    }

    public override void Draw()
    {

    }

    public override void Load()
    {

    }

    public override void Update()
    {

    }

    ~GameObject2D()
    {
        Game.FreeImage(_textureImage);
        Game.FreeDeviceMemory(_deviceImageMemory);
    }
}
