using System;
using System.Collections.Generic;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public interface IVisible
    {
        bool Visible
        {
            get;
            set;
        }

        bool AbsoluteVisible
        {
            get;
        }

        IVisible Parent
        {
            get;
        }
    }
}