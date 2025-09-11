namespace MultiBot.Interfaces;

internal interface IBot
{
    string Name { get; }
    List<IBotCommand> Commands { get; }
    void OnCommand(string message);
    Task Shutdown();
}
