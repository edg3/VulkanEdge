using Silk.NET.Input;
using VE;
using VE.Contents;
using VE.Contents.GameObjects;

namespace SampleGame;

public class SampleGameState : GameState
{
    public override void Draw()
    {
        sampleImg.Draw();
    }

    GameObject2D sampleImg;

    public override void Load()
    {
        sampleImg = new GameObject2D("./sample.png");
    }

    public override void Load_Draw()
    {

    }

    public override void Load_Imports()
    {

    }

    public override void Load_Update()
    {

    }

    public override void Update()
    {
        if (Game.Input.Key_Pressed(Key.Escape)) Game.Exit();
    }
}
