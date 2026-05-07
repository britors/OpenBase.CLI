using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public static class DotNet
{
    private const string DotnetExecutable = "dotnet";

    public static readonly string[] TemplatePackages =
    [
        "w3ti.OpenBaseNET.SQLServer.Template",
        "w3ti.OpenBaseNET.Postgres.Template",
    ];

    private static readonly string[] WindowsKnownPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), DotnetExecutable),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), DotnetExecutable),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", DotnetExecutable),
    ];

    private static readonly string[] MacOsKnownPaths =
    [
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "usr", "local", "share", DotnetExecutable),
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "opt", "homebrew", "bin"),
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "usr", "local", "bin"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"),
    ];

    private static readonly string[] LinuxKnownPaths =
    [
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "usr", "bin"),
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "usr", "local", "bin"),
        Path.Combine(Path.DirectorySeparatorChar.ToString(), "usr", "share", DotnetExecutable),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"),
    ];

    public static string GetDotnetPath()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var fileName = isWindows ? "dotnet.exe" : DotnetExecutable;

        string[] knownPaths;
        if (isWindows) knownPaths = WindowsKnownPaths;
        else if (isMacOs) knownPaths = MacOsKnownPaths;
        else knownPaths = LinuxKnownPaths;

        foreach (var p in knownPaths)
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

    public static async Task<(bool Success, string Error)> RunAsync(string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(GetDotnetPath(), arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return (false, "Não foi possível iniciar o processo dotnet.");

        var errorOutput = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode == 0, (await errorOutput).Trim());
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