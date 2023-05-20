using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using VE.Types;

namespace VE;

public static class E
{
    public static Engine I { get; set; }
}

public class Engine
{
    private IWindow _window;
    private IInputContext _inputContext;
    private Instance _instance;
    private Vk _vk;
    private ExtDebugUtils _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    private bool EnableValidationLayers = true;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;

    public string Title { get; private set; }
    public Version32 Version { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    private readonly string[] _validationLayers = new[] {
        "VK_LAYER_KHRONOS_validation"
    };

    // Last = current game state
    List<IGameState> GameStates { get; } = new();
    // Last = shown popup; as in can do multiple staggered popups if need be
    List<IGameState> PopupStates { get; } = new();

    #region START_UP
    public Engine(string title, Version32 version, uint width, uint height, IGameState firstState)
    {
        if (null != E.I) throw new Exception("CAn't create multiple engine instances at once.");
        E.I = this;

        Title = title;
        Version = version;
        Width = width;
        Height = height;

        /* Create Window */
        WindowOptions options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>((int)width, (int)height),
            Title = title
        };
        _window = Window.Create(options);

        _window.Initialize();
        if (_window.VkSurface is null)
        {
            throw new Exception("Vulkan isn't supported.");
        }

        /* Hmm... This should work according to what I could find but no matter what I try it can never be used. Might be outdated references */
        //var icon = Silk.NET.Core.Native.Image.Load("ve_icon_1.ico");
        //_window.SetWindowIcon(ref icon);

        _window.Load += InitialLoad;
        _window.Update += Update;
        _window.Render += Render;

        /* Initial game state */
        GameStates.Add(firstState);

        /* Run Engine */
        _window.Run();
    }

    private void InitialLoad()
    {
        /* Vulkan */
        _vk = Vk.GetApi();

        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("No validation layers available, was requested.");
        }

        ApplicationInfo appInfo;
        InstanceCreateInfo createInfo;
        unsafe
        {
            appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(Title),
                ApplicationVersion = Version,
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("EV"),
                EngineVersion = new Version32(0, 0, 1),
                ApiVersion = Vk.Version13
            };

            createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            var glfwExtensions = _window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionsCount);

            createInfo.EnabledExtensionCount = glfwExtensionsCount;
            createInfo.PpEnabledExtensionNames = glfwExtensions;
            createInfo.EnabledLayerCount = 0;

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);

                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
                PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            if (_vk.CreateInstance(createInfo, null, out _instance) != Result.Success)
            {
                throw new Exception("Failed to make a Vulkan instance.");
            }

            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }
        }

        PickPhysicalDevice();
        CreateLogicalDevice();

        /* Input */
        _inputContext = _window.CreateInput();
        _inputContext.ConnectionChanged += InputConnectionChanged;

        foreach (var mouse in _inputContext.Mice)
        {
            mouse.Click += Mouse_Click;
        }
    }

    private bool CheckValidationLayerSupport()
    {
        unsafe
        {
            uint layerCount = 0;
            _vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                _vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
            }

            var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

            return _validationLayers.All(availableLayerNames.Contains);
        }
    }

    private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.WriteLine($"Validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        return Vk.False;
    }

    private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        unsafe
        {
            createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
        }
    }

    private void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!_vk!.TryGetInstanceExtension(_instance, out _debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        unsafe
        {
            if (_debugUtils!.CreateDebugUtilsMessenger(_instance, in createInfo, null, out _debugMessenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
            }
        }
    }

    private string[] GetRequiredExtensions()
    {
        unsafe
        {
            var glfwExtensions = _window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

            if (EnableValidationLayers)
            {
                return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
            }

            return extensions;
        }
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = FindQueueFamilies(device);

        return indices.IsComplete();
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        unsafe
        {
            _vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);
        }

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        unsafe
        {
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                _vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
            }
        }

        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    private void PickPhysicalDevice()
    {
        uint devicedCount = 0;
        unsafe
        {
            _vk!.EnumeratePhysicalDevices(_instance, ref devicedCount, null);
        }

        if (devicedCount == 0)
        {
            throw new Exception("failed to find GPUs with Vulkan support!");
        }

        var devices = new PhysicalDevice[devicedCount];
        unsafe
        {
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                _vk!.EnumeratePhysicalDevices(_instance, ref devicedCount, devicesPtr);
            }
        }


        foreach (var device in devices)
        {
            if (IsDeviceSuitable(device))
            {
                _physicalDevice = device;
                break;
            }
        }

        if (_physicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    private void CreateLogicalDevice()
    {
        unsafe
        {
            var indices = FindQueueFamilies(_physicalDevice);

            DeviceQueueCreateInfo queueCreateInfo = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = indices.GraphicsFamily!.Value,
                QueueCount = 1
            };

            float queuePriority = 1.0f;

            queueCreateInfo.PQueuePriorities = &queuePriority;

            PhysicalDeviceFeatures deviceFeatures = new();

            DeviceCreateInfo createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = 1,
                PQueueCreateInfos = &queueCreateInfo,

                PEnabledFeatures = &deviceFeatures,

                EnabledExtensionCount = 0
            };

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            if (_vk!.CreateDevice(_physicalDevice, in createInfo, null, out _device) != Result.Success)
            {
                throw new Exception("failed to create logical device!");
            }

            _vk!.GetDeviceQueue(_device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }
        }
    }
    #endregion

    #region SHUT_DOWN
    ~Engine()
    {
        unsafe
        {
            if (EnableValidationLayers)
            {
                _debugUtils!.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
            }

            _vk!.DestroyInstance(_instance, null);
            _vk!.Dispose();
        }

        _window?.Dispose();
    }
    #endregion

    #region INPUTS
    private void InputConnectionChanged(IInputDevice device, bool b)
    {
        if (device is IMouse mouse)
        {
            mouse.Click += Mouse_Click;
        }
        else if (device is IKeyboard keyboard)
        {
            keyboard.KeyDown += Keyboard_KeyDown;
            keyboard.KeyUp += Keyboard_KeyUp;
        }
    }

    private void Keyboard_KeyUp(IKeyboard keyboard, Key key, int i)
    {

    }

    private void Keyboard_KeyDown(IKeyboard keyboard, Key key, int i)
    {

    }

    /* 0,0 top left of window => 1280,720 bottom right usually*/
    private void Mouse_Click(IMouse mouse, MouseButton button, Vector2 pos)
    {

    }
    #endregion

    #region ENGINE_CODE
    private void Render(double d)
    {
        if (GameStates.Count == 0) return;
        GameStates.Last().Draw();
        if (PopupStates.Count > 0) PopupStates.Last().Draw();

    }

    private void Update(double d)
    {
        if (GameStates.Count == 0)
        {
            _window.Close();
            return;
        }

        if (PopupStates.Count == 0)
        {
            GameStates.Last().Update();
        }
        else
        {
            PopupStates.Last().Update();
        }
    }
    #endregion

    #region CONTENT_MANAGEMENT
    
    #endregion
}