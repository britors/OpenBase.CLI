using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenBase.CLI.Helpers.IO;

public sealed class BrowserLauncher : IBrowserLauncher
{
    public void Open(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
        catch { }
    }
}
