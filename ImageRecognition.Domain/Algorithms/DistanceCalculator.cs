namespace ImageRecognition.Domain;

/// <summary>
/// Вспомогательный класс для вычисления расстояний между векторами признаков.
/// </summary>
public static class DistanceCalculator
{
    /// <summary>
    /// Вычисляет евклидово расстояние между двумя точками одинаковой размерности.
    /// </summary>
    /// <param name="a">Первая точка.</param>
    /// <param name="b">Вторая точка.</param>
    /// <returns>Евклидово расстояние.</returns>
    /// <exception cref="ArgumentNullException">Если один из векторов равен null.</exception>
    /// <exception cref="ArgumentException">Если размерности векторов не совпадают.</exception>
    public static double EuclideanDistance(double[] a, double[] b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Векторы должны иметь одинаковую размерность.");
        }

        double sum = 0.0;
        for (int i = 0; i < a.Length; i++)
        {
            double diff = a[i] - b[i];
            sum += diff * diff;
        }

        return Math.Sqrt(sum);
    }
}

