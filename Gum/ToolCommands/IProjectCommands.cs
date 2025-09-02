using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.ToolCommands
{
    public interface IProjectCommands
    {
        ScreenSave AddScreen(string screenName);
        void AddScreen(ScreenSave screenSave);
        
        void RemoveElement(ElementSave element);
        
        void RemoveBehavior(BehaviorSave behavior);
        
        void AddComponent(ComponentSave componentSave);
        void PrepareNewComponentSave(ComponentSave componentSave, string componentName);
    }
}