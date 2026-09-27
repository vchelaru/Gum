using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

public class HeadlessWindowDriverTests
{
    [AvaloniaFact]
    public void ContentHandedBackToAKeptTemplate_MovesToTheNewPresenter_WhenTheTemplateIsRebuiltLater()
    {
        // The view's first host lets go of the content; the second keeps the ScrollViewer's
        // template, so the driver hands the old presenter its content back.
        ItemsControl items = new ItemsControl { ItemsSource = new[] { "a", "b" } };
        ScrollViewer scroll = new ScrollViewer { Content = items };
        Border view = new Border { Child = scroll };
        new HeadlessWindowDriver(view, width: 200, height: 200, framesFolderName: "GumDriverTests", contentOutlivesTest: true).Dispose();
        using HeadlessWindowDriver driver = new HeadlessWindowDriver(view, width: 200, height: 200, framesFolderName: "GumDriverTests", contentOutlivesTest: true);
        ContentPresenter handedBack = scroll.Presenter.ShouldNotBeNull();
        items.GetVisualParent().ShouldBe(handedBack);

        // A later layout rebuilds the template, as a theme change in a new test session does.
        scroll.Template = new FuncControlTemplate<ScrollViewer>((owner, scope) => new ScrollContentPresenter
        {
            Name = "PART_ContentPresenter",
            [!ContentPresenter.ContentProperty] = owner[!ContentControl.ContentProperty],
        }.RegisterInNameScope(scope));
        driver.Layout();

        scroll.Presenter.ShouldNotBeSameAs(handedBack);
        items.GetVisualParent().ShouldBe(scroll.Presenter);
        items.GetVisualRoot().ShouldBe(driver.Window);
    }
}
