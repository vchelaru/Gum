using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers;
public interface INameVerifier
{
    bool IsCategoryNameValid(string name, IStateContainer categoryContainer, out string whyNotValid);
}
