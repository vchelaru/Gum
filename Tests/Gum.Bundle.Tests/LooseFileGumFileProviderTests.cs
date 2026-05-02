using System;
using System.Collections.Generic;
using System.IO;
using Gum.Bundle;

namespace Gum.Bundle.Tests;

public class LooseFileGumFileProviderTests : IGumFileProviderContractTests, IDisposable
{
    private readonly List<string> _tempDirectories;

    public LooseFileGumFileProviderTests()
    {
        _tempDirectories = new List<string>();
    }

    protected override IGumFileProvider CreateProvider(IReadOnlyDictionary<string, byte[]> files)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumBundleTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);

        foreach (KeyValuePair<string, byte[]> kvp in files)
        {
            string fullPath = Path.Combine(tempDir, kvp.Key.Replace('/', Path.DirectorySeparatorChar));
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(fullPath, kvp.Value);
        }

        return new LooseFileGumFileProvider(tempDir);
    }

    public void Dispose()
    {
        foreach (string dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
