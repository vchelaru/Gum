using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Input;


/// <summary>
/// Provides keyboard input handling for XNA-like projects such as MonoGame and KNI.
/// </summary>
public partial class Keyboard : IInputReceiverKeyboardMonoGame
{
    #region Fields/Properties

    // Stored so the Android partial class can reach Game.Services.GetService<View>()
    // to drive the on-screen keyboard. Null on headless/test paths.
    internal Game? _game;

    HashSet<char> ignoredWindowTextInputCharacters;
    /// <summary>
    /// Gets or sets the processor responsible for reading and reporting raw input data.
    /// </summary>
    /// <remarks>
    /// This is sety by default on the Keyboard and should not be modified except for unit tests or
    /// to customize how input reading is done in advanced scenarios.
    /// </remarks>
    public KeyboardStateProcessor KeyboardStateProcessor { get; set; } = new KeyboardStateProcessor();

    bool[] mKeysTyped;
    double[] mLastTimeKeyTyped;
    bool[] mLastTypedFromPush;

    char[] mKeyToChar;

    /// <summary>
    /// Delay that happens after initial keypress, before the key is repeated.
    /// </summary>
    public TimeSpan RepeatDelay { get; set; } = KeyRepeatTimer.DefaultDelay;
    /// <summary>
    /// Time to wait before repeating the character (letter, number, symbol) that is held down
    /// </summary>
    public TimeSpan RepeatRate { get; set; } = KeyRepeatTimer.DefaultRate;

    /// <summary>
    /// Returns whether either Shift key is currently held down.
    /// </summary>
    public bool IsShiftDown => KeyDownCore(Keys.LeftShift) || KeyDownCore(Keys.RightShift);

    /// <summary>
    /// Returns whether either Control key is currently held down.
    /// </summary>
    public bool IsCtrlDown => KeyDownCore(Keys.LeftControl) || KeyDownCore(Keys.RightControl);

    /// <summary>
    /// Returns whether either Command key is held on macOS, where MonoGame reports it as a Windows key. Always
    /// false elsewhere, so the Windows key never triggers text shortcuts (Windows+V is the clipboard history).
    /// </summary>
    public bool IsCommandDown => OperatingSystem.IsMacOS() && (KeyDownCore(Keys.LeftWindows) || KeyDownCore(Keys.RightWindows));

    /// <summary>
    /// Returns whether either Alt key is currently held down.
    /// </summary>
    public bool IsAltDown => KeyDownCore(Keys.LeftAlt) || KeyDownCore(Keys.RightAlt);

    bool[] mKeysIgnoredForThisFrame;

    List<Keys> keysTypedInternal = new List<Keys>();


    /// <summary>
    /// Returns a collection of keys that were typed this frame.
    /// </summary>
    public IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped
    {
        get
        {
            keysTypedInternal.Clear();

            for (int i = 0; i < NumberOfKeys; i++)
            {
                var key = (Keys)i;
                if (KeyTypedCore(key))
                {
                    keysTypedInternal.Add(key);
                }
            }
            return keysTypedInternal;
        }
    }

    IEnumerable<Gum.Forms.Input.Keys> IInputReceiverKeyboard.KeysTyped
    {
        get
        {
            foreach (var item in KeysTyped)
            {
                yield return (Gum.Forms.Input.Keys)(int)item;
            }
        }
    }

    /// <summary>
    /// Returns whether the specified key is currently held down, accepting a shared
    /// <see cref="Gum.Forms.Input.Keys"/> value. Casts directly to XNA <see cref="Keys"/>
    /// because both enums share the same numeric values.
    /// </summary>
    public bool KeyDown(Gum.Forms.Input.Keys key) => KeyDownCore((Keys)(int)key);

    /// <summary>
    /// Returns whether the specified key was pressed this frame, accepting a shared
    /// <see cref="Gum.Forms.Input.Keys"/> value.
    /// </summary>
    public bool KeyPushed(Gum.Forms.Input.Keys key) => KeyPushedCore((Keys)(int)key);

    /// <summary>
    /// Returns whether the specified key was released this frame, accepting a shared
    /// <see cref="Gum.Forms.Input.Keys"/> value.
    /// </summary>
    public bool KeyReleased(Gum.Forms.Input.Keys key) => KeyReleasedCore((Keys)(int)key);

    /// <summary>
    /// Returns whether the specified key was "typed" this frame (pushed or OS-repeat),
    /// accepting a shared <see cref="Gum.Forms.Input.Keys"/> value.
    /// </summary>
    public bool KeyTyped(Gum.Forms.Input.Keys key) => KeyTypedCore((Keys)(int)key);

    public const int NumberOfKeys = 255;

    #endregion

    /// <summary>
    /// Constructs a <see cref="Keyboard"/> for the current (MonoGame/KNI/FNA) platform, passing
    /// the <see cref="Game"/> so it can subscribe to the Game's Window TextInput event. Mirrors
    /// the Raylib/Sokol platforms' <c>Keyboard.CreateForCurrentPlatform()</c>.
    /// </summary>
    /// <param name="game">The optional <see cref="Game"/> instance, used to subscribe to the
    /// Game's Window TextInput event if it is available on the current platform.</param>
    internal static Keyboard CreateForCurrentPlatform(Game? game) => new Keyboard(game);

    /// <summary>
    /// Instantiates a new Keyboard instance, optionally subscribing to the provided Game's Window TextInput event.
    /// </summary>
    /// <param name="game">The optional Game. If passed, the keyboard attempts to subscribe to the Game's Window TextInput event
    /// if it is available on the current platform.</param>
    public Keyboard(Game? game = null)
    {
        _game = game;

        // characters 1 - 26 are CTRL+letter characters
        // for example CTRL+A is 1, CTRL+B is 2, etc.
        // 27, 28, and 29 are also ignored, these are ctrl and
        // [, ], and \
        ignoredWindowTextInputCharacters = new HashSet<char>();
        for (int i = 0; i <= 29; i++)
        {
            ignoredWindowTextInputCharacters.Add((char)i);
        }

        mKeysTyped = new bool[NumberOfKeys];
        mLastTimeKeyTyped = new double[NumberOfKeys];
        mLastTypedFromPush = new bool[NumberOfKeys];
        mKeysIgnoredForThisFrame = new bool[NumberOfKeys];

        FillKeyCodes();

        for (int i = 0; i < NumberOfKeys; i++)
        {
            mLastTimeKeyTyped[i] = 0;
            mKeysTyped[i] = false;
            mLastTypedFromPush[i] = false;
        }
        try
        {
            var isMobile = System.OperatingSystem.IsAndroid() || System.OperatingSystem.IsIOS();
            // this isn't supported on mobile:
            if (!isMobile)
            {
                TrySubscribeToGameWindowInput(game);
            }
        }
        catch
        {

        }
    }

    private void TrySubscribeToGameWindowInput(Game? game)
    {
        if (game?.Window != null && windowTextInputBuffer == null)
        {
#if FNA
            try
            {
                Microsoft.Xna.Framework.Input.TextInputEXT.TextInput += HandleFnaTextInput;
                Microsoft.Xna.Framework.Input.TextInputEXT.StartTextInput();
                windowTextInputBuffer = new StringBuilder();
            }
            catch
            {

            }
#elif ANDROID
            // MonoGame's AndroidGameWindow does not expose the TextInput event that the
            // desktop GameWindow does. On Android we rely on Keyboard.Android.cs — an IME
            // KeyPress listener registered lazily from ShowKeyboard() — to populate
            // processedString, which GetTypedText returns unchanged as this frame's typed text.
#else
            var succeeded = false;
            try
            {
                game.Window.TextInput += HandleWindowTextInput;
                succeeded = true;
            }
            catch
            {

            }

            if (succeeded)
            {
                windowTextInputBuffer = new StringBuilder();
            }
#endif
        }
    }

    // Null until TrySubscribeToGameWindowInput succeeds; the handlers are subscribed just before it is
    // assigned, so they skip input that arrives in that window.
    StringBuilder? windowTextInputBuffer;
    // This frame's typed text, set once per Activity from GetTypedText.
    string typedTextThisFrame = string.Empty;
    // Whether Enter (main or number pad) was typed this frame; see AppendNumPadEnter.
    bool enterTypedThisFrame;


#if !FNA && !ANDROID
    private void HandleWindowTextInput(object? sender, TextInputEventArgs e)
    {
        var buffer = windowTextInputBuffer;
        if (buffer == null)
        {
            return;
        }

        lock (buffer)
        {
            // In DirectX environments, which use Windows Forms, certain characters
            // are returned for certain hotkey combinations like '\u0001' for CTRL+A.
            // We need to ignore these:

            //System.Diagnostics.Debug.WriteLine($"Char: \\u{((int)e.Character):X4}" + $" ({e.Character} , {(int)e.Character})");
            if (ignoredWindowTextInputCharacters.Contains(e.Character) == false)
            {
                buffer.Append(e.Character);
            }

        }
    }
#endif

#if FNA
    private void HandleFnaTextInput(char character)
    {
        var buffer = windowTextInputBuffer;
        if (buffer == null)
        {
            return;
        }

        lock (buffer)
        {
            if (ignoredWindowTextInputCharacters.Contains(character) == false)
            {
                buffer.Append(character);
            }
        }
    }
#endif

    /// <summary>
    /// Returns the text typed this frame, as produced by <see cref="GetTypedText"/> during
    /// <see cref="Activity(double)"/>.
    /// </summary>
    public string GetStringTyped() => typedTextThisFrame;

    /// <summary>
    /// Returns whether the specified XNA key is currently held down.
    /// </summary>
    public bool KeyDown(Keys key) => KeyDownCore(key);

    private bool KeyDownCore(Keys key)
    {
        if (mKeysIgnoredForThisFrame[(int)key])
        {
            return false;
        }

#if ANDROID
			if(KeyDownAndroid(key))
			{
				return KeyDownAndroid(key);
			}
#endif



        return
            //!InputManager.CurrentFrameInputSuspended && 
            KeyboardStateProcessor.IsKeyDown(key);
    }

    /// <summary>
    /// Returns whether the specified XNA key was "typed" this frame (pushed or repeating while held).
    /// </summary>
    public bool KeyTyped(Keys key) => KeyTypedCore(key);

    private bool KeyTypedCore(Keys key)
    {
        if (mKeysIgnoredForThisFrame[(int)key])
        {
            return false;
        }

#if ANDROID
        if (KeyPushedAndroid(key))
        {
            return true;
        }
#endif

        return
            //!InputManager.CurrentFrameInputSuspended && 
            mKeysTyped[(int)key];
    }

    /// <summary>
    /// Returns whether the specified XNA key was pressed this frame.
    /// </summary>
    public bool KeyPushed(Keys key) => KeyPushedCore(key);

    private bool KeyPushedCore(Keys key)
    {
        if (mKeysIgnoredForThisFrame[(int)key] /*|| InputManager.mIgnorePushesThisFrame*/)
        {
            return false;
        }

#if ANDROID
			if(KeyPushedAndroid(key))
			{
				return true;
			}
#endif


        return /* !InputManager.CurrentFrameInputSuspended && */ KeyboardStateProcessor.KeyPushed(key);
    }

    /// <summary>
    /// Returns whether the specified XNA key was released this frame.
    /// </summary>
    public bool KeyReleased(Keys key) => KeyReleasedCore(key);

    private bool KeyReleasedCore(Keys key)
    {
        if (mKeysIgnoredForThisFrame[(int)key] /*|| InputManager.mIgnorePushesThisFrame*/)
        {
            return false;
        }

        return /* !InputManager.CurrentFrameInputSuspended && */ KeyboardStateProcessor.KeyReleased(key);

    }

    /// <summary>
    /// Gathers all the keyboard input since the last frame using Monogame's TextInput and Keyboard.GetKeys()
    /// Since Keyboard.GetKeys() doesn't handle repeate rate, attempts to handle that also by ignorning keys 
    /// pressed too fast.
    /// </summary>
    /// <param name="currentTime"></param>
    public void Activity(double currentTime)
    {
#if ANDROID
			ProcessAndroidKeys();
#endif

        var isMobile = System.OperatingSystem.IsAndroid() || System.OperatingSystem.IsIOS();
        // this isn't supported on mobile:


        // This could be done in initialize, we don't want
        // to break projects. Instead, GumService.Initialize
        // now has the GraphicsDevice version obsolete
        if (windowTextInputBuffer == null && _game != null && !isMobile)
        {
            try
            {
                // Only happens once per run of the application/game
                // Probably should't attempt to do this on platforms that don't support it?
                TrySubscribeToGameWindowInput(_game);
            }
            catch
            {

            }
        }

        enterTypedThisFrame = false;

        // Gather all the keys from Keyboard.GetState()
        KeyboardStateProcessor.Update();

        // Process any fresh key presses from Keyboard.GetState()
        HandleFreshKeyPress(currentTime);

        // Process any held down keys from Keyboard.GetState()
        HandleKeyStillDown(currentTime);

        // After the key handling, so the key-press fallback in GetTypedText sees this frame's keys.
        // TextBox reads the result through GetStringTyped.
        typedTextThisFrame = GetTypedText() ?? string.Empty;
    }

    /// <summary>
    /// Returns the text typed this frame. <see cref="Activity(double)"/> calls it once per frame, after this
    /// frame's keys are read, and <see cref="GetStringTyped"/> returns the result. The default returns the
    /// game window's text events when the keyboard is subscribed to them, and otherwise the characters of
    /// this frame's typed keys, skipping Ctrl+V/C/Z/A/X. A subclass overrides it to replace the input source,
    /// for automation and tests; an override returns the characters to deliver, such as "Hello", or an
    /// empty string when nothing was typed. It never returns null.
    /// </summary>
    protected virtual string GetTypedText()
    {
        StringBuilder? buffer = windowTextInputBuffer;
        if (buffer != null)
        {
            string text;
            lock (buffer)
            {
                text = buffer.ToString();
                buffer.Clear();
            }
            return AppendNumPadEnter(text);
        }

#if ANDROID
        return processedString;
#else
        string returnString = "";

        bool isCtrlPressed = IsCtrlDown;

        for (int i = 0; i < NumberOfKeys; i++)
        {
            if (mKeysTyped[i])
            {
                // If the user pressed CTRL + some key combination then ignore that input so that
                // the letters aren't written.
                Keys asKey = (Keys)i;

                if (isCtrlPressed && (asKey == Keys.V || asKey == Keys.C || asKey == Keys.Z || asKey == Keys.A || asKey == Keys.X))
                {
                    continue;
                }
                returnString += KeyToStringAtCurrentState(i);
            }
        }

        return returnString;
#endif
    }

    // Handles initial Key press
    //  Reset all possible ascii keys (0->NumberOfKeys [255])
    //  Then look at the current state of each key
    //  Set that key's pressed time
    //  so we can manually handle initial repeat rate and repeat interval later
    private void HandleFreshKeyPress(double currentTime)
    {
        for (int i = 0; i < NumberOfKeys; i++)
        {
            mKeysIgnoredForThisFrame[i] = false;
            mKeysTyped[i] = false;

            if (KeyPushedCore((Keys)(i)))
            {
                mKeysTyped[i] = true;
                mLastTimeKeyTyped[i] = currentTime;
                mLastTypedFromPush[i] = true;

                HandleNumPadEnter(i);
            }
        }
    }

    // Handles key already pressed, remains down
    //  Go over each key to see if it is currently down
    //  If it is, validate it wasn't pressed too recently
    //  or validate it's been enough time since the last keydown event
    //  so we can record another key being pressed
    private void HandleKeyStillDown(double currentTime)
    {

        for (int i = 0; i < NumberOfKeys; i++)
        {
            if (KeyDownCore((Keys)(i)))
            {
                if ((mLastTypedFromPush[i] && currentTime - mLastTimeKeyTyped[i] > RepeatDelay.TotalSeconds) ||      // Fresh key press, with long enough initial press delay
                    (mLastTypedFromPush[i] == false && currentTime - mLastTimeKeyTyped[i] > RepeatRate.TotalSeconds) // held key, with long enough repeat rate between
                  )
                {
                    mLastTypedFromPush[i] = false;
                    mLastTimeKeyTyped[i] = currentTime;
                    mKeysTyped[i] = true;

                    HandleNumPadEnter(i);
                }
            }
        }
    }

    private void HandleNumPadEnter(int key)
    {
        if (key == 13 || key == 10)
        {
            enterTypedThisFrame = true;
        }
    }

    // Window text path only. The ENTER key (char 13 \r) is normally handled by TextInput. However, when the
    // Number Pad ENTER key is pressed, TextInput does not receive it. It is detected through the key state instead, so a newline
    // is added unless the typed text already has one, which keeps the normal ENTER key from firing twice.
    private string AppendNumPadEnter(string text)
    {
        if (enterTypedThisFrame && !text.Contains('\n') && !text.Contains('\r'))
        {
            return text + '\n';
        }
        return text;
    }

    private string KeyToStringAtCurrentState(int key)
    {
        bool isShiftDown = KeyDownCore(Keys.LeftShift) || KeyDownCore(Keys.RightShift);

        #region If Shift is down, return a different key
        if (isShiftDown && IsKeyLetterCore((Keys)key))
        {
            return ((char)(mKeyToChar[key] - 32)).ToString();
        }

        else
        {
            if (KeyDownCore(Keys.LeftShift) || KeyDownCore(Keys.RightShift))
            {
                switch ((Keys)key)
                {
                    case Keys.D1: return "!";
                    case Keys.D2: return "@";
                    case Keys.D3: return "#";
                    case Keys.D4: return "$";
                    case Keys.D5: return "%";
                    case Keys.D6: return "^";
                    case Keys.D7: return "&";
                    case Keys.D8: return "*";
                    case Keys.D9: return "(";
                    case Keys.D0: return ")";

                    case Keys.OemTilde: return "~";
                    case Keys.OemSemicolon: return ":";
                    case Keys.OemQuotes: return "\"";
                    case Keys.OemQuestion: return "?";
                    case Keys.OemPlus: return "+";
                    case Keys.OemPipe: return "|";
                    case Keys.OemPeriod: return ">";
                    case Keys.OemOpenBrackets: return "{";
                    case Keys.OemCloseBrackets: return "}";
                    case Keys.OemMinus: return "_";
                    case Keys.OemComma: return "<";
                    case Keys.Space: return " ";
                    default: return "";
                }
            }
            else if (mKeyToChar[key] != (char)0)
            {
                return mKeyToChar[key].ToString();
            }
            else
            {
                return "";
            }
        }

        #endregion

    }

    /// <summary>
    /// Returns whether the specified XNA key is a letter (A through Z).
    /// </summary>
    public bool IsKeyLetter(Keys key) => IsKeyLetterCore(key);

    private static bool IsKeyLetterCore(Keys key)
    {
        return key >= Keys.A && key <= Keys.Z;
    }

    /// <summary>
    /// Returns whether the specified key is a letter (A through Z), accepting a shared
    /// <see cref="Gum.Forms.Input.Keys"/> value.
    /// </summary>
    public bool IsKeyLetter(Gum.Forms.Input.Keys key) => IsKeyLetterCore((Keys)(int)key);

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(mKeyToChar))]
    private void FillKeyCodes()
    {
        mKeyToChar = new char[NumberOfKeys];

        for (int i = 0; i < NumberOfKeys; i++)
        {
            mKeyToChar[i] = (char)0;
        }

        mKeyToChar[(int)Keys.A] = 'a';
        mKeyToChar[(int)Keys.B] = 'b';
        mKeyToChar[(int)Keys.C] = 'c';
        mKeyToChar[(int)Keys.D] = 'd';
        mKeyToChar[(int)Keys.E] = 'e';
        mKeyToChar[(int)Keys.F] = 'f';
        mKeyToChar[(int)Keys.G] = 'g';
        mKeyToChar[(int)Keys.H] = 'h';
        mKeyToChar[(int)Keys.I] = 'i';
        mKeyToChar[(int)Keys.J] = 'j';
        mKeyToChar[(int)Keys.K] = 'k';
        mKeyToChar[(int)Keys.L] = 'l';
        mKeyToChar[(int)Keys.M] = 'm';
        mKeyToChar[(int)Keys.N] = 'n';
        mKeyToChar[(int)Keys.O] = 'o';
        mKeyToChar[(int)Keys.P] = 'p';
        mKeyToChar[(int)Keys.Q] = 'q';
        mKeyToChar[(int)Keys.R] = 'r';
        mKeyToChar[(int)Keys.S] = 's';
        mKeyToChar[(int)Keys.T] = 't';
        mKeyToChar[(int)Keys.U] = 'u';
        mKeyToChar[(int)Keys.V] = 'v';
        mKeyToChar[(int)Keys.W] = 'w';
        mKeyToChar[(int)Keys.X] = 'x';
        mKeyToChar[(int)Keys.Y] = 'y';
        mKeyToChar[(int)Keys.Z] = 'z';

        mKeyToChar[(int)Keys.D1] = '1';
        mKeyToChar[(int)Keys.D2] = '2';
        mKeyToChar[(int)Keys.D3] = '3';
        mKeyToChar[(int)Keys.D4] = '4';
        mKeyToChar[(int)Keys.D5] = '5';
        mKeyToChar[(int)Keys.D6] = '6';
        mKeyToChar[(int)Keys.D7] = '7';
        mKeyToChar[(int)Keys.D8] = '8';
        mKeyToChar[(int)Keys.D9] = '9';
        mKeyToChar[(int)Keys.D0] = '0';

        mKeyToChar[(int)Keys.NumPad1] = '1';
        mKeyToChar[(int)Keys.NumPad2] = '2';
        mKeyToChar[(int)Keys.NumPad3] = '3';
        mKeyToChar[(int)Keys.NumPad4] = '4';
        mKeyToChar[(int)Keys.NumPad5] = '5';
        mKeyToChar[(int)Keys.NumPad6] = '6';
        mKeyToChar[(int)Keys.NumPad7] = '7';
        mKeyToChar[(int)Keys.NumPad8] = '8';
        mKeyToChar[(int)Keys.NumPad9] = '9';
        mKeyToChar[(int)Keys.NumPad0] = '0';

        mKeyToChar[(int)Keys.Decimal] = '.';

        mKeyToChar[(int)Keys.Space] = ' ';
        mKeyToChar[(int)Keys.Enter] = '\n';


        mKeyToChar[(int)Keys.Subtract] = '-';
        mKeyToChar[(int)Keys.Add] = '+';
        mKeyToChar[(int)Keys.Divide] = '/';
        mKeyToChar[(int)Keys.Multiply] = '*';

        mKeyToChar[(int)Keys.OemTilde] = '`';
        mKeyToChar[(int)Keys.OemSemicolon] = ';';
        mKeyToChar[(int)Keys.OemQuotes] = '\'';
        mKeyToChar[(int)Keys.OemQuestion] = '/';
        mKeyToChar[(int)Keys.OemPlus] = '=';
        mKeyToChar[(int)Keys.OemPipe] = '\\';
        mKeyToChar[(int)Keys.OemPeriod] = '.';
        mKeyToChar[(int)Keys.OemOpenBrackets] = '[';
        mKeyToChar[(int)Keys.OemCloseBrackets] = ']';
        mKeyToChar[(int)Keys.OemMinus] = '-';
        mKeyToChar[(int)Keys.OemComma] = ',';


    }

}
