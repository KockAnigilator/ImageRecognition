namespace ImageRecognition.Application.Interfaces;

/// <summary>
/// Сервис предварительной обработки изображения и извлечения вектора признаков.
/// </summary>
public interface IImagePreprocessingService
{
    /// <summary>
    /// Загружает изображение с диска, масштабирует до 16x16, переводит в оттенки серого
    /// и формирует вектор признаков длиной 256.
    /// </summary>
    /// <param name="filePath">Путь к исходному файлу изображения.</param>
    /// <returns>Вектор признаков (256 значений double).</returns>
    double[] ExtractFeatures(string filePath);
}

