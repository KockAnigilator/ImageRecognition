using ImageRecognition.Domain;
using ImageRecognition.Application.Interfaces;
using ImageRecognition.Application.Services;

namespace ImageRecognition.Tests;

[TestClass]
public sealed class DomainAlgorithmTests
{
    [TestMethod]
    public void EuclideanDistance_ReturnsExpectedValue()
    {
        var a = new[] { 0.0, 0.0 };
        var b = new[] { 3.0, 4.0 };

        double distance = DistanceCalculator.EuclideanDistance(a, b);

        Assert.AreEqual(5.0, distance, 1e-9);
    }

    [TestMethod]
    public void KDTree_NearestNeighbor_ReturnsCorrectLabel()
    {
        var points = new List<double[]>
        {
            new[] { 1.0, 1.0 },
            new[] { 5.0, 5.0 },
            new[] { 9.0, 9.0 }
        };
        var labels = new List<int> { 1, 2, 3 };

        var tree = new KDTree(2);
        tree.BuildTree(points, labels);

        var nearest = tree.NearestNeighbor(new[] { 5.1, 5.1 });

        Assert.IsNotNull(nearest);
        Assert.AreEqual(2, nearest.Label);
    }

    [TestMethod]
    public void KNearestNeighbors_LinearAndKdTree_GiveSameClass()
    {
        var points = new List<double[]>
        {
            new[] { 1.0, 1.0 },
            new[] { 1.1, 1.1 },
            new[] { 8.0, 8.0 },
            new[] { 8.2, 8.1 }
        };
        var labels = new List<int> { 0, 0, 1, 1 };

        var tree = new KDTree(2);
        tree.BuildTree(points, labels);

        var knn = new KNearestNeighbors();
        var target = new[] { 1.05, 1.0 };

        int kdTreeResult = knn.Classify(tree, target, 3);
        int linearResult = knn.ClassifyLinear(points, labels, target, 3);

        Assert.AreEqual(linearResult, kdTreeResult);
    }

    [TestMethod]
    public async Task RecognitionService_TrainAsync_Throws_WhenNoSamples()
    {
        var preprocessing = new FakePreprocessingService();
        var repository = new FakeRecognitionRepository
        {
            TrainingVectors = Array.Empty<(double[] Vector, int ClassId)>()
        };

        var service = new RecognitionService(preprocessing, repository);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.TrainAsync("test", 3));
    }

    [TestMethod]
    public async Task RecognitionService_ClassifyAsync_Throws_WhenModelNotTrained()
    {
        var preprocessing = new FakePreprocessingService();
        var repository = new FakeRecognitionRepository();
        var service = new RecognitionService(preprocessing, repository);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.ClassifyImageAsync("fake.png", 3, true));
    }

    private sealed class FakePreprocessingService : IImagePreprocessingService
    {
        public double[] ExtractFeatures(string filePath) => new double[256];
    }

    private sealed class FakeRecognitionRepository : IRecognitionRepository
    {
        public IReadOnlyList<(double[] Vector, int ClassId)> TrainingVectors { get; set; }
            = new List<(double[] Vector, int ClassId)>
            {
                (new double[] { 0.0, 0.0 }, 1)
            };

        public Task InitializeDatabaseAsync() => Task.CompletedTask;

        public Task<(int Classes, int Images, int Features, int Models, int Experiments, int Predictions)> GetStatisticsAsync()
            => Task.FromResult((0, 0, 0, 0, 0, 0));

        public Task<string?> GetClassNameByIdAsync(int classId) => Task.FromResult<string?>("class_1");

        public Task<int> EnsureClassAsync(string className) => Task.FromResult(1);

        public Task<int> AddImageWithFeaturesAsync(string imageName, byte[] imageData, int classId, double[] features)
            => Task.FromResult(1);

        public Task<IReadOnlyList<(double[] Vector, int ClassId)>> GetTrainingVectorsAsync()
            => Task.FromResult(TrainingVectors);

        public Task<int> CreateModelAsync(ModelInfo modelInfo) => Task.FromResult(1);

        public Task SaveExperimentAsync(Experiment experiment) => Task.CompletedTask;

        public Task SavePredictionAsync(Prediction prediction) => Task.CompletedTask;
    }
}
