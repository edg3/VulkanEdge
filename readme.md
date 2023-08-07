VE
---
VE effectively is shortest hand for 'VulkanEdge'. The goal: have a speedy method to create games in C# using Vulkan with OpenGL as backup.

Author: edg3

The goal is to have a easily reusable layer of an engine between existing Vulkan interaction in C# and your own game ideas. Originally this is to bring back my old games on many platforms in Vulkan, and in the future will lead to the new games I am slowly designing and creating.

Note:
---
2023.08.07: v0.0.1
	Spent ages working on my own implementation for the 3D space and kept feeling my code was way too messy; so decided on a different approach. For ease I've taken the initial engine concept which I want, and mapped it out a little. Then worked out the easiest way to get the Vulkan 3D rendering 'started' with the least dirty 'excess' code around it.
	Next step (a) is to revise the Vulkan code being used now to separate the logic 'as intended'; that is, move the start of draw into 'Graphics.StartDraw', then the end into 'Graphics.EndDraw', with the triangle renderer in 'SampleGameState.Draw'.
	I'm currently guessing I will want to put base shaders (which do nothing) in; so you can create a bland project without shaders. I'm just considering what to do for the different shaders when creating a game with the engine to be loaded, and used appropriately.
	I would still need to consider how to get the proper 2D layer rendering once I've got the code adjusted in (a), however I (b) need to work out how to load 3D models, and (c) their texture mapping with (d) my own content management in memory, then can put in (e) my own content management system for load/unload/memory assistance.
	I figure, my action plan in order is (a), (b), (c), (e), then (d) - work out how it is structured and used, then move working code to use (d) compression
	As an asside: need to, at some point as I progress, (z) make mouse input, (y) turn off mouse visibility on window, (x) turn off window resize, (w) send parameters on windows create optionally to specify these details, (v) work out shaders and shader compilation better, (u) move (a to e) into GameObject, (t) setup EventManager using InputManager, (s) sort out audio, (r) organise audio 2d and 3d, but firstly sort out (a to e) then put in the OpenGL base code. That way if a platform doesn't support the Vulkan it can still run - I just need to consider, properly, how to keep both up to date and equal.

Uses:
---
Silk.NET - https://github.com/dotnet/Silk.NET
=> Vulkan - https://www.vulkan.org/

Nuget:
2.17.1 - Silk.NET
2.17.1 - Silk.NET.Core
2.17.1 - Silk.NET.Input
2.17.1 - Silk.NET.Input.Common
2.17.1 - Silk.NET.Maths
2.17.1 - Silk.NET.OpenGL
2.17.1 - Silk.NET.Vulkan
2.17.1 - Silk.NET.Vulkan.Extensions.KHR
2.17.1 - Silk.NET.Vulkan.Extensions.EXT
2.17.1 - Silk.NET.Windowing
2.17.1 - Silk.NET.Windowing.Common

Transitive nuget:
Microsoft.CSharp
Microsoft.DonNet.PlatformAbstractions
Microsoft.Extensions.DependencyModel
Silk.NET.GLFW
Silk.NET.Input.GLFW
Silk.NET.Input.Sdl
Silk.NET.OpenAL
Silk.NET.SDL
Silk.NET.Windowing.Glfw
Silk.NET.Windowing.Sdl
System.Buffers
System.Memory
System.Numerics.Vectors
System.Runtime.ComplierServices.Unsafe
System.Text.Encoding.Web
System.Text.Json
Ultz.Native.GLFW
Ultz.Native.SDL