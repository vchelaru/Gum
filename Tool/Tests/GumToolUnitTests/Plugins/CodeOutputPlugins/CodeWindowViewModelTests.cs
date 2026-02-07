using CodeOutputPlugin.Models;
using CodeOutputPlugin.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumToolUnitTests.Plugins.CodeOutputPlugins;

public class CodeWindowViewModelTests
{
    private readonly AutoMocker _mocker;
    private CodeWindowViewModel _viewModel;

    public CodeWindowViewModelTests()
    {
        _viewModel = _mocker.CreateInstance<CodeWindowViewModel>();

    }

}
