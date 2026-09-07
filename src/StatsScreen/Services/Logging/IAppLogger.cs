namespace StatsScreen.Services.Logging;

public interface IAppLogger
{
    string LogFilePath { get; }

    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
