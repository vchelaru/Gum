using System;

namespace Gum.Services;

public interface IDispatcher
{
    void Invoke(Action action);
    void Post(Action action);
}