using System;
using System.Collections.Generic;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ProjectServices;

/// <summary>
/// <see cref="IHeadlessErrorChecker.GetProjectErrors"/> in two halves. <see cref="PrepareNext"/> reads the
/// project a step at a time on the thread that owns it; <see cref="Check"/> then checks the disk using
/// only strings, so it may run on another thread, and returns the rows <c>GetProjectErrors</c> returns
/// for the project as it was while it was prepared.
/// </summary>
public sealed class ProjectErrorCheck
{
    private readonly HeadlessErrorChecker _checker;
    private readonly GumProjectSave _project;
    private readonly FontReferenceCache _fontReferences;
    private readonly IFileNameCaseChecker _caseChecker;
    private readonly IReadOnlyList<ElementSave> _elements;
    private readonly List<PreparedElement> _preparedElements = new List<PreparedElement>();
    private HeadlessErrorChecker.PreparedProjectFiles? _projectFiles;
    private readonly string? _projectFullFileName;
    private List<ErrorResult>? _rowsBeforeBehaviors;
    private List<ErrorResult>? _rowsAfterBehaviors;
    private List<(string Name, string RelativePath)>? _missingBehaviors;
    private int _next;

    internal ProjectErrorCheck(HeadlessErrorChecker checker, GumProjectSave project, FontReferenceCache fontReferences,
        IFileNameCaseChecker caseChecker)
    {
        _checker = checker;
        _project = project;
        _fontReferences = fontReferences;
        _caseChecker = caseChecker;
        _elements = new List<ElementSave>(project.AllElements);
        _projectFullFileName = project.FullFileName;
    }

    /// <summary>The project this check reads.</summary>
    public GumProjectSave Project => _project;

    /// <summary>True once every step has read the project, so <see cref="Check"/> may run.</summary>
    public bool IsPrepared { get; private set; }

    /// <summary>Reads the next element, or after the last one the project-wide parts.</summary>
    public void PrepareNext()
    {
        if (IsPrepared)
        {
            return;
        }

        using IDisposable? finderScope = _checker.UseOwnFinderFor(_project);
        ObjectFinder.Self.EnableCache();
        try
        {
            if (_next < _elements.Count)
            {
                ElementSave element = _elements[_next++];
                _preparedElements.Add(new PreparedElement(element.Name, HeadlessErrorChecker.GetExpectedRelativePath(element, _project),
                    HeadlessErrorChecker.PrepareExternalFiles(element, _project, _fontReferences)));
                return;
            }

            _projectFiles = HeadlessErrorChecker.PrepareProjectFiles(_project, _fontReferences);
            _rowsBeforeBehaviors = new List<ErrorResult>();
            _rowsBeforeBehaviors.AddRange(_checker.GetFontRangeEntryErrorsFor(_project));
            _rowsBeforeBehaviors.AddRange(_checker.GetMissingComponentBaseTypeErrorsFor(_project));
            _missingBehaviors = HeadlessErrorChecker.GetMissingBehaviorFiles(_project);
            _rowsAfterBehaviors = new List<ErrorResult>();
            _rowsAfterBehaviors.AddRange(_checker.GetDuplicateReferenceErrorsFor(_project));
            _rowsAfterBehaviors.AddRange(_checker.GetOrphanCodeFileErrorsFor(_project));
            IsPrepared = true;
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }
    }

    /// <summary>
    /// The project's rows, in <c>GetProjectErrors</c>'s order. Reads only what <see cref="PrepareNext"/>
    /// kept, plus the disk.
    /// </summary>
    public IReadOnlyList<ErrorResult> Check()
    {
        if (!IsPrepared)
        {
            throw new InvalidOperationException("The check has not finished reading the project.");
        }

        // The element checks own the GUM0008 rows for element files and the files elements
        // reference; running only those two checks finds the paths to skip.
        HashSet<string> caseMismatchPaths = new HashSet<string>(StringComparer.Ordinal);
        string? fullFileName = _projectFullFileName;
        foreach (PreparedElement element in _preparedElements)
        {
            HeadlessErrorChecker.TryGetCaseMismatchError(element.Name, fullFileName, _caseChecker, element.Name,
                element.ExpectedRelativePath, caseMismatchPaths, out _);
            if (element.ExternalFiles != null)
            {
                _checker.CheckExternalFiles(element.Name, fullFileName!, element.ExternalFiles.Run(), caseMismatchPaths, _caseChecker);
            }
        }

        List<ErrorResult> errors = _projectFiles == null
            ? new List<ErrorResult>()
            : HeadlessErrorChecker.CheckProjectFiles(_projectFiles, caseMismatchPaths, _caseChecker);
        // Same order as GetProjectErrors: font ranges and base types, then behaviors, duplicates and orphans.
        errors.AddRange(_rowsBeforeBehaviors!);
        errors.AddRange(HeadlessErrorChecker.GetMissingBehaviorFileErrors(_missingBehaviors!, caseMismatchPaths));
        errors.AddRange(_rowsAfterBehaviors!);
        return errors;
    }

    private sealed class PreparedElement
    {
        public PreparedElement(string name, string expectedRelativePath, PreparedWalk? externalFiles)
        {
            Name = name;
            ExpectedRelativePath = expectedRelativePath;
            ExternalFiles = externalFiles;
        }

        public string Name { get; }
        public string ExpectedRelativePath { get; }
        public PreparedWalk? ExternalFiles { get; }
    }
}
