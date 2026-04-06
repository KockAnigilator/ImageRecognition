namespace ImageRecognition.Domain;

public interface IRecognitionRepository
{
    Task InitializeDatabaseAsync();
    Task<(int Classes, int Images, int Features, int Models, int Experiments, int Predictions)> GetStatisticsAsync();
    Task<string?> GetClassNameByIdAsync(int classId);
    Task<int> EnsureClassAsync(string className);
    Task<int> AddImageWithFeaturesAsync(string filePath, int classId, double[] features);
    Task<IReadOnlyList<(double[] Vector, int ClassId)>> GetTrainingVectorsAsync();
    Task<int> CreateModelAsync(ModelInfo modelInfo);
    Task SaveExperimentAsync(Experiment experiment);
    Task SavePredictionAsync(Prediction prediction);
}
