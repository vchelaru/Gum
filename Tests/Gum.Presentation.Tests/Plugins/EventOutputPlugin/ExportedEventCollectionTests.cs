using EventOutputPlugin.Models;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests.Plugins.EventOutput;

public class ExportedEventCollectionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"UserEvents\":null}")]
    [InlineData("{\"UserEvents\":{\"user\":null}}")]
    public void FromJson_IncompleteFile_ReturnsUsableCollection(string json)
    {
        ExportedEventCollection collection = ExportedEventCollection.FromJson(json);

        collection.UserEvents.ShouldNotBeNull();
        foreach (List<ExportedEvent> list in collection.UserEvents.Values)
        {
            list.ShouldNotBeNull();
        }
    }
}
