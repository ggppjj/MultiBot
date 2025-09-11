using Discord;
using MultiBot.Interfaces;

namespace MultiBot.Commands;

internal class CinephileCommand : IBotCommand
{
    public string Name { get; } = "Cinephile";
    public string Description { get; } =
        "Roll the dice and come up craps! See if you can get the lucky photo you were hoping for, or just set the tone!";
    public List<BotPlatforms> CommandPlatforms { get; } = [BotPlatforms.Discord];

    private readonly string _fileName = "image.png";
    private readonly List<Embed> _embeds = [];
    private readonly Random _random = new();

    internal CinephileCommand()
    {
        _embeds.Add(
            new EmbedBuilder { Title = "Movie 1", ImageUrl = $"attachment://{_fileName}" }.Build()
        );
        _embeds.Add(
            new EmbedBuilder { Title = "Movie 2", ImageUrl = $"attachment://{_fileName}" }.Build()
        );
        _embeds.Add(
            new EmbedBuilder { Title = "Movie 3", ImageUrl = $"attachment://{_fileName}" }.Build()
        );
    }

    public Func<BotPlatforms, object> Response =>
        botPlatforms =>
            botPlatforms == BotPlatforms.Discord && _embeds.Count > 0
                ? _embeds[_random.Next(_embeds.Count)]
                : "";
}
