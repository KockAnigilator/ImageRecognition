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
    /// Имя исходного файла изображения.
    /// </summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// Бинарные данные изображения, хранящиеся в PostgreSQL (BYTEA).
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

