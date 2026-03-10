using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Вектор признаков изображения.
/// Соответствует таблице features, где вектор хранится как FLOAT8[].
/// </summary>
public sealed class ImageFeatures
{
    public int Id { get; set; }

    /// <summary>
    /// Внешний ключ на таблицу images.
    /// </summary>
    public int ImageId { get; set; }

    /// <summary>
    /// Вектор признаков фиксированной длины (для 16x16 — 256 элементов).
    /// </summary>
    public double[] Vector { get; set; } = Array.Empty<double>();
}

