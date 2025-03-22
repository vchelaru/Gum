using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using System.Runtime.CompilerServices;

namespace AutomatedTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var options = new AppiumOptions
        {
            // todo- fill this in...
        };

        //options.App =
        //    "C:\\Users\\vchel\\Documents\\GitHub\\Gum\\Gum\\bin\\Debug\\Data\\Gum.exe";
        ////options.DeviceName = "WindowsPC";
        //options.PlatformName = "Windows";
        //options.AutomationName = "Windows";

        options.AddAdditionalCapability("app",
            @"C:\Users\vchel\Documents\GitHub\Gum\Gum\bin\Debug\Data\Gum.exe");

        //options.AddAdditionalCapability("processArguments",
        //    @"C:\Users\vchel\Documents\GitHub\Kimuzukash-chibi-kuto-urufu\CrankyChibiCthulu\Content\GumProject\GumProject.gumx");

        options.AddAdditionalCapability("appArguments",
            @"C:\Users\vchel\Documents\GitHub\Kimuzukash-chibi-kuto-urufu\CrankyChibiCthulu\Content\GumProject\GumProject.gumx");

        //    var processArguments = new Dictionary<string, object>
        //{
        //    { "args", new List<string> { "C:\\Users\\vchel\\Documents\\GitHub\\Kimuzukash-chibi-kuto-urufu\\CrankyChibiCthulu\\Content\\GumProject\\GumProject.gumx" } },
        //    //{ "env", new Dictionary<string, string> { { "MY_ENV_VAR", "test" } } }
        //};
        //    options.AddAdditionalCapability("processArguments", processArguments);
        var driver = new WindowsDriver<WindowsElement>(
            new Uri(" http://127.0.0.1:4723/"), 
            options);

        driver.FindElementByImage()

        //var element = driver.FindElementByAccessibilityId("OutputTab");
        //element.Click();
        var actions = new Actions(driver);

        var screensTreeNode = driver.FindElementByName("Screens");
        actions.DoubleClick(screensTreeNode).Perform();

        //screensTreeNode.Click();
        //screensTreeNode.Execute();

        await Task.Delay(5_000);

        driver.Close();
    }






}

public static class DriverExtensionMethods
{

}



