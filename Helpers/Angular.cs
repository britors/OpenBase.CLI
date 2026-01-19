using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBaseSqlServerCLI.Helpers;

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

      string output = process.StandardOutput.ReadToEnd();
      process.WaitForExit();

      var lines = output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
      return lines.FirstOrDefault()?.Trim() ?? "--";
    }
    catch
    {
      return "--";
    }
  }

  public static string GetAngularPath()
  {
    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    var binaryName = isWindows ? "ng.cmd" : "ng";
    return ResolveBinaryPath(binaryName);
  }

  private static string ResolveBinaryPath(string binary)
  {
    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    string[] windows = [@"C:\Program Files\nodejs", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm")];
    string[] linux = ["/usr/bin",
        "/usr/local/bin",
        "/usr/share/npm/bin",
        "/usr/lib/node_modules/npm/bin",
        "/usr/local/lib/node_modules/npm/bin",
        "/usr/bin/npm",
        "/usr/local/bin/npm"
        ];

    string[] paths;
    if (isWindows)
    {

      paths = windows;
      foreach (var path in paths)
      {
        var fullPath = Path.Combine(path, binary);
        if (File.Exists(fullPath)) return fullPath;
      }

    }
    else
    {
      var nvmBin = Environment.GetEnvironmentVariable("NVM_BIN");

      paths = [];
      if (!string.IsNullOrEmpty(nvmBin))
        paths = [.. paths, nvmBin];


      paths = [.. paths, .. linux];

    }

    foreach (var path in paths)
    {
      var fullPath = Path.Combine(path, binary);
      if (File.Exists(fullPath)) return fullPath;
    }

    return binary;
  }
}