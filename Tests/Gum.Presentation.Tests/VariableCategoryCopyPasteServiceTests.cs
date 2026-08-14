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
        public object? Value { get; set; }
        public int SetCount { get; private set; }

        public bool TrySetValue(object? value)
        {
            Value = value;
            SetCount++;
            return true;
        }
    }

    private readonly Mock<IUndoManager> _undoManager = new();

    private VariableCategoryCopyPasteService CreateService() => new(_undoManager.Object);

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
    public void Paste_ShouldSkipValuesWhoseTypeDoesNotMatchTheTargetsCurrentValue()
    {
        FakeRow sourceFont = new FakeRow { RootVariableName = "Font", Value = "Luckiest Guy" };
        FakeRow targetFont = new FakeRow { RootVariableName = "Font", Value = 3 };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("Font", new IVariableCategoryRow[] { sourceFont });
        VariableCategoryPasteResult result = service.Paste(new IVariableCategoryRow[] { targetFont });

        targetFont.Value.ShouldBe(3);
        result.SkippedVariableNames.ShouldBe(new[] { "Font" });
    }

    [Fact]
    public void Copy_ShouldExcludeIdentityVariablesAndNullValues()
    {
        FakeRow name = new FakeRow { RootVariableName = "Name", Value = "TitleText" };
        FakeRow baseType = new FakeRow { RootVariableName = "BaseType", Value = "Text" };
        FakeRow locked = new FakeRow { RootVariableName = "Locked", Value = true };
        FakeRow references = new FakeRow { RootVariableName = "VariableReferences", Value = new List<string>() };
        FakeRow unset = new FakeRow { RootVariableName = "MaxLettersToShow", Value = null };
        FakeRow fontSize = new FakeRow { RootVariableName = "FontSize", Value = 36 };

        VariableCategoryCopyPasteService service = CreateService();
        service.Copy("General", new IVariableCategoryRow[] { name, baseType, locked, references, unset, fontSize });

        service.CopiedCategory.ShouldNotBeNull();
        service.CopiedCategory!.CategoryName.ShouldBe("General");
        service.CopiedCategory.Values.Select(item => item.RootVariableName).ShouldBe(new[] { "FontSize" });
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
}
