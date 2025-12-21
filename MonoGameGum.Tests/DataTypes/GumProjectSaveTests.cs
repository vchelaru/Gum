using Gum.DataTypes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class GumProjectSaveTests : BaseTestClass
{
    [Fact]
    public void Load_ShouldUseFileManagerCustomGetStreamFromFile_IfSet()
    {
        bool wasCalled = false;

        GumProjectSave gumProject = new GumProjectSave();
        FileManager.XmlSerialize(gumProject, out string xml);

        FileManager.CustomGetStreamFromFile = (filePath) =>
        {
            wasCalled = true;
            return new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        };
        string fakeFilePath = "fakeFilePath.gumx";
        Gum.DataTypes.GumProjectSave.Load(fakeFilePath);
        wasCalled.ShouldBeTrue();
    }
}
