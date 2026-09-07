using System.IO;
using System.Text.Json;
using StatsScreen.Services.Logging;

namespace StatsScreen.Services.Settings;

public sealed class SettingsService
{
    private readonly IAppLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService(IAppLogger logger, string? settingsDirectory = null)
    {
        _logger = logger;
        SettingsDirectory = settingsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StatsScreen");
        SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
    }

    public string SettingsDirectory { get; }

    public string SettingsFilePath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            string json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch (Exception exception)
        {
            _logger.Error($"Unable to load settings from '{SettingsFilePath}'. Defaults will be used.", exception);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(settings, _jsonOptions);
            string temporaryPath = SettingsFilePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, SettingsFilePath, overwrite: true);
        }
        catch (Exception exception)
        {
            _logger.Error($"Unable to save settings to '{SettingsFilePath}'.", exception);
        }
    }
}
