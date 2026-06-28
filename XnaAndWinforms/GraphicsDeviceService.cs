#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceService.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
#endregion

// The IGraphicsDeviceService interface requires a DeviceCreated event, but we
// always just create the device inside our constructor, so we have no place to
// raise that event. The C# compiler warns us that the event is never used, but
// we don't care so we just disable this warning.
#pragma warning disable 67

namespace XnaAndWinforms;

/// <summary>
/// Helper class responsible for creating and managing the GraphicsDevice.
/// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
/// so even though there can be many controls, there will only ever be a single
/// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
/// interface, which provides notification events for when the device is reset
/// or disposed.
/// </summary>
class GraphicsDeviceService : IGraphicsDeviceService
{
    #region Fields


    // Singleton device service instance.
    static GraphicsDeviceService singletonInstance;


    // Keep track of how many controls are sharing the singletonInstance.
    static int referenceCount;


    #endregion


    /// <summary>
    /// Constructor is private, because this is a singleton class:
    /// client controls should use the public AddRef method instead.
    /// </summary>
    GraphicsDeviceService(IntPtr windowHandle, int width, int height)
    {
        parameters = new PresentationParameters();

        parameters.BackBufferWidth = Math.Max(width, 1);
        parameters.BackBufferHeight = Math.Max(height, 1);
        parameters.BackBufferFormat = SurfaceFormat.Color;
        parameters.DepthStencilFormat = DepthFormat.Depth24;
        parameters.DeviceWindowHandle = windowHandle;
        parameters.PresentationInterval = PresentInterval.Immediate;
        // needed for rendering IsRenderTarget containers
        parameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        parameters.IsFullScreen = false;

        graphicsDevice = CreateGraphicsDevice(parameters);
    }


    /// <summary>
    /// Creates the graphics device, preferring the highest feature level the adapter supports.
    /// FL10_0 (guaranteed 8192-texture support) is ideal and is what real Windows GPUs get, but it
    /// has no built-in fallback and throws on adapters that top out lower - notably Wine on macOS,
    /// which exposes only feature level 9_3 and would otherwise crash the tool on launch. Falling
    /// back through HiDef and Reach lets the tool still start (at a lower max texture size) instead
    /// of failing outright. On hardware that supports FL10_0 the first attempt succeeds, so behavior
    /// there is unchanged.
    /// </summary>
    static GraphicsDevice CreateGraphicsDevice(PresentationParameters parameters)
    {
        // Highest-to-lowest. KNI supports FL10_0 (8192 textures); HiDef/Reach negotiate down to
        // whatever the adapter actually provides (e.g. 9_3 = 4096 textures under Wine on macOS).
        GraphicsProfile[] profilesHighestToLowest =
        {
            GraphicsProfile.FL10_0,
            GraphicsProfile.HiDef,
            GraphicsProfile.Reach,
        };

        NoSuitableGraphicsDeviceException? lastException = null;
        foreach (GraphicsProfile profile in profilesHighestToLowest)
        {
            try
            {
                return new GraphicsDevice(GraphicsAdapter.DefaultAdapter, profile, parameters);
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                lastException = exception;
            }
        }

        throw lastException ?? new NoSuitableGraphicsDeviceException("No supported graphics profile could create a device.");
    }


    /// <summary>
    /// Gets a reference to the singleton instance.
    /// </summary>
    public static GraphicsDeviceService AddRef(IntPtr windowHandle,
                                               int width, int height)
    {
        // Increment the "how many controls sharing the device" reference count.
        if (Interlocked.Increment(ref referenceCount) == 1)
        {
            // If this is the first control to start using the
            // device, we must create the singleton instance.
            singletonInstance = new GraphicsDeviceService(windowHandle,
                                                          width, height);
        }

        return singletonInstance;
    }


    /// <summary>
    /// Releases a reference to the singleton instance.
    /// </summary>
    public void Release(bool disposing)
    {
        // Decrement the "how many controls sharing the device" reference count.
        if (Interlocked.Decrement(ref referenceCount) == 0)
        {
            // If this is the last control to finish using the
            // device, we should dispose the singleton instance.
            if (disposing)
            {
                if (DeviceDisposing != null)
                    DeviceDisposing(this, EventArgs.Empty);

                graphicsDevice.Dispose();
            }

            graphicsDevice = null;
        }
    }

    
    /// <summary>
    /// Resets the graphics device to whichever is bigger out of the specified
    /// resolution or its current size. This behavior means the device will
    /// demand-grow to the largest of all its GraphicsDeviceControl clients.
    /// </summary>
    public void ResetDevice(int width, int height)
    {
        if (DeviceResetting != null)
            DeviceResetting(this, EventArgs.Empty);

        parameters.BackBufferWidth = Math.Max(parameters.BackBufferWidth, width);
        parameters.BackBufferHeight = Math.Max(parameters.BackBufferHeight, height);

        graphicsDevice.Reset(parameters);

        if (DeviceReset != null)
            DeviceReset(this, EventArgs.Empty);
    }

    
    /// <summary>
    /// Gets the current graphics device.
    /// </summary>
    public GraphicsDevice GraphicsDevice
    {
        get { return graphicsDevice; }
    }

    GraphicsDevice graphicsDevice;


    // Store the current device settings.
    PresentationParameters parameters;


    // IGraphicsDeviceService events.
    public event EventHandler<EventArgs>? DeviceCreated;
    public event EventHandler<EventArgs>? DeviceDisposing;
    public event EventHandler<EventArgs>? DeviceReset;
    public event EventHandler<EventArgs>? DeviceResetting;
}
