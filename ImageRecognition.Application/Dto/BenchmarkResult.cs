namespace ImageRecognition.Application;

public sealed class BenchmarkResult
{
    public double Accuracy { get; init; }
    public TimeSpan KdTreeSearchTime { get; init; }
    public TimeSpan LinearSearchTime { get; init; }
}
