using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SDL;
using Sdl3GumSample.Backend;
using static SDL.SDL3;

namespace Sdl3GumSample;

// Gum rendered by SDL3's 2D renderer. The left half is built in code, the right half is a screen
// loaded from Content/GumProject/Sdl3GumProject.gumx. Run with --screenshot <file.png> to save the
// third frame and exit.
internal static unsafe class Program
{
    private static int Main(string[] args)
    {
        string? screenshotPath = args.Length == 2 && args[0] == "--screenshot" ? args[1] : null;

        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
        {
            Console.Error.WriteLine($"SDL_Init failed: {SDL_GetError()}");
            return 1;
        }

        SDL_Window* window;
        SDL_Renderer* renderer;
        SDL_CreateWindowAndRenderer("Gum on SDL3", 800, 600, SDL_WindowFlags.SDL_WINDOW_RESIZABLE, &window, &renderer);
        SDL_SetRenderVSync(renderer, 1);

        string contentDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "GumProject") + Path.DirectorySeparatorChar;
        SdlGum.Initialize(renderer, contentDirectory);

        GraphicalUiElement root = new GraphicalUiElement(new InvisibleRenderable());
        root.WidthUnits = DimensionUnitType.RelativeToParent;
        root.HeightUnits = DimensionUnitType.RelativeToParent;
        root.Width = 0;
        root.Height = 0;

        GraphicalUiElement codeHalf = BuildCodeOnlyUi();
        root.Children.Add(codeHalf);

        GraphicalUiElement fileHalf = SdlGum.LoadScreen(Path.Combine(contentDirectory, "Sdl3GumProject.gumx"), "MainScreen");
        fileHalf.WidthUnits = DimensionUnitType.PercentageOfParent;
        fileHalf.Width = 50;
        fileHalf.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        fileHalf.X = 50;
        root.Children.Add(fileHalf);

        int frame = 0;
        bool running = true;
        while (running)
        {
            SDL_Event e;
            while (SDL_PollEvent(&e))
            {
                if (e.Type == SDL_EventType.SDL_EVENT_QUIT)
                {
                    running = false;
                }
            }

            // Gum lays out against the canvas size; refresh it when the window size changes.
            int width;
            int height;
            SDL_GetRenderOutputSize(renderer, &width, &height);
            if (GraphicalUiElement.CanvasWidth != width || GraphicalUiElement.CanvasHeight != height)
            {
                GraphicalUiElement.CanvasWidth = width;
                GraphicalUiElement.CanvasHeight = height;
                root.UpdateLayout();
            }

            SDL_SetRenderDrawColor(renderer, 40, 44, 52, 255);
            SDL_RenderClear(renderer);
            SdlGum.Draw(root, renderer);

            frame++;
            if (screenshotPath != null && frame == 3)
            {
                SDL_Surface* pixels = SDL_RenderReadPixels(renderer, null);
                SDL_SavePNG(pixels, screenshotPath);
                SDL_DestroySurface(pixels);
                running = false;
            }

            SDL_RenderPresent(renderer);
        }

        SdlTexture.DestroyAll();
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        SDL_Quit();
        return 0;
    }

    // The same kind of UI as the .gumx screen, built in code: a nine-slice panel holding a
    // left-to-right row of sprites.
    private static GraphicalUiElement BuildCodeOnlyUi()
    {
        GraphicalUiElement panel = new GraphicalUiElement(new SdlNineSlice());
        panel.SetProperty("SourceFile", "Frame.png");
        panel.X = 20;
        panel.Y = 20;
        panel.Width = 340;
        panel.Height = 220;

        GraphicalUiElement row = new GraphicalUiElement(new InvisibleRenderable());
        row.X = 16;
        row.Y = 16;
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 8;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        panel.Children.Add(row);

        row.Children.Add(CreateBear(widthPercent: 100));
        row.Children.Add(CreateBear(widthPercent: 200));

        GraphicalUiElement cropped = CreateBear(widthPercent: 400);
        cropped.TextureAddress = TextureAddress.Custom;
        cropped.TextureWidth = 20;
        cropped.TextureHeight = 20;
        row.Children.Add(cropped);

        GraphicalUiElement wideFrame = new GraphicalUiElement(new SdlNineSlice());
        wideFrame.SetProperty("SourceFile", "Frame.png");
        wideFrame.X = 16;
        wideFrame.Y = -16;
        wideFrame.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        wideFrame.YOrigin = VerticalAlignment.Bottom;
        wideFrame.WidthUnits = DimensionUnitType.RelativeToParent;
        wideFrame.Width = -32;
        wideFrame.Height = 40;
        panel.Children.Add(wideFrame);

        return panel;
    }

    private static GraphicalUiElement CreateBear(float widthPercent)
    {
        GraphicalUiElement bear = new GraphicalUiElement(new SdlSprite());
        bear.SetProperty("SourceFile", "BearTexture.png");
        bear.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        bear.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        bear.Width = widthPercent;
        bear.Height = widthPercent;
        return bear;
    }
}
