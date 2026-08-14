using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class VariableCategoryCopyPasteServiceTests
{
    private class FakeRow : IVariableCategoryRow
    {
        public string RootVariableName { get; set; } = "";
        public bool IsReadOnly { get; set; }
        public bool IsAssignedByReference { get; set; }
        public bool IsIndeterminate { get; set; }
        public object? Value { get; set; }
        public int SetCount { get; private set; }

        /// <summary>Defaults to the declared type of whatever the row currently holds, as a real row would.</summary>
        public Type? DeclaredType { get; set; }

        /// <summary>Optional shared log recording write order across several rows.</summary>
        public List<string>? WriteLog { get; set; }

        public Type? ValueType => DeclaredType ?? Value?.GetType();

        public bool TrySetValue(object value)
        {
            Value = value;
            SetCount++;
            WriteLog?.Add(RootVariableName);
            return true;
        }
    }

    private readonly Mock<IUndoManager> _undoManager = new();

    private VariableCategoryCopyPasteService CreateService() => new(_undoManager.Object);

    [Fact]
    public void Copy_ShouldExcludeIdentityVariablesNullValuesAndLists()
    {
        FakeRow name = new FakeRow { RootVariableName = "Name", Value = "TitleText" };
        FakeRow baseType = new FakeRow { RootVariableName = "BaseType", Value = "Text" };
        FakeRow defaultChild = new FakeRow { RootVariableName = "DefaultChildContainer", Value = "Inner" };
        FakeRow locked = new FakeRow { RootVariableName = "Locked", Value = true };
        FakeRow parent = new FakeRow { RootVariableName = "Parent", Value = "SomeContainer" };
        FakeRow references = new FakeRow { RootVariableName = "VariableReferences", Value = new List<string>() };
        FakeRow someList = new FakeRow { RootVariableName = "SomeListVariable", Value = new List<string> { "a" } };
        FakeRow unset = new FakeRow { RootVariableName = "MaxLettersToShow", Value = null };
        FakeRow fontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("General", new IVariableCategoryRow[]
        {
            name, baseType, defaultChild, locked, parent, references, someList, unset, fontSize
        });

        service.CopiedCategory.ShouldNotBeNull();
        service.CopiedCategory!.CategoryName.ShouldBe("General");
        service.CopiedCategory.Values.Select(item => item.RootVariableName).ShouldBe(new[] { "FontSize" });
    }

    /// <summary>
    /// A multi-select row whose instances disagree reports null, which is indistinguishable from an unset
    /// variable by value alone. Copy must report it so the user knows the capture is incomplete.
    /// </summary>
    [Fact]
    public void Copy_ShouldReportIndeterminateRowsInsteadOfSilentlyDroppingThem()
    {
        FakeRow fontSize = new FakeRow { RootVariableName = "FontSize", Value = null, IsIndeterminate = true };
        FakeRow isBold = new FakeRow { RootVariableName = "IsBold", Value = true };

        VariableCategoryCopyPasteService service = CreateService();
        CopiedVariableCategory copied = service.Copy("Font", new IVariableCategoryRow[] { fontSize, isBold });

        copied.Values.Select(item => item.RootVariableName).ShouldBe(new[] { "IsBold" });
        copied.IndeterminateVariableNames.ShouldBe(new[] { "FontSize" });
    }

    [Fact]
    public void Paste_ShouldApplyMatchingVariables_AndSkipOnesTheTargetDoesNotHave()
    {
        FakeRow sourceFontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };
        FakeRow sourceIsBold = new FakeRow { RootVariableName = "IsBold", Value = true };
        FakeRow targetFontSize = new FakeRow { RootVariableName = "FontSize", Value = 12 };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFontSize, sourceIsBold });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { targetFontSize });

        targetFontSize.Value.ShouldBe(36);
        result.AppliedVariableNames.ShouldBe(new[] { "FontSize" });
        result.SkippedVariableNames.ShouldBe(new[] { "IsBold" });
    }

    [Fact]
    public void Paste_ShouldApplyUnitVariablesBeforeTheirValues()
    {
        List<string> writeLog = new List<string>();
        FakeRow sourceX = new FakeRow { RootVariableName = "X", Value = 100f };
        FakeRow sourceXUnits = new FakeRow { RootVariableName = "XUnits", Value = "PixelsFromLeft" };
        FakeRow targetX = new FakeRow { RootVariableName = "X", Value = 5f, WriteLog = writeLog };
        FakeRow targetXUnits = new FakeRow { RootVariableName = "XUnits", Value = "PercentageOfParent", WriteLog = writeLog };

        VariableCategoryCopyPasteService service = CreateService();
        // Category order is X then XUnits; writing X first would let the unit change convert it away.
        service.Copy("Position", new IVariableCategoryRow[] { sourceX, sourceXUnits });
        service.Paste(new IVariableCategoryRow[] { targetX, targetXUnits });

        writeLog.ShouldBe(new[] { "XUnits", "X" });
    }

    [Fact]
    public void Paste_ShouldConvertNumericValuesToTheTargetRowsNumericType()
    {
        FakeRow sourceFontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };
        FakeRow targetFontSize = new FakeRow { RootVariableName = "FontSize", Value = 12f, DeclaredType = typeof(float) };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFontSize });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { targetFontSize });

        targetFontSize.Value.ShouldBe(36f);
        result.AppliedVariableNames.ShouldBe(new[] { "FontSize" });
    }

    /// <summary>
    /// Nullable-declared variables (int? MaxLettersToShow, float? MinWidth) are common, and reflection's
    /// IsInstanceOfType is always false for a boxed value against a Nullable&lt;T&gt; type.
    /// </summary>
    [Fact]
    public void Paste_ShouldNotRejectANullableTargetWhoseUnderlyingTypeMatches()
    {
        FakeRow sourceMaxLetters = new FakeRow { RootVariableName = "MaxLettersToShow", Value = 20 };
        FakeRow targetMaxLetters = new FakeRow { RootVariableName = "MaxLettersToShow", Value = null, DeclaredType = typeof(int?) };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Text", new IVariableCategoryRow[] { sourceMaxLetters });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { targetMaxLetters });

        targetMaxLetters.Value.ShouldBe(20);
        result.AppliedVariableNames.ShouldBe(new[] { "MaxLettersToShow" });
    }

    [Fact]
    public void Paste_ShouldNotRewriteAValueTheTargetAlreadyShows()
    {
        FakeRow sourceFontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };
        // Matches already, but only by inheriting it - rewriting would author it explicitly for no gain.
        FakeRow targetFontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFontSize });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { targetFontSize });

        targetFontSize.SetCount.ShouldBe(0);
        result.AppliedVariableNames.ShouldBeEmpty();
        result.AlreadyMatchedVariableNames.ShouldBe(new[] { "FontSize" });
    }

    [Fact]
    public void Paste_ShouldSkipReadOnlyAndReferenceAssignedRows()
    {
        FakeRow sourceFontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };
        FakeRow sourceFont = new FakeRow { RootVariableName = "Font", Value = "Luckiest Guy" };
        FakeRow lockedFontSize = new FakeRow { RootVariableName = "FontSize", Value = 12, IsReadOnly = true };
        FakeRow referencedFont = new FakeRow { RootVariableName = "Font", Value = "Arial", IsAssignedByReference = true };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFontSize, sourceFont });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { lockedFontSize, referencedFont });

        lockedFontSize.SetCount.ShouldBe(0);
        referencedFont.SetCount.ShouldBe(0);
        result.AppliedVariableNames.ShouldBeEmpty();
        result.SkippedVariableNames.ShouldBe(new[] { "FontSize", "Font" }, ignoreOrder: true);
    }

    [Fact]
    public void Paste_ShouldSkipValuesWhoseTypeTheTargetRowDoesNotAccept()
    {
        FakeRow sourceFont = new FakeRow { RootVariableName = "Font", Value = "Luckiest Guy" };
        // The target holds no value yet, so only its declared type can reject the paste.
        FakeRow targetFont = new FakeRow { RootVariableName = "Font", Value = null, DeclaredType = typeof(int) };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFont });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { targetFont });

        targetFont.SetCount.ShouldBe(0);
        result.SkippedVariableNames.ShouldBe(new[] { "Font" });
    }

    [Fact]
    public void Paste_ShouldTakeASingleUndoLockHeldAcrossEveryWrite()
    {
        FakeRow sourceFontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };
        FakeRow sourceIsBold = new FakeRow { RootVariableName = "IsBold", Value = true };
        FakeRow targetFontSize = new FakeRow { RootVariableName = "FontSize", Value = 12 };
        FakeRow targetIsBold = new FakeRow { RootVariableName = "IsBold", Value = false };

        int writesWhenLockReleased = -1;
        _undoManager
            .Setup(item => item.RequestLock())
            .Returns(() => new UndoLock(() =>
                writesWhenLockReleased = targetFontSize.SetCount + targetIsBold.SetCount));

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFontSize, sourceIsBold });
        service.Paste(new IVariableCategoryRow[] { targetFontSize, targetIsBold });

        _undoManager.Verify(item => item.RequestLock(), Times.Once);
        writesWhenLockReleased.ShouldBe(2, "the lock must still be held while every value is written");
        targetFontSize.Value.ShouldBe(36);
        targetIsBold.Value.ShouldBe(true);
    }

    /// <summary>
    /// A multi-select row whose instances disagree reports a null value (indeterminate). Pasting onto it
    /// must write, unifying the selection - the skip-if-equal guard must not treat "no single value" as
    /// "already matches".
    /// </summary>
    [Fact]
    public void Paste_ShouldWriteToAnIndeterminateTargetRow()
    {
        FakeRow sourceVisible = new FakeRow { RootVariableName = "Visible", Value = true };
        FakeRow indeterminateVisible = new FakeRow { RootVariableName = "Visible", Value = null };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("States and Visibility", new IVariableCategoryRow[] { sourceVisible });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { indeterminateVisible });

        indeterminateVisible.SetCount.ShouldBe(1);
        indeterminateVisible.Value.ShouldBe(true);
        result.AppliedVariableNames.ShouldBe(new[] { "Visible" });
    }
}
