using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBase.CLI.Helpers;

public static class DotNet
{
    public static readonly string[] TemplatePackages =
    [
        "w3ti.OpenBaseNET.SQLServer.Template",
        "w3ti.OpenBaseNET.Postgres.Template",
    ];

    public static string GetDotnetPath()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var fileName = isWindows ? "dotnet.exe" : "dotnet";

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] trustedPaths = isWindows
            ? [@"C:\Program Files\dotnet", @"C:\Program Files (x86)\dotnet", Path.Combine(home, @"AppData\Local\Microsoft\dotnet")]
            : isMacOs
                ? ["/usr/local/share/dotnet", "/opt/homebrew/bin", "/usr/local/bin", Path.Combine(home, ".dotnet")]
                : ["/usr/bin", "/usr/local/bin", "/usr/share/dotnet", Path.Combine(home, ".dotnet")];

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