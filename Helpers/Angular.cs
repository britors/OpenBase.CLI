using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBase.CLI.Helpers;

public static class Angular
{
    public static string GetAngularVersion()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetAngularPath(),
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) return "--";

            var outputTask = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            var output = outputTask.Result;
            var lines = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault()?.Trim() ?? "--";
        }
        catch
        {
            return "--";
        }
    }

    private static string GetAngularPath()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var binaryName = isWindows ? "ng.cmd" : "ng";
        return ResolveBinaryPath(binaryName);
    }

    private static string ResolveBinaryPath(string binary)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        string[] windowsPaths =
        [
            @"C:\Program Files\nodejs",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm")
        ];

        string[] linuxPaths =
        [
            "/usr/bin",
            "/usr/local/bin",
            "/usr/share/npm/bin",
            "/usr/lib/node_modules/.bin",
            "/usr/local/lib/node_modules/.bin"
        ];

        List<string> searchPaths = [];

        if (!isWindows)
        {
            // NVM tem prioridade sobre caminhos fixos no Linux/macOS
            var nvmBin = Environment.GetEnvironmentVariable("NVM_BIN");
            if (!string.IsNullOrEmpty(nvmBin))
                searchPaths.Add(nvmBin);

            // também verifica PATH do sistema antes dos fixos
            var envPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(envPath))
                searchPaths.AddRange(envPath.Split(':'));

            searchPaths.AddRange(linuxPaths);
        }
        else
        {
            searchPaths.AddRange(windowsPaths);
        }

        foreach (var path in searchPaths)
        {
            var fullPath = Path.Combine(path, binary);
            if (File.Exists(fullPath)) return fullPath;
        }

        return binary;
    }
}