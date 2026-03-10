using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Результат отдельной классификации изображения.
/// Соответствует таблице predictions.
/// </summary>
public sealed class Prediction
{
    public int Id { get; set; }

    /// <summary>
    /// Внешний ключ на таблицу images.
    /// </summary>
    public int ImageId { get; set; }

    /// <summary>
    /// Внешний ключ на таблицу models.
    /// </summary>
    public int ModelId { get; set; }

    /// <summary>
    /// Предсказанный класс.
    /// </summary>
    public int PredictedClassId { get; set; }

    /// <summary>
    /// Фактический класс (если он известен). Может быть null, если пользователь только проверяет картинку.
    /// </summary>
    public int? ActualClassId { get; set; }

    /// <summary>
    /// Расстояние до ближайшего соседа (например, евклидово расстояние).
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Флаг, указывающий, использовалось ли KD-дерево (true) или линейный поиск (false).
    /// </summary>
    public bool UsedKdTree { get; set; }

    /// <summary>
    /// Использованное значение k.
    /// </summary>
    public int K { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

