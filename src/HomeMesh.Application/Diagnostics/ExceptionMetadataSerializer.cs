using System.Text.Json;

namespace HomeMesh.Application.Diagnostics;

public static class ExceptionMetadataSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static string Serialize(Exception exception, object? context = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return JsonSerializer.Serialize(new
        {
            exception = Map(exception),
            context
        }, JsonOptions);
    }

    public static string Summarize(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{exception.GetType().Name}: {exception.Message}";
    }

    private static ExceptionDetail Map(Exception exception)
    {
        return new ExceptionDetail(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message,
            exception.Source,
            exception.StackTrace,
            exception.InnerException is null ? null : Map(exception.InnerException));
    }

    private sealed record ExceptionDetail(
        string Type,
        string Message,
        string? Source,
        string? StackTrace,
        ExceptionDetail? InnerException);
}
