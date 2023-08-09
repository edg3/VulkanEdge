//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VE.Contents;

public enum Renderer
{
    Vulkan,
    Default
}

public class GraphicsManager
{
    private static string _version = "0.0.2";

    #region GLOBALS
    public Renderer Renderer { get; private set; } = Renderer.Vulkan;
    private WindowOptions _windowOptions;
    private static IWindow _window;
    public GraphicsManager()
    {
        // Create Window
        _windowOptions = WindowOptions.DefaultVulkan with
        {
            Title = "VE." + _version,
            IsEventDriven = true
        };

        try
        {
            _window = Window.Create(_windowOptions);
        }
        catch
        {
            _windowOptions = WindowOptions.Default with
            {
                Title = "VE-ogl." + _version
            };
            Renderer = Renderer.Default;
            _window = Window.Create(_windowOptions);
        }

        // Prevent SDL? // Window.PrioritizeSdl();
        _window = Window.Create(_windowOptions);
        _window.Initialize();

        if (Renderer == Renderer.Vulkan)
        {
            if (_window?.VkSurface is null) throw new Exception("Windowing platform doesn't support vulkan; rethink above a little edg3?");
        }

        _window.FramebufferResize += OnFrameBufferResize;
    }

    private void OnFrameBufferResize(Vector2D<int> d)
    {
        vk_frameBufferResized = true;
        Vk_RecreateSwapChain();
        _window.DoRender();
    }

    public event Action? Load;
    public event Action<double>? Update;
    public event Action<double>? Draw;

    public void Run()
    {
        _window.Load += Load;
        _window.Update += Update;
        _window.Render += Draw;

        InitiateRenderer();

        _window.Run();
        _vk.DeviceWaitIdle(vk_device);
    }

    private void InitiateRenderer()
    {
        // Vulkan
        if (Renderer == Renderer.Vulkan)
        {
            InitiateVulkan();
        }
        // Default
        else
        {
            InitiateDefault();
        }
    }

    internal void Close()
    {
        _window.Close();
    }

    public void SetInputContext()
    {
        Game.InputContext = _window.CreateInput();
    }

    internal unsafe void StartDraw()
    {
        if (Renderer == Renderer.Vulkan)
        {
            var fence = vk_inFlightFences[vk_currentFrame];
            _vk.WaitForFences(vk_device, 1, in fence, Vk.True, ulong.MaxValue);

            uint imageIndex;
            Result result = _vkSwapchain.AcquireNextImage
                (vk_device, vk_swapchain, ulong.MaxValue, vk_imageAvailableSemaphores[vk_currentFrame], default, &imageIndex);

            if (result == Result.ErrorOutOfDateKhr)
            {
                Vk_RecreateSwapChain();
                return;
            }
            else if (result != Result.Success && result != Result.SuboptimalKhr)
            {
                throw new Exception("failed to acquire swap chain image!");
            }

            if (vk_imagesInFlight[imageIndex].Handle != 0)
            {
                _vk.WaitForFences(vk_device, 1, in vk_imagesInFlight[imageIndex], Vk.True, ulong.MaxValue);
            }

            vk_imagesInFlight[imageIndex] = vk_inFlightFences[vk_currentFrame];

            SubmitInfo submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };

            Semaphore[] waitSemaphores = { vk_imageAvailableSemaphores[vk_currentFrame] };
            PipelineStageFlags[] waitStages = { PipelineStageFlags.ColorAttachmentOutputBit };
            submitInfo.WaitSemaphoreCount = 1;
            var signalSemaphore = vk_renderFinishedSemaphores[vk_currentFrame];
            fixed (Semaphore* waitSemaphoresPtr = waitSemaphores)
            {
                fixed (PipelineStageFlags* waitStagesPtr = waitStages)
                {
                    submitInfo.PWaitSemaphores = waitSemaphoresPtr;
                    submitInfo.PWaitDstStageMask = waitStagesPtr;

                    submitInfo.CommandBufferCount = 1;
                    var buffer = vk_commandBuffers[imageIndex];
                    submitInfo.PCommandBuffers = &buffer;

                    submitInfo.SignalSemaphoreCount = 1;
                    submitInfo.PSignalSemaphores = &signalSemaphore;

                    _vk.ResetFences(vk_device, 1, &fence);

                    if (_vk.QueueSubmit
                            (vk_graphicsQueue, 1, &submitInfo, vk_inFlightFences[vk_currentFrame]) != Result.Success)
                    {
                        throw new Exception("failed to submit draw command buffer!");
                    }
                }
            }

            fixed (SwapchainKHR* swapchain = &vk_swapchain)
            {
                PresentInfoKHR presentInfo = new PresentInfoKHR
                {
                    SType = StructureType.PresentInfoKhr,
                    WaitSemaphoreCount = 1,
                    PWaitSemaphores = &signalSemaphore,
                    SwapchainCount = 1,
                    PSwapchains = swapchain,
                    PImageIndices = &imageIndex
                };

                result = _vkSwapchain.QueuePresent(vk_presentQueue, &presentInfo);
            }

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || vk_frameBufferResized)
            {
                vk_frameBufferResized = false;
                Vk_RecreateSwapChain();
            }
            else if (result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

            vk_currentFrame = (vk_currentFrame + 1) % vk_MaxFramesInFlight;
        }
        else
        {

        }
    }

    internal unsafe void EndDraw()
    {
        if (Renderer == Renderer.Vulkan)
        {

        }
        else
        {

        }
    }

    internal void Cleanup()
    {
        if (Renderer == Renderer.Vulkan)
        {
            Vk_Cleanup();
        }
        else
        {
            // default cleanup
        }
    }
    #endregion

    #region VULKAN
    public const bool vk_EnableValidationLayers = true;
    public const int vk_MaxFramesInFlight = 8;
    public const bool vk_EventBasedRendering = false;

    private Instance vk_instance;
    private DebugUtilsMessengerEXT vk_debugMessenger;
    private SurfaceKHR vk_surface;

    private PhysicalDevice vk_physicalDevice;
    private Device vk_device;

    private Queue vk_graphicsQueue;
    private Queue vk_presentQueue;

    private SwapchainKHR vk_swapchain;
    private Image[] vk_swapchainImages;
    private Format vk_swapchainImageFormat;
    private Extent2D vk_swapchainExtent;
    private ImageView[] vk_swapchainImageViews;
    private Framebuffer[] vk_swapchainFramebuffers;

    private RenderPass vk_renderPass;
    private PipelineLayout vk_pipelineLayout;
    private Pipeline vk_graphicsPipeline;

    private CommandPool vk_commandPool;
    private CommandBuffer[] vk_commandBuffers;

    private Semaphore[] vk_imageAvailableSemaphores;
    private Semaphore[] vk_renderFinishedSemaphores;
    private Fence[] vk_inFlightFences;
    private Fence[] vk_imagesInFlight;
    private uint vk_currentFrame;

    private bool vk_frameBufferResized = false;

    private Vk _vk;
    private KhrSurface _vkSurface;
    private KhrSwapchain _vkSwapchain;
    private ExtDebugUtils vk_debugUtils;
    private string[][] vk_validationLayerNamesPriorityList =
    {
        new [] { "VK_LAYER_KHRONOS_validation" },
        new [] { "VK_LAYER_LUNARG_standard_validation" },
        new []
        {
            "VK_LAYER_GOOGLE_threading",
            "VK_LAYER_LUNARG_parameter_validation",
            "VK_LAYER_LUNARG_object_tracker",
            "VK_LAYER_LUNARG_core_validation",
            "VK_LAYER_GOOGLE_unique_objects",
        }
    };
    private string[] vk_validationLayers;
    private string[] vk_instanceExtensions = { ExtDebugUtils.ExtensionName };
    private string[] vk_deviceExtensions = { KhrSwapchain.ExtensionName };

    public struct Vk_QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }

    public struct Vk_SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities { get; set; }
        public SurfaceFormatKHR[] Formats { get; set; }
        public PresentModeKHR[] PresentModes { get; set; }
    }

    public void InitiateVulkan()
    {
        Vk_CreateInstance();
        Vk_SetupDebugMessenger();
        Vk_CreateSurface();
        Vk_PickPhysicalDevice();
        Vk_CreateLogicalDevice();
        Vk_CreateSwapChain();
        Vk_CreateImageViews();
        Vk_CreateRenderPass();
        Vk_CreateGraphicsPipeline();
        Vk_CreateFramebuffers();
        Vk_CreateCommandPool();
        Vk_CreateCommandBuffers();
        Vk_CreateSyncObjects();
    }

    private unsafe void Vk_CreateInstance()
    {
        _vk = Vk.GetApi();

        if (vk_EnableValidationLayers)
        {
            vk_validationLayers = Vk_GetOptimalValidationLayers();
            if (vk_validationLayers is null)
            {
                throw new NotSupportedException("Validation layers requested, but not available!");
            }
        }

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version11
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = _window.VkSurface!.GetRequiredExtensions(out var extCount);
        // TODO Review that this count doesn't realistically exceed 1k (recommended max for stackalloc)
        // Should probably be allocated on heap anyway as this isn't super performance critical.
        var newExtensions = stackalloc byte*[(int)(extCount + vk_instanceExtensions.Length)];
        for (var i = 0; i < extCount; i++)
        {
            newExtensions[i] = extensions[i];
        }

        for (var i = 0; i < vk_instanceExtensions.Length; i++)
        {
            newExtensions[extCount + i] = (byte*)SilkMarshal.StringToPtr(vk_instanceExtensions[i]);
        }

        extCount += (uint)vk_instanceExtensions.Length;
        createInfo.EnabledExtensionCount = extCount;
        createInfo.PpEnabledExtensionNames = newExtensions;

        if (vk_EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)vk_validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(vk_validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        fixed (Instance* instance = &vk_instance)
        {
            if (_vk.CreateInstance(&createInfo, null, instance) != Result.Success)
            {
                throw new Exception("Failed to create instance!");
            }
        }

        _vk.CurrentInstance = vk_instance;

        if (!_vk.TryGetInstanceExtension(vk_instance, out _vkSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);

        if (vk_EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private unsafe void Vk_SetupDebugMessenger()
    {
        if (!vk_EnableValidationLayers) return;
        if (!_vk.TryGetInstanceExtension(vk_instance, out vk_debugUtils)) return;

        var createInfo = new DebugUtilsMessengerCreateInfoEXT();
        Vk_PopulateDebugMessengerCreateInfo(ref createInfo);

        fixed (DebugUtilsMessengerEXT* debugMessenger = &vk_debugMessenger)
        {
            if (vk_debugUtils.CreateDebugUtilsMessenger
                    (vk_instance, &createInfo, null, debugMessenger) != Result.Success)
            {
                throw new Exception("Failed to create debug messenger.");
            }
        }
    }

    private unsafe void Vk_PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)Vk_DebugCallback;
    }

    private unsafe uint Vk_DebugCallback
       (
           DebugUtilsMessageSeverityFlagsEXT messageSeverity,
           DebugUtilsMessageTypeFlagsEXT messageTypes,
           DebugUtilsMessengerCallbackDataEXT* pCallbackData,
           void* pUserData
       )
    {
        if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt)
        {
            Console.WriteLine
                ($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        }

        return Vk.False;
    }

    private unsafe void Vk_CreateSurface()
    {
        vk_surface = _window.VkSurface!.Create<AllocationCallbacks>(vk_instance.ToHandle(), null).ToSurface();
    }

    private unsafe void Vk_PickPhysicalDevice()
    {
        var devices = _vk.GetPhysicalDevices(vk_instance);

        if (!devices.Any())
        {
            throw new NotSupportedException("Failed to find GPUs with Vulkan support.");
        }

        vk_physicalDevice = devices.FirstOrDefault(device =>
        {
            var indices = Vk_FindQueueFamilies(device);

            var extensionsSupported = Vk_CheckDeviceExtensionSupport(device);

            var swapChainAdequate = false;
            if (extensionsSupported)
            {
                var swapChainSupport = Vk_QuerySwapChainSupport(device);
                swapChainAdequate = swapChainSupport.Formats.Length != 0 && swapChainSupport.PresentModes.Length != 0;
            }

            return indices.IsComplete() && extensionsSupported && swapChainAdequate;
        });

        if (vk_physicalDevice.Handle == 0)
            throw new Exception("No suitable device.");
    }

    private unsafe Vk_SwapChainSupportDetails Vk_QuerySwapChainSupport(PhysicalDevice device)
    {
        var details = new Vk_SwapChainSupportDetails();
        _vkSurface.GetPhysicalDeviceSurfaceCapabilities(device, vk_surface, out var surfaceCapabilities);
        details.Capabilities = surfaceCapabilities;

        var formatCount = 0u;
        _vkSurface.GetPhysicalDeviceSurfaceFormats(device, vk_surface, &formatCount, null);

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];

            using var mem = GlobalMemory.Allocate((int)formatCount * sizeof(SurfaceFormatKHR));
            var formats = (SurfaceFormatKHR*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            _vkSurface.GetPhysicalDeviceSurfaceFormats(device, vk_surface, &formatCount, formats);

            for (var i = 0; i < formatCount; i++)
            {
                details.Formats[i] = formats[i];
            }
        }

        var presentModeCount = 0u;
        _vkSurface.GetPhysicalDeviceSurfacePresentModes(device, vk_surface, &presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];

            using var mem = GlobalMemory.Allocate((int)presentModeCount * sizeof(PresentModeKHR));
            var modes = (PresentModeKHR*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            _vkSurface.GetPhysicalDeviceSurfacePresentModes(device, vk_surface, &presentModeCount, modes);

            for (var i = 0; i < presentModeCount; i++)
            {
                details.PresentModes[i] = modes[i];
            }
        }

        return details;
    }

    private unsafe bool Vk_CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        return vk_deviceExtensions.All(ext => _vk.IsDeviceExtensionPresent(device, ext));
    }

    private unsafe Vk_QueueFamilyIndices Vk_FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new Vk_QueueFamilyIndices();

        uint queryFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, null);

        using var mem = GlobalMemory.Allocate((int)queryFamilyCount * sizeof(QueueFamilyProperties));
        var queueFamilies = (QueueFamilyProperties*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, queueFamilies);
        for (var i = 0u; i < queryFamilyCount; i++)
        {
            var queueFamily = queueFamilies[i];
            // note: HasFlag is slow on .NET Core 2.1 and below.
            // if you're targeting these versions, use ((queueFamily.QueueFlags & QueueFlags.QueueGraphicsBit) != 0)
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            _vkSurface.GetPhysicalDeviceSurfaceSupport(device, i, vk_surface, out var presentSupport);

            if (presentSupport == Vk.True)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }
        }

        return indices;
    }

    private unsafe void Vk_CreateLogicalDevice()
    {
        var indices = Vk_FindQueueFamilies(vk_physicalDevice);
        var uniqueQueueFamilies = indices.GraphicsFamily.Value == indices.PresentFamily.Value
            ? new[] { indices.GraphicsFamily.Value }
            : new[] { indices.GraphicsFamily.Value, indices.PresentFamily.Value };

        using var mem = GlobalMemory.Allocate((int)uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        var queuePriority = 1f;
        for (var i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            var queueCreateInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
            queueCreateInfos[i] = queueCreateInfo;
        }

        var deviceFeatures = new PhysicalDeviceFeatures();

        var createInfo = new DeviceCreateInfo();
        createInfo.SType = StructureType.DeviceCreateInfo;
        createInfo.QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length;
        createInfo.PQueueCreateInfos = queueCreateInfos;
        createInfo.PEnabledFeatures = &deviceFeatures;
        createInfo.EnabledExtensionCount = (uint)vk_deviceExtensions.Length;

        var enabledExtensionNames = SilkMarshal.StringArrayToPtr(vk_deviceExtensions);
        createInfo.PpEnabledExtensionNames = (byte**)enabledExtensionNames;

        if (vk_EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)vk_validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(vk_validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        fixed (Device* device = &vk_device)
        {
            if (_vk.CreateDevice(vk_physicalDevice, &createInfo, null, device) != Result.Success)
            {
                throw new Exception("Failed to create logical device.");
            }
        }

        fixed (Queue* graphicsQueue = &vk_graphicsQueue)
        {
            _vk.GetDeviceQueue(vk_device, indices.GraphicsFamily.Value, 0, graphicsQueue);
        }

        fixed (Queue* presentQueue = &vk_presentQueue)
        {
            _vk.GetDeviceQueue(vk_device, indices.PresentFamily.Value, 0, presentQueue);
        }

        _vk.CurrentDevice = vk_device;

        if (!_vk.TryGetDeviceExtension(vk_instance, vk_device, out _vkSwapchain))
        {
            throw new NotSupportedException("KHR_swapchain extension not found.");
        }

        Console.WriteLine($"{_vk.CurrentInstance?.Handle} {_vk.CurrentDevice?.Handle}");
    }

    private unsafe bool Vk_CreateSwapChain()
    {
        var swapChainSupport = Vk_QuerySwapChainSupport(vk_physicalDevice);

        var surfaceFormat = Vk_ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = Vk_ChooseSwapPresentMode(swapChainSupport.PresentModes);
        var extent = Vk_ChooseSwapExtent(swapChainSupport.Capabilities);

        // TODO: On SDL minimizing the window does not affect the frameBufferSize.
        // This check can be removed if it does
        if (extent.Width == 0 || extent.Height == 0)
            return false;

        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 &&
            imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = vk_surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit
        };

        var indices = Vk_FindQueueFamilies(vk_physicalDevice);
        uint[] queueFamilyIndices = { indices.GraphicsFamily.Value, indices.PresentFamily.Value };

        fixed (uint* qfiPtr = queueFamilyIndices)
        {
            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = qfiPtr;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
            createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
            createInfo.PresentMode = presentMode;
            createInfo.Clipped = Vk.True;

            createInfo.OldSwapchain = default;

            if (!_vk.TryGetDeviceExtension(vk_instance, _vk.CurrentDevice.Value, out _vkSwapchain))
            {
                throw new NotSupportedException("KHR_swapchain extension not found.");
            }

            fixed (SwapchainKHR* swapchain = &vk_swapchain)
            {
                if (_vkSwapchain.CreateSwapchain(vk_device, &createInfo, null, swapchain) != Result.Success)
                {
                    throw new Exception("failed to create swap chain!");
                }
            }
        }

        _vkSwapchain.GetSwapchainImages(vk_device, vk_swapchain, &imageCount, null);
        vk_swapchainImages = new Image[imageCount];
        fixed (Image* swapchainImage = vk_swapchainImages)
        {
            _vkSwapchain.GetSwapchainImages(vk_device, vk_swapchain, &imageCount, swapchainImage);
        }

        vk_swapchainImageFormat = surfaceFormat.Format;
        vk_swapchainExtent = extent;

        return true;
    }

    private Extent2D Vk_ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }

        var actualExtent = new Extent2D
        { Height = (uint)_window.FramebufferSize.Y, Width = (uint)_window.FramebufferSize.X };
        actualExtent.Width = new[]
        {
                capabilities.MinImageExtent.Width,
                new[] {capabilities.MaxImageExtent.Width, actualExtent.Width}.Min()
            }.Max();
        actualExtent.Height = new[]
        {
                capabilities.MinImageExtent.Height,
                new[] {capabilities.MaxImageExtent.Height, actualExtent.Height}.Min()
            }.Max();

        return actualExtent;
    }

    private PresentModeKHR Vk_ChooseSwapPresentMode(PresentModeKHR[] presentModes)
    {
        foreach (var availablePresentMode in presentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    private SurfaceFormatKHR Vk_ChooseSwapSurfaceFormat(SurfaceFormatKHR[] formats)
    {
        foreach (var format in formats)
        {
            if (format.Format == Format.B8G8R8A8Unorm)
            {
                return format;
            }
        }

        return formats[0];
    }

    private unsafe void Vk_CreateImageViews()
    {
        vk_swapchainImageViews = new ImageView[vk_swapchainImages.Length];

        for (var i = 0; i < vk_swapchainImages.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = vk_swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = vk_swapchainImageFormat,
                Components =
                    {
                        R = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity
                    },
                SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    }
            };

            ImageView imageView = default;
            if (_vk.CreateImageView(vk_device, &createInfo, null, &imageView) != Result.Success)
            {
                throw new Exception("failed to create image views!");
            }

            vk_swapchainImageViews[i] = imageView;
        }
    }

    private unsafe void Vk_CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = vk_swapchainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef
        };

        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit
        };

        var renderPassInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency
        };

        fixed (RenderPass* renderPass = &vk_renderPass)
        {
            if (_vk.CreateRenderPass(vk_device, &renderPassInfo, null, renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            }
        }
    }

    private unsafe void Vk_CreateGraphicsPipeline()
    {
        var vertShaderCode = LoadEmbeddedResourceBytes("shader.vert.spv");
        var fragShaderCode = LoadEmbeddedResourceBytes("shader.frag.spv");

        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);

        var vertShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var fragShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
        shaderStages[0] = vertShaderStageInfo;
        shaderStages[1] = fragShaderStageInfo;

        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = Vk.False
        };

        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = vk_swapchainExtent.Width,
            Height = vk_swapchainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        var scissor = new Rect2D { Offset = default, Extent = vk_swapchainExtent };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = Vk.False,
            RasterizerDiscardEnable = Vk.False,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = Vk.False
        };

        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = Vk.False,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };

        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit |
                             ColorComponentFlags.GBit |
                             ColorComponentFlags.BBit |
                             ColorComponentFlags.ABit,
            BlendEnable = Vk.False
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = Vk.False,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };

        colorBlending.BlendConstants[0] = 0.0f;
        colorBlending.BlendConstants[1] = 0.0f;
        colorBlending.BlendConstants[2] = 0.0f;
        colorBlending.BlendConstants[3] = 0.0f;

        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 0
        };

        fixed (PipelineLayout* pipelineLayout = &vk_pipelineLayout)
        {
            if (_vk.CreatePipelineLayout(vk_device, &pipelineLayoutInfo, null, pipelineLayout) != Result.Success)
            {
                throw new Exception("failed to create pipeline layout!");
            }
        }

        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = vk_pipelineLayout,
            RenderPass = vk_renderPass,
            Subpass = 0,
            BasePipelineHandle = default
        };

        fixed (Pipeline* graphicsPipeline = &vk_graphicsPipeline)
        {
            if (_vk.CreateGraphicsPipelines
                    (vk_device, default, 1, &pipelineInfo, null, graphicsPipeline) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }

        _vk.DestroyShaderModule(vk_device, fragShaderModule, null);
        _vk.DestroyShaderModule(vk_device, vertShaderModule, null);
    }

    private unsafe ShaderModule CreateShaderModule(byte[] code)
    {
        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)code.Length
        };
        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;
        }

        var shaderModule = new ShaderModule();
        if (_vk.CreateShaderModule(vk_device, &createInfo, null, &shaderModule) != Result.Success)
        {
            throw new Exception("failed to create shader module!");
        }

        return shaderModule;
    }

    private unsafe void Vk_CreateFramebuffers()
    {
        vk_swapchainFramebuffers = new Framebuffer[vk_swapchainImageViews.Length];

        for (var i = 0; i < vk_swapchainImageViews.Length; i++)
        {
            var attachment = vk_swapchainImageViews[i];
            var framebufferInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = vk_renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = vk_swapchainExtent.Width,
                Height = vk_swapchainExtent.Height,
                Layers = 1
            };

            var framebuffer = new Framebuffer();
            if (_vk.CreateFramebuffer(vk_device, &framebufferInfo, null, &framebuffer) != Result.Success)
            {
                throw new Exception("failed to create framebuffer!");
            }

            vk_swapchainFramebuffers[i] = framebuffer;
        }
    }

    private unsafe void Vk_CreateCommandPool()
    {
        var queueFamilyIndices = Vk_FindQueueFamilies(vk_physicalDevice);

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily.Value
        };

        fixed (CommandPool* commandPool = &vk_commandPool)
        {
            if (_vk.CreateCommandPool(vk_device, &poolInfo, null, commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }
        }
    }

    private unsafe void Vk_CreateCommandBuffers()
    {
        vk_commandBuffers = new CommandBuffer[vk_swapchainFramebuffers.Length];

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = vk_commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)vk_commandBuffers.Length
        };

        fixed (CommandBuffer* commandBuffers = vk_commandBuffers)
        {
            if (_vk.AllocateCommandBuffers(vk_device, &allocInfo, commandBuffers) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }

        for (var i = 0; i < vk_commandBuffers.Length; i++)
        {
            var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };

            if (_vk.BeginCommandBuffer(vk_commandBuffers[i], &beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            var renderPassInfo = new RenderPassBeginInfo
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = vk_renderPass,
                Framebuffer = vk_swapchainFramebuffers[i],
                RenderArea = { Offset = new Offset2D { X = 0, Y = 0 }, Extent = vk_swapchainExtent }
            };

            var clearColor = new ClearValue
            { Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 } };
            renderPassInfo.ClearValueCount = 1;
            renderPassInfo.PClearValues = &clearColor;

            _vk.CmdBeginRenderPass(vk_commandBuffers[i], &renderPassInfo, SubpassContents.Inline);

            _vk.CmdBindPipeline(vk_commandBuffers[i], PipelineBindPoint.Graphics, vk_graphicsPipeline);

            _vk.CmdDraw(vk_commandBuffers[i], 3, 1, 0, 0);

            _vk.CmdEndRenderPass(vk_commandBuffers[i]);

            if (_vk.EndCommandBuffer(vk_commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }
    }

    private unsafe void Vk_CreateSyncObjects()
    {
        vk_imageAvailableSemaphores = new Semaphore[vk_MaxFramesInFlight];
        vk_renderFinishedSemaphores = new Semaphore[vk_MaxFramesInFlight];
        vk_inFlightFences = new Fence[vk_MaxFramesInFlight];
        vk_imagesInFlight = new Fence[vk_MaxFramesInFlight];

        SemaphoreCreateInfo semaphoreInfo = new SemaphoreCreateInfo();
        semaphoreInfo.SType = StructureType.SemaphoreCreateInfo;

        FenceCreateInfo fenceInfo = new FenceCreateInfo();
        fenceInfo.SType = StructureType.FenceCreateInfo;
        fenceInfo.Flags = FenceCreateFlags.SignaledBit;

        for (var i = 0; i < vk_MaxFramesInFlight; i++)
        {
            Semaphore imgAvSema, renderFinSema;
            Fence inFlightFence;
            if (_vk.CreateSemaphore(vk_device, &semaphoreInfo, null, &imgAvSema) != Result.Success ||
                _vk.CreateSemaphore(vk_device, &semaphoreInfo, null, &renderFinSema) != Result.Success ||
                _vk.CreateFence(vk_device, &fenceInfo, null, &inFlightFence) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }

            vk_imageAvailableSemaphores[i] = imgAvSema;
            vk_renderFinishedSemaphores[i] = renderFinSema;
            vk_inFlightFences[i] = inFlightFence;
        }
    }

    private unsafe void Vk_Cleanup()
    {
        Vk_CleanupSwapchain();

        for (var i = 0; i < vk_MaxFramesInFlight; i++)
        {
            _vk.DestroySemaphore(vk_device, vk_renderFinishedSemaphores[i], null);
            _vk.DestroySemaphore(vk_device, vk_imageAvailableSemaphores[i], null);
            _vk.DestroyFence(vk_device, vk_inFlightFences[i], null);
        }

        _vk.DestroyCommandPool(vk_device, vk_commandPool, null);

        _vk.DestroyDevice(vk_device, null);

        if (vk_EnableValidationLayers)
        {
            vk_debugUtils.DestroyDebugUtilsMessenger(vk_instance, vk_debugMessenger, null);
        }

        _vkSurface.DestroySurface(vk_instance, vk_surface, null);
        _vk.DestroyInstance(vk_instance, null);
    }

    private unsafe void Vk_CleanupSwapchain()
    {
        foreach (var framebuffer in vk_swapchainFramebuffers)
        {
            _vk.DestroyFramebuffer(vk_device, framebuffer, null);
        }

        fixed (CommandBuffer* buffers = vk_commandBuffers)
        {
            _vk.FreeCommandBuffers(vk_device, vk_commandPool, (uint)vk_commandBuffers.Length, buffers);
        }

        _vk.DestroyPipeline(vk_device, vk_graphicsPipeline, null);
        _vk.DestroyPipelineLayout(vk_device, vk_pipelineLayout, null);
        _vk.DestroyRenderPass(vk_device, vk_renderPass, null);

        foreach (var imageView in vk_swapchainImageViews)
        {
            _vk.DestroyImageView(vk_device, imageView, null);
        }

        _vkSwapchain.DestroySwapchain(vk_device, vk_swapchain, null);
    }

    private void Vk_RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = _window.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = _window.FramebufferSize;
            _window.DoEvents();
        }

        _ = _vk.DeviceWaitIdle(vk_device);

        Vk_CleanupSwapchain();

        // TODO: On SDL it is possible to get an invalid swap chain when the window is minimized.
        // This check can be removed when the above frameBufferSize check catches it.
        while (!Vk_CreateSwapChain())
        {
            _window.DoEvents();
        }

        Vk_CreateImageViews();
        Vk_CreateRenderPass();
        Vk_CreateGraphicsPipeline();
        Vk_CreateFramebuffers();
        Vk_CreateCommandBuffers();

        vk_imagesInFlight = new Fence[vk_swapchainImages.Length];
    }

    private unsafe string[]? Vk_GetOptimalValidationLayers()
    {
        var layerCount = 0u;
        _vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*)0);

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            _vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(availableLayer => Marshal.PtrToStringAnsi((nint)availableLayer.LayerName)).ToArray();
        foreach (var validationLayerNameSet in vk_validationLayerNamesPriorityList)
        {
            if (validationLayerNameSet.All(validationLayerName => availableLayerNames.Contains(validationLayerName)))
            {
                return validationLayerNameSet;
            }
        }

        return null;
    }

    internal unsafe (Image, DeviceMemory) OpenImage(string file)
    {
        Image textureImage;
        DeviceMemory textureImageMemory;

        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(file);

        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

        Silk.NET.Vulkan.Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, stagingBufferMemory);

        void* data;
        _vk!.MapMemory(vk_device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        _vk!.UnmapMemory(vk_device, stagingBufferMemory);

        (textureImage, textureImageMemory) = CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit);

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        _vk!.DestroyBuffer(vk_device, stagingBuffer, null);
        _vk!.FreeMemory(vk_device, stagingBufferMemory, null);

        return (textureImage, textureImageMemory);
    }

    private void CopyBufferToImage(Silk.NET.Vulkan.Buffer stagingBuffer, Image textureImage, uint width, uint height)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(width, height, 1),

        };

        _vk!.CmdCopyBufferToImage(commandBuffer, stagingBuffer, textureImage, ImageLayout.TransferDstOptimal, 1, region);

        EndSingleTimeCommands(commandBuffer);
    }

    private unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        _vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        _vk!.QueueSubmit(vk_graphicsQueue, 1, submitInfo, default);
        _vk!.QueueWaitIdle(vk_graphicsQueue);

        _vk!.FreeCommandBuffers(vk_device, vk_commandPool, 1, commandBuffer);
    }

    private CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = vk_commandPool,
            CommandBufferCount = 1,
        };

        _vk!.AllocateCommandBuffers(vk_device, allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        _vk!.BeginCommandBuffer(commandBuffer, beginInfo);

        return commandBuffer;
    }

    private unsafe void TransitionImageLayout(Image textureImage, Format r8G8B8A8Srgb, ImageLayout oldLayout, ImageLayout newLayout)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = textureImage,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }

        _vk!.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

        EndSingleTimeCommands(commandBuffer);

    }

    private unsafe (Image, DeviceMemory) CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties)
    {
        Image image = new();
        DeviceMemory textureImageMemory = new();

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
        };

        Image* imagePtr = &image;
        if (_vk!.CreateImage(vk_device, imageInfo, null, imagePtr) != Result.Success)
        {
            throw new Exception("failed to create image!");
        }

        _vk!.GetImageMemoryRequirements(vk_device, image, out MemoryRequirements memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        DeviceMemory* imageMemoryPtr = &textureImageMemory;
        if (_vk!.AllocateMemory(vk_device, allocInfo, null, imageMemoryPtr) != Result.Success)
        {
            throw new Exception("failed to allocate image memory!");
        }

        _vk!.BindImageMemory(vk_device, image, textureImageMemory, 0);

        return (image, textureImageMemory);
    }

    private uint FindMemoryType(uint memoryTypeBits, MemoryPropertyFlags properties)
    {
        _vk!.GetPhysicalDeviceMemoryProperties(vk_physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((memoryTypeBits & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new Exception("failed to find suitable memory type!");
    }

    private unsafe void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer)
        {
            // This fails... hmm
            if (_vk!.CreateBuffer(vk_device, bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        MemoryRequirements memRequirements = new();
        _vk!.GetBufferMemoryRequirements(vk_device, buffer, out memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        DeviceMemory* bufferMemoryPtr = &bufferMemory;
        if (_vk!.AllocateMemory(vk_device, allocateInfo, null, bufferMemoryPtr) != Result.Success)
        {
            throw new Exception("failed to allocate vertex buffer memory!");
        }

        _vk!.BindBufferMemory(vk_device, buffer, bufferMemory, 0);
    }

    internal unsafe void EFreeImage(Image textureImage)
    {
        _vk.DestroyImage(vk_device, textureImage, null);
    }

    internal unsafe void EFreeDeviceMemory(DeviceMemory deviceImageMemory)
    {
        _vk.FreeMemory(vk_device, deviceImageMemory, null);
    }
    #endregion

    #region DEFAULT
    public void InitiateDefault()
    {

    }
    #endregion

    #region PROGRAM
    internal static byte[] LoadEmbeddedResourceBytes(string path)
    {
        using (var s = File.OpenRead(path))
        {
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
    #endregion
}
