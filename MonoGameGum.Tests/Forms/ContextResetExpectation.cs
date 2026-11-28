namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The value a bound target shows after its BindingContext becomes null or its binding is cleared (#8543): the
/// value it held before it was first bound. Tests read it from here so the rule is stated once.
/// </summary>
internal static class ContextResetExpectation
{
    public static T Of<T>(T unboundValue) => unboundValue;
}
