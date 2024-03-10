using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Database.Dapper.Pgsql.LoggingExtensions;

internal static partial class PgsqlDatabaseLoggingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Retrying transient exception {exceptionType}, on attempt {retryCount} of {maxRetries}. Current delay is {delay}ms: {details}")]
    public static partial void LogTransientExceptions(this ILogger<PgsqlDatabase> logger, string exceptionType, int retryCount, int maxRetries, double delay, string details, Exception exception);
}