using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Extensions;
public static class IMessengerExtensions
{
    public static Task SendAsync<T>(this IMessenger messenger, T message)
        where T : AsyncRequestMessage
    {
        messenger.Send(message);
        return message.TaskCompletionSource.Task;
    }
}
