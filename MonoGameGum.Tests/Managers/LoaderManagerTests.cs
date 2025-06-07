using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Managers;
public class LoaderManagerTests
{
    [Fact]
    public void AddDisposable_ShouldStoreDisposable()
    {
        // See ObjectFinderTests and StandardElementsManagerTests for discussion on why Self is needed here.
        // Do not change this unless you have tested all runtimes including FlatRedBall Gum in a real game, and
        // unless you really know what you're doing!
        LoaderManager loaderManager = LoaderManager.Self;

        loaderManager.AddDisposable("Example1", new ExampleDisposable());

        loaderManager.GetDisposable("Example1").ShouldNotBeNull();
    }



    class ExampleDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose()
        {
            IsDisposed = true;
        }
        public override string ToString()
        {
            return "ExampleDisposable";
        }


    }
}