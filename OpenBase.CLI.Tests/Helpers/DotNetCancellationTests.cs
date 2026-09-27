using System.Diagnostics;
using System.Security;
using OpenBase.CLI.Helpers.Execution;

namespace OpenBase.CLI.Tests.Helpers;

public class DotNetCancellationTests
{
    [Fact]
    public async Task CancellationKillsMsbuildAndItsChildProcess()
    {
        if (OperatingSystem.IsWindows()) return; // POSIX child pid probe; command cancellation is also tested portably.
        var root = Path.Combine(Path.GetTempPath(), "openbase-process-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        var marker = Path.Combine(root, "child.pid");
        var project = Path.Combine(root, "wait.proj");
        File.WriteAllText(project, $"<Project><Target Name=\"Wait\"><Exec Command=\"{SecurityElement.Escape($"echo $$ > '{marker}'; exec sleep 60")}\" /></Target></Project>");
        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var pending = DotNet.RunAsync(new[] { "msbuild", project, "/t:Wait", "/nodeReuse:false", "/maxcpucount:1" }, source.Token);
        try
        {
            var limit = DateTime.UtcNow.AddSeconds(10);
            while (!File.Exists(marker) && !pending.IsCompleted && DateTime.UtcNow < limit) await Task.Delay(50);
            Assert.True(File.Exists(marker), "Child process did not start.");
            var pid = int.Parse(File.ReadAllText(marker).Trim());
            using var child = Process.GetProcessById(pid);
            Assert.False(child.HasExited);
            source.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            await Task.Delay(100);
            Assert.False(Directory.Exists($"/proc/{pid}"), "Child process survived cancellation.");
        }
        finally
        {
            source.Cancel();
            try { await pending; } catch (OperationCanceledException) { }
            Directory.Delete(root, true);
        }
    }
}
