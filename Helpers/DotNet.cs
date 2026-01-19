using System.Runtime.InteropServices;

namespace OpenBaseNetSqlServerCLI.Helpers;

public static class DotNet
{
  public static string GetDotnetPath()
  {
    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    var fileName = isWindows ? "dotnet.exe" : "dotnet";

    // 1. Lista de caminhos fixos e seguros (Trusted Locations)
    string[] linux = ["/usr/bin", "/usr/local/bin", "/usr/share/dotnet"];
    string[] windows = [@"C:\Program Files\dotnet", @"C:\Program Files (x86)\dotnet"];
    var trustedPaths = isWindows
        ? windows
        : linux;

    foreach (var p in trustedPaths)
    {
      var fullPath = Path.Combine(p, fileName);
      if (File.Exists(fullPath)) return fullPath;
    }

    // 2. Se não achou nos caminhos fixos, busca no PATH (Fallback)
    var envPath = Environment.GetEnvironmentVariable("PATH")?
        .Split(isWindows ? ';' : ':')
        .Select(p => Path.Combine(p, fileName))
        .FirstOrDefault(File.Exists);

    return envPath ?? fileName;
  }
}