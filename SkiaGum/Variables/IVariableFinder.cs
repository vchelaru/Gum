namespace GumDataTypes.Variables
{
    public interface IVariableFinder
    {
        T GetValue<T>(string variableName);
    }
}
