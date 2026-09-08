using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using StatsScreen.Models;
using StatsScreen.Services.Presentation;
using StatsScreen.Services.Temperature;
using WpfBrush = System.Windows.Media.Brush;

namespace StatsScreen.ViewModels;

public sealed class MetricViewModel : INotifyPropertyChanged
{
    private string _valueText = "N/A";
    private string _unitText = string.Empty;
    private string _sourceText = string.Empty;
    private bool _isAvailable;
    private string _detailText = "SENSOR UNAVAILABLE";
    private TemperatureStatus _temperatureStatus = TemperatureStatus.Unavailable;
    private double _temperaturePercent;
    private readonly string _defaultAccentKey;
    private readonly bool _showTemperatureSource;
    private string _accentKey;
    private WpfBrush _accentBrush;
    private WpfBrush _temperatureValueBrush;
    private WpfBrush _temperatureBarBrush;

    public MetricViewModel(
        bool isTemperature = false,
        bool showTemperatureSource = false,
        string? defaultAccentKey = null)
    {
        IsTemperature = isTemperature;
        _showTemperatureSource = showTemperatureSource;
        _defaultAccentKey = AccentPalette.NormalizeKey(
            defaultAccentKey,
            AccentPalette.DefaultCpuTemperatureKey);
        _accentKey = _defaultAccentKey;
        _accentBrush = AccentPalette.Get(_accentKey).Brush;
        _temperatureValueBrush = AccentPalette.UnavailableTemperatureValueBrush;
        _temperatureBarBrush = AccentPalette.UnavailableTemperatureBarBrush;
    }

    public bool IsTemperature { get; }

    public string AccentKey
    {
        get => _accentKey;
        private set => SetField(ref _accentKey, value);
    }

    public WpfBrush AccentBrush
    {
        get => _accentBrush;
        private set => SetField(ref _accentBrush, value);
    }

    public WpfBrush TemperatureValueBrush
    {
        get => _temperatureValueBrush;
        private set => SetField(ref _temperatureValueBrush, value);
    }

    public WpfBrush TemperatureBarBrush
    {
        get => _temperatureBarBrush;
        private set => SetField(ref _temperatureBarBrush, value);
    }

    public string DetailText { get => _detailText; private set => SetField(ref _detailText, value); }

    public TemperatureStatus TemperatureStatus
    {
        get => _temperatureStatus;
        private set => SetField(ref _temperatureStatus, value);
    }

    public double TemperaturePercent
    {
        get => _temperaturePercent;
        private set => SetField(ref _temperaturePercent, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ValueText
    {
        get => _valueText;
        private set => SetField(ref _valueText, value);
    }

    public string UnitText
    {
        get => _unitText;
        private set => SetField(ref _unitText, value);
    }

    public string SourceText
    {
        get => _sourceText;
        private set => SetField(ref _sourceText, value);
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        private set => SetField(ref _isAvailable, value);
    }

    public void SetAccent(string? accentKey)
    {
        string normalizedKey = AccentPalette.NormalizeKey(accentKey, _defaultAccentKey);
        AccentKey = normalizedKey;
        AccentBrush = AccentPalette.Get(normalizedKey).Brush;
        UpdateTemperatureBrushes();
    }

    public void Apply(SensorMetric metric)
    {
        if (metric.Value is { } value && !double.IsNaN(value) && !double.IsInfinity(value))
        {
            ValueText = value.ToString(metric.Format, CultureInfo.InvariantCulture);
            UnitText = metric.Unit;
            SourceText = metric.Source;
            DetailText = IsTemperature && _showTemperatureSource
                ? TemperatureSourceFormatter.Format(metric.Source)
                : string.Empty;
            TemperatureStatus = IsTemperature
                ? TemperatureStatusRules.Classify(value)
                : TemperatureStatus.Unavailable;
            TemperaturePercent = IsTemperature
                ? TemperatureStatusRules.ToDisplayPercent(value)
                : 0;
            IsAvailable = true;
            UpdateTemperatureBrushes();
            return;
        }

        ValueText = "N/A";
        UnitText = string.Empty;
        SourceText = string.Empty;
        DetailText = IsTemperature ? "SENSOR UNAVAILABLE" : string.Empty;
        TemperatureStatus = TemperatureStatus.Unavailable;
        TemperaturePercent = 0;
        IsAvailable = false;
        UpdateTemperatureBrushes();
    }

    private void UpdateTemperatureBrushes()
    {
        if (!IsTemperature)
        {
            TemperatureValueBrush = AccentPalette.NormalTemperatureValueBrush;
            TemperatureBarBrush = AccentBrush;
            return;
        }

        switch (TemperatureStatus)
        {
            case TemperatureStatus.Warm:
                TemperatureValueBrush = AccentPalette.WarmTemperatureBrush;
                TemperatureBarBrush = AccentPalette.WarmTemperatureBrush;
                break;
            case TemperatureStatus.Hot:
                TemperatureValueBrush = AccentPalette.HotTemperatureBrush;
                TemperatureBarBrush = AccentPalette.HotTemperatureBrush;
                break;
            case TemperatureStatus.Unavailable:
                TemperatureValueBrush = AccentPalette.UnavailableTemperatureValueBrush;
                TemperatureBarBrush = AccentPalette.UnavailableTemperatureBarBrush;
                break;
            default:
                TemperatureValueBrush = AccentPalette.NormalTemperatureValueBrush;
                TemperatureBarBrush = AccentBrush;
                break;
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
