using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Эксперимент по обучению и тестированию модели.
/// Позволяет хранить качество и время работы алгоритмов.
/// Соответствует таблице experiments.
/// </summary>
public sealed class Experiment
{
    public int Id { get; set; }

    /// <summary>
    /// Внешний ключ на таблицу models.
    /// </summary>
    public int ModelId { get; set; }

    public int TrainSampleCount { get; set; }

    public int TestSampleCount { get; set; }

    /// <summary>
    /// Доля правильно классифицированных объектов (0..1).
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Время построения KD-дерева в миллисекундах.
    /// </summary>
    public double KdTreeBuildTimeMs { get; set; }

    /// <summary>
    /// Время поиска ближайших соседей через KD-дерево (суммарное по всем тестовым объектам), мс.
    /// </summary>
    public double KdTreeSearchTimeMs { get; set; }

    /// <summary>
    /// Время линейного поиска ближайших соседей, мс.
    /// </summary>
    public double LinearSearchTimeMs { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}

