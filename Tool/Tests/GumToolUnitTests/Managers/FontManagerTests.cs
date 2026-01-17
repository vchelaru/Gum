using Gum.Services.Fonts;
using Moq.AutoMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Managers;
public class FontManagerTests : BaseTestClass
{
    private readonly AutoMocker _mocker;

    private readonly FontManager _fontManager;

    public FontManagerTests()
    {
        _mocker = new AutoMocker();
        _fontManager = _mocker.CreateInstance<FontManager>();
    }
}
