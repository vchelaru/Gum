# Layout System Test Plan

## Summary

This document defines a comprehensive test plan for Gum's layout system, centered on `GraphicalUiElement`. The goal is to build a regression suite that covers all dimension unit types, position unit types, origin combinations, stacking behaviors, and layout propagation. Tests should catch regressions when layout logic is refactored or extended.

All tests use xUnit `[Fact]`, Shouldly assertions, and the `Feature_ShouldExpectedBehavior_WhenCondition` naming convention. Tests are organized by `#region` and alphabetized within each region.

## File Organization

Create a new file: `MonoGameGum.Tests/Runtimes/LayoutUnitTests.cs`

- `GraphicalUiElementTests.cs` is already large (~1800 lines) and covers animation, state application, parent/child relationships, anchor/dock helpers, and some layout tests.
- The new `LayoutUnitTests.cs` should extend `BaseTestClass` and focus exclusively on layout computation (absolute positions and sizes resulting from unit configurations).
- Existing layout tests in `GraphicalUiElementTests.cs` stay where they are; this plan does not propose moving them.

---

## Test Categories

### Region 1: Width Units - Absolute and Relative

Tests for `DimensionUnitType.Absolute`, `PercentageOfParent`, `RelativeToParent`, and `ScreenPixel` applied to Width.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 1 | `WidthAbsolute_ShouldReturnExactPixels` | P0 | Width=150, assert GetAbsoluteWidth()=150 |
| 2 | `WidthAbsolute_ShouldReturnZero_WhenSetToZero` | P1 | Edge case: zero width |
| 3 | `WidthAbsolute_ShouldAllowNegativeValues` | P2 | Verify negative width doesn't crash |
| 4 | `WidthPercentageOfParent_ShouldReturnHalfParentWidth_WhenFiftyPercent` | P0 | Parent=400, Width=50 |
| 5 | `WidthPercentageOfParent_ShouldReturnFullParentWidth_WhenHundredPercent` | P0 | Width=100 |
| 6 | `WidthPercentageOfParent_ShouldReturnZero_WhenParentWidthIsZero` | P1 | Edge case |
| 7 | `WidthRelativeToParent_ShouldMatchParent_WhenZero` | P0 | Width=0 means same as parent |
| 8 | `WidthRelativeToParent_ShouldBeSmallerThanParent_WhenNegative` | P0 | Width=-20, parent=200 -> 180 |
| 9 | `WidthRelativeToParent_ShouldBeLargerThanParent_WhenPositive` | P1 | Width=20, parent=200 -> 220 |
| 10 | `WidthScreenPixel_ShouldUseCanvasWidth` | P1 | ScreenPixel uses canvas, not parent |

### Region 2: Height Units - Absolute and Relative

Mirror of Region 1 for Height.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 11 | `HeightAbsolute_ShouldReturnExactPixels` | P0 | |
| 12 | `HeightAbsolute_ShouldReturnZero_WhenSetToZero` | P1 | |
| 13 | `HeightPercentageOfParent_ShouldReturnHalfParentHeight_WhenFiftyPercent` | P0 | |
| 14 | `HeightPercentageOfParent_ShouldReturnFullParentHeight_WhenHundredPercent` | P0 | |
| 15 | `HeightPercentageOfParent_ShouldReturnZero_WhenParentHeightIsZero` | P1 | |
| 16 | `HeightRelativeToParent_ShouldMatchParent_WhenZero` | P0 | |
| 17 | `HeightRelativeToParent_ShouldBeSmallerThanParent_WhenNegative` | P0 | |
| 18 | `HeightRelativeToParent_ShouldBeLargerThanParent_WhenPositive` | P1 | |
| 19 | `HeightScreenPixel_ShouldUseCanvasHeight` | P1 | |

### Region 3: RelativeToChildren Sizing

Tests for `DimensionUnitType.RelativeToChildren` on both Width and Height.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 20 | `WidthRelativeToChildren_ShouldMatchWidestChild` | P0 | Single child at X=0 with Width=100 |
| 21 | `WidthRelativeToChildren_ShouldIncludeChildXOffset` | P0 | Child at X=50, Width=100 -> parent=150 |
| 22 | `WidthRelativeToChildren_ShouldAddPaddingValue` | P0 | Width=20 (padding), child width=100 -> 120 |
| 23 | `WidthRelativeToChildren_ShouldIgnoreInvisibleChildren` | P1 | Invisible child should not contribute |
| 24 | `WidthRelativeToChildren_ShouldReturnPaddingOnly_WhenNoChildren` | P1 | No children, Width=10 -> 10 |
| 25 | `WidthRelativeToChildren_ShouldUpdateWhenChildResizes` | P0 | Change child width, parent should update |
| 26 | `WidthRelativeToChildren_ShouldUpdateWhenChildMoves` | P0 | Change child X, parent should update |
| 27 | `HeightRelativeToChildren_ShouldMatchTallestChild` | P0 | |
| 28 | `HeightRelativeToChildren_ShouldIncludeChildYOffset` | P0 | |
| 29 | `HeightRelativeToChildren_ShouldAddPaddingValue` | P0 | |
| 30 | `HeightRelativeToChildren_ShouldIgnoreInvisibleChildren` | P1 | |
| 31 | `HeightRelativeToChildren_ShouldReturnPaddingOnly_WhenNoChildren` | P1 | |
| 32 | `HeightRelativeToChildren_ShouldUpdateWhenChildResizes` | P0 | |

### Region 4: PercentageOfOtherDimension

Tests for `DimensionUnitType.PercentageOfOtherDimension`.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 33 | `WidthPercentOfOtherDimension_ShouldEqualHeight_WhenHundredPercent` | P0 | Width=100, Height=200 absolute -> Width becomes 200 |
| 34 | `WidthPercentOfOtherDimension_ShouldBeHalfHeight_WhenFiftyPercent` | P0 | Width=50 |
| 35 | `HeightPercentOfOtherDimension_ShouldEqualWidth_WhenHundredPercent` | P0 | |
| 36 | `HeightPercentOfOtherDimension_ShouldBeHalfWidth_WhenFiftyPercent` | P0 | |
| 37 | `WidthPercentOfOtherDimension_ShouldUpdate_WhenHeightChanges` | P1 | Verify recalculation |

### Region 5: AbsoluteMultipliedByFontScale

Tests for `DimensionUnitType.AbsoluteMultipliedByFontScale`.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 38 | `WidthAbsoluteMultipliedByFontScale_ShouldScaleByGlobalFontScale` | P0 | Width=100, scale=2 -> 200 |
| 39 | `WidthAbsoluteMultipliedByFontScale_ShouldReturnUnscaled_WhenScaleIsOne` | P1 | |
| 40 | `HeightAbsoluteMultipliedByFontScale_ShouldScaleByGlobalFontScale` | P0 | |
| 41 | `HeightAbsoluteMultipliedByFontScale_ShouldHandleFractionalScale` | P1 | Scale=1.5 |

### Region 6: Ratio Width and Height

Tests for `DimensionUnitType.Ratio`. Note: basic Ratio width test exists [EXISTS].

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 42 | `WidthUnits_Ratio_ShouldUseAvailableSpace` | **[EXISTS]** | In GraphicalUiElementTests |
| 43 | `WidthUnits_Ratio_ShouldRespectAbsoluteMultipliedByFontScale` | **[EXISTS]** | In GraphicalUiElementTests |
| 44 | `HeightRatio_ShouldDistributeRemainingSpace_AmongRatioSiblings` | P0 | Two children with ratio 1 and 2 |
| 45 | `HeightRatio_ShouldSubtractAbsoluteSiblings_BeforeDistributing` | P0 | One absolute child + one ratio child |
| 46 | `WidthRatio_ShouldDistributeEvenly_WhenMultipleSiblingsHaveEqualRatio` | P0 | Three children ratio=1 each |
| 47 | `WidthRatio_ShouldDistributeProportionally_WhenDifferentRatios` | P0 | Children with ratio 1, 2, 3 |
| 48 | `WidthRatio_ShouldReturnZero_WhenParentHasNoRemainingSpace` | P1 | Absolute siblings fill parent |
| 49 | `WidthRatio_ShouldIgnoreInvisibleRatioSiblings` | P1 | Invisible ratio child excluded from distribution |
| 50 | `HeightRatio_ShouldSubtractPercentageSiblings_BeforeDistributing` | P1 | Mix of percentage + ratio children |

### Region 7: X Position Units

Tests for `GeneralUnitType` applied to X positioning.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 51 | `XPixelsFromSmall_ShouldPositionFromLeftEdge` | P0 | X=30 from left |
| 52 | `XPixelsFromSmall_ShouldOffsetByXValue` | P0 | Parent at 100, child X=50 -> AbsoluteX=150 |
| 53 | `XPixelsFromLarge_ShouldPositionFromRightEdge` | P0 | X=0 from right edge |
| 54 | `XPixelsFromLarge_ShouldOffsetInward_WhenPositive` | P0 | X=10 inward from right |
| 55 | `XPixelsFromMiddle_ShouldPositionFromCenter` | P0 | X=0 at center |
| 56 | `XPixelsFromMiddle_ShouldOffsetFromCenter_WhenNonZero` | P0 | X=50 right of center |
| 57 | `XPercentage_ShouldPositionAsPercentOfParentWidth` | P0 | X=50 -> halfway |
| 58 | `XPercentage_ShouldReturnZero_WhenParentWidthIsZero` | P1 | Edge case |
| 59 | `XValues_ShouldUpdateLayoutImmediately` | **[EXISTS]** | In GraphicalUiElementTests |

### Region 8: Y Position Units

Tests for `GeneralUnitType` applied to Y positioning.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 60 | `YPixelsFromSmall_ShouldPositionFromTopEdge` | P0 | |
| 61 | `YPixelsFromSmall_ShouldOffsetByYValue` | P0 | |
| 62 | `YPixelsFromLarge_ShouldPositionFromBottomEdge` | P0 | |
| 63 | `YPixelsFromLarge_ShouldOffsetInward_WhenPositive` | P0 | |
| 64 | `YPixelsFromMiddle_ShouldPositionFromCenter` | P0 | |
| 65 | `YPixelsFromMiddle_ShouldOffsetFromCenter_WhenNonZero` | P0 | |
| 66 | `YPercentage_ShouldPositionAsPercentOfParentHeight` | P0 | |
| 67 | `YPercentage_ShouldReturnZero_WhenParentHeightIsZero` | P1 | |

### Region 9: X Origin

Tests for `XOrigin` (HorizontalAlignment) effect on absolute position.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 68 | `XOriginLeft_ShouldAlignLeftEdgeToPosition` | P0 | Default behavior |
| 69 | `XOriginCenter_ShouldAlignCenterToPosition` | P0 | Child 100 wide, XOrigin=Center, X=0 -> AbsoluteX=-50 |
| 70 | `XOriginRight_ShouldAlignRightEdgeToPosition` | P0 | |
| 71 | `XOriginCenter_WithPixelsFromMiddle_ShouldCenterInParent` | P0 | Common centering pattern |
| 72 | `XOriginRight_WithPixelsFromLarge_ShouldAlignToRightEdge` | P0 | Common right-align pattern |

### Region 10: Y Origin

Tests for `YOrigin` (VerticalAlignment) effect on absolute position.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 73 | `YOriginTop_ShouldAlignTopEdgeToPosition` | P0 | |
| 74 | `YOriginCenter_ShouldAlignCenterToPosition` | P0 | |
| 75 | `YOriginBottom_ShouldAlignBottomEdgeToPosition` | P0 | |
| 76 | `YOriginCenter_WithPixelsFromMiddle_ShouldCenterInParent` | P0 | |
| 77 | `YOriginBottom_WithPixelsFromLarge_ShouldAlignToBottomEdge` | P0 | |

### Region 11: Origin + Units Combinations

Combined tests for less obvious origin + unit interactions.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 78 | `XOriginRight_WithPercentage_ShouldOffsetByChildWidth` | P1 | |
| 79 | `YOriginBottom_WithPercentage_ShouldOffsetByChildHeight` | P1 | |
| 80 | `XOriginCenter_WithPixelsFromSmall_ShouldShiftLeftByHalfWidth` | P1 | |
| 81 | `YOriginCenter_WithPixelsFromSmall_ShouldShiftUpByHalfHeight` | P1 | |
| 82 | `XOriginLeft_WithPixelsFromLarge_ShouldPositionFromRightEdge` | P1 | Origin left but measured from right |

### Region 12: TopToBottomStack

Tests for `ChildrenLayout.TopToBottomStack`.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 83 | `TopToBottomStack_ShouldStackChildrenVertically` | P0 | Three children, verify Y positions |
| 84 | `TopToBottomStack_ShouldRespectStackSpacing` | P0 | StackSpacing=10 |
| 85 | `TopToBottomStack_ShouldSkipInvisibleChildren` | P0 | Invisible child should not take space |
| 86 | `TopToBottomStack_ShouldUpdatePositions_WhenChildHeightChanges` | P0 | |
| 87 | `TopToBottomStack_ShouldRespectChildXUnits` | P1 | X positioning should still work independently |
| 88 | `TopToBottomStack_ShouldIgnoreChildYUnits_ForSubsequentSiblings` | **[EXISTS]** | YUnits_ShouldBeIgnored_ForSubsequentStackedSiblings |
| 89 | `TopToBottomStack_ShouldPositionFirstChildAtZero` | P0 | |
| 90 | `TopToBottomStack_ShouldHandleMixedHeightUnits` | P1 | Absolute + percentage children in stack |
| 91 | `TopToBottomStack_ShouldHandleZeroHeightChildren` | P2 | |

### Region 13: LeftToRightStack

Tests for `ChildrenLayout.LeftToRightStack`.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 92 | `LeftToRightStack_ShouldStackChildrenHorizontally` | P0 | |
| 93 | `LeftToRightStack_ShouldRespectStackSpacing` | P0 | |
| 94 | `LeftToRightStack_ShouldSkipInvisibleChildren` | P0 | |
| 95 | `LeftToRightStack_ShouldUpdatePositions_WhenChildWidthChanges` | P0 | |
| 96 | `LeftToRightStack_ShouldRespectChildYUnits` | P1 | |
| 97 | `LeftToRightStack_ShouldPositionFirstChildAtZero` | P0 | |
| 98 | `LeftToRightStack_ShouldHandleMixedWidthUnits` | P1 | |

### Region 14: WrapsChildren

Tests for stack wrapping behavior.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 99 | `MaxHeight_ShouldNotWrapVerticalStack_UntilExceeded` | **[EXISTS]** | |
| 100 | `MaxHeight_ShouldWrapVerticalStack_IfExceeded` | **[EXISTS]** | |
| 101 | `MaxWidth_ShouldWrapHorizontalStack_IfExceeded` | **[EXISTS]** | |
| 102 | `WrapsChildren_TopToBottom_ShouldCreateNewColumn_WhenExceedingMaxHeight` | P0 | Verify X offset of wrapped column |
| 103 | `WrapsChildren_LeftToRight_ShouldCreateNewRow_WhenExceedingMaxWidth` | P0 | Verify Y offset of wrapped row |
| 104 | `WrapsChildren_ShouldPositionWrappedItems_AtCorrectOffsets` | P0 | Multiple wraps, verify all positions |
| 105 | `WrapsChildren_ShouldRespectStackSpacing_AcrossWrappedLines` | P1 | Spacing between wrapped rows/columns |
| 106 | `WrapsChildren_ShouldNotWrap_WhenMaxDimensionNotSet` | P1 | No MaxWidth/MaxHeight means no wrapping |
| 107 | `WrapsChildren_RelativeToChildren_ShouldSizeToWrappedContent` | P1 | Parent with RelativeToChildren sizing wrapping |

### Region 15: RelativeToChildren with Stacking

Tests for RelativeToChildren combined with stacking layouts.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 108 | `WidthRelativeToChildren_TopToBottomStack_ShouldUseWidestChild` | P0 | |
| 109 | `HeightRelativeToChildren_TopToBottomStack_ShouldSumChildHeights` | P0 | Sum of all child heights |
| 110 | `HeightRelativeToChildren_TopToBottomStack_ShouldIncludeStackSpacing` | P0 | N-1 gaps |
| 111 | `WidthRelativeToChildren_LeftToRightStack_ShouldSumChildWidths` | P0 | |
| 112 | `WidthRelativeToChildren_LeftToRightStack_ShouldIncludeStackSpacing` | P0 | |
| 113 | `HeightRelativeToChildren_LeftToRightStack_ShouldUseTallestChild` | P0 | |
| 114 | `HeightRelativeToChildren_ShouldUseChildrenHeight_AutoGrid` | **[EXISTS]** | |
| 115 | `WidthRelativeToChildren_ShouldUseChildrenWidth_AutoGrid` | **[EXISTS]** | |

### Region 16: Nested Layout Propagation

Tests for layout updates propagating through nested hierarchies.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 116 | `NestedPercentageOfParent_ShouldCascade_ThroughThreeLevels` | P0 | Grandparent=400, parent=50%, child=50% -> child=100 |
| 117 | `NestedRelativeToParent_ShouldCascade` | P0 | |
| 118 | `NestedRelativeToChildren_ShouldPropagateUpward` | P0 | Child resize -> parent resize -> grandparent resize |
| 119 | `ParentResize_ShouldUpdatePercentageChildren` | P0 | Change parent width, verify child recalculates |
| 120 | `ParentResize_ShouldUpdateRelativeToParentChildren` | P0 | |
| 121 | `ChildResize_ShouldUpdateRelativeToChildrenParent` | P0 | |
| 122 | `ChildMove_ShouldUpdateRelativeToChildrenParent` | P1 | |
| 123 | `GrandchildResize_ShouldPropagateToGrandparent_WhenBothRelativeToChildren` | P1 | Three-level RelativeToChildren chain |
| 124 | `NestedPercentageOfParent_ShouldHandleZeroParent` | P2 | Edge case: parent is 0 |

### Region 17: Position Updates and Absolute Coordinates

Tests for absolute coordinate computation with parent offsets.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 125 | `AbsoluteX_ShouldIncludeParentAbsoluteX` | P0 | Parent at X=100, child at X=50 -> AbsoluteX=150 |
| 126 | `AbsoluteY_ShouldIncludeParentAbsoluteY` | P0 | |
| 127 | `AbsoluteLeft_ShouldAccountForOrigin` | P0 | XOrigin=Center, Width=100 -> AbsoluteLeft = AbsoluteX - 50 |
| 128 | `AbsoluteTop_ShouldAccountForOrigin` | P0 | |
| 129 | `GetAbsoluteLeft_ShouldWorkWithPixelsFromLarge` | P1 | Right-aligned child |
| 130 | `GetAbsoluteTop_ShouldWorkWithPixelsFromLarge` | P1 | Bottom-aligned child |
| 131 | `AbsoluteCoordinates_ShouldUpdate_WhenParentMoves` | P0 | Move parent, verify child absolute positions change |

### Region 18: Layout Suspension

Tests for `SuspendLayout`, `ResumeLayout`, and `IsAllLayoutSuspended`.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 132 | `SuspendLayout_ShouldPreventLayoutUpdates` | P0 | Change child size during suspension, verify no update |
| 133 | `ResumeLayout_ShouldApplyPendingLayoutChanges` | P0 | After resuming, layout should reflect changes |
| 134 | `IsAllLayoutSuspended_ShouldPreventAllInstances` | P0 | Static flag |
| 135 | `IsAllLayoutSuspended_ShouldResumeCorrectly_WhenSetToFalse` | P0 | |
| 136 | `SuspendLayout_ShouldBeNestable` | P1 | Multiple suspends require multiple resumes |
| 137 | `ResumeLayout_ShouldNotCrash_WhenCalledWithoutSuspend` | P2 | |
| 138 | `ApplyState_ShouldSuspendLayout_ToReduceLayoutCallCount` | **[EXISTS]** | In GraphicalUiElementTests |

### Region 19: Stacking with Ratio Children

Tests for Ratio units inside stacked layouts.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 139 | `TopToBottomStack_HeightRatio_ShouldDistributeRemainingVerticalSpace` | P0 | |
| 140 | `LeftToRightStack_WidthRatio_ShouldDistributeRemainingHorizontalSpace` | P0 | |
| 141 | `TopToBottomStack_MixedAbsoluteAndRatio_ShouldCalculateCorrectly` | P0 | |
| 142 | `LeftToRightStack_MixedAbsoluteAndRatio_ShouldCalculateCorrectly` | P0 | |
| 143 | `TopToBottomStack_RatioChild_ShouldUpdateOnSiblingResize` | P1 | |

### Region 20: PercentageOfSourceFile and MaintainFileAspectRatio

These require a mock renderable with a SourceRectangle. Test patterns follow the mock setup in existing Ratio tests.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 144 | `WidthPercentageOfSourceFile_ShouldReturnSourceWidth_WhenHundredPercent` | P0 | Source 200x100, Width=100 -> 200 |
| 145 | `WidthPercentageOfSourceFile_ShouldReturnHalfSourceWidth_WhenFiftyPercent` | P0 | Width=50 -> 100 |
| 146 | `HeightPercentageOfSourceFile_ShouldReturnSourceHeight_WhenHundredPercent` | P0 | |
| 147 | `HeightPercentageOfSourceFile_ShouldReturnHalfSourceHeight_WhenFiftyPercent` | P0 | |
| 148 | `WidthMaintainFileAspectRatio_ShouldScaleProportionally_BasedOnHeight` | P0 | Source 200x100, Height set to 200 -> Width=400 |
| 149 | `HeightMaintainFileAspectRatio_ShouldScaleProportionally_BasedOnWidth` | P0 | |
| 150 | `MaintainFileAspectRatio_ShouldHandleSquareSource` | P1 | 100x100 source |

### Region 21: Regular (non-stacked) Children Layout

Tests for `ChildrenLayout.Regular` (default) ensuring children position independently.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 151 | `RegularLayout_ShouldNotStackChildren` | P0 | Two children, both at Y=0 |
| 152 | `RegularLayout_ShouldAllowOverlappingChildren` | P0 | Two children at same position |
| 153 | `RegularLayout_ShouldRespectIndividualXYUnits` | P0 | Each child has different units |

### Region 22: StackedRowOrColumnDimensions

Tests for per-row/column sizing in stacked layouts.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 154 | `StackedRowOrColumnDimensions_ShouldControlRowHeight_InWrappedTopToBottomStack` | P1 | |
| 155 | `StackedRowOrColumnDimensions_ShouldControlColumnWidth_InWrappedLeftToRightStack` | P1 | |

### Region 23: ClipsChildren

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 156 | `ClipsChildren_ShouldNotAffectLayoutCalculation` | P1 | Clipping is visual only, layout still computes normally |
| 157 | `ClipsChildren_RelativeToChildren_ShouldStillSizeToChildren_EvenWhenClipped` | P1 | |

### Region 24: Visible Affecting Layout

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 158 | `Visible_ShouldUpdateChildren_IfWidthUnitsRatio` | **[EXISTS]** | |
| 159 | `Visible_ShouldUpdateChildren_IfLeftToRightStack` | **[EXISTS]** | |
| 160 | `Visible_ShouldExcludeFromRelativeToChildren_WhenFalse` | P0 | |
| 161 | `Visible_ShouldExcludeFromStackPosition_WhenFalse` | P0 | TopToBottom stack, middle child invisible |
| 162 | `Visible_ShouldRecalculateRatioSiblings_WhenToggled` | P1 | |

### Region 25: Edge Cases and Stress

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 163 | `Layout_ShouldHandleZeroSizeParent_WithPercentageChildren` | P1 | |
| 164 | `Layout_ShouldHandleZeroSizeParent_WithRelativeToParentChildren` | P1 | |
| 165 | `Layout_ShouldHandleDeeplyNestedHierarchy` | P2 | 10+ levels deep |
| 166 | `Layout_ShouldHandleChildWithNoParent` | P2 | Orphan element |
| 167 | `Layout_ShouldNotCrash_WhenNegativeWidthResultsFromRelativeToParent` | P2 | |
| 168 | `Layout_ShouldNotCrash_WhenNegativeHeightResultsFromRelativeToParent` | P2 | |
| 169 | `Layout_ShouldHandleRapidParentReassignment` | P2 | Move child between parents |
| 170 | `Layout_ShouldRecalculate_WhenChildRemovedFromStack` | P0 | Remove middle child from stack |
| 171 | `Layout_ShouldRecalculate_WhenChildAddedToStack` | P0 | Add child to existing stack |

### Region 26: Multiple Dimension Units Interacting

Tests where children use different unit types within the same parent.

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 172 | `MixedWidthUnits_AbsoluteAndPercentage_ShouldCoexistInParent` | P0 | |
| 173 | `MixedWidthUnits_AbsoluteAndRatio_ShouldCalculateCorrectly` | P0 | Verify ratio gets remaining space |
| 174 | `MixedWidthUnits_PercentageAndRelativeToParent_ShouldCoexistInParent` | P1 | |
| 175 | `MixedHeightUnits_InTopToBottomStack_ShouldStackCorrectly` | P1 | Absolute, percentage, and ratio |

### Region 27: Canvas-Relative Behavior

Tests for elements without parents (positioned relative to canvas).

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 176 | `NoParent_PercentageOfParent_ShouldUseCanvasDimensions` | P1 | Width=50% of canvas |
| 177 | `NoParent_RelativeToParent_ShouldUseCanvasDimensions` | P1 | |
| 178 | `CanvasResize_ShouldUpdateRootLevelChildren` | P1 | Change CanvasWidth/Height, verify recalculation |

### Region 28: PixelsFromBaseline (Y only)

| # | Test Name | Priority | Notes |
|---|-----------|----------|-------|
| 179 | `YPixelsFromBaseline_ShouldPositionRelativeToTextBaseline` | P1 | Requires text element |

---

## Priority Summary

| Priority | Count | Description |
|----------|-------|-------------|
| P0 | ~85 | Core behavior - must pass for layout system to be reliable |
| P1 | ~55 | Important edge cases and combinations |
| P2 | ~10 | Nice-to-have stress tests and unusual scenarios |
| [EXISTS] | ~15 | Already implemented in existing test files |
| **Total** | ~165 | |

## Implementation Order

1. **Phase 1 (P0 core units)**: Regions 1-2, 7-8, 9-10 -- basic dimension and position units
2. **Phase 2 (P0 stacking + relative)**: Regions 3, 12-13, 15, 17 -- stacking and RelativeToChildren
3. **Phase 3 (P0 ratio + propagation)**: Regions 6, 16, 19, 25 (P0 items only) -- ratio distribution and nesting
4. **Phase 4 (P0 remaining)**: Regions 4, 5, 20-21, 24 (P0 items)
5. **Phase 5 (P1)**: All P1 tests across all regions
6. **Phase 6 (P2)**: All P2 tests

## Notes

- Tests requiring a source texture (PercentageOfSourceFile, MaintainFileAspectRatio) need a mock `IRenderable` with a `SourceRectangle`. Follow the mock pattern in the existing `WidthUnits_Ratio_ShouldUseAvailableSpace` test.
- `PixelsFromBaseline` may require a `TextRuntime` or similar element that exposes baseline information.
- The `BaseTestClass.Dispose` method resets `IsAllLayoutSuspended`, `CanvasWidth`, `CanvasHeight`, and `GlobalFontScale`, so tests in those areas can safely modify these statics.
- All tests should create their own parent/child hierarchies from scratch to avoid cross-test state leakage.
