namespace VE.Contents;

public class AudioManager
{
    // TODO: consider having time of when audio was last played and can clean out of memory if audio not played for a long enough time?
    private Dictionary<string, object> _audioFiles = new();
    public void Load(string filename)
    {
        // TODO: load file and add into _audioFiles in memory if not exists
    }

    // 2D
    public void PlaySound(string filename)
    {

    }

    // 3D
    public void PlaySound(string filename, object coordinates)
    {

    }

    // TODO: work out how I will store and use current sounds playing using the sound system eventually
    public void StopSound(string filename)
    {

    }
}
