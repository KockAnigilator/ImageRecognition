using System.Diagnostics;
using ImageRecognition.Application.Interfaces;
using ImageRecognition.Domain;

namespace ImageRecognition.Application.Services;

public sealed class RecognitionService : IRecognitionService
{
    private readonly IImagePreprocessingService _preprocessingService;
    private readonly IRecognitionRepository _repository;
    private readonly KNearestNeighbors _classifier = new();

    private KDTree? _tree;
    private IReadOnlyList<(double[] Vector, int ClassId)> _samples = Array.Empty<(double[] Vector, int ClassId)>();
    private int _modelId;

    public RecognitionService(IImagePreprocessingService preprocessingService, IRecognitionRepository repository)
    {
        _preprocessingService = preprocessingService;
        _repository = repository;
    }

    public Task InitializeDatabaseAsync() => _repository.InitializeDatabaseAsync();

    public async Task<DatabaseOverviewResult> GetDatabaseOverviewAsync()
    {
        var stats = await _repository.GetStatisticsAsync();
        return new DatabaseOverviewResult
        {
            Classes = stats.Classes,
            Images = stats.Images,
            Features = stats.Features,
            Models = stats.Models,
            Experiments = stats.Experiments,
            Predictions = stats.Predictions
        };
    }

    public async Task<int> AddTrainingImageAsync(string filePath, string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            throw new ArgumentException("Class name is required.", nameof(className));
        }

        var features = _preprocessingService.ExtractFeatures(filePath);
        int classId = await _repository.EnsureClassAsync(className);
        return await _repository.AddImageWithFeaturesAsync(filePath, classId, features);
    }

    public async Task<TrainingResult> TrainAsync(string modelName, int k)
    {
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));

        _samples = await _repository.GetTrainingVectorsAsync();
        if (_samples.Count == 0)
        {
            throw new InvalidOperationException("No training samples found. Add images first.");
        }

        int dimension = _samples[0].Vector.Length;
        var points = _samples.Select(s => s.Vector).ToList();
        var labels = _samples.Select(s => s.ClassId).ToList();

        var sw = Stopwatch.StartNew();
        var tree = new KDTree(dimension);
        tree.BuildTree(points, labels);
        sw.Stop();

        _tree = tree;

        _modelId = await _repository.CreateModelAsync(new ModelInfo
        {
            Name = string.IsNullOrWhiteSpace(modelName) ? $"kNN_k={k}" : modelName,
            Dimension = dimension,
            TrainingSampleCount = _samples.Count,
            DefaultK = k,
            CreatedAt = DateTime.UtcNow,
            Description = "Model trained in coursework app."
        });

        return new TrainingResult
        {
            ModelId = _modelId,
            SampleCount = _samples.Count,
            BuildTime = sw.Elapsed
        };
    }

    public async Task<ClassificationResult> ClassifyImageAsync(string filePath, int k, bool useKdTree)
    {
        if (_samples.Count == 0 || _tree is null)
        {
            throw new InvalidOperationException("Model is not trained. Run training first.");
        }

        var vector = _preprocessingService.ExtractFeatures(filePath);
        var sw = Stopwatch.StartNew();

        int predicted = useKdTree
            ? _classifier.Classify(_tree, vector, k)
            : _classifier.ClassifyLinear(_samples.Select(s => s.Vector).ToList(), _samples.Select(s => s.ClassId).ToList(), vector, k);

        sw.Stop();

        await _repository.SavePredictionAsync(new Prediction
        {
            ImageId = 0,
            ModelId = _modelId,
            PredictedClassId = predicted,
            ActualClassId = null,
            Distance = 0.0,
            UsedKdTree = useKdTree,
            K = k,
            CreatedAt = DateTime.UtcNow
        });

        string predictedClassName = await _repository.GetClassNameByIdAsync(predicted) ?? $"class_{predicted}";

        return new ClassificationResult
        {
            PredictedClassId = predicted,
            PredictedClassName = predictedClassName,
            SearchTime = sw.Elapsed,
            UsedKdTree = useKdTree,
            K = k
        };
    }

    public async Task<BenchmarkResult> RunBenchmarkAsync(int k)
    {
        if (_samples.Count == 0 || _tree is null)
        {
            throw new InvalidOperationException("Model is not trained. Run training first.");
        }

        var kdSw = Stopwatch.StartNew();
        int kdCorrect = 0;
        foreach (var sample in _samples)
        {
            int predicted = _classifier.Classify(_tree, sample.Vector, k);
            if (predicted == sample.ClassId) kdCorrect++;
        }
        kdSw.Stop();

        var linearSw = Stopwatch.StartNew();
        foreach (var sample in _samples)
        {
            _classifier.ClassifyLinear(_samples.Select(s => s.Vector).ToList(), _samples.Select(s => s.ClassId).ToList(), sample.Vector, k);
        }
        linearSw.Stop();

        double accuracy = (double)kdCorrect / _samples.Count;

        await _repository.SaveExperimentAsync(new Experiment
        {
            ModelId = _modelId,
            TrainSampleCount = _samples.Count,
            TestSampleCount = _samples.Count,
            Accuracy = accuracy,
            KdTreeBuildTimeMs = 0,
            KdTreeSearchTimeMs = kdSw.Elapsed.TotalMilliseconds,
            LinearSearchTimeMs = linearSw.Elapsed.TotalMilliseconds,
            PerformedAt = DateTime.UtcNow,
            Notes = "Benchmark on training set"
        });

        return new BenchmarkResult
        {
            Accuracy = accuracy,
            KdTreeSearchTime = kdSw.Elapsed,
            LinearSearchTime = linearSw.Elapsed
        };
    }
}
