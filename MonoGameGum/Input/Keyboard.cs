using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace MonoGameGum.Input;

#region IInputReceiverKeyboardMonoGame Interface

public interface IInputReceiverKeyboardMonoGame : IInputReceiverKeyboard
{
    IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped { get; }
    bool KeyDown(Keys key);
    bool KeyPushed(Keys key);
    bool KeyReleased(Keys key);
    bool KeyTyped(Keys key);
}

#endregion

public class Keyboard : IInputReceiverKeyboardMonoGame
{
    #region Fields/Properties

    HashSet<char> ignoredWindowTextInputCharacters;
    KeyboardStateProcessor keyboardStateProcessor = new KeyboardStateProcessor();

    bool[] mKeysTyped;
    double[] mLastTimeKeyTyped;
    bool[] mLastTypedFromPush;

    char[] mKeyToChar;

    /// <summary>
    /// Delay that happens after initial keypress, before the key is repeated.
    /// </summary>
    public TimeSpan RepeatDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    /// <summary>
    /// Time to wait before repeating the character (letter, number, symbol) that is held down
    /// </summary>
    public TimeSpan RepeatRate { get; set; } = TimeSpan.FromMilliseconds(70);


    public bool IsShiftDown => KeyDown(Keys.LeftShift) || KeyDown(Keys.RightShift);

    public bool IsCtrlDown => KeyDown(Keys.LeftControl) || KeyDown(Keys.RightControl);

    public bool IsAltDown => KeyDown(Keys.LeftAlt) || KeyDown(Keys.RightAlt);

    bool[] mKeysIgnoredForThisFrame;

    List<Keys> keysTypedInternal = new List<Keys>();



    public IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped
    {
        get
        {
            keysTypedInternal.Clear();

            for (int i = 0; i < NumberOfKeys; i++)
            {
                var key = (Keys)i;
                if (KeyTyped(key))
                {
                    keysTypedInternal.Add(key);
                }
            }
            return keysTypedInternal;
        }
    }

    public const int NumberOfKeys = 255;

    #endregion

    public Keyboard(Game? game = null)
    {

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
            TrySubscribeToGameWindowInput(game);
        }
        catch
        {

        }
    }

    private void TrySubscribeToGameWindowInput(Game? game)
    {
        if (game?.Window != null && windowTextInputBuffer == null)
        {
#if !FNA
            var succeeded = false;
            try
            {
                game.Window.TextInput += HandleWindowTextInput;
                succeeded = true;
            }
            catch
            {

            }

            if(succeeded)
            {
                windowTextInputBuffer = new StringBuilder();
            }
#endif
        }
    }

    StringBuilder windowTextInputBuffer;
    string processedStringFromWindow;


#if !FNA
    private void HandleWindowTextInput(object? sender, TextInputEventArgs e)
    {
        lock(windowTextInputBuffer)
        {
            // In DirectX environments, which use Windows Forms, certain characters
            // are returned for certain hotkey combinations like '\u0001' for CTRL+A.
            // We need to ignore these:

            System.Diagnostics.Debug.WriteLine($"Char: \\u{((int)e.Character):X4}" + $" ({e.Character} , {(int)e.Character})");
            if(ignoredWindowTextInputCharacters.Contains(e.Character) == false)
            {
                windowTextInputBuffer.Append(e.Character);
            }

        }
    }
#endif

    public string GetStringTyped()
    {
        //if (InputManager.CurrentFrameInputSuspended)
        //    return "";

        ///////////////////////////////////////early out//////////////////////////////////
        if(windowTextInputBuffer != null)
        {
            return processedStringFromWindow;
        }
        //////////////////////////////////End Early Out//////////////////////////////////

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

        #region Add Text if the user presses CTRL+V
        if (
            isCtrlPressed
            && KeyPushed(Keys.V)
            )
        {

#if !MONOGAME && !KNI && !FNA
            bool isSTAThreadUsed =
                System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA;

#if DEBUG
            if (!isSTAThreadUsed)
            {
                throw new InvalidOperationException("Need to set [STAThread] on Main to support copy/paste");
            }
#endif

            if (isSTAThreadUsed && System.Windows.Forms.Clipboard.ContainsText())
            {
                returnString += System.Windows.Forms.Clipboard.GetText();

            }
#endif

        }
        #endregion

        return returnString;
#endif
    }

    public bool KeyDown(Keys key)
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
            keyboardStateProcessor.IsKeyDown(key);
    }

    public bool KeyTyped(Keys key)
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

    public bool KeyPushed(Keys key)
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


        return /* !InputManager.CurrentFrameInputSuspended && */ keyboardStateProcessor.KeyPushed(key);
    }

    public bool KeyReleased(Keys key)
    {
        if (mKeysIgnoredForThisFrame[(int)key] /*|| InputManager.mIgnorePushesThisFrame*/)
        {
            return false;
        }

        return /* !InputManager.CurrentFrameInputSuspended && */ keyboardStateProcessor.KeyReleased(key);

    }

    /// <summary>
    /// Gathers all the keyboard input since the last frame using Monogame's TextInput and Keyboard.GetKeys()
    /// Since Keyboard.GetKeys() doesn't handle repeate rate, attempts to handle that also by ignorning keys 
    /// pressed too fast.
    /// </summary>
    /// <param name="currentTime"></param>
    /// <param name="game"></param>
    public void Activity(double currentTime, Game? game = null)
    {
#if ANDROID
			ProcessAndroidKeys();
#endif
        // This could be done in initialize, we don't want
        // to break projects. Instead, GumService.Initialize
        // now has the GraphicsDevice version obsolete 
        if (windowTextInputBuffer == null && game != null)
        {
            try
            {
                // Only happens once per run of the application/game
                // Probably should't attempt to do this on platforms that don't support it?
                TrySubscribeToGameWindowInput(game);
            }
            catch
            {

            }
        }

        // Check for any new keys found by the TextInput event and store them in processedStringFromWindow 
        // which we will later handle in TextBox.HandleCharEntered() via Property GetStringTyped()
        if (windowTextInputBuffer != null)
        {
            lock(windowTextInputBuffer)
            {
                processedStringFromWindow = windowTextInputBuffer.ToString();
                windowTextInputBuffer.Clear();
            }
        }

        // Gather all the keys from Keyboard.GetState()
        keyboardStateProcessor.Update();

        // Process any fresh key presses from Keyboard.GetState()
        HandleFreshKeyPress(currentTime);

        // Process any held down keys from Keyboard.GetState()
        HandleKeyStillDown(currentTime);
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

            if (KeyPushed((Keys)(i)))
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
            if (KeyDown((Keys)(i)))
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

    // The ENTER key (char 13 \r) is normally handled by TextInput
    // However, when the Number Pad ENTER key is pressed, TextInput does not receive it.
    // It is however detected during during Keyboard.GetKeys.
    // But, we don't want the normal ENTER key to fire twice, so we check that first.
    private void HandleNumPadEnter(int key)
    {
        if ((key == 13 || key == 10)
            && !processedStringFromWindow.Contains('\n') 
            && !processedStringFromWindow.Contains('\r'))
        {
            processedStringFromWindow += '\n';
        }
    }

    private string KeyToStringAtCurrentState(int key)
    {
        bool isShiftDown = KeyDown(Keys.LeftShift) || KeyDown(Keys.RightShift);

#if !MONOGAME && !KNI && !FNA
        if (System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock))
        {
            isShiftDown = !isShiftDown;
        }
#endif

        #region If Shift is down, return a different key
        if (isShiftDown && IsKeyLetter((Keys)key))
        {
            return ((char)(mKeyToChar[key] - 32)).ToString();
        }

        else
        {
            if (KeyDown(Keys.LeftShift) || KeyDown(Keys.RightShift))
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

    public bool IsKeyLetter(Keys key)
    {
        return key >= Keys.A && key <= Keys.Z;
    }

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

#region KeyboardStateProcessor

public class KeyboardStateProcessor
{
    KeyboardState mLastFrameKeyboardState = new KeyboardState();
    KeyboardState mKeyboardState;

    public bool AnyKeyPushed()
    {
        // loop through all pressed keys...
        for (int i = 0; i < Keyboard.NumberOfKeys; i++)
        {
            // And see if it's pushed (was not down this frame, is down this frame)
            if (KeyPushed((Keys)i))
            {
                // if so, we can return true
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Clears the keyboard states, simulating the keyboard
    /// not having any values down or pressed
    /// </summary>
    public void Clear()
    {
        mKeyboardState = new KeyboardState();
        mLastFrameKeyboardState = new KeyboardState();
    }

    public bool IsKeyDown(Keys key)
    {
        return mKeyboardState.IsKeyDown(key);
    }

    public bool KeyPushed(Keys key)
    {
        return mKeyboardState.IsKeyDown(key) &&
            !mLastFrameKeyboardState.IsKeyDown(key);
    }

    public bool KeyReleased(Keys key)
    {
        return mLastFrameKeyboardState.IsKeyDown(key) &&
            !mKeyboardState.IsKeyDown(key);
    }

    public void Update()
    {
        mLastFrameKeyboardState = mKeyboardState;
        mKeyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
    }

}
#endregion