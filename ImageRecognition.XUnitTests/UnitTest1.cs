using ImageRecognition.Domain;

namespace ImageRecognition.XUnitTests;

public sealed class DomainDistanceTests
{
    [Fact]
    public void EuclideanDistance_ForClassic345Triangle_Returns5()
    {
        var a = new[] { 0.0, 0.0 };
        var b = new[] { 3.0, 4.0 };

        double result = DistanceCalculator.EuclideanDistance(a, b);

        Assert.Equal(5.0, result, 9);
    }

    [Fact]
    public void EuclideanDistance_ForDifferentDimensions_ThrowsArgumentException()
    {
        var a = new[] { 1.0, 2.0 };
        var b = new[] { 1.0 };

        Assert.Throws<ArgumentException>(() => DistanceCalculator.EuclideanDistance(a, b));
    }
}
