/*
 * 1. Create a new C# console project on latest .Net
 * 2. Properties -> Windows Application
 * 3. Use logic shown below
 */
using SampleGame;
using VE;

var e = new Engine("Sample Game", new(0, 0, 1), 1280, 720, new MenuState());