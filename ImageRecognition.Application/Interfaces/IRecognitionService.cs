namespace ImageRecognition.Application.Interfaces;

public interface IRecognitionService
{
    Task InitializeDatabaseAsync();
    Task<int> AddTrainingImageAsync(string filePath, string className);
    Task<TrainingResult> TrainAsync(string modelName, int k);
    Task<ClassificationResult> ClassifyImageAsync(string filePath, int k, bool useKdTree);
    Task<BenchmarkResult> RunBenchmarkAsync(int k);
}
