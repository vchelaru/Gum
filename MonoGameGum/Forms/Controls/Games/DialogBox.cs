#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using FlatRedBall.Glue.StateInterpolation;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if FRB

using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
namespace FlatRedBall.Forms.Controls.Games;

#else
namespace Gum.Forms.Controls.Games;

#endif
using global::RenderingLibrary.Graphics;

#if RAYLIB
using RaylibGum;
using RaylibGum.Renderables;
#endif

#if SOKOL
using SokolGum;
#endif

#if !FRB && !XNALIKE
using GamepadButton = Gum.Input.GamepadButton;
#endif

#if XNALIKE
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using System.Security.Cryptography;
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
#else
using Gum.Renderables;
using Keys = Gum.Forms.Input.Keys;
#endif

#region DialogPageTask

public class DialogPageTask
{
    public string Page { get; set; }
    public Func<Task>  Task { get; set; }

    public static implicit operator DialogPageTask(string page) => 
        new DialogPageTask { Page = page };

    public static implicit operator DialogPageTask(Func<Task> task) =>
        new DialogPageTask { Task = task };

}

#endregion

public class DialogBox : FrameworkElement, IInputReceiver
#if !FRB
    , Gum.Wireframe.IUpdateEveryFrame
#endif
{
    #region Fields/Properties

    GraphicalUiElement textComponent;
    GraphicalUiElement continueIndicatorInstance;
    Text coreTextObject;


    public IInputReceiver? ParentInputReceiver =>
        this.GetParentInputReceiver();
    public static double LastTimeDismissed { get; private set; }

    List<DialogPageTask> Pages = new List<DialogPageTask>();

    public int PagesRemaining => Pages.Count;

    static global::Gum.DataTypes.Variables.StateSave NoTextShownState;

    string currentPageText;

#if FRB
    Tweener showLetterTweener;
#else
    bool isTyping;
    double typingElapsedSeconds;
    int typingTargetLetterCount;
#endif

    public event Action<IInputReceiver> FocusUpdate;

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput { get; set; } = true;

    public IInputReceiver NextInTabSequence { get; set; }

    public override bool IsFocused
    {
        get => base.IsFocused;
        set
        {
            base.IsFocused = value;
            UpdateToIsFocused();
        }
    }

    /// <summary>
    /// The number of letters to show per second when printing out in "typewriter style". 
    /// If null, 0, or negative, then the text is shown immediately.
    /// </summary>
    public int? LettersPerSecond { get; set; } = 20;

    public bool TypeNextPageImmediatelyOnCancelPush { get; set; } = true;

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog box finishes showing all pages.
    /// </summary>
    public event EventHandler FinishedShowing;

    /// <summary>
    /// Raised whenever a page finishes typing out, either automatically or in response to input.
    /// </summary>
    public event EventHandler FinishedTypingPage;

    public event EventHandler PageAdvanced;

    /// <summary>
    /// If not null, this predicate is used to determine if input
    /// has been pressed to advance the input. If null, the default 
    /// page-advancing logic will be performed.
    /// </summary>
    public Func<bool> AdvancePageInputPredicate;

    #endregion

    #region Initialize

    static DialogBox()
    {
        NoTextShownState = new global::Gum.DataTypes.Variables.StateSave();
        NoTextShownState.Variables.Add(new global::Gum.DataTypes.Variables.VariableSave
        {
            Name = "TextInstance.MaxLettersToShow",
            Value = 0,
            SetsValue = true
        });
    }

    public DialogBox() : base() { }

    public DialogBox(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

        // it's okay if this is null
        continueIndicatorInstance = base.Visual.GetGraphicalUiElementByName("ContinueIndicatorInstance");

        coreTextObject = textComponent?.RenderableComponent as Text;

#if FRB
        Visual.Click += this.HandleClick(this, EventArgs.Empty);
#else
        Visual.Click += this.HandleClick;
#endif

        base.ReactToVisualChanged();
    }

    #endregion

    #region Show Methods
    /// <summary>
    /// Shows the dialog box (adds it to managers and sets IsVisible to true) and begins showing the text.
    /// </summary>
    /// <param name="text">The text to print out, either immediately or letter-by-letter according to LettersPerSecond.</param>
    /// <param name="frbLayer">The FlatRedBall Layer to add the DialogBox to. If null, the dialog box will not be layered. This will attempt to use a Gum layer matching the FRB layer. This will automatically work if the Layer has been added through the FlatRedBall Editor.</param>
    public void Show(string text, Layer frbLayer = null)
    {
        // Delegate to the IEnumerable overload which calls ConvertToPages on each
        // entry; doing it here too would double-paginate.
        Show(new[] { text }, frbLayer);
    }

    public void Show(IEnumerable<string> pages, Layer frbLayer = null)
    {
        base.Show(frbLayer);
        EnsureVisualInRoot();

        Pages.Clear();
#if FRB
        showLetterTweener?.Stop();
#else
        isTyping = false;
#endif

        showNextPageOnDismissedPage = true;
        if (pages.Any())
        {
            foreach(var page in pages)
            {
                // Each entry is a caller-supplied page break, but if the entry
                // itself overflows the visual's height it gets split further.
                foreach (var split in ConvertToPages(page))
                {
                    this.Pages.Add(split);
                }
            }

            ShowNextPage();
        }
    }

    // base.Show uses the (obsolete) AddToManagers path which sets up rendering
    // but does not parent the Visual into GumService.Default.Root. The per-frame
    // pump (GumService.Update -> Root.AnimateSelf) only walks Root.Children, so
    // without this the typewriter (IUpdateEveryFrame.Activity) never ticks.
    // Skipped under FRB — that runtime has its own pump path via base.Show.
    private void EnsureVisualInRoot()
    {
#if !FRB
        if (Visual == null || Visual.Parent != null) return;
#if XNALIKE
        var root = global::MonoGameGum.GumService.Default?.Root;
#elif SOKOL
        var root = global::SokolGum.GumService.Default?.Root;
#else
        global::Gum.Wireframe.GraphicalUiElement root = null;
#endif
        if (root != null && !root.Children.Contains(Visual))
        {
            root.Children.Add(Visual);
        }
#endif
    }

    /// <summary>
    /// Shows the dialog box (adds it to managers and sets IsVisible to true) and begins showing the text.
    /// </summary>
    /// <param name="text">The text to print out, either immediately or letter-by-letter according to LettersPerSecond.</param>
    /// <returns>A task which completes when the text has been displayed and the DialogBox has been dismissed.</returns>
    public Task ShowAsync(string text)
    {
        // Delegate to the IEnumerable overload which calls ConvertToPages on each
        // entry; doing it here too would double-paginate.
        return ShowAsync(new[] { text });
    }


    public async Task ShowAsync(IEnumerable<string> pages, Layer frbLayer = null)
    {
        base.Show(frbLayer);
        EnsureVisualInRoot();

        Pages.Clear();
#if FRB
        showLetterTweener?.Stop();
#else
        isTyping = false;
#endif

        showNextPageOnDismissedPage = false;
        if (pages.Any())
        {
            foreach (var page in pages)
            {
                foreach (var split in ConvertToPages(page))
                {
                    this.Pages.Add(split);
                }
            }
            await StartShowAllPagesLoop();
        }
    }

    public async Task ShowAsync(IEnumerable<DialogPageTask> pageTasks, Layer frbLayer = null)
    {
        base.Show(frbLayer);
        EnsureVisualInRoot();

        Pages.Clear();
#if FRB
        showLetterTweener?.Stop();
#else
        isTyping = false;
#endif

        showNextPageOnDismissedPage = false;
        if (pageTasks.Any())
        {
            this.Pages.AddRange(pageTasks);
            await StartShowAllPagesLoop();
        }
    }

    // September 28, 2023
    // Vic asks - why do we 
    // have ShowDialog? Is it
    // to match the ShowDialog 
    // from the base FrameworkElement?
    // If so, what's the point because
    // this doesn't override the base parameters.
    // It requires pages. 
    [Obsolete("Use ShowAsync")]
    public async Task<bool?> ShowDialog(IEnumerable<string> pageTasks, Layer frbLayer = null)
    {
#if DEBUG
        if (Visual == null)
        {
            throw new InvalidOperationException("Visual must be set before calling Show");
        }
#endif
        await ShowAsync(pageTasks, frbLayer);

        this.Close();

        return null;
    }

#if FRB
    // See comment above about why this is obsolete.
    [Obsolete("Use ShowAsync")]
    public async Task<bool?> ShowDialog(IEnumerable<DialogPageTask> pageTasks, Layer frbLayer = null)
    {
#if DEBUG
        if (Visual == null)
        {
            throw new InvalidOperationException("Visual must be set before calling Show");
        }
#endif
        var semaphoreSlim = new SemaphoreSlim(1);

        void HandleRemovedFromManagers(object sender, EventArgs args) => semaphoreSlim.Release();
        Visual.RemovedFromGuiManager += HandleRemovedFromManagers;

        semaphoreSlim.Wait();
        await ShowAsync(pageTasks, frbLayer);
        await semaphoreSlim.WaitAsync();

        Visual.RemovedFromGuiManager -= HandleRemovedFromManagers;
        // for now, return null, todo add dialog results

        semaphoreSlim.Dispose();

        return null;
    }
#endif


    public void ShowNextPage(bool forceImmediatePrint = false)
    {
        var page = Pages.FirstOrDefault();

        if(page != null)
        {
            ShowInternal(page.Page, forceImmediatePrint);
            Pages.RemoveAt(0);
        }
    }

    bool wasLastAdvancePressPrintImmediate = false;
    private async Task StartShowAllPagesLoop()
    {
        var page = Pages.FirstOrDefault();
        wasLastAdvancePressPrintImmediate = false;

        while (page != null)
        {
            // remove it before calling ShowInternal so that the dialog box hides if there are no pages
            Pages.RemoveAt(0);
            if(page.Task != null)
            {
                this.IsVisible = false;
                this.IsFocused = false;
                await page.Task();

                // special case if ending on a dialog:
                if(Pages.Count == 0)
                {
#if FRB
                    LastTimeDismissed = TimeManager.CurrentTime;
#elif XNALIKE
                    LastTimeDismissed = GumService.Default.GameTime.TotalGameTime.TotalSeconds;
#else
                    LastTimeDismissed = GumService.Default.GameTime;
#endif
                    PageAdvanced?.Invoke(this, null);
                    FinishedShowing?.Invoke(this, null);
                }
            }
            else
            {
                this.IsVisible = true;
                // todo - do we want to always focus it?
                // Update August 9, 2023 - no, don't always 
                // focus it. The user may have intentionally 
                // unfocused:
                //this.IsFocused = true;

                var semaphoreSlim = new SemaphoreSlim(1);

                void ReleaseSemaphor(object sender, EventArgs args) => 
                    semaphoreSlim.Release();

                this.PageAdvanced += ReleaseSemaphor;

                semaphoreSlim.Wait();
                ShowInternal(page.Page, forceImmediatePrint: wasLastAdvancePressPrintImmediate);

                await semaphoreSlim.WaitAsync();
                semaphoreSlim.Dispose();
                this.PageAdvanced -= ReleaseSemaphor;

            }
            page = Pages.FirstOrDefault();
        }
    }

    private void ShowInternal(string text, bool forceImmediatePrint)
    {
        IsVisible = true;

        currentPageText = text;

#if FRB
        showLetterTweener?.Stop();
#else
        isTyping = false;
#endif
#if DEBUG
        ReportMissingTextInstance();
#endif
        // go through the component instead of the core text object to force a layout refresh if necessary
        textComponent.SetProperty("Text", text);


        var tags = BbCodeParser.Parse(text, CustomSetPropertyOnRenderable.Tags);
        var strippedLength = BbCodeParser.RemoveTags(text, tags).Length;

        var shouldPrintCharacterByCharacter = LettersPerSecond > 0 && !forceImmediatePrint;
        if(shouldPrintCharacterByCharacter)
        {
            coreTextObject.MaxLettersToShow = 0;

            if (continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = false;
            }

#if FRB
            var allTextShownState = new global::Gum.DataTypes.Variables.StateSave();
            allTextShownState.Variables.Add(new global::Gum.DataTypes.Variables.VariableSave
            {
                Name = "TextInstance.MaxLettersToShow",
                Value = strippedLength,
                SetsValue = true
            });

            var duration = strippedLength / (float)LettersPerSecond;

            showLetterTweener = this.Visual.InterpolateTo(NoTextShownState, allTextShownState, duration, InterpolationType.Linear, Easing.Out);
            showLetterTweener.Ended += () =>
            {
                if (TakingInput && continueIndicatorInstance != null)
                {
                    continueIndicatorInstance.Visible = true;
                }
                FinishedTypingPage?.Invoke(this, null);
            };
#else
            typingTargetLetterCount = strippedLength;
            typingElapsedSeconds = 0;
            isTyping = true;
#endif
        }
        else
        {
            coreTextObject.MaxLettersToShow = strippedLength;

            if (TakingInput && continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = true;
            }

            if (continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = true;
            }
            FinishedTypingPage?.Invoke(this, null);
        }
    }

#if !FRB
    void Gum.Wireframe.IUpdateEveryFrame.Activity(double secondDifference)
    {
        if (!isTyping) return;

        typingElapsedSeconds += secondDifference;

        var lettersToShow = (int)(typingElapsedSeconds * LettersPerSecond);
        if (lettersToShow >= typingTargetLetterCount)
        {
            lettersToShow = typingTargetLetterCount;
            isTyping = false;
            coreTextObject.MaxLettersToShow = lettersToShow;

            if (TakingInput && continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = true;
            }
            FinishedTypingPage?.Invoke(this, null);
        }
        else
        {
            coreTextObject.MaxLettersToShow = lettersToShow;
        }
    }
#endif

    private string[] ConvertToPages(string text)
    {

        var limitsLines = 
            this.coreTextObject.MaxNumberOfLines != null || 
            this.textComponent.HeightUnits != global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        if(!limitsLines)
        {
            // it can show any number of lines, it's up to the user to handle spillover
            // by limiting the page length or by expanding the dialog box.
            return new string[] { text };
        }
        else
        {
            // To remove the tags, we must keep newlines in since since that's how the tags are removed...
            var foundTagsWithNewlines = BbCodeParser.Parse(text, CustomSetPropertyOnRenderable.Tags);
            // ...but when we add the tags back in, we do it without counting newlines, so we need to remove newlines for 
            // the tags that are added back in:
            var foundTagsWithoutNewlines = BbCodeParser.Parse(text.Replace("\n", ""), CustomSetPropertyOnRenderable.Tags);
            var withRemovedTags = BbCodeParser.RemoveTags(text, foundTagsWithNewlines);

            var unlimitedLines = new List<string>();
            var oldVerticalMode = this.textComponent.TextOverflowVerticalMode;
            this.textComponent.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;

            coreTextObject.RawText = withRemovedTags;
            coreTextObject.UpdateLines(unlimitedLines);

            this.textComponent.TextOverflowVerticalMode = oldVerticalMode;

            this.textComponent.SetProperty("Text", withRemovedTags);
            this.textComponent.TextOverflowVerticalMode = oldVerticalMode;

            var limitedLines = coreTextObject.WrappedText;

            if (unlimitedLines.Count == limitedLines.Count)
            {
                // no need to break it up
                return new string[] { text };
            }
            else
            {
                var pages = new List<string>();

                var absoluteLineNumber = 0;

                var currentPage = new StringBuilder();
                currentPage.Clear();

                StringBuilder stringBuilder = new StringBuilder();
                int strippedTextCount = 0;
                while(absoluteLineNumber < unlimitedLines.Count)
                {
                    stringBuilder.Clear();

                    for(int i = 0; i < limitedLines.Count && absoluteLineNumber < unlimitedLines.Count; i++)
                    {
                        var toAppend = unlimitedLines[absoluteLineNumber];
                        var sizeBeforeTags = toAppend.Length;
                        if(foundTagsWithoutNewlines.Count > 0)
                        {
                            toAppend = BbCodeParser.AddTags(toAppend, foundTagsWithoutNewlines, strippedTextCount);
                        }

                        strippedTextCount += sizeBeforeTags;
                        if(toAppend?.EndsWith(" ") == true)
                        {
                            toAppend = toAppend.Substring(0, toAppend.Length - 1);
                        }
                        stringBuilder.Append(toAppend + "\n");
                        absoluteLineNumber++;
                    }
                    pages.Add(stringBuilder.ToString());
                }

                return pages.ToArray();
            }
        }


    }

#endregion

    #region Event Handler Methods

    private void HandleClick(object? sender, EventArgs args)
    {
        if(AdvancePageInputPredicate == null)
        {
            ReactToConfirmInput();
        }
    }

    /// <summary>
    /// This makes the next page auto-show when pushing input on an already-typed out page.
    /// This should be true if doing a normal Show call, but false if in an async call since
    /// the async call will internally loop through all pages.
    /// </summary>
    bool showNextPageOnDismissedPage = true;
    private void ReactToConfirmInput()
    {
        ReactToInputForAdvancing(forceImmediatePrint: false);
    }

    private void ReactToCancelInput()
    {
        wasLastAdvancePressPrintImmediate = TypeNextPageImmediatelyOnCancelPush;
        ReactToInputForAdvancing(forceImmediatePrint: true);
    }

    private void ReactToInputForAdvancing(bool forceImmediatePrint)
    {
        ////////////////////Early Out/////////////////////
        if (!TakingInput)
        {
            return;
        }
        //////////////////End Early Out///////////////////
        //var hasMoreToType = coreTextObject.MaxLettersToShow < currentPageText?.Length;

        // Use the raw text since that has stripped out the tags
        var hasMoreToType = coreTextObject.MaxLettersToShow < coreTextObject.RawText.Length;
        if (hasMoreToType)
        {
#if FRB
            showLetterTweener?.Stop();
#else
            isTyping = false;
#endif

            if (continueIndicatorInstance != null && TakingInput)
            {
                continueIndicatorInstance.Visible = true;
            }

            coreTextObject.MaxLettersToShow = currentPageText.Length;

            FinishedTypingPage?.Invoke(this, null);
        }
        else if (Pages.Count > 0)
        {
            if (showNextPageOnDismissedPage)
            {
                ShowNextPage(forceImmediatePrint);
            }

            PageAdvanced?.Invoke(this, null);
        }
        else
        {
            Dismiss();
        }
    }

    public void Dismiss()
    {
        this.IsVisible = false;
#if FRB
        LastTimeDismissed = TimeManager.CurrentTime;
#elif XNALIKE
        LastTimeDismissed = GumService.Default.GameTime.TotalGameTime.TotalSeconds;
#else
        LastTimeDismissed = GumService.Default.GameTime;
#endif
        PageAdvanced?.Invoke(this, null);
        FinishedShowing?.Invoke(this, null);
        this.Pages.Clear();
        IsFocused = false;
    }

    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
    }

    public void OnLoseFocus()
    {
        IsFocused = false;
    }

    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
#if !FRB
        ReceiveInput();

        //var shift = keyboard.IsShiftDown;
        //var ctrl = keyboard.IsCtrlDown;
        //var alt = keyboard.IsAltDown;




        //// This allocates. We could potentially make this return 
        //// an IList or List. That's a breaking change for a tiny amount
        //// of allocation....what to do....

        //// Handle all the special situations only based on Keyboard.GetState
        ////   Situations: LEFT, HOME, END, BACK (Backspace), RIGHT, UP, DOWN, DELETE, CTRL+C, CTRL+X, CTRL+V, CTRL+A
        //foreach (Keys key in keyboard.KeysTyped)
        //{
        //    HandleKeyDown(key, shift, alt, ctrl);
        //}

        //// String of letters typed and captured via the TextInput() Monogame event
        //var stringTyped = keyboard.GetStringTyped();

        //if (stringTyped != null)
        //{
        //    for (int i = 0; i < stringTyped.Length; i++)
        //    {
        //        // If a \t character is here it could be from..
        //        // * pressing tab
        //        // * repeat rate tab
        //        // * paste
        //        // If AcceptsTab is false, we should ignore tabs altogether
        //        // Maybe in the future we can inspect if it was pasted but this
        //        // is trickier because we are relying on the Windows implementation.
        //        // receiver could get nulled out by itself when something like enter is pressed
        //        var character = stringTyped[i];
        //        if (character != '\t' || AcceptsTab)
        //        {
        //            HandleCharEntered(character);
        //        }
        //    }
        //}
#endif
    }

#endregion

    #region Utilities

#if DEBUG
    private void ReportMissingTextInstance()
    {
        if (textComponent == null)
        {
            throw new Exception(
                $"This button was created with a Gum component ({Visual?.ElementSave}) " +
                "that does not have an instance called 'text'. A 'text' instance must be added to modify the button's Text property.");
        }
    }
#endif

    #endregion

    #region IInputReceiver Methods

    public void OnFocusUpdate()
    {
        if(AdvancePageInputPredicate != null)
        {
            if(AdvancePageInputPredicate())
            {
                ReactToConfirmInput();
            }
        }
        else
        {

            var gamepads = FrameworkElement.GamePadsForUiControl;
            for (int i = 0; i < gamepads.Count; i++)
            {
                var gamepad = gamepads[i];

#if FRB
                var inputDevice = gamepad as IInputDevice;

                if(inputDevice.DefaultConfirmInput.WasJustPressed)
                {
                    ReactToConfirmInput();
                }

                if(inputDevice.DefaultCancelInput.WasJustPressed)
                {
                    ReactToCancelInput();
                }
#else

                if(gamepad.ButtonPushed(GamepadButton.A))
                {
                    ReactToConfirmInput();
                }

                if(gamepad.ButtonPushed(GamepadButton.B))
                {
                    ReactToCancelInput();
                }
#endif
            }

#if FRB
            var genericGamepads = GuiManager.GenericGamePadsForUiControl;
            for(int i = 0; i < genericGamepads.Count; i++)
            {
                var gamepad = gamepads[i];

                var inputDevice = gamepad as IInputDevice;

                if (inputDevice.DefaultConfirmInput.WasJustPressed)
                {
                    ReactToConfirmInput();
                }

                if(inputDevice.DefaultCancelInput.WasJustPressed)
                {
                    ReactToCancelInput();
                }
            }
            var keyboardAsInputDevice = InputManager.Keyboard as IInputDevice;

            if(keyboardAsInputDevice.DefaultPrimaryActionInput.WasJustPressed)
            {
                ReactToConfirmInput();
            }
            if(keyboardAsInputDevice.DefaultCancelInput.WasJustPressed)
            {
                ReactToCancelInput();
            }
#else
            foreach (var keyboard in KeyboardsForUiControl)
            {
                foreach (var combo in FrameworkElement.ClickCombos)
                {
                    if (combo.IsComboPushed())
                    {
                        ReactToConfirmInput();
                        break;
                    }
                }
                // todo - customize this, for now let's go with ESC
                if(keyboard.KeyPushed(Input.Keys.Escape))
                {
                    ReactToCancelInput();
                }
            }

#endif
        }

    }

    public void OnGainFocus()
    {
    }

    public void LoseFocus()
    {
    }

    public void ReceiveInput()
    {
    }

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {

    }

    public void HandleCharEntered(char character)
    {

    }

#endregion

    #region UpdateTo Methods

    private void UpdateToIsFocused()
    {
        UpdateState();

        if (isFocused)
        {
            if (InteractiveGue.CurrentInputReceiver != this)
            {
                InteractiveGue.CurrentInputReceiver = this;
            }
        }

        else if (!isFocused)
        {
            if (InteractiveGue.CurrentInputReceiver == this)
            {
                InteractiveGue.CurrentInputReceiver = null;
            }

            // Vic says - why do we need to deselect when it loses focus? It could stay selected
            //SelectionLength = 0;
        }
    }

    #endregion
}
