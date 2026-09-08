using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace StatsScreen.Services.Presentation;

public sealed class AccentOption
{
    public AccentOption(string key, string displayName, string hex)
    {
        Key = key;
        DisplayName = displayName;
        Hex = hex;
        Brush = CreateBrush(hex);
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Hex { get; }

    public WpfSolidColorBrush Brush { get; }

    private static WpfSolidColorBrush CreateBrush(string hex)
    {
        WpfColor color = (WpfColor)WpfColorConverter.ConvertFromString(hex)!;
        var brush = new WpfSolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

public static class AccentPalette
{
    public const string CyanKey = "Cyan";
    public const string BlueKey = "Blue";
    public const string GreenKey = "Green";
    public const string LimeKey = "Lime";
    public const string AmberKey = "Amber";
    public const string OrangeKey = "Orange";
    public const string RedKey = "Red";
    public const string PinkKey = "Pink";
    public const string PurpleKey = "Purple";
    public const string WhiteKey = "White";
    public const string GreyKey = "Grey";

    public const string DefaultCpuTemperatureKey = CyanKey;
    public const string DefaultGpuTemperatureKey = BlueKey;
    public const string DefaultCpuPowerKey = AmberKey;
    public const string DefaultGpuPowerKey = PurpleKey;

    public static IReadOnlyList<AccentOption> Options { get; } =
    [
        new AccentOption(CyanKey, "Cyan", "#4DC6C7"),
        new AccentOption(BlueKey, "Blue", "#5F9FEA"),
        new AccentOption(GreenKey, "Green", "#6CCB8A"),
        new AccentOption(LimeKey, "Lime", "#A8C95D"),
        new AccentOption(AmberKey, "Amber", "#D5A85B"),
        new AccentOption(OrangeKey, "Orange", "#E38B57"),
        new AccentOption(RedKey, "Red", "#D96B6B"),
        new AccentOption(PinkKey, "Pink", "#D38AB5"),
        new AccentOption(PurpleKey, "Purple", "#B98BE7"),
        new AccentOption(WhiteKey, "White", "#E6EDF3"),
        new AccentOption(GreyKey, "Grey", "#8C9AA8")
    ];

    public static WpfSolidColorBrush NormalTemperatureValueBrush { get; } = CreateBrush("#F3F7FA");

    public static WpfSolidColorBrush WarmTemperatureBrush { get; } = CreateBrush("#E6B85F");

    public static WpfSolidColorBrush HotTemperatureBrush { get; } = CreateBrush("#F07171");

    public static WpfSolidColorBrush UnavailableTemperatureValueBrush { get; } = CreateBrush("#74808D");

    public static WpfSolidColorBrush UnavailableTemperatureBarBrush { get; } = CreateBrush("#53606C");

    private static readonly IReadOnlyDictionary<string, AccentOption> OptionsByKey =
        Options.ToDictionary(option => option.Key, StringComparer.OrdinalIgnoreCase);

    public static AccentOption Resolve(string? key, string fallbackKey)
    {
        if (!string.IsNullOrWhiteSpace(key) && OptionsByKey.TryGetValue(key.Trim(), out AccentOption? option))
            return option;

        if (OptionsByKey.TryGetValue(fallbackKey, out option))
            return option;

        return Options[0];
    }

    public static string NormalizeKey(string? key, string fallbackKey) => Resolve(key, fallbackKey).Key;

    public static AccentOption Get(string key) => Resolve(key, DefaultCpuTemperatureKey);

    private static WpfSolidColorBrush CreateBrush(string hex)
    {
        WpfColor color = (WpfColor)WpfColorConverter.ConvertFromString(hex)!;
        var brush = new WpfSolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
