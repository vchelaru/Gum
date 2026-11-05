using Gum.ProjectServices.CodeGeneration;
using System;
using System.Collections.Generic;
using System.IO;
using Gum;
using Gum.Forms.Controls;
using Gum.Forms;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Input;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using ToolsUtilities;

namespace GumPreview;

/// <summary>
/// Loads the .gumx/.gumj passed on the command line and shows the requested screen/component, live
/// and interactive (issue #4697). No codegen, no recompile - Forms controls work out of the box via
/// <c>GumService.Initialize</c>'s from-file registration (see the gum-forms-behaviors skill).
/// </summary>
public class Game1 : Game, IPreviewDefaultsTarget
{
    private const double SelectionPollIntervalSeconds = 0.25;

    private readonly GraphicsDeviceManager _graphics;
    private readonly string _gumxPath;
    private readonly string? _selectionFilePath;
    private readonly string? _contentRootDirectory;
    private readonly UnattendedPreviewRun? _unattended;
    private readonly string? _screenshotPath;
    private readonly string? _focusName;
    private readonly string? _typedText;
    private readonly bool _selectAll;
    private readonly PreviewDebugLog? _debugLog;
    private IInputReceiver? _loggedFocus;
    private PreviewFrameCapture? _frameCapture;
    private static readonly Microsoft.Xna.Framework.Input.Keys[] AllKeys =
        (Microsoft.Xna.Framework.Input.Keys[])Enum.GetValues(typeof(Microsoft.Xna.Framework.Input.Keys));
    private Action<string>? _logSelectionUpdate;
    private bool _logGeometryAfterReload;
    private string? _languageName;
    private PreviewInputTracker? _inputTracker;

    private readonly PreviewReloadPolicy _reloadPolicy = new PreviewReloadPolicy();
    private readonly PreviewCanvasSize _canvasSize = new PreviewCanvasSize();
    private PreviewSelectionMessage _selection;
    private DateTime _lastSelectionFileWriteTimeUtc;
    private double _secondsSinceLastSelectionPoll;
    private bool _isElementMissing;
    private string? _loadError;
    private readonly PreviewFailureLog _failureLog = new PreviewFailureLog();
    private bool _isRetinaBackingEnabled;
    private BackingScale _backingScale;

    /// <summary>The process exit code: 0, or 1 when an unattended run failed.</summary>
    public int UnattendedExitCode { get; private set; }

    /// <param name="contentRootDirectory">
    /// Overrides where relative content (fonts, textures) resolves from after load (issue #4748).
    /// Needed when <paramref name="gumxPath"/> is a temporary JSON copy of a .gumx project elsewhere
    /// (the Native AOT build can't load .gumx directly) - content still lives beside the original
    /// .gumx, not the temp copy. Null for an ordinary project, where the project's own directory is
    /// already correct.
    /// </param>
    /// <param name="unattended">Set for an <c>--exit-after</c> run: the first drawn frame ends it.</param>
    /// <param name="screenshotPath">With <paramref name="unattended"/>, where that frame is saved as a PNG.</param>
    /// <param name="focusName">A Forms control in the shown element to focus once it loads.</param>
    /// <param name="typedText">With <paramref name="focusName"/>, text typed into that text box.</param>
    public Game1(string gumxPath, string elementName, string? selectionFilePath, string? contentRootDirectory = null,
        UnattendedPreviewRun? unattended = null, string? screenshotPath = null, string? focusName = null, string? typedText = null, bool selectAll = false,
        PreviewDebugLog? debugLog = null)
    {
        _debugLog = debugLog;
        _focusName = focusName;
        _typedText = typedText;
        _selectAll = selectAll;
        _gumxPath = gumxPath;
        _selection = new PreviewSelectionMessage(elementName);
        _selectionFilePath = selectionFilePath;
        _contentRootDirectory = contentRootDirectory;
        _unattended = unattended;
        _screenshotPath = screenshotPath;
        _backingScale = new BackingScale(1);

        _graphics = new GraphicsDeviceManager(this);
        // The HiDef profile Apos.Shapes needs; the list lives in PreviewDefaults.
        PreviewDefaults.Apply(this, PreviewDefaultPhase.Constructor);
        _graphics.PreparingDeviceSettings += PreviewDeviceSettings.PreserveBackBufferContents;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += HandleClientSizeChanged;
        KeepWindowOffScreen();
        UpdateWindowTitle();
    }

    void IPreviewDefaultsTarget.UseHiDefProfile() => _graphics.GraphicsProfile = GraphicsProfile.HiDef;
    void IPreviewDefaultsTarget.InitializeShapes() => ShapeRenderer.Self.Initialize();
    void IPreviewDefaultsTarget.UseKeyboardDefaults() => GumService.Default.UseKeyboardDefaults();
    void IPreviewDefaultsTarget.UseGamepadDefaults() => GumService.Default.UseGamepadDefaults();

    // MonoGame re-centers the window when the graphics settings apply, after the constructor, so
    // the placement is repeated here and on every update.
    private void KeepWindowOffScreen()
    {
        if (UnattendedWindow.StartPosition(isUnattended: _unattended != null) is (int x, int y)
            && (Window.Position.X != x || Window.Position.Y != y))
        {
            Window.Position = new Point(x, y);
        }
    }

    protected override void Initialize()
    {
        string? interactionError;
        try
        {
            interactionError = LoadProject();
        }
        catch (Exception exception)
        {
            // A missing or malformed project. Unattended, that is a failed run; attended, the window
            // stays up with the reason in its title, because the tool launches it with no console.
            string message = PreviewFailureLog.LoadMessage(_gumxPath, exception);
            _loadError = message;
            _debugLog?.LogException("load", exception);
            UnattendedExitCode = 1;
            Window.Title = PreviewWindowTitle.ForLoadFailure(message);
            if (_unattended != null)
            {
                FailUnattended(_unattended.TryFail(message.TrimEnd('.') + "."));
            }
            else
            {
                Console.Error.WriteLine($"GumPreview: {message}");
            }
            base.Initialize();
            return;
        }

        if (_unattended != null)
        {
            if (_isElementMissing)
            {
                FailUnattended(_unattended.TryFail("the element to show was not found."));
            }
            else if (interactionError != null)
            {
                FailUnattended(_unattended.TryFail(interactionError));
            }
            else
            {
                _unattended.MarkLoaded();
            }
        }
        else if (interactionError != null)
        {
            Console.Error.WriteLine($"GumPreview: {interactionError}");
        }

        base.Initialize();
    }

    private string? LoadProject()
    {
        KeepWindowOffScreen();
        GumService.Default.Initialize(this, _gumxPath);

        // GumService.Initialize just set this to _gumxPath's own directory - override it back to the
        // original project's directory when _gumxPath is actually a temporary converted copy
        // elsewhere (issue #4748), so relative content (fonts, textures) still resolves correctly.
        if (!string.IsNullOrEmpty(_contentRootDirectory))
        {
            FileManager.RelativeDirectory = _contentRootDirectory;
        }

        // Shapes, tab/keyboard/gamepad navigation: everything in PreviewDefaults. Must come after
        // GumService.Default.Initialize - ShapeRenderer.Initialize reads the initialized GumService/GraphicsDevice.
        PreviewDefaults.Apply(this, PreviewDefaultPhase.AfterGumInitialize);

        // Registered before the project's elements are built so a container's SourceShaderFile
        // resolves as it loads.
        PreviewShaderResolver.Register(GraphicsDevice);

        // The window is sized for the backing scale on the first Update, once it is showing; a resize
        // here is undone when MonoGame shows it.
        _isRetinaBackingEnabled = MacRetinaBacking.TryEnable(Window.Handle);

        // Preview rebuilds after such a reload, so "the old UI stays on screen" would be wrong.
        GumHotReloadManager.PrintsShownElementGone = false;
        GumService.Default.EnableHotReload(_gumxPath);
        GumService.Default.HotReloadFinished += HandleHotReloadFinished;
        if (_debugLog != null)
        {
            // Every variable the tool saved that a reload applied, for the log; costs nothing when logging is off.
            GumHotReloadManager.VariableApplied += LogAppliedVariable;
        }

        ApplyCanvasSizeIfChanged(isWindowShown: false);

        // The tool writes the full selection (state, orderer) before launching; --element alone is
        // the fallback for a launch with no selection file.
        if (_selectionFilePath != null && File.Exists(_selectionFilePath))
        {
            _lastSelectionFileWriteTimeUtc = File.GetLastWriteTimeUtc(_selectionFilePath);
            PreviewSelectionMessage? initial = TryReadSelectionFile();
            if (initial != null)
            {
                _selection = initial;
            }
        }
        _debugLog?.LogToolSelection(_selection.ElementName, _selection.CategoryName, _selection.StateName);
        ApplySiblingOrdering();
        _languageName = PreviewLanguage.Apply(ObjectFinder.Self.GumProjectSave, GumService.Default.LocalizationService);
        ShowElement();
        return ApplyInteraction();
    }

    protected override void Update(GameTime gameTime)
    {
        KeepWindowOffScreen();
        if (_loadError != null)
        {
            base.Update(gameTime);
            return;
        }
        UpdateBackingScale();
        ApplyCanvasSizeIfChanged(isWindowShown: true);
        RebuildAfterReloadIfNeeded();
        SyncLanguage();
        if (_debugLog != null)
        {
            LogInput(_debugLog);
        }
        GumService.Default.Update(gameTime);
        PollSelectionFile(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_loadError != null)
        {
            // Nothing loaded to draw; a dark red window with the reason in its title.
            GraphicsDevice.Clear(new Color(96, 16, 16));
            base.Draw(gameTime);
            return;
        }

        if (_unattended != null && _unattended.TryClaimCapture())
        {
            FinishUnattended();
        }

        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        base.Draw(gameTime);
        if (_frameCapture is { HasPending: true })
        {
            CaptureBackBuffer(_frameCapture);
        }
    }

    // What is on the window right now, before it is presented: the log's "frame" lines point at these
    // files so the picture can be compared with the geometry lines around it.
    private void CaptureBackBuffer(PreviewFrameCapture capture)
    {
        string path = capture.Claim(out string reason);
        try
        {
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            Color[] pixels = new Color[width * height];
            GraphicsDevice.GetBackBufferData(pixels);
            WriteOpaquePng(pixels, width, height, path);
            _debugLog?.LogFrame(Path.GetFileName(path), reason);
        }
        catch (Exception exception)
        {
            _debugLog?.LogException("frame capture", exception);
        }
    }

    // Only runs with --log. Focus is read before Update, so a focus change shows up the frame after
    // the click or key that caused it; keys are the ones Update is about to forward to that control.
    private void LogInput(PreviewDebugLog log)
    {
        IInputReceiver? focus = InteractiveGue.CurrentInputReceiver;
        if (!ReferenceEquals(focus, _loggedFocus))
        {
            if (_loggedFocus is TextBox previousBox)
            {
                previousBox.SelectionVisualsUpdated -= _logSelectionUpdate;
            }
            _loggedFocus = focus;
            log.LogFocus(DescribeControl(focus));
            if (focus is TextBox newBox)
            {
                string name = DescribeControl(newBox)!;
                _logSelectionUpdate = caller => log.LogSelectionUpdate(name, caller);
                newBox.SelectionVisualsUpdated += _logSelectionUpdate;
            }
        }

        if (_logGeometryAfterReload && focus is TextBox reloadedBox)
        {
            _logGeometryAfterReload = false;
            _frameCapture?.Request("reload");
            log.LogGeometry(DescribeControl(reloadedBox)!, reloadedBox.DescribeSelectionGeometry());
        }

        _frameCapture ??= new PreviewFrameCapture(Path.GetDirectoryName(log.FilePath) ?? ".", () => Environment.TickCount64);
        _inputTracker ??= new PreviewInputTracker(log, requestFrame: reason => _frameCapture.Request(reason));
        Cursor cursor = GumService.Default.Cursor;
        _inputTracker.ObserveMouse(cursor.X, cursor.Y, cursor.PrimaryDown,
            () => cursor.VisualOver == null ? null : $"{cursor.VisualOver.GetType().Name} '{cursor.VisualOver.Name}'");
        _inputTracker.ObserveTextBox(DescribeTextBox(focus as TextBox));

        LogGamepads(log);

        Keyboard keyboard = GumService.Default.Keyboard;
        List<string>? released = null;
        foreach (Microsoft.Xna.Framework.Input.Keys key in AllKeys)
        {
            if (keyboard.KeyReleased(key))
            {
                (released ??= new List<string>()).Add(key.ToString());
            }
        }
        if (released != null)
        {
            log.LogKeyUp(released);
        }

        IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> typed = keyboard.KeysTyped;
        if (typed.Count == 0)
        {
            return;
        }

        List<string> keys = new List<string>(typed.Count);
        foreach (Microsoft.Xna.Framework.Input.Keys key in typed)
        {
            keys.Add(key.ToString());
        }
        if (keys.Count > 0)
        {
            string modifiers = $"{(keyboard.IsCtrlDown ? "Ctrl" : "")}{(keyboard.IsShiftDown ? "Shift" : "")}{(keyboard.IsAltDown ? "Alt" : "")}";
            log.LogKeys(keys, modifiers.Length == 0 ? "none" : modifiers, DescribeControl(focus));
        }
    }

    private readonly int?[] _loggedGamepadState = new int?[4];
    private bool? _loggedWindowActive;
    private readonly PreviewGamepadLog _gamepadLog = new PreviewGamepadLog();
    private bool _loggedGamepadStartup;
    private readonly PreviewGamepadLog.GamepadProbes _gamepadProbes =
        PreviewGamepadLog.ParseProbes(Environment.GetEnvironmentVariable(PreviewGamepadLog.ProbesEnvironmentVariable));

    // Only runs with --log. Logs what the OS reports (raw MonoGame state) next to what Gum holds, so a
    // dead controller shows whether the device is undetected, the window is inactive, or Gum drops it.
    // Runs from LogInput, i.e. BEFORE GumService.Update in the same frame: raw is this frame's OS state,
    // Gum's pad holds the state committed by the previous frame's Update (ButtonPushed lasts one Update,
    // so a push is still readable here on the next frame, never already consumed).
    private void LogGamepads(PreviewDebugLog log)
    {
        if (!_loggedGamepadStartup)
        {
            _loggedGamepadStartup = true;
            log.LogGamepad($"startup os={Environment.OSVersion} runtime={Environment.Version} "
                + $"monogame={typeof(Microsoft.Xna.Framework.Game).Assembly.GetName().Version} "
                + $"maxPads={Microsoft.Xna.Framework.Input.GamePad.MaximumGamePadCount} "
                + $"SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS={Environment.GetEnvironmentVariable("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS") ?? "(unset)"} "
                + "order=LogGamepads reads before GumService.Update (gum state is one frame behind raw)");
            log.LogGamepad($"probes {PreviewGamepadLog.ProbesEnvironmentVariable}={_gamepadProbes.Active}"
                + (_gamepadProbes.Warning != null ? $" WARNING {_gamepadProbes.Warning}" : "")
                + $" (joystick={_gamepadProbes.Joystick} xinput={_gamepadProbes.XInput} fix={_gamepadProbes.Fix})");
            if (_gamepadProbes.Fix)
            {
                bool set = SdlLibrary.TrySetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
                log.LogGamepad($"fix SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS=1 set={set}");
            }
            log.LogGamepad(PreviewGamepadLog.InputLostHint);
        }

        if (_loggedWindowActive != IsActive)
        {
            _loggedWindowActive = IsActive;
            log.LogGamepad($"window active={IsActive} navigationPads={FrameworkElement.GamePadsForUiControl.Count}");
        }

        // Every ~5 s the heartbeat re-states each layer, so "no change" is distinguishable from "not logging".
        bool heartbeat = _gamepadLog.HeartbeatDue(Environment.TickCount64);
        if (_gamepadProbes.Joystick || _gamepadProbes.XInput)
        {
            LogJoysticksAndXInput(log, heartbeat);
        }

        Gum.Input.GamePad[] pads = GumService.Default.Gamepads;
        for (int i = 0; i < pads.Length && i < _loggedGamepadState.Length; i++)
        {
            bool rawConnected = Microsoft.Xna.Framework.Input.GamePad.GetState(i).IsConnected;
            bool gumConnected = pads[i]?.IsConnected == true;
            // Logged on the first frame and whenever either side changes; encoded as raw*2+gum.
            int code = (rawConnected ? 2 : 0) + (gumConnected ? 1 : 0);
            if (_loggedGamepadState[i] != code)
            {
                _loggedGamepadState[i] = code;
                log.LogGamepad($"pad {i} raw connected={rawConnected} gum connected={gumConnected}");
                if (rawConnected)
                {
                    Microsoft.Xna.Framework.Input.GamePadCapabilities caps = Microsoft.Xna.Framework.Input.GamePad.GetCapabilities(i);
                    log.LogGamepad($"pad {i} caps name='{caps.DisplayName}' id='{caps.Identifier}' type={caps.GamePadType} "
                        + $"dpad(up={caps.HasDPadUpButton} down={caps.HasDPadDownButton} left={caps.HasDPadLeftButton} right={caps.HasDPadRightButton}) "
                        + $"A={caps.HasAButton} leftStick={caps.HasLeftXThumbStick && caps.HasLeftYThumbStick} isConnected={caps.IsConnected}");
                }
            }

            if (rawConnected)
            {
                // GamePadDeadZone.None so a stick the default dead zone swallows is still visible.
                Microsoft.Xna.Framework.Input.GamePadState state =
                    Microsoft.Xna.Framework.Input.GamePad.GetState(i, Microsoft.Xna.Framework.Input.GamePadDeadZone.None);
                string? rawLine = _gamepadLog.Changed($"raw{i}", PreviewGamepadLog.FormatRaw(
                    state.IsConnected, state.PacketNumber, PressedButtons(state),
                    state.ThumbSticks.Left.X, state.ThumbSticks.Left.Y, state.ThumbSticks.Right.X, state.ThumbSticks.Right.Y,
                    state.Triggers.Left, state.Triggers.Right));
                if (rawLine != null)
                {
                    log.LogGamepad($"pad {i} raw {rawLine}");
                }
                if (heartbeat)
                {
                    log.LogGamepad($"pad {i} raw " + PreviewGamepadLog.FormatHeartbeat(
                        IsActive, state.IsConnected, state.PacketNumber, PressedButtons(state)));
                }
            }

            if (pads[i] == null)
            {
                continue;
            }

            string? membership = _gamepadLog.Changed($"member{i}", FrameworkElement.GamePadsForUiControl.Contains(pads[i]).ToString());
            if (membership != null)
            {
                log.LogGamepad($"pad {i} inGamePadsForUiControl={membership}");
            }
            string resting = PreviewGamepadLog.FormatGumButton(false, false, false, false);
            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                bool pushed = pads[i].ButtonPushed(button);
                string? gumLine = _gamepadLog.Changed($"gum{i}.{button}", PreviewGamepadLog.FormatGumButton(
                    pads[i].ButtonDown(button), pushed, pads[i].ButtonReleased(button), pads[i].ButtonRepeatRate(button)));
                // The all-false resting state is the starting value and not worth a line.
                if (gumLine != null && gumLine != resting)
                {
                    log.LogGamepad($"pad {i} gum {button} {gumLine}");
                }
                if (pushed)
                {
                    log.LogGamepad($"pad {i} pushed {button} focused={DescribeControl(InteractiveGue.CurrentInputReceiver) ?? "(none)"}");
                }
            }
        }
    }

    // SDL's joystick layer (below the GameController mapping GamePad.GetState uses) and Windows XInput
    // (independent of SDL): together they show where pad input is lost.
    private void LogJoysticksAndXInput(PreviewDebugLog log, bool heartbeat)
    {
        string prefix = heartbeat ? "heartbeat " : "";
        int last = -1;
        try
        {
            if (_gamepadProbes.Joystick)
            {
                last = Microsoft.Xna.Framework.Input.Joystick.LastConnectedIndex;
            }
        }
        catch (Exception ex)
        {
            string? failed = _gamepadLog.Changed("joystick.error", ex.Message);
            if (failed != null) log.LogGamepad($"joystick error {failed}");
        }
        if (heartbeat && _gamepadProbes.Joystick)
        {
            log.LogGamepad($"joystick heartbeat lastConnectedIndex={last} supported={Microsoft.Xna.Framework.Input.Joystick.IsSupported}");
        }
        for (int i = 0; i <= last; i++)
        {
            Microsoft.Xna.Framework.Input.JoystickCapabilities caps = Microsoft.Xna.Framework.Input.Joystick.GetCapabilities(i);
            string? capsLine = _gamepadLog.Changed($"joycaps{i}", PreviewGamepadLog.FormatJoystickCaps(
                caps.IsConnected, caps.DisplayName, caps.Identifier, caps.ButtonCount, caps.AxisCount, caps.HatCount));
            if (capsLine != null) log.LogGamepad($"joystick {i} caps {capsLine}");
            if (!caps.IsConnected) continue;

            Microsoft.Xna.Framework.Input.JoystickState state = Microsoft.Xna.Framework.Input.Joystick.GetState(i);
            List<bool> buttons = new();
            foreach (Microsoft.Xna.Framework.Input.ButtonState b in state.Buttons)
            {
                buttons.Add(b == Microsoft.Xna.Framework.Input.ButtonState.Pressed);
            }
            List<string> hats = new();
            foreach (Microsoft.Xna.Framework.Input.JoystickHat hat in state.Hats)
            {
                string h = (hat.Up == Microsoft.Xna.Framework.Input.ButtonState.Pressed ? "U" : "")
                    + (hat.Down == Microsoft.Xna.Framework.Input.ButtonState.Pressed ? "D" : "")
                    + (hat.Left == Microsoft.Xna.Framework.Input.ButtonState.Pressed ? "L" : "")
                    + (hat.Right == Microsoft.Xna.Framework.Input.ButtonState.Pressed ? "R" : "");
                hats.Add(h);
            }
            string text = PreviewGamepadLog.FormatJoystickState(state.IsConnected, buttons, state.Axes, hats);
            string? line = _gamepadLog.Changed($"joystate{i}", text);
            if (line != null || heartbeat) log.LogGamepad($"joystick {i} {prefix}state {text}");
        }

        for (int i = 0; i < 4 && _gamepadProbes.XInput; i++)
        {
            string? text = XInputProbe.Read(i);
            if (text == null)
            {
                if (heartbeat && i == 0) log.LogGamepad($"xinput {prefix}unavailable lib={XInputProbe.Library}");
                continue;
            }
            string? line = _gamepadLog.Changed($"xinput{i}", text);
            if (line != null || (heartbeat && text != "connected=False"))
            {
                log.LogGamepad($"xinput {i} {prefix}{text}");
            }
        }
    }

    private static List<string> PressedButtons(Microsoft.Xna.Framework.Input.GamePadState state)
    {
        List<string> pressed = new List<string>();
        void Add(Microsoft.Xna.Framework.Input.ButtonState buttonState, string name)
        {
            if (buttonState == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                pressed.Add(name);
            }
        }
        Add(state.Buttons.A, "A");
        Add(state.Buttons.B, "B");
        Add(state.Buttons.X, "X");
        Add(state.Buttons.Y, "Y");
        Add(state.Buttons.LeftShoulder, "LeftShoulder");
        Add(state.Buttons.RightShoulder, "RightShoulder");
        Add(state.Buttons.Start, "Start");
        Add(state.Buttons.Back, "Back");
        Add(state.Buttons.BigButton, "BigButton");
        Add(state.Buttons.LeftStick, "LeftStick");
        Add(state.Buttons.RightStick, "RightStick");
        Add(state.DPad.Up, "DPadUp");
        Add(state.DPad.Down, "DPadDown");
        Add(state.DPad.Left, "DPadLeft");
        Add(state.DPad.Right, "DPadRight");
        return pressed;
    }

    private static TextBoxSnapshot? DescribeTextBox(TextBox? textBox)
    {
        if (textBox == null)
        {
            return null;
        }

        // The highlight is a pool of clones of "SelectionInstance" that share its parent and name.
        int count = 0;
        int visible = 0;
        GraphicalUiElement? original = textBox.Visual?.GetGraphicalUiElementByName("SelectionInstance");
        if (original?.Parent != null)
        {
            foreach (GraphicalUiElement sibling in original.Parent.Children)
            {
                if (sibling.Name == original.Name)
                {
                    count++;
                    if (sibling.Visible)
                    {
                        visible++;
                    }
                }
            }
        }
        return new TextBoxSnapshot(DescribeControl(textBox)!, textBox.Text ?? "", textBox.TextWrapping == TextWrapping.Wrap,
            textBox.SelectionStart, textBox.SelectionLength, textBox.CaretIndex, count, visible, textBox.DescribeSelectionGeometry);
    }

    private static string? DescribeControl(IInputReceiver? receiver) =>
        receiver == null ? null : $"{receiver.GetType().Name} '{(receiver as FrameworkElement)?.Visual?.Name}'";

    // Runs once at startup for --focus/--type; a later selection change shows a fresh element.
    private string? ApplyInteraction()
    {
        if (_focusName == null || _isElementMissing)
        {
            return null;
        }
        foreach (GraphicalUiElement root in GumService.Default.Root.Children)
        {
            if (root.ElementSave != null && root.ElementSave.Name == _selection.ElementName)
            {
                return PreviewInteraction.Apply(root, _focusName, _typedText, _selectAll);
            }
        }
        return null;
    }

    private void FinishUnattended()
    {
        if (_screenshotPath != null)
        {
            try
            {
                SaveFrame(_screenshotPath);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"GumPreview: could not write the screenshot {_screenshotPath}: {exception.Message}");
                UnattendedExitCode = 1;
                Exit();
                return;
            }
            Console.WriteLine($"GumPreview: wrote {_screenshotPath}");
        }
        Exit();
    }

    // Draws a frame into an offscreen target of the canvas size and saves it. The window's own
    // back buffer can't be used: on the first frame its drawable may not have been resized to the
    // project's canvas yet, and a window partly off the screen doesn't keep every pixel.
    private void SaveFrame(string path)
    {
        int width = (int)GraphicalUiElement.CanvasWidth;
        int height = (int)GraphicalUiElement.CanvasHeight;
        using RenderTarget2D target = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        GraphicsDevice.SetRenderTarget(null);

        // Blending leaves partial alpha where translucent content was drawn; the window shows the
        // frame opaque, so the PNG is too.
        Color[] pixels = new Color[width * height];
        target.GetData(pixels);
        WriteOpaquePng(pixels, width, height, path);
    }

    private void WriteOpaquePng(Color[] pixels, int width, int height, string path)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].A = 255;
        }
        using Texture2D opaque = new Texture2D(GraphicsDevice, width, height);
        opaque.SetData(pixels);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        using FileStream stream = File.Create(path);
        opaque.SaveAsPng(stream, width, height);
    }

    private void FailUnattended(string? message)
    {
        if (message == null)
        {
            return;
        }
        Console.Error.WriteLine(message);
        UnattendedExitCode = 1;
        Exit();
    }

    // The tool has no live channel to this process, so it hands off new selections by rewriting
    // this file (see PreviewLauncher.PushSelection in Tool/EditorTabPlugin.Core); polling the
    // file's last-write time is simpler and more robust across platforms than a named pipe.
    private void PollSelectionFile(GameTime gameTime)
    {
        if (_selectionFilePath == null)
        {
            return;
        }

        _secondsSinceLastSelectionPoll += gameTime.ElapsedGameTime.TotalSeconds;
        if (_secondsSinceLastSelectionPoll < SelectionPollIntervalSeconds)
        {
            return;
        }
        _secondsSinceLastSelectionPoll = 0;

        if (!File.Exists(_selectionFilePath))
        {
            return;
        }

        DateTime writeTimeUtc = File.GetLastWriteTimeUtc(_selectionFilePath);
        if (writeTimeUtc <= _lastSelectionFileWriteTimeUtc)
        {
            return;
        }

        PreviewSelectionMessage? message = TryReadSelectionFile();
        if (message == null)
        {
            // The tool may still be mid-write; try again on the next poll rather than skipping it.
            return;
        }

        _lastSelectionFileWriteTimeUtc = writeTimeUtc;

        // Activate is set only for an explicit re-click of the tool's Preview button, so this
        // process - not the tool, which has no cross-platform way to foreground another process's
        // window - raises its own window. A passive selection change while browsing the tool's
        // tree leaves it unset, so the preview updates live without stealing focus (issue #4717
        // follow-up).
        if (message.Activate)
        {
            ActivateWindow();
        }

        bool selectionChanged = !message.HasSameSelection(_selection);
        _selection = message;
        if (selectionChanged)
        {
            _debugLog?.LogToolSelection(message.ElementName, message.CategoryName, message.StateName);
        }
        ApplySiblingOrdering();
        if (selectionChanged)
        {
            // A throwing build must not escape Update; the last good frame stays, reported once.
            _failureLog.TryRun(ShowElement, $"could not show '{_selection.ElementName}'", Console.Error, _debugLog);
        }
    }

    private PreviewSelectionMessage? TryReadSelectionFile() => PreviewSelectionFile.TryRead(_selectionFilePath!);

    // Mirrors the tool's Performance panel "Sort by batch" option so the preview renders in the
    // same sibling order the tool's canvas does (issue #4856).
    private void ApplySiblingOrdering() =>
        Renderer.SiblingOrdering = _selection.SortByBatchKey ? BatchKeyGroupedOrderer.Instance : HierarchicalOrderer.Instance;

    private void ActivateWindow() => SdlLibrary.TryRaiseWindow(Window.Handle);

    // The project's canvas size drives the window and the canvas at launch and again whenever the
    // saved size changes. An unchanged size is left alone, so a window the user resized is kept until
    // the saved size changes (#7167). Checked every frame: a reload swaps the project the finder holds,
    // and the check is two integer compares.
    private void ApplyCanvasSizeIfChanged(bool isWindowShown)
    {
        if (!_canvasSize.TryTakeChange(ObjectFinder.Self.GumProjectSave, out int width, out int height))
        {
            return;
        }

        if (isWindowShown && _isRetinaBackingEnabled)
        {
            // A showing Retina window is sized in points, like a user resize or a change of display
            // scale, and HandleClientSizeChanged then sets the back buffer. Asking MonoGame for the
            // pixels as points would size it twice too large for a frame, and only the height of a
            // showing window is held to the display, where a launching one is held on both axes.
            if (SdlLibrary.TryGetUsableClientSize(Window.Handle, out int maxPointWidth, out int maxPointHeight))
            {
                (width, height) = PreviewCanvasSize.FitToDisplay(width, height, _backingScale.Factor, maxPointWidth, maxPointHeight);
            }
            (int pointWidth, int pointHeight) = _backingScale.ToPoints(width, height);
            MacRetinaBacking.SetWindowSize(Window.Handle, pointWidth, pointHeight);
        }
        else
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();
        }
        PreviewCanvasSize.ApplyToCanvas(GumService.Default.Root, width, height);
    }

    // Runs every frame: the window can move to a display with a different scale (a Retina laptop
    // and an external monitor), which changes how many pixels each point covers.
    private void UpdateBackingScale()
    {
        if (!_isRetinaBackingEnabled)
        {
            return;
        }
        BackingScale scale = MacRetinaBacking.GetScale(Window.Handle);
        if (scale.Factor != _backingScale.Factor)
        {
            _backingScale = scale;
            FitWindowToBackBuffer();
        }
    }

    // On a Retina display the back buffer stays at the canvas's pixel size and the window shrinks to
    // fit it in points, so Preview draws 1:1 with the tool's canvas (#5571). The cursor arrives in
    // points and is scaled up to pixels.
    private void FitWindowToBackBuffer()
    {
        Cursor? cursor = GumService.Default.Cursor;
        if (cursor != null)
        {
            cursor.TransformMatrix = Matrix.CreateScale((float)_backingScale.Factor);
        }
        PresentationParameters parameters = GraphicsDevice.PresentationParameters;
        (int width, int height) = _backingScale.ToPoints(parameters.BackBufferWidth, parameters.BackBufferHeight);
        MacRetinaBacking.SetWindowSize(Window.Handle, width, height);
    }

    private void HandleClientSizeChanged(object? sender, EventArgs e)
    {
        // MonoGame sets the back buffer to the window's size in points; put it back in pixels.
        if (_isRetinaBackingEnabled)
        {
            (int width, int height) = _backingScale.ToPixels(Window.ClientBounds.Width, Window.ClientBounds.Height);
            GraphicsDevice.PresentationParameters.BackBufferWidth = width;
            GraphicsDevice.PresentationParameters.BackBufferHeight = height;
            GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
        }
        PreviewCanvasSize.ApplyToCanvas(GumService.Default.Root, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
    }

    private void ShowElement()
    {
        if (!PreviewElementPresenter.Show(_selection))
        {
            UpdateWindowTitle(missingElement: true);
            _isElementMissing = true;
            return;
        }

        _isElementMissing = false;
        UpdateWindowTitle();
    }

    // A reload that left the tree different from what a restart would show (a renamed or deleted
    // shown screen, a missing file, a failed read) is followed by a rebuild of the shown element from
    // the project the reload loaded, so Preview ends up as a fresh launch would. It runs at the start
    // of a frame, outside the reload's own callbacks.
    private void RebuildAfterReloadIfNeeded()
    {
        if (!_reloadPolicy.TryTakePendingReload())
        {
            return;
        }

        try
        {
            ShowElement();
        }
        catch (Exception exception)
        {
            // The root was cleared before the build threw. Treating the element as missing makes the
            // next finished reload try again.
            _isElementMissing = true;
            UpdateWindowTitle(missingElement: true);
            _debugLog?.LogException("rebuild after a reload", exception);
            Console.Error.WriteLine($"GumPreview: could not rebuild '{_selection.ElementName}' after a reload: {exception.Message}");
        }
    }

    // Raised from inside GumService.Update; only records the result, so nothing here can throw into
    // the reload.
    private void HandleHotReloadFinished(HotReloadResult result)
    {
        try
        {
            _debugLog?.LogReload(result);
            _logGeometryAfterReload = _debugLog != null;
            _reloadPolicy.OnReloadFinished(result, PreviewReloadPolicy.IsStateSelected(_selection), _isElementMissing);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"GumPreview: could not handle a reload result: {exception.Message}");
        }
    }

    private void LogAppliedVariable(GraphicalUiElement element, VariableSave variable) =>
        _debugLog?.LogToolVariable(element.ElementSave?.Name ?? element.Name ?? "?", variable.SourceObject, variable.GetRootName(), variable.Value);

    private void UpdateWindowTitle(bool missingElement = false) =>
        Window.Title = PreviewWindowTitle.For(_selection.ElementName, missingElement, _languageName);

    // The tool's selected language is saved with the project, so a save that changes it reaches the
    // Preview through the same hot reload as any other change; checked every frame, it is two compares.
    private void SyncLanguage()
    {
        string? name = PreviewLanguage.Apply(ObjectFinder.Self.GumProjectSave, GumService.Default.LocalizationService);
        if (name != _languageName)
        {
            _languageName = name;
            UpdateWindowTitle(_isElementMissing);
        }
    }
}
