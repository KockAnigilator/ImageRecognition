using ImageRecognition.Application.ImageProcessing;
using ImageRecognition.Application.Interfaces;
using ImageRecognition.Infrastructure.Database;
using ImageRecognition.Infrastructure.Repositories;

namespace ImageRecognition.Application.Services;

public static class ApplicationFactory
{
    public static IRecognitionService CreateDefaultRecognitionService(PostgresOptions options)
    {
        var preprocessing = new ImagePreprocessingService();
        var factory = new PostgresConnectionFactory(options);
        var repository = new RecognitionRepository(factory);
        return new RecognitionService(preprocessing, repository);
    }
}
