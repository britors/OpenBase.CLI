using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBase.CLI.Helpers.Execution;

public static class DotNet
{
    private const string DotnetExecutable = "dotnet";

    public static readonly string[] TemplatePackages = PackageIds.Templates;

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

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(process.WaitForExitAsync(cancellationToken), stdoutTask, stderrTask);

        var stderr = (await stderrTask).Trim();
        var stdout = (await stdoutTask).Trim();

        var error = string.IsNullOrEmpty(stderr) ? stdout : stderr;
        return (process.ExitCode == 0, error);
    }

    public static async Task<int> RunLiveAsync(string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(GetDotnetPath(), arguments)
        {
            UseShellExecute = false,
        };

        using var process = Process.Start(psi);
        if (process == null) return 1;

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }

        return process.ExitCode;
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
            return "--";
        }

        return "--";
    }

    public static bool IsSdkVersionSufficient(int requiredMajor)
    {
        var versionString = GetDotnetVersion();
        return Version.TryParse(versionString, out var parsed) && parsed.Major >= requiredMajor;
    }

    public static async Task<string?> GetInstalledToolVersionAsync(string packageId, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(GetDotnetPath(), "tool list -g")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };

        using var process = Process.Start(psi);
        if (process == null) return null;

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return ParseToolVersion(output, packageId);
    }

    public static async Task<string?> GetInstalledTemplateVersionAsync(string packageId, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(GetDotnetPath(), "new uninstall")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };

        using var process = Process.Start(psi);
        if (process == null) return null;

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return ParseTemplateVersion(output, packageId);
    }

    public static string? ParseToolVersion(string output, string packageId)
    {
        foreach (var line in output.Split('\n'))
        {
            var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Equals(packageId, StringComparison.OrdinalIgnoreCase))
                return parts[1];
        }
        return null;
    }

    public static string? ParseTemplateVersion(string output, string packageId)
    {
        var lines = output.Split('\n');
        for (var i = 0; i < lines.Length - 1; i++)
        {
            if (!lines[i].Trim().Equals(packageId, StringComparison.OrdinalIgnoreCase))
                continue;

            for (var j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
            {
                var trimmed = lines[j].Trim();
                if (trimmed.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["Version:".Length..].Trim();
                if (trimmed.StartsWith("Versão:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["Versão:".Length..].Trim();
            }
        }
        return null;
    }
}