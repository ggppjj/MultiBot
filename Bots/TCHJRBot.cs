using MultiBot.Commands;
using MultiBot.Interfaces;
using MultiBot.Platforms;
using Serilog;

namespace MultiBot.Bots;

internal class TCHJRBot : IBot
{
    public string Name { get; } = "TCHJR";
    public List<IBotCommand> Commands { get; } = [];

    private readonly List<IBotPlatform> _platforms = [];
    private readonly ILogger _logger;

    public void OnCommand(string message) => Console.WriteLine("Event received: " + message);

    internal TCHJRBot()
    {
        _logger = LogController.SetupLogging(typeof(TCHJRBot));
        _logger.Information("Starting...");
        Commands.Add(new CinephileCommand());
        try
        {
            _platforms.Add(new DiscordPlatform(this));
            _logger.Information("Started.");
        }
        catch (InvalidDataException e)
        {
            _logger.Fatal(e.Message);
            throw;
        }
    }

    public async Task Shutdown()
    {
        _logger.Information("Shutting down...");
        var tasks = new List<Task>();
        foreach (var platform in _platforms)
            tasks.Add(platform.Shutdown());
        await Task.WhenAll(tasks);
        _logger.Information("Shutdown complete.");
    }
}
