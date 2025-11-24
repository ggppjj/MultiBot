using System.Diagnostics;
using Discord;
using MultiBot.Interfaces;

namespace MultiBot.Commands;

internal class CinephileCommand : IBotCommand
{
    public string Name { get; } = "Cinephile";
    public string Description { get; } =
        "Roll the dice and come up craps! See if you can get the photo you were hoping for, or set the tone!";
    public List<BotPlatforms> CommandPlatforms { get; } = [BotPlatforms.Discord];

    private readonly string _fileName1 = "image1.png";
    private readonly string _fileName2 = "image2.png";
    private readonly string _fileName3 = "image3.png";

    private readonly string _imagePath1 = Path.Combine("Resources", "Images", "image1.png");
    private readonly string _imagePath2 = Path.Combine("Resources", "Images", "image2.png");
    private readonly string _imagePath3 = Path.Combine("Resources", "Images", "image3.png");

    private readonly List<(Embed embed, string filePath, string fileName)> _movieData = [];
    private readonly Random _random = new();

    internal CinephileCommand()
    {
        _movieData.Add(
            (
                new EmbedBuilder
                {
                    Title = "Movie 1",
                    ImageUrl = $"attachment://{_fileName1}",
                    Description = "A dark film of adventure and friendship.",
                }.Build(),
                _imagePath1,
                _fileName1
            )
        );
        _movieData.Add(
            (
                new EmbedBuilder
                {
                    Title = "Movie 2",
                    ImageUrl = $"attachment://{_fileName2}",
                    Description = "An epic tale of adventure and heroism.",
                }.Build(),
                _imagePath2,
                _fileName2
            )
        );
        _movieData.Add(
            (
                new EmbedBuilder
                {
                    Title = "Movie 3",
                    ImageUrl = $"attachment://{_fileName3}",
                    Description = "A classic adventure film that never gets old.",
                }.Build(),
                _imagePath3,
                _fileName3
            )
        );
    }

    public Func<BotPlatforms, object> Response =>
        botPlatforms =>
        {
            if (botPlatforms == BotPlatforms.Discord && _movieData.Count > 0)
            {
                var (embed, filePath, fileName) = _movieData[_random.Next(_movieData.Count)];
                Debug.WriteLine(filePath);
                if (!File.Exists(filePath))
                    return $"Error: Image file not found at {filePath}";
                var fileAttachment = new FileAttachment(filePath, fileName);
                return new { Embed = embed, Attachment = fileAttachment };
            }
            return "";
        };
}
