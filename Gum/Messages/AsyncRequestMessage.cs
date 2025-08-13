using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Messages;
public class AsyncRequestMessage
{
    // .NET 6+ has non-generic TCS, but we're still on 4
    public TaskCompletionSource<bool> TaskCompletionSource { get; set; }

    public AsyncRequestMessage()
    {
        TaskCompletionSource = new TaskCompletionSource<bool>();
    }
}