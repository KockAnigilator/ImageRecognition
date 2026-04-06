namespace ImageRecognition.Application;

public sealed class DatabaseOverviewResult
{
    public int Classes { get; init; }
    public int Images { get; init; }
    public int Features { get; init; }
    public int Models { get; init; }
    public int Experiments { get; init; }
    public int Predictions { get; init; }
}
