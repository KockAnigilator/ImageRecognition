namespace ImageRecognition.Application;

public sealed class ClassificationResult
{
    public int PredictedClassId { get; init; }
    public string PredictedClassName { get; init; } = string.Empty;
    public TimeSpan SearchTime { get; init; }
    public bool UsedKdTree { get; init; }
    public int K { get; init; }
}
