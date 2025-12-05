# Silk.NET

## Introduction

This page assumes you have an existing Silk.NET project. This can be an empty project or an existing game.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.SkiaSharp NuGet package ([https://www.nuget.org/packages/Gum.SkiaSharp](https://www.nuget.org/packages/Gum.SkiaSharp))

Modify csproj:

```xml
<PackageReference Include="Gum.SkiaSharp" />
```

Or add through command line:

```bash
dotnet add package Gum.SkiaSharp
```

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following projects to your solution:

* \<Gum Root>/SkiaGum/SkiaGum.csproj
* \<GumRoot>/GumCommon/GumCommon.csproj

Next, add SkiaGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\SkiaGum\SkiaGum.csproj" />
```

## Initializing Gum

Gum requires SkiaSharp to render in Silk.NET. A full Silk.NET project requires a few hundred lines of code, so an entire Program file is not included here. The relevant code is included in the following block of code:

```csharp
using SkiaSharp;
using RenderingLibrary;
using Gum.Wireframe;
using SkiaGum.GueDeriving;
using RenderingLibrary.Graphics;
using SkiaGum;
using SilkNetGum.Screens;
using Gum.Managers;
using GumRuntime;

unsafe class Program
{
    #region Fields/Properties

    private static Sdl sdl;
    private static GL gl;
    private static Window* window;
    private static void* glContext;
    private static bool running = true;

    static SKCanvas canvas;

    #endregion



    #region General Setup/Functions

    private static string GetSdlError()
    {
        byte* error = sdl.GetError();
        return Marshal.PtrToStringUTF8((IntPtr)error) ?? "Unknown error";
    }
    static unsafe void Main(string[] args)
    {
        try
        {
            //...
            using var grGlInterface = GRGlInterface.Create(loadFunction);
            grGlInterface.Validate();
            using var grContext = GRContext.CreateGl(grGlInterface);
            var renderTarget = new GRBackendRenderTarget(800, 600, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
            using var surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            canvas = surface.Canvas;

            GumUI.Initialize(canvas);

            gl.Viewport(0, 0, 600, 600);

            Event ev = new Event();
            // Main loop

            int frames = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (running)
            {

                if (sw.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(frames);
                    sw.Restart();

                    frames = 0;
                }
                frames++;
                // Render
                gl.ClearColor(0.2f, 0.3f, 0.8f, 1.0f);
                gl.Clear((uint)GLEnum.ColorBufferBit);

                grContext.ResetContext();
                // canvas.Clear(SKColors.Cyan);
                //_renderer.Render(canvas);

                GumUI.Default.Draw();
                canvas.Flush();

                // Swap buffers
                sdl.GLSwapWindow(window);
            }
        }
        finally
        {
            canvas.Dispose();
            ///
        }
    }

    private static nint loadFunction(string name)
    {
        return (nint)sdl.GLGetProcAddress(name);
    }

    #endregion
}
```

For a working project, see the Gum Silk.NET sample:



{% embed url="https://github.com/vchelaru/Gum/tree/master/Samples/SilkNetGum" %}

<figure><img src="../../../../.gitbook/assets/22_12 20 45.png" alt=""><figcaption><p>Gum running in a Silk.NET project</p></figcaption></figure>
