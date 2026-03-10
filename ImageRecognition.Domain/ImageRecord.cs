using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Описание изображения в базе данных.
/// Соответствует строке в таблице images.
/// </summary>
public sealed class ImageRecord
{
    public int Id { get; set; }

    /// <summary>
    /// Внешний ключ на таблицу classes.
    /// </summary>
    public int ClassId { get; set; }

    /// <summary>
    /// Путь к файлу изображения на диске.
    /// Для курсовой достаточно относительного пути или имени файла.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

