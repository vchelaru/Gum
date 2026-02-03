using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XnaAndWinforms;

public class RenderingError
{
    public string Message
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// Indicates whether the device simply needs a reset, which is something we can
    /// recover from rather easily.
    /// </summary>
    public bool GraphicsDeviceNeedsReset
    {
        get;
        set;
    }

    /// <summary>
    ///  Indicates whether the device has been lost, which means a new graphics device is needed.
    /// </summary>
    public bool GraphicsDeviceLost
    {
        get;
        set;
    }

    /// <summary>
    ///  Indicates that we previously tried to reset the graphics device and it failed to reset
    /// </summary>
    public bool GraphicsDeviceResetFailed
    {
        get;
        set;
    }

    public bool HasErrors
    {
        get
        {
            return !string.IsNullOrEmpty(Message);
        }
    }

    public string ProcessedMessage 
    { 
        get
        {
            if(GraphicsDeviceLost)
            {
                return "The graphics device has been lost.  This means you must restart the app.";
            }
            else
            {
                return this.Message;
            }
        }
    }
}
