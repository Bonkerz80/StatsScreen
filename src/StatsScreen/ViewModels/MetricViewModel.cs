using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using StatsScreen.Models;
using StatsScreen.Services.Presentation;
using StatsScreen.Services.Temperature;

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
    private readonly bool _showTemperatureSource;

    public MetricViewModel(bool isTemperature = false, bool showTemperatureSource = false)
    {
        IsTemperature = isTemperature;
        _showTemperatureSource = showTemperatureSource;
    }

    public bool IsTemperature { get; }

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
            return;
        }

        ValueText = "N/A";
        UnitText = string.Empty;
        SourceText = string.Empty;
        DetailText = IsTemperature ? "SENSOR UNAVAILABLE" : string.Empty;
        TemperatureStatus = TemperatureStatus.Unavailable;
        TemperaturePercent = 0;
        IsAvailable = false;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
