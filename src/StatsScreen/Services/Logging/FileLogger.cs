using System.IO;
using System.Globalization;
using System.Text;

namespace StatsScreen.Services.Logging;

public sealed class FileLogger : IAppLogger, IDisposable
{
    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public FileLogger(string? directory = null)
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StatsScreen",
            "logs");

        root = directory ?? root;
        Directory.CreateDirectory(root);
        LogFilePath = Path.Combine(root, $"stats-screen-{DateTime.Now:yyyyMMdd}.log");
        _writer = new StreamWriter(
            new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

        Info($"Stats Screen started. Version {typeof(FileLogger).Assembly.GetName().Version}.");
    }

    public string LogFilePath { get; }

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        string fullMessage = exception is null
            ? message
            : $"{message} | {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}";

        Write("ERROR", fullMessage);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _writer.Dispose();
        }
    }

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _writer.WriteLine(
                $"{DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)} [{level}] {message}");
        }
    }
}
