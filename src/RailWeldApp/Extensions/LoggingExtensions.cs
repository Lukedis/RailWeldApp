using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace RailWeldApp.Extensions;

public static class LoggingExtensions
{
    private const string _fileNamePrefix = "railweld-";

    private const int _retainedDays = 30;

    private const string _outputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}";

    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        // 绝对路径
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");

        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                path: Path.Combine(logDirectory, _fileNamePrefix + ".log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: _retainedDays,
                shared: true,
                outputTemplate: _outputTemplate)
            .CreateLogger();

        builder.AddSerilog(Log.Logger, dispose: true);

        return builder;
    }
}