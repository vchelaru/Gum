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

using GamepadButton = Gum.Input.GamepadButton;

#if XNALIKE
using MonoGameGum;
#endif

#if !FRB || !XNALIKE
using Keys = Gum.Forms.Input.Keys;
#endif

#region DialogPageTask

public class DialogPageTask
{
    public string? Page { get; set; }
    public Func<Task>? Task { get; set; }

    /// <summary>
    /// True for a page piece already translated and split when Show was called, which is shown as-is.
    /// False for a key or literal that is translated when the page is shown.
    /// </summary>
    internal bool IsTranslated { get; set; }

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

    GraphicalUiElement? textComponent;
    IFormsText? coreTextObject;

    // Required to show text. Checked on use rather than when the visual is assigned so a
    // visual without a TextInstance can still be loaded.
    GraphicalUiElement TextComponent => textComponent
        ?? throw new InvalidOperationException("The DialogBox visual must have an object called \"TextInstance\"");
    IFormsText CoreTextObject => coreTextObject
        ?? throw new InvalidOperationException("The DialogBox's \"TextInstance\" must be a text object");

    GraphicalUiElement? continueIndicatorInstance;


    public IInputReceiver? ParentInputReceiver =>
        this.GetParentInputReceiver();
    public static double LastTimeDismissed { get; private set; }

    List<DialogPageTask> Pages = new List<DialogPageTask>();

    public int PagesRemaining => Pages.Count;

    static global::Gum.DataTypes.Variables.StateSave NoTextShownState;

    string currentPageText = string.Empty;

#if FRB
    Tweener showLetterTweener;
#else
    bool isTyping;
    double typingElapsedSeconds;
#endif

    public event Action<IInputReceiver>? FocusUpdate;

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput { get; set; } = true;

    public IInputReceiver? NextInTabSequence { get; set; }

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
    public event EventHandler? FinishedShowing;

    /// <summary>
    /// Raised whenever a page finishes typing out, either automatically or in response to input.
    /// </summary>
    public event EventHandler? FinishedTypingPage;

    public event EventHandler? PageAdvanced;

    /// <summary>
    /// If not null, this predicate is used to determine if input
    /// has been pressed to advance the input. If null, the default 
    /// page-advancing logic will be performed.
    /// </summary>
    public Func<bool>? AdvancePageInputPredicate;

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

        coreTextObject = textComponent?.RenderableComponent as IFormsText;

#if FRB
        Visual.Click += this.HandleClick(this, EventArgs.Empty);
#else
        Visual.Click += this.HandleClick;
#endif

        base.ReactToVisualChanged();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        // FRB1 only: these lambdas cannot be unsubscribed when the Visual is replaced, so the old
        // Visual keeps driving this control after a swap (see #6208). Not fixed until a FRB1 project needs it.
#if !FRB
        Visual.Click -= this.HandleClick;
#endif
    }

    #endregion

    #region Show Methods
    /// <summary>
    /// Shows the dialog box (adds it to managers and sets IsVisible to true) and begins showing the text.
    /// </summary>
    /// <param name="text">The text to print out, either immediately or letter-by-letter according to LettersPerSecond.</param>
    /// <param name="frbLayer">The FlatRedBall Layer to add the DialogBox to. If null, the dialog box will not be layered. This will attempt to use a Gum layer matching the FRB layer. This will automatically work if the Layer has been added through the FlatRedBall Editor.</param>
    public void Show(string text, Layer? frbLayer = null)
    {
        // Delegate to the IEnumerable overload which calls ConvertToPages on each
        // entry; doing it here too would double-paginate.
        Show(new[] { text }, frbLayer);
    }

    public void Show(IEnumerable<string> pages, Layer? frbLayer = null)
    {
        ShowVisual(frbLayer);

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
                AddPages(page);
            }

            ShowNextPage();
        }
    }

    // base.Show uses the (obsolete) AddToManagers path which sets up rendering
    // but does not parent the Visual into GumService.Default.Root. The per-frame
    // pump (GumService.Update -> Root.AnimateSelf) only walks Root.Children, so
    // without this the typewriter (IUpdateEveryFrame.Activity) never ticks.
    // Skipped under FRB — that runtime has its own pump path via base.Show.
    // Parents first, so base.Show sees a managed ancestor and does not also register the
    // renderable as a top-level layer item, which would draw the dialog twice.
    private void ShowVisual(Layer? frbLayer)
    {
        EnsureVisualInRoot();
#pragma warning disable CS0618 // DialogBox's layer overloads are built on the obsolete Show(Layer)
        base.Show(frbLayer);
#pragma warning restore CS0618
    }

    private void EnsureVisualInRoot()
    {
#if !FRB
        if (Visual == null || Visual.Parent != null) return;
        var root = global::RenderingLibrary.IGumService.Default?.Root;
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


    public async Task ShowAsync(IEnumerable<string> pages, Layer? frbLayer = null)
    {
        ShowVisual(frbLayer);

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
                AddPages(page);
            }
            await StartShowAllPagesLoop();
        }
    }

    public async Task ShowAsync(IEnumerable<DialogPageTask> pageTasks, Layer? frbLayer = null)
    {
        ShowVisual(frbLayer);

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
    public async Task<bool?> ShowDialog(IEnumerable<string> pageTasks, Layer? frbLayer = null)
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
    public async Task<bool?> ShowDialog(IEnumerable<DialogPageTask> pageTasks, Layer? frbLayer = null)
    {
#if DEBUG
        if (Visual == null)
        {
            throw new InvalidOperationException("Visual must be set before calling Show");
        }
#endif
        var semaphoreSlim = new SemaphoreSlim(1);

        void HandleRemovedFromManagers(object? sender, EventArgs args) => semaphoreSlim.Release();
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
            ShowInternal(page.Page ?? string.Empty, forceImmediatePrint, page.IsTranslated);
            Pages.RemoveAt(0);
        }
    }

    // Splits one entry into the pages that fit the text box and queues them. The split measures the
    // translated text, since a key is short and its translation is what has to fit.
    private void AddPages(string page)
    {
        string translated = TranslateForPagination(page);
        string[] pieces = ConvertToPages(translated);

        if (pieces.Length == 1)
        {
            // It fits: keep the original so it is translated when shown and follows a language change.
            Pages.Add(new DialogPageTask { Page = page });
        }
        else
        {
            // The pieces are slices of one language's translation, so they are shown as-is: looked up
            // again they would be missing keys ("(loc)" suffix) and could not follow a language change.
            foreach (var piece in pieces)
            {
                Pages.Add(new DialogPageTask { Page = piece, IsTranslated = true });
            }
        }
    }

    // Mirrors the Text assignment: a value containing markup is used as-is, otherwise it is translated.
    private static string TranslateForPagination(string text)
    {
        var localizationService = global::Gum.Localization.LocalizationRuntimeState.Current;
        if (localizationService == null || text.Contains("["))
        {
            return text;
        }
        return localizationService.Translate(text);
    }

    // Stops the typewriter so a later Activity tick cannot finish a page that is no longer showing.
    private void StopTyping()
    {
#if !FRB
        isTyping = false;
#endif
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
#else
                    LastTimeDismissed = global::RenderingLibrary.IGumService.Default?.GameTime ?? 0;
#endif
                    PageAdvanced?.Invoke(this, EventArgs.Empty);
                    FinishedShowing?.Invoke(this, EventArgs.Empty);
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

                void ReleaseSemaphor(object? sender, EventArgs args) => 
                    semaphoreSlim.Release();

                this.PageAdvanced += ReleaseSemaphor;

                semaphoreSlim.Wait();
                ShowInternal(page.Page ?? string.Empty, forceImmediatePrint: wasLastAdvancePressPrintImmediate, isTranslated: page.IsTranslated);

                await semaphoreSlim.WaitAsync();
                semaphoreSlim.Dispose();
                this.PageAdvanced -= ReleaseSemaphor;

            }
            page = Pages.FirstOrDefault();
        }
    }

    private void ShowInternal(string text, bool forceImmediatePrint, bool isTranslated)
    {
        // Every show path ends here, so Glyph tags become their character before letters are counted.
        text = BbCodeParser.ReplaceGlyphTags(text);

        IsVisible = true;

        currentPageText = text;

#if FRB
        showLetterTweener?.Stop();
#else
        isTyping = false;
#endif
        // go through the component instead of the core text object to force a layout refresh if necessary
        TextComponent.SetProperty(isTranslated ? "TextNoTranslate" : "Text", text);

        // The component translated the text and stripped its tags, so count what it holds rather than
        // the untranslated input: a translation can be longer or shorter than the key.
        var strippedLength = CoreTextObject.RawText?.Length ?? 0;

        var shouldPrintCharacterByCharacter = LettersPerSecond > 0 && !forceImmediatePrint;
        if(shouldPrintCharacterByCharacter)
        {
            CoreTextObject.MaxLettersToShow = 0;

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
                FinishedTypingPage?.Invoke(this, EventArgs.Empty);
            };
#else
            typingElapsedSeconds = 0;
            isTyping = true;
#endif
        }
        else
        {
            CoreTextObject.MaxLettersToShow = strippedLength;

            if (TakingInput && continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = true;
            }

            if (continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = true;
            }
            FinishedTypingPage?.Invoke(this, EventArgs.Empty);
        }
    }

#if !FRB
    void Gum.Wireframe.IUpdateEveryFrame.Activity(double secondDifference)
    {
        if (!isTyping) return;

        typingElapsedSeconds += secondDifference;

        // Read the length every frame: the text can be replaced while typing (a language change
        // re-translates it), so a count stored in ShowInternal would go stale.
        var targetLetterCount = CoreTextObject.RawText?.Length ?? 0;
        var lettersToShow = (int)(typingElapsedSeconds * (LettersPerSecond ?? 0));
        if (lettersToShow >= targetLetterCount)
        {
            lettersToShow = targetLetterCount;
            isTyping = false;
            CoreTextObject.MaxLettersToShow = lettersToShow;

            if (TakingInput && continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = true;
            }
            FinishedTypingPage?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            CoreTextObject.MaxLettersToShow = lettersToShow;
        }
    }
#endif

    private string[] ConvertToPages(string text)
    {
        // Glyph tags become their character first so pagination measures the real text.
        text = BbCodeParser.ReplaceGlyphTags(text);

        var limitsLines = 
            this.CoreTextObject.MaxNumberOfLines != null || 
            this.TextComponent.HeightUnits != global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        if(!limitsLines)
        {
            // it can show any number of lines, it's up to the user to handle spillover
            // by limiting the page length or by expanding the dialog box.
            return new string[] { text };
        }
        else
        {
            // To remove the tags, we must keep newlines in since since that's how the tags are removed...
            var foundTagsWithNewlines = BbCodeParser.Parse(text, BbCodeParser.KnownTags);
            // ...but when we add the tags back in, we do it without counting newlines, so we need to remove newlines for 
            // the tags that are added back in:
            var foundTagsWithoutNewlines = BbCodeParser.Parse(text.Replace("\n", ""), BbCodeParser.KnownTags);
            var withRemovedTags = BbCodeParser.RemoveTags(text, foundTagsWithNewlines);

            var unlimitedLines = new List<string>();
            var oldVerticalMode = this.TextComponent.TextOverflowVerticalMode;
            this.TextComponent.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;

            CoreTextObject.RawText = withRemovedTags;
            CoreTextObject.UpdateLines(unlimitedLines);

            this.TextComponent.TextOverflowVerticalMode = oldVerticalMode;

            // The text is already translated, so it must not be looked up again.
            this.TextComponent.SetProperty("TextNoTranslate", withRemovedTags);
            this.TextComponent.TextOverflowVerticalMode = oldVerticalMode;

            var limitedLines = CoreTextObject.WrappedText;

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
        var hasMoreToType = CoreTextObject.MaxLettersToShow < (CoreTextObject.RawText?.Length ?? 0);
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

            CoreTextObject.MaxLettersToShow = CoreTextObject.RawText?.Length ?? 0;

            FinishedTypingPage?.Invoke(this, EventArgs.Empty);
        }
        else if (Pages.Count > 0)
        {
            // Leaving this page ends its typing. The async loop resumes after this returns, so until it
            // does a tick must not finish a page that was already advanced past.
            StopTyping();
            if (showNextPageOnDismissedPage)
            {
                ShowNextPage(forceImmediatePrint);
            }

            PageAdvanced?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Dismiss();
        }
    }

    public void Dismiss()
    {
        StopTyping();
        this.IsVisible = false;
#if FRB
        LastTimeDismissed = TimeManager.CurrentTime;
#else
        LastTimeDismissed = global::RenderingLibrary.IGumService.Default?.GameTime ?? 0;
#endif
        PageAdvanced?.Invoke(this, EventArgs.Empty);
        // Finish cleanup BEFORE raising FinishedShowing so a handler that re-shows the
        // dialog (e.g. a chained sequence of dialogs) is not undone by the state reset
        // below. Matches the async path, which fires the event only after the show loop ends.
        this.Pages.Clear();
        IsFocused = false;
        FinishedShowing?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    protected override void ReactToClosed()
    {
        base.ReactToClosed();
        StopTyping();
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

    #region IInputReceiver Methods

    public void OnFocusUpdate()
    {
#if !FRB
        // A disabled control can hold focus (AllowFocusWhenDisabled) but does not act on input.
        if (!IsEnabled)
        {
            FocusUpdate?.Invoke(this);
            return;
        }
#endif
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
                var gamepad = genericGamepads[i];

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

        // Navigation out of the dialog (Tab/Shift+Tab, arrows, d-pad) works like any other focusable control,
        // independent of how the advance input is configured. Confirm may have dismissed the dialog, and one
        // navigation call may have moved focus, so each step checks that this dialog still holds focus.
        var navigationGamepads = FrameworkElement.GamePadsForUiControl;
        for (int i = 0; i < navigationGamepads.Count && IsFocused; i++)
        {
            HandleGamepadNavigation(navigationGamepads[i]);
        }
#if FRB
        var navigationGenericGamepads = GuiManager.GenericGamePadsForUiControl;
        for (int i = 0; i < navigationGenericGamepads.Count && IsFocused; i++)
        {
            HandleGamepadNavigation(navigationGenericGamepads[i]);
        }
#else
        if (IsFocused)
        {
            HandleKeyboardFocusUpdate();
        }
#endif

        FocusUpdate?.Invoke(this);
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
