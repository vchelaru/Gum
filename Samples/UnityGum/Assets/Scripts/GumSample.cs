using System;
using System.IO;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Unity;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using UnityEngine;

/// <summary>
/// Shows Gum in Unity: a screen loaded by name from the project in StreamingAssets/GumProject, plus
/// Forms controls built in code. Created on scene load, so the scene itself stays empty. Run
/// Unity/build-unity-package.ps1 once before opening the project; it fills the Gum package's Plugins/.
///
/// --force-cpu uses the CPU fallback even on Direct3D 11.
/// --smoke-test runs without GumInput, pushes a click onto the button itself, and after a few frames
/// checks that the click reached the button and that the loaded screen's blue rectangle was drawn. It
/// writes the result to --smoke-result (default smoke-result.txt next to the player) and quits with 0
/// on success, 1 otherwise.
/// </summary>
public sealed class GumSample : MonoBehaviour
{
    const string ProjectFile = "GumProject/GumProject.gumx";

    GumRenderer _renderer;
    Button _button;
    Label _label;
    int _clickCount;
    bool _smokeTest;
    int _frame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool smokeTest = Array.IndexOf(args, "--smoke-test") >= 0;

        // Inactive while it is set up: GumRenderer initializes Gum in Awake, which runs as soon as the
        // GameObject is active.
        var host = new GameObject("Gum");
        host.SetActive(false);
        var renderer = host.AddComponent<GumRenderer>();
        renderer.ProjectFile = ProjectFile;
        renderer.ForceCpu = Array.IndexOf(args, "--force-cpu") >= 0;
        if (!smokeTest)
        {
            host.AddComponent<GumInput>();
        }
        var sample = host.AddComponent<GumSample>();
        sample._renderer = renderer;
        sample._smokeTest = smokeTest;
        host.SetActive(true);
    }

    void Start()
    {
        ElementSave screen = ObjectFinder.Self.GetElementSave("FirstScreen");
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        screenVisual.AddToRoot();

        var panel = new StackPanel();
        panel.X = 20;
        panel.Y = 20;
        panel.Spacing = 8;
        panel.AddToRoot();

        _label = new Label();
        _label.Text = "Clicks: 0";
        panel.AddChild(_label);

        _button = new Button();
        _button.Text = "Click me";
        _button.Click += (_, _) =>
        {
            _clickCount++;
            _label.Text = $"Clicks: {_clickCount}";
        };
        panel.AddChild(_button);

        var textBox = new TextBox();
        textBox.Width = 200;
        textBox.Placeholder = "Type here";
        panel.AddChild(textBox);

        GumService.Default.UseKeyboardDefaults();
    }

    void Update()
    {
        if (!_smokeTest)
        {
            return;
        }

        // GumInput isn't running, so this frame's cursor state is pushed here instead. Update runs after
        // GumRenderer's (it has DefaultExecutionOrder -50), so a push lands on the next frame's update.
        _frame++;
        float x = _button.Visual.AbsoluteLeft + _button.Visual.AbsoluteWidth / 2;
        float y = _button.Visual.AbsoluteTop + _button.Visual.AbsoluteHeight / 2;
        bool down = _frame == 3;
        GumService.Default.Cursor.SetMouseState(x, y, leftDown: down, middleDown: false, rightDown: false);

        if (_frame == 8)
        {
            FinishSmokeTest();
        }
    }

    void FinishSmokeTest()
    {
        // The loaded screen centers a 312x77 blue rectangle; sample inside it, clear of its text.
        int width = _renderer.CanvasPixelWidth;
        int height = _renderer.CanvasPixelHeight;
        int sampleX = width / 2 - 140;
        int sampleY = height / 2 + 28;
        Color32 pixel = ReadPixel(sampleX, sampleY);

        bool clicked = _clickCount == 1 && _label.Text == "Clicks: 1";
        bool drewScreen = pixel.b > 150 && pixel.r < 60 && pixel.g < 60;
        bool pass = clicked && drewScreen;

        string result =
            $"result: {(pass ? "PASSED" : "FAILED")}\n" +
            $"graphicsDevice: {SystemInfo.graphicsDeviceType}\n" +
            $"renderPath: {(_renderer.IsUsingGpu ? "GPU (SkiaGameRendering)" : "CPU fallback")}\n" +
            $"scriptingBackend: {(Application.platform == RuntimePlatform.WindowsPlayer ? ScriptingBackend() : "Editor")}\n" +
            $"canvas: {width}x{height}\n" +
            $"clicks: {_clickCount}, label: {_label.Text}\n" +
            $"pixel at ({sampleX},{sampleY}): {pixel}\n";

        string path = ArgumentAfter("--smoke-result") ?? Path.Combine(Application.dataPath, "..", "smoke-result.txt");
        File.WriteAllText(path, result);
        Debug.Log(result);
        Application.Quit(pass ? 0 : 1);
    }

    // Reads Gum's output at canvas pixel (x, y), counted from the top left like Skia. The texture's
    // row 0 is its bottom row.
    Color32 ReadPixel(int x, int y)
    {
        Texture texture = _renderer.Texture;
        int row = texture.height - 1 - y;
        if (texture is Texture2D texture2D)
        {
            return texture2D.GetPixel(x, row);
        }

        var readback = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = (RenderTexture)texture;
        readback.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        RenderTexture.active = previous;
        Color32 pixel = readback.GetPixel(x, row);
        Destroy(readback);
        return pixel;
    }

    static string ScriptingBackend()
    {
#if ENABLE_IL2CPP
        return "IL2CPP";
#else
        return "Mono";
#endif
    }

    static string ArgumentAfter(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
