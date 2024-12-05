using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Services;

internal class Builder
{
    public static IHost App { get; private set; }

    public void Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<IEditVariableService, EditVariableService>();
        builder.Services.AddSingleton<IExposeVariableService, ExposeVariableService>();

        App = builder.Build();
    }
}
