using System.Collections.Generic;
using System.IO;
using Gum.Bundle;

namespace Gum.Bundle.Tests;

public class BundleGumFileProviderTests : IGumFileProviderContractTests
{
    protected override IGumFileProvider CreateProvider(IReadOnlyDictionary<string, byte[]> files)
    {
        List<(string, byte[])> entries = new List<(string, byte[])>();
        foreach (KeyValuePair<string, byte[]> kvp in files)
        {
            entries.Add((kvp.Key, kvp.Value));
        }

        MemoryStream buffer = new MemoryStream();
        GumBundleWriter.Write(buffer, entries);
        buffer.Position = 0;
        GumBundle bundle = GumBundleReader.Read(buffer);
        return new BundleGumFileProvider(bundle);
    }
}
