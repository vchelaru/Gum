using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Narrow headless port <see cref="CopyPasteLogic"/> uses to create new screens/components when
/// pasting or promoting an instance into a component, so the copy/paste logic no longer needs to
/// depend on the concrete, WinForms-entangled <c>ProjectCommands</c> — whose wider surface covers
/// the whole project mutation API — for these three calls (ADR-0005 Phase 3). The live
/// implementation is <c>ProjectCommands</c>, bridged via DI to the same singleton.
/// </summary>
public interface ICopyPasteProjectCommands
{
    void AddScreen(ScreenSave screenSave);

    void AddComponent(ComponentSave componentSave);

    void PrepareNewComponentSave(ComponentSave componentSave, string componentName, string baseType = "Container");
}
