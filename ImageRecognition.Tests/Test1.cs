using ImageRecognition.Domain;

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
}
