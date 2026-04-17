using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBase.CLI.Helpers;

public static class DotNet
{
    public static string GetDotnetPath()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName = isWindows ? "dotnet.exe" : "dotnet";

        string[] linux = ["/usr/bin", "/usr/local/bin", "/usr/share/dotnet"];
        string[] windows = [@"C:\Program Files\dotnet", @"C:\Program Files (x86)\dotnet"];
        var trustedPaths = isWindows ? windows : linux;

        foreach (var p in trustedPaths)
        {
            var fullPath = Path.Combine(p, fileName);
            if (File.Exists(fullPath)) return fullPath;
        }

        var envPath = Environment.GetEnvironmentVariable("PATH")?
            .Split(isWindows ? ';' : ':')
            .Select(p => Path.Combine(p, fileName))
            .FirstOrDefault(File.Exists);

        return envPath ?? fileName;
    }

    public static string GetDotnetVersion()
    {
        try
        {
            var psi = new ProcessStartInfo(GetDotnetPath(), "--version")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
        }
        catch
        {
            // Retornar versão desconhecida em caso de erro
        }

        return "--";
    }
}