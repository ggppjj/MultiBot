using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MultiBot;

internal class LogController
{
    internal static ILogger SetupLogging(
        Type contextType,
        IConfigurationRoot? localApplicationConfig = null
    )
    {
        var logLevelFromConfig = localApplicationConfig?["LogLevel"];
        var logLevel = ParseLogLevel(logLevelFromConfig);
        var levelSwitch = new LoggingLevelSwitch(logLevel);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                "logs/multibot-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
        if (localApplicationConfig != null)
        {
            _ = ChangeToken.OnChange(
                localApplicationConfig.GetReloadToken,
                () =>
                {
                    var updatedLogLevelValue = localApplicationConfig["LogLevel"];

                    var updatedLogLevel = ParseLogLevel(updatedLogLevelValue);
                    if (levelSwitch.MinimumLevel != updatedLogLevel)
                    {
                        levelSwitch.MinimumLevel = updatedLogLevel;
                        Log.Information(
                            "Log level changed at runtime to: {Level}",
                            updatedLogLevel
                        );
                    }
                }
            );
        }
        return Log.Logger.ForContext(contextType);
    }

    private static LogEventLevel ParseLogLevel(string? value) =>
        Enum.TryParse<LogEventLevel>(value, true, out var level) && Enum.IsDefined(level)
            ? level
            : LogWarningAndReturnDefault(value);

    private static LogEventLevel LogWarningAndReturnDefault(string? value)
    {
        if (value != null)
            Log.Warning("Unknown log level '{Level}' in config, defaulting to Information", value);
        return LogEventLevel.Information;
    }
}
