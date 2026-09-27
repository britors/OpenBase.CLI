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

    public static Task<(bool Success, string Error)> RunAsync(string arguments, CancellationToken cancellationToken)
        => RunProcessAsync(new ProcessStartInfo(GetDotnetPath(), arguments), cancellationToken);

    public static Task<(bool Success, string Error)> RunAsync(IReadOnlyList<string> arguments,
        CancellationToken cancellationToken, string? standardInput = null)
    {
        var psi = new ProcessStartInfo(GetDotnetPath());
        foreach (var arg in arguments) psi.ArgumentList.Add(arg);
        return RunProcessAsync(psi, cancellationToken, standardInput);
    }

    private static async Task<(bool Success, string Error)> RunProcessAsync(ProcessStartInfo psi,
        CancellationToken token, string? standardInput = null)
    {
        token.ThrowIfCancellationRequested();
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = standardInput is not null;
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        using var process = Process.Start(psi);
        if (process is null) return (false, "Não foi possível iniciar dotnet.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            if (standardInput is not null)
            {
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), token);
                process.StandardInput.Close();
            }
            await process.WaitForExitAsync(token);
            await Task.WhenAll(stdout, stderr);
            return (process.ExitCode == 0, string.IsNullOrWhiteSpace(stderr.Result) ? stdout.Result.Trim() : stderr.Result.Trim());
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdout, stderr);
            throw;
        }
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
        var result = await RunAsync(new[] { "tool", "list", "-g" }, cancellationToken);
        return result.Success ? ParseToolVersion(result.Error, packageId) : null;
    }

    public static async Task<string?> GetInstalledTemplateVersionAsync(string packageId, CancellationToken cancellationToken)
    {
        var result = await RunAsync(new[] { "new", "uninstall" }, cancellationToken);
        return result.Success ? ParseTemplateVersion(result.Error, packageId) : null;
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
                if (trimmed.Length > 0 && lines[j].TakeWhile(char.IsWhiteSpace).Count() <= lines[i].TakeWhile(char.IsWhiteSpace).Count()) break;
                if (trimmed.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["Version:".Length..].Trim();
                if (trimmed.StartsWith("Versão:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["Versão:".Length..].Trim();
            }
        }
        return null;
    }
}