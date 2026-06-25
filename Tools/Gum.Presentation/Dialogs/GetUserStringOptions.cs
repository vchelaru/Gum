using System;

namespace Gum.Services.Dialogs;

public class GetUserStringOptions
{
    public Func<string?,string?>? Validator { get; set; }
    public string? InitialValue { get; set; }
    public bool PreSelect { get; set; }
}
