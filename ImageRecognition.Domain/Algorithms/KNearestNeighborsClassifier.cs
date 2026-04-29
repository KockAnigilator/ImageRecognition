using System.Collections.Generic;
using System.Linq;

namespace ImageRecognition.Domain;

/// <summary>
/// Реализация алгоритма k ближайших соседей (kNN) поверх KD-дерева или линейного поиска.
/// </summary>
public sealed class KNearestNeighbors
{
    /// <summary>
    /// Классифицирует объект по его вектору признаков с использованием KD-дерева.
    /// </summary>
    /// <param name="tree">Построенное KD-дерево.</param>
    /// <param name="featureVector">Вектор признаков объекта.</param>
    /// <param name="k">Число ближайших соседей.</param>
    /// <returns>Предсказанная метка класса.</returns>
    public int Classify(KDTree tree, double[] featureVector, int k)
    {
        if (tree is null) throw new ArgumentNullException(nameof(tree));
        if (featureVector is null) throw new ArgumentNullException(nameof(featureVector));

        var neighbors = tree.KNearestNeighbors(featureVector, k);
        if (neighbors.Count == 0)
        {
            throw new InvalidOperationException("KD-дерево не содержит точек.");
        }

        return MajorityVote(neighbors.Select(n => n.Node.Label));
    }

    /// <summary>
    /// Классификация через линейный поиск по всей обучающей выборке.
    /// Используется для сравнения с KD-деревом по точности и времени.
    /// </summary>
    /// <param name="trainingPoints">Список векторов признаков обучающей выборки.</param>
    /// <param name="trainingLabels">Список меток классов обучающей выборки.</param>
    /// <param name="featureVector">Классифицируемый объект.</param>
    /// <param name="k">Число ближайших соседей.</param>
    /// <returns>Предсказанная метка класса.</returns>
    public int ClassifyLinear(
        IReadOnlyList<double[]> trainingPoints,
        IReadOnlyList<int> trainingLabels,
        double[] featureVector,
        int k)
    {
        if (trainingPoints is null) throw new ArgumentNullException(nameof(trainingPoints));
        if (trainingLabels is null) throw new ArgumentNullException(nameof(trainingLabels));
        if (featureVector is null) throw new ArgumentNullException(nameof(featureVector));
        if (trainingPoints.Count != trainingLabels.Count)
        {
            throw new ArgumentException("Обучающие точки и метки должны иметь одинаковую длину.");
        }

        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "Параметр k должен быть положительным.");
        if (trainingPoints.Count == 0) throw new InvalidOperationException("Обучающая выборка пуста.");

        var distances = new List<(int Label, double Distance)>(trainingPoints.Count);
        for (int i = 0; i < trainingPoints.Count; i++)
        {
            double distance = DistanceCalculator.EuclideanDistance(trainingPoints[i], featureVector);
            distances.Add((trainingLabels[i], distance));
        }

        var nearest = distances
            .OrderBy(p => p.Distance)
            .Take(k)
            .ToList();

        return MajorityVote(nearest.Select(p => p.Label));
    }

    private static int MajorityVote(IEnumerable<int> labels)
    {
        var grouped = labels
            .GroupBy(l => l)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label) // Детеминизм при равенстве частот
            .First();

        return grouped.Label;
    }
}

