using ImageRecognition.Domain;

namespace ImageRecognition.NUnitTests;

public sealed class KdTreeSmokeTests
{
    [Test]
    public void NearestNeighbor_ReturnsLabelForClosestPoint()
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

        var nearest = tree.NearestNeighbor(new[] { 5.2, 5.1 });

        Assert.That(nearest, Is.Not.Null);
        Assert.That(nearest!.Label, Is.EqualTo(2));
    }
}
