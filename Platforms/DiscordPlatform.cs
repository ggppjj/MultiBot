using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using MultiBot.Interfaces;
using Newtonsoft.Json;
using Serilog;

namespace MultiBot.Platforms;

public delegate void CommandEventHandler(object? sender, EventArgs? e);

internal class DiscordPlatform : IBotPlatform
{
    public string Name { get; } = "Discord";
    public IBot Bot { get; }
    public List<IBotCommand> Commands { get; } = [];

    private readonly string _tokenFilePath = Path.Combine(
        AppContext.BaseDirectory,
        "DiscordTokens.json"
    );
    private readonly IConfiguration _tokenConfig;
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger _logger;
    private readonly DiscordSocketConfig _discordClientConfig = new()
    {
        LogLevel = LogSeverity.Info,
    };
    private readonly string? _token;

    public event CommandEventHandler? OnCommand;

    internal DiscordPlatform(IBot bot)
    {
        Bot = bot;
        _logger = LogController.SetupLogging(typeof(DiscordPlatform));
        _logger.Information($"Starting for {Bot.Name}...");

        if (!File.Exists(_tokenFilePath))
        {
            var template = new Dictionary<string, string>
            {
                { bot.Name, "YOUR_DISCORD_BOT_TOKEN_HERE" },
            };
            File.WriteAllText(
                _tokenFilePath,
                JsonConvert.SerializeObject(template, Formatting.Indented)
            );
            _logger.Warning(
                $"Created {_tokenFilePath} with a placeholder token. Please update it."
            );
        }

        _tokenConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("DiscordTokens.json", optional: true, reloadOnChange: true)
            .Build();

        _token = _tokenConfig[Bot.Name];
        if (string.IsNullOrWhiteSpace(_token) || _token == "YOUR_DISCORD_BOT_TOKEN_HERE")
        {
            _logger.Fatal("Missing bot token in DiscordTokens.json file");
            throw new InvalidDataException("Missing bot token!");
        }
        _discordClient = new(_discordClientConfig);
        _discordClient.Log += LogDiscordMessageToSerilog;
        _discordClient.SlashCommandExecuted += SlashCommandHandler;
        _discordClient.Ready += OnClientReady;
        LoadCommands();
        StartAsync().GetAwaiter().GetResult();
        _logger.Information($"Started for {Bot.Name}.");
    }

    internal async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            _logger.Fatal("Missing bot token in DiscordTokens.json file");
            throw new InvalidDataException("Missing bot token!");
        }
        await _discordClient.LoginAsync(TokenType.Bot, _token);
        await _discordClient.StartAsync();
    }

    public async Task Shutdown()
    {
        _logger.Information($"Shutting down for {Bot.Name}...");
        await _discordClient.LogoutAsync();
        _logger.Information($"Shutdown for {Bot.Name} complete.");
    }

    internal async Task SlashCommandHandler(SocketSlashCommand command)
    {
        try
        {
            var matchingCommand = Commands.FirstOrDefault(c => c.Name == command.CommandName);
            var response = matchingCommand?.Response(BotPlatforms.Discord);
            if (matchingCommand is not null && response is not null and Embed embed)
                await command.RespondAsync(embed: embed);
            else
                await command.RespondAsync("Unknown botCommand.", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error processing slash botCommand '{command.CommandName}'");
            await command.RespondAsync(
                "An error occurred while processing your botCommand.",
                ephemeral: true
            );
        }
    }

    internal async Task OnClientReady()
    {
        List<ApplicationCommandProperties> applicationCommandProperties = [];
        foreach (var command in Commands)
        {
            applicationCommandProperties.Add(
                new SlashCommandBuilder()
                    .WithName(command.Name)
                    .WithDescription(command.Description)
                    .Build()
            );
        }
        try
        {
            _ = await _discordClient.BulkOverwriteGlobalApplicationCommandsAsync(
                [.. applicationCommandProperties]
            );
        }
        catch (HttpException exception)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception.Errors, Formatting.Indented));
        }
    }

    private void LoadCommands()
    {
        foreach (var botCommand in Bot.Commands)
        {
            if (botCommand.CommandPlatforms.Contains(BotPlatforms.Discord))
                Commands.Add(botCommand);
        }
    }

    private static Task LogDiscordMessageToSerilog(LogMessage message)
    {
        var logger = Log.Logger;
        var logEventLevel = message.Severity switch
        {
            LogSeverity.Critical => Serilog.Events.LogEventLevel.Fatal,
            LogSeverity.Error => Serilog.Events.LogEventLevel.Error,
            LogSeverity.Warning => Serilog.Events.LogEventLevel.Warning,
            LogSeverity.Info => Serilog.Events.LogEventLevel.Information,
            LogSeverity.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogSeverity.Debug => Serilog.Events.LogEventLevel.Debug,
            _ => Serilog.Events.LogEventLevel.Information,
        };

        if (message.Exception != null)
            logger.Write(logEventLevel, message.Exception, $"DDN: {message.Message}");
        else
            logger.Write(logEventLevel, $"DDN: {message.Message}");

        return Task.CompletedTask;
    }

    protected virtual void RaiseCommandEvent(EventArgs e) => OnCommand?.Invoke(this, e);
}
