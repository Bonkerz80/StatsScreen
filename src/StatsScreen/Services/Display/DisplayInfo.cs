namespace StatsScreen.Services.Display;

public sealed record DisplayInfo(
    string DeviceName,
    string DisplayName,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary)
{
    public string ResolutionText => $"{Width} × {Height}";

    public string DisplayLabel =>
        $"{DisplayName}  ·  {ResolutionText}{(IsPrimary ? "  ·  Primary" : string.Empty)}";

    public bool IsTargetSize => Width == 800 && Height == 600;
}
