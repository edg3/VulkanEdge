using Silk.NET.Input;

namespace VE.Contents;

public class InputManager
{
    public Dictionary<Key, bool> Keyboard_Pressed = new();
    public Dictionary<Key, DateTime> Keyboard_DaySecond = new();
    public Dictionary<Key, bool> Keyboard_PressedChecked = new();

    private DateTime _startDateTime;

    public InputManager()
    {
        _startDateTime = DateTime.Now;

        foreach (var key in Enum.GetValues(typeof(Key)))
        {
            if (!Keyboard_Pressed.ContainsKey((Key)key))
            {
                Keyboard_Pressed.Add((Key)key, false);
                Keyboard_DaySecond.Add((Key)key, _startDateTime);
                Keyboard_PressedChecked.Add((Key)key, false);
            }
        }
    }

    internal void KeyDown(IKeyboard keyboard, Key key, int keyMode)
    {
        Keyboard_Pressed[key] = true;
        Keyboard_DaySecond[key] = DateTime.Now;
        Keyboard_PressedChecked[key] = false;
    }

    internal void KeyUp(IKeyboard keyboard, Key key, int keyMode)
    {
        Keyboard_Pressed[key] = false;
        Keyboard_DaySecond[key] = DateTime.Now;
    }

    public bool Key_Pressed(Key key)
    {
        if ((DateTime.Now - Keyboard_DaySecond[key]).Milliseconds < 250 && !Keyboard_PressedChecked[key] && Keyboard_Pressed[key])
        {
            Keyboard_PressedChecked[key] = true;
            return true;
        }
        return false;
    }

    public bool Key_Held(Key key)
    {
        if ((DateTime.Now - Keyboard_DaySecond[key]).Milliseconds >= 250 && Keyboard_Pressed[key])
        {
            return true;
        }
        return false;
    }

    public bool Key_Lifted(Key key)
    {
        if ((DateTime.Now - Keyboard_DaySecond[key]).Milliseconds < 250 && !Keyboard_Pressed[key])
        {
            Keyboard_PressedChecked[key] = true;
            return true;
        }
        return false;
    }

    internal void Register()
    {
        Game.Graphics.SetInputContext();
        foreach (var keyboard in Game.InputContext.Keyboards)
        {
            keyboard.KeyDown += KeyDown;
            keyboard.KeyUp += KeyUp;
        }
    }
    // TODO: trigger input events with 'nameofevent:key-or-button-or...' such as 'mouse:leftclicked' or 'mouse:leftheld' for instance
}