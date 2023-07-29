namespace VE.Contents;

public enum AssetType
{
    Texture2D,
    Model3D,
    Audio,
    Shader,
    Other
}

public struct LoadedAsset
{
    public string Path;
    public object FileInMemory;
}

public class AssetManager
{
    private Dictionary<AssetType, List<LoadedAsset>> _assets = new();

    public AssetManager()
    {
        _assets.Add(AssetType.Texture2D, new());
        _assets.Add(AssetType.Model3D, new());
        _assets.Add(AssetType.Audio, new());
        _assets.Add(AssetType.Shader, new());
        _assets.Add(AssetType.Other, new());
    }

    public void LoadAsset(AssetType type, string path)
    {

    }

    // TODO: hmm, this isn't a pretty way to do it with, I think I must rethink this idea a bit
    public object Get(string filename)
    {
        throw new NotImplementedException();
    }
}
