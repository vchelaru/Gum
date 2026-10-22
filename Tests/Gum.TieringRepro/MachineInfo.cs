using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Gum.TieringRepro;

/// <summary>Describes the machine the numbers came from.</summary>
internal static class MachineInfo
{
    public static string CpuModel()
    {
        string? model = null;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                model = key?.GetValue("ProcessorNameString") as string;
            }
            else if (OperatingSystem.IsLinux())
            {
                foreach (string line in File.ReadLines("/proc/cpuinfo"))
                {
                    if (line.StartsWith("model name", StringComparison.Ordinal))
                    {
                        model = line.Substring(line.IndexOf(':') + 1);
                        break;
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                model = Capture("sysctl", "-n machdep.cpu.brand_string");
            }
        }
        catch (Exception)
        {
            // Falls through to the environment fallback.
        }

        model = model?.Trim();
        if (string.IsNullOrEmpty(model))
        {
            model = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        }
        return string.IsNullOrWhiteSpace(model) ? "unknown" : model.Trim();
    }

    public static string Os() => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

    public static string Runtime() => RuntimeInformation.FrameworkDescription;

    public static string Sdk(string dotnetHost)
    {
        try
        {
            return Capture(dotnetHost, "--version")?.Trim() ?? "unknown";
        }
        catch (Exception)
        {
            return "unknown";
        }
    }

    public static string Vectors()
    {
        return $"AVX {YesNo(Avx.IsSupported)}, AVX2 {YesNo(Avx2.IsSupported)}, AVX-512F {YesNo(Avx512F.IsSupported)}; " +
            $"JIT vectors: 256-bit {YesNo(Vector256.IsHardwareAccelerated)}, 512-bit {YesNo(Vector512.IsHardwareAccelerated)}";
    }

    private static string YesNo(bool value) => value ? "yes" : "no";

    private static string? Capture(string file, string arguments)
    {
        ProcessStartInfo info = new ProcessStartInfo(file, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using Process? process = Process.Start(info);
        if (process == null)
        {
            return null;
        }
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        return output;
    }
}
