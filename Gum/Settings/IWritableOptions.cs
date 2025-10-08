using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Settings;

public interface IWritableOptions<out T> : IOptionsMonitor<T> where T : class, new()
{
    /// <summary>
    /// Apply changes and persist them to disk, then reload configuration.
    /// </summary>
    void Update(Action<T> applyChanges);
}