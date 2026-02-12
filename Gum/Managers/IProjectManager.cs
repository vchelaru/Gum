using Gum.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers;

public interface IProjectManager
{
    GeneralSettingsFile GeneralSettingsFile { get; }
    void SaveProject();
}
