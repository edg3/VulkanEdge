using Silk.NET.Input;
using VE;
using VE.Contents;

namespace SampleGame;

public class SampleGameState : GameState
{
    public override void Draw()
    {
        //var b = 2;
    }

    public override void Load()
    {
        //var a = 1;
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
