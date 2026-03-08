using Gum.Cli;

namespace Gum.Cli.Tests;

/// <summary>
/// Captures stdout, stderr, and exit code when running CLI commands via <see cref="Program.Main"/>.
/// </summary>
public class CliTestHelper
{
    public string StandardOutput { get; }
    public string StandardError { get; }
    public int ExitCode { get; }

    private CliTestHelper(string standardOutput, string standardError, int exitCode)
    {
        StandardOutput = standardOutput;
        StandardError = standardError;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Runs <see cref="Program.Main"/> with the given arguments, capturing all output.
    /// </summary>
    public static CliTestHelper Run(params string[] args)
    {
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        using StringWriter outWriter = new StringWriter();
        using StringWriter errWriter = new StringWriter();

        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            int exitCode = Program.Main(args);

            return new CliTestHelper(
                outWriter.ToString().Trim(),
                errWriter.ToString().Trim(),
                exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
