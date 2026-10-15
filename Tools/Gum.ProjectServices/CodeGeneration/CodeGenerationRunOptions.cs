using Gum.DataTypes;
using System;
using System.Collections.Generic;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Generates the code files for one element.
/// </summary>
/// <param name="checkForMissing">True when referenced elements that have no code files yet should be generated too.</param>
/// <returns>True if the element's code was generated.</returns>
public delegate bool ElementCodeGenerator(
    ElementSave element, CodeOutputElementSettings elementSettings, bool checkForMissing);

/// <summary>
/// What <see cref="IHeadlessCodeGenerationService.GenerateCodeForElements"/> generates, and how.
/// </summary>
public class CodeGenerationRunOptions
{
    /// <summary>
    /// The elements to generate. Null means every Screen and Component in the project, which is also
    /// what makes the run write the project-wide Standard Elements fallback file.
    /// </summary>
    public IReadOnlyList<ElementSave>? Elements { get; init; }

    /// <summary>
    /// Runs before each element that isn't set to never generate. Return false to skip the element
    /// (the gate reports why); skipped elements count toward <see cref="CodeGenerationRunResult.BlockedCount"/>.
    /// </summary>
    public Func<ElementSave, bool>? Gate { get; init; }

    /// <summary>
    /// Replaces the headless per-element generator, for callers (the tool) whose per-element step
    /// differs. Null uses <see cref="IHeadlessCodeGenerationService.GenerateCodeForElement"/>.
    /// </summary>
    public ElementCodeGenerator? ElementGenerator { get; init; }
}

/// <summary>
/// The outcome of <see cref="IHeadlessCodeGenerationService.GenerateCodeForElements"/>.
/// </summary>
public class CodeGenerationRunResult
{
    /// <summary>The number of elements whose code was generated.</summary>
    public int GeneratedCount { get; init; }

    /// <summary>The number of elements the <see cref="CodeGenerationRunOptions.Gate"/> skipped.</summary>
    public int BlockedCount { get; init; }
}
