using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    #region GLOBALS
    public Renderer Renderer { get; private set; } = Renderer.Vulkan;
    private WindowOptions _windowOptions;
    private static IWindow _window;
    public GraphicsManager()
    {
        // Create Window
        _windowOptions = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "My first Silk.Net application",
            API = GraphicsAPI.DefaultVulkan
        };

        try
        {
            _window = Window.Create(_windowOptions);
        }
        catch
        {
            Renderer = Renderer.Default;
            _windowOptions.API = GraphicsAPI.Default;
            _window = Window.Create(_windowOptions);
        }

        _window = Window.Create(_windowOptions);
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

    internal void StartDraw()
    {
        if (Renderer == Renderer.Vulkan)
        {

        }
        else
        {

        }
    }

    internal void EndDraw()
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
    private ImageView vk_swapchainImageViews;
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

    public void InitiateVulkan()
    {
        Vk_InitWindow();
        Vk_InitVulkan();
        //Vk_MainLoop();
        //Vk_Cleanup();
    }

    private void Vk_InitWindow()
    {

    }

    private void Vk_InitVulkan()
    {

    }

    //private void Vk_MainLoop()
    //{
    //
    //}

    private void Vk_Cleanup()
    {

    }
    #endregion

    #region DEFAULT
    public void InitiateDefault()
    {

    }
    #endregion
}
