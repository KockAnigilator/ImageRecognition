using System;

namespace ImageRecognition.Domain;

/// <summary>
/// Класс (метка) распознаваемого объекта: цифра или геометрическая фигура.
/// Соответствует строке в таблице classes.
/// </summary>
public sealed class ImageClass
{
    public int Id { get; set; }

    /// <summary>
    /// Имя класса, например "0", "1", "circle", "triangle".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Произвольное текстовое описание класса.
    /// </summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

