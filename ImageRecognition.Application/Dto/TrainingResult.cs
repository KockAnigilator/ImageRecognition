namespace ImageRecognition.Application;

public sealed class TrainingResult
{
    public int ModelId { get; init; }
    public int SampleCount { get; init; }
    public TimeSpan BuildTime { get; init; }
}
