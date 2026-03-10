using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Описание обученной модели (например, набора обучающих векторов и параметров kNN).
/// Соответствует таблице models.
/// </summary>
public sealed class ModelInfo
{
    public int Id { get; set; }

    /// <summary>
    /// Человекочитаемое имя модели (например, "kNN_16x16_k=3").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Размерность пространства признаков (например, 256).
    /// </summary>
    public int Dimension { get; set; }

    /// <summary>
    /// Количество обучающих примеров, использованных при построении модели.
    /// </summary>
    public int TrainingSampleCount { get; set; }

    /// <summary>
    /// Значение k, использованное по умолчанию при классификации.
    /// </summary>
    public int DefaultK { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дополнительное описание или комментарий.
    /// </summary>
    public string? Description { get; set; }
}

