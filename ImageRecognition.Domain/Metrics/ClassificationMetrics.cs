using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Итоговые метрики для оценки качества и скорости работы алгоритмов.
/// Эти данные удобно выводить в UI.
/// </summary>
public sealed class ClassificationMetrics
{
    /// <summary>
    /// Точность классификации (0..1).
    /// </summary>
    public double Accuracy { get; set; }

    public TimeSpan KdTreeBuildTime { get; set; }

    public TimeSpan KdTreeSearchTime { get; set; }

    public TimeSpan LinearSearchTime { get; set; }
}

