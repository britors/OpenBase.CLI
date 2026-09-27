using System.Text.Json;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Creation;

public sealed class CliException(string code, string message, int exitCode = 2) : Exception(message)
{
    public string Code { get; } = code;
    public int ExitCode { get; } = exitCode;
}

public sealed record Notice(string Code, string Message);

public static class CommandResult
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Write(IAnsiConsole console, bool json, string command, object? data = null,
        CliException? error = null, IReadOnlyList<Notice>? warnings = null)
    {
        warnings ??= [];
        if (json)
            console.Profile.Out.Writer.WriteLine(JsonSerializer.Serialize(new
            {
                protocolVersion = 1, command, ok = error is null, data = error is null ? data : null,
                error = error is null ? null : new { code = error.Code, message = error.Message }, warnings
            }, Options));
        else
        {
            foreach (var warning in warnings) console.WriteLine($"{warning.Code}: {warning.Message}");
            console.WriteLine(error is null ? JsonSerializer.Serialize(data, Options) : $"{error.Code}: {error.Message}");
        }
        return error?.ExitCode ?? 0;
    }
}
