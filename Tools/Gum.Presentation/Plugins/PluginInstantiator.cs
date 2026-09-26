using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;

namespace Gum.Plugins;

/// <summary>
/// Creates one instance of each plugin part in a catalog, one part at a time, so a plugin that
/// cannot be created (its constructor throws, or it imports a service this head does not export)
/// is reported and skipped instead of failing the whole composition and every plugin with it.
/// A third-party plugin built against an older or other head of the tool is the usual case.
/// </summary>
internal class PluginInstantiator
{
    private static readonly string PluginContractName = AttributedModelServices.GetContractName(typeof(PluginBase));

    private readonly IOutputManager _outputManager;

    public PluginInstantiator(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    /// <summary>
    /// Returns the plugins exported by <paramref name="catalog"/>, in catalog order, with their
    /// imports satisfied from <paramref name="container"/>.
    /// </summary>
    public List<PluginBase> CreatePlugins(CompositionContainer container, ComposablePartCatalog catalog)
    {
        List<PluginBase> plugins = new List<PluginBase>();

        foreach (ComposablePartDefinition definition in catalog.Parts)
        {
            ExportDefinition? pluginExport = definition.ExportDefinitions
                .FirstOrDefault(export => export.ContractName == PluginContractName);
            if (pluginExport == null)
            {
                continue;
            }

            string? missingContract = FindMissingImport(container, definition);
            if (missingContract != null)
            {
                _outputManager.AddError($"Plugin {DescribePart(definition)} was not loaded: it needs {missingContract}, which this version of Gum does not provide.");
                continue;
            }

            try
            {
                ComposablePart part = definition.CreatePart();
                container.SatisfyImportsOnce(part);
                if (part.GetExportedValue(pluginExport) is PluginBase plugin)
                {
                    plugins.Add(plugin);
                }
            }
            catch (Exception exception)
            {
                ReportFailure(definition, exception);
            }
        }

        return plugins;
    }

    /// <summary>The contract of the first required import nothing in the container exports, or null.</summary>
    private static string? FindMissingImport(CompositionContainer container, ComposablePartDefinition definition)
    {
        foreach (ImportDefinition import in definition.ImportDefinitions)
        {
            try
            {
                container.GetExports(import);
            }
            catch (ImportCardinalityMismatchException)
            {
                return import.ContractName;
            }
        }
        return null;
    }

    private void ReportFailure(ComposablePartDefinition definition, Exception exception)
    {
        // MEF's message already carries the root cause and the part chain; its stack trace is noise.
        string details = exception is CompositionException ? exception.Message : exception.ToString();
        _outputManager.AddError($"Plugin {DescribePart(definition)} was not loaded: it failed while it was being created.\n{details}");
    }

    private static string DescribePart(ComposablePartDefinition definition)
    {
        try
        {
            Type type = ReflectionModelServices.GetPartType(definition).Value;
            return $"'{type.FullName}' ({type.Assembly.GetName().Name})";
        }
        catch (Exception)
        {
            // A type that cannot even be loaded; MEF's own description is the best there is.
            return $"'{definition}'";
        }
    }
}
