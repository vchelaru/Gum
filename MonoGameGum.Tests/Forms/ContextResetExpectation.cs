namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The value a bound target shows after its BindingContext becomes null (#8543): the value it held before it
/// was bound. Tests read it from here so the rule is stated once.
/// </summary>
internal static class ContextResetExpectation
{
    public static T Of<T>(T unboundValue) => unboundValue;
}
