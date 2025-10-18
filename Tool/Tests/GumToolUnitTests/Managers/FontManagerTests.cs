using Gum.Managers;
using Moq.AutoMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Managers;
public class FontManagerTests : BaseTestClass
{
    private readonly AutoMocker mocker;

    private readonly FontManager _fontManager;

    public FontManagerTests()
    {
        mocker = new AutoMocker();
        _fontManager = mocker.CreateInstance<FontManager>();
    }
}
