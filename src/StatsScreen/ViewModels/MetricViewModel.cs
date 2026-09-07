using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using StatsScreen.Models;

namespace StatsScreen.ViewModels;

public sealed class MetricViewModel : INotifyPropertyChanged
{
    private string _valueText = "N/A";
    private string _unitText = string.Empty;
    private string _sourceText = string.Empty;
    private bool _isAvailable;
    private string _detailText = "WAITING FOR SENSOR";
    public string DetailText { get => _detailText; private set => SetField(ref _detailText, value); }

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
            DetailText = metric.Source.Contains("Tctl/Tdie", StringComparison.OrdinalIgnoreCase) ? "Tctl/Tdie · CPU FALLBACK" :
                metric.Source.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ? "Tdie · CPU FALLBACK" : "LIVE SENSOR";
            IsAvailable = true;
            return;
        }

        ValueText = "N/A";
        UnitText = string.Empty;
        SourceText = string.Empty;
        DetailText = "SENSOR UNAVAILABLE";
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
