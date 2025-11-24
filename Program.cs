using Microsoft.Extensions.Configuration;
using MultiBot.Bots;
using MultiBot.Helper_Classes;
using MultiBot.Interfaces;
using Serilog;

_ = args;

List<IBot> bots = [];

CancellationTokenSource? shutdownCts = new();
var shutdownCompleted = false;

var localApplicationConfig = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("config.json", optional: true, reloadOnChange: true)
    .Build();

var logController = new LogController();
var logger = LogController.SetupLogging(typeof(Program), localApplicationConfig);

logger.Information("Starting...");

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    logger.Information("Received shutdown signal...");
    _ = Task.Run(async () => await Shutdown());
};
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    logger.Information("Application process exiting...");
    _ = Task.Run(async () => await Shutdown());
};

try
{
    bots.Add(new TCHJRBot());
    logger.Information("Started.");
    Task.Delay(Timeout.Infinite, shutdownCts.Token).Wait();
}
catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { }
catch (Exception)
{
    logger.Fatal("Fatal error occurred during application execution.");
}
finally
{
    if (!shutdownCompleted)
        await Shutdown();
}

async Task Shutdown()
{
    if (!shutdownCompleted)
    {
        logger.Information("Starting shutdown process...");
        var tasks = new List<Task>();
        foreach (var bot in bots)
            tasks.Add(bot.Shutdown());
        await Task.WhenAll(tasks);
        shutdownCompleted = true;
        shutdownCts?.Cancel();
        logger.Information("Application shutdown completed.");
        Log.CloseAndFlush();
    }
}
