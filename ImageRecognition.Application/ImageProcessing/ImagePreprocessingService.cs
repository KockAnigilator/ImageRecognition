using System.Drawing;
using System.Runtime.Versioning;
using ImageRecognition.Application.Interfaces;

namespace ImageRecognition.Application.ImageProcessing;

/// <summary>
/// Простая реализация предварительной обработки изображения:
/// загрузка, масштабирование до 16x16, перевод в оттенки серого и формирование вектора признаков.
/// </summary>
public sealed class ImagePreprocessingService : IImagePreprocessingService
{
    private const int TargetWidth = 16;
    private const int TargetHeight = 16;
    private const int FeatureVectorLength = TargetWidth * TargetHeight;

    [SupportedOSPlatform("windows")]
    public double[] ExtractFeatures(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        using var original = new Bitmap(filePath);
        using var resized = new Bitmap(original, new Size(TargetWidth, TargetHeight));

        var features = new double[FeatureVectorLength];

        int index = 0;
        for (int y = 0; y < TargetHeight; y++)
        {
            for (int x = 0; x < TargetWidth; x++)
            {
                Color pixel = resized.GetPixel(x, y);
                // Простое преобразование в оттенки серого по формуле яркости.
                double gray = (0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B) / 255.0;
                features[index++] = gray;
            }
        }

        return features;
    }
}

