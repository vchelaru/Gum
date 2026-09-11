using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.DataUi;

/// <summary>
/// The editing rules <see cref="TextBoxDisplayLogic"/> applies for any head's text box: focus,
/// Enter, Escape, double-click, and value-state tinting.
/// </summary>
public class TextBoxDisplayLogicEditingTests
{
    private sealed class FakeTextBox : IDataUiTextBox
    {
        public string Text { get; set; } = "";
        public bool WasSelectAllCalled { get; private set; }
        public DataUiValueState? AppliedState { get; private set; }

        public void SelectAll() => WasSelectAllCalled = true;

        public void ApplyValueState(DataUiValueState state) => AppliedState = state;
    }

    private sealed class FakeDataUi : IDataUi
    {
        private readonly FakeTextBox _textBox;

        public FakeDataUi(FakeTextBox textBox)
        {
            _textBox = textBox;
            Logic = new TextBoxDisplayLogic(this, textBox);
        }

        public TextBoxDisplayLogic Logic { get; }
        public InstanceMember? InstanceMember { get; set; }
        public bool SuppressSettingProperty { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int RefreshCount { get; private set; }

        public void Refresh(bool forceRefreshEvenIfFocused = false) => RefreshCount++;

        public ApplyValueResult TryGetValueOnUi(out object? result) => Logic.TryGetValueOnUi(out result);

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            _textBox.Text = Logic.ConvertNumberToString(value);
            return ApplyValueResult.Success;
        }
    }

    private sealed class Target
    {
        public float Width { get; set; }
    }

    private static FakeDataUi CreateBoundTo(Target target, FakeTextBox textBox)
    {
        FakeDataUi dataUi = new FakeDataUi(textBox);
        InstanceMember member = new InstanceMember(nameof(Target.Width), target);
        dataUi.InstanceMember = member;
        dataUi.Logic.InstanceMember = member;
        dataUi.Logic.RefreshDisplay(out _);
        return dataUi;
    }

    [Fact]
    public void HandleEnterKey_CommitsANumberClampedToMax_AndConsumesTheKey()
    {
        Target target = new Target { Width = 1 };
        FakeTextBox textBox = new FakeTextBox();
        FakeDataUi dataUi = CreateBoundTo(target, textBox);
        dataUi.Logic.MaxValue = 100;
        dataUi.Logic.HandleGotFocus();

        textBox.Text = "150";
        dataUi.Logic.HandleTextChanged();
        bool handled = dataUi.Logic.HandleEnterKey();

        handled.ShouldBeTrue();
        target.Width.ShouldBe(100f);
        textBox.Text.ShouldBe("100");
        dataUi.RefreshCount.ShouldBe(1);
    }

    [Fact]
    public void HandleEnterKey_EvaluatesMath()
    {
        Target target = new Target { Width = 1 };
        FakeTextBox textBox = new FakeTextBox();
        FakeDataUi dataUi = CreateBoundTo(target, textBox);
        dataUi.Logic.HandleGotFocus();

        textBox.Text = "60*2";
        dataUi.Logic.HandleTextChanged();
        dataUi.Logic.HandleEnterKey();

        target.Width.ShouldBe(120f);
    }

    [Fact]
    public void HandleEnterKey_DoesNothing_WhenTheFieldDoesNotHandleEnter()
    {
        Target target = new Target { Width = 1 };
        FakeTextBox textBox = new FakeTextBox();
        FakeDataUi dataUi = CreateBoundTo(target, textBox);
        dataUi.Logic.HandlesEnter = false;

        textBox.Text = "5";
        bool handled = dataUi.Logic.HandleEnterKey();

        handled.ShouldBeFalse();
        target.Width.ShouldBe(1f);
    }

    [Fact]
    public void HandleEscapeKey_RestoresTheTextFromWhenEditingStarted()
    {
        Target target = new Target { Width = 7 };
        FakeTextBox textBox = new FakeTextBox();
        FakeDataUi dataUi = CreateBoundTo(target, textBox);

        dataUi.Logic.HandleGotFocus();
        textBox.WasSelectAllCalled.ShouldBeTrue();
        textBox.Text = "12";
        dataUi.Logic.HandleTextChanged();
        dataUi.Logic.HandleEscapeKey();

        textBox.Text.ShouldBe("7");
        dataUi.Logic.HasUserChangedAnything.ShouldBeFalse();
    }

    [Fact]
    public void ShouldSelectAllOnClick_OnlyForDoubleClickOnNumbers()
    {
        FakeDataUi dataUi = CreateBoundTo(new Target(), new FakeTextBox());

        dataUi.Logic.ShouldSelectAllOnClick(clickCount: 2).ShouldBeTrue();
        dataUi.Logic.ShouldSelectAllOnClick(clickCount: 1).ShouldBeFalse();
    }

    [Fact]
    public void RefreshBackgroundColor_PassesTheMembersValueState()
    {
        FakeTextBox textBox = new FakeTextBox();
        CreateBoundTo(new Target(), textBox);

        // A reflection member is never default or indeterminate.
        textBox.AppliedState.ShouldBe(DataUiValueState.Custom);
    }
}
