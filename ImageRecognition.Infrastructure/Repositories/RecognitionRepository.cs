using ImageRecognition.Domain;
using ImageRecognition.Infrastructure.Database;
using Npgsql;

namespace ImageRecognition.Infrastructure.Repositories;

public sealed class RecognitionRepository : IRecognitionRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public RecognitionRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        string sql = """
            CREATE TABLE IF NOT EXISTS classes (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                description TEXT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS images (
                id SERIAL PRIMARY KEY,
                class_id INT NOT NULL REFERENCES classes(id),
                file_path TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS features (
                id SERIAL PRIMARY KEY,
                image_id INT NOT NULL UNIQUE REFERENCES images(id),
                vector FLOAT8[] NOT NULL
            );

            CREATE TABLE IF NOT EXISTS models (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL,
                dimension INT NOT NULL,
                training_sample_count INT NOT NULL,
                default_k INT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                description TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS experiments (
                id SERIAL PRIMARY KEY,
                model_id INT NOT NULL REFERENCES models(id),
                train_sample_count INT NOT NULL,
                test_sample_count INT NOT NULL,
                accuracy DOUBLE PRECISION NOT NULL,
                kd_tree_build_time_ms DOUBLE PRECISION NOT NULL,
                kd_tree_search_time_ms DOUBLE PRECISION NOT NULL,
                linear_search_time_ms DOUBLE PRECISION NOT NULL,
                performed_at TIMESTAMP NOT NULL DEFAULT NOW(),
                notes TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS predictions (
                id SERIAL PRIMARY KEY,
                image_id INT NOT NULL DEFAULT 0,
                model_id INT NOT NULL REFERENCES models(id),
                predicted_class_id INT NOT NULL,
                actual_class_id INT NULL,
                distance DOUBLE PRECISION NOT NULL,
                used_kd_tree BOOLEAN NOT NULL,
                k INT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> EnsureClassAsync(string className)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("""
            INSERT INTO classes(name) VALUES (@name)
            ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
            RETURNING id;
            """, connection);
        command.Parameters.AddWithValue("name", className);
        object? value = await command.ExecuteScalarAsync();
        return Convert.ToInt32(value);
    }

    public async Task<string?> GetClassNameByIdAsync(int classId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("SELECT name FROM classes WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", classId);
        object? value = await command.ExecuteScalarAsync();
        return value?.ToString();
    }

    public async Task<int> AddImageWithFeaturesAsync(string filePath, int classId, double[] features)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var tx = await connection.BeginTransactionAsync();

        int imageId;
        await using (var imageCommand = new NpgsqlCommand("""
            INSERT INTO images(class_id, file_path) VALUES (@classId, @filePath)
            RETURNING id;
            """, connection, tx))
        {
            imageCommand.Parameters.AddWithValue("classId", classId);
            imageCommand.Parameters.AddWithValue("filePath", filePath);
            object? imageIdObj = await imageCommand.ExecuteScalarAsync();
            imageId = Convert.ToInt32(imageIdObj);
        }

        await using (var featureCommand = new NpgsqlCommand("""
            INSERT INTO features(image_id, vector) VALUES (@imageId, @vector);
            """, connection, tx))
        {
            featureCommand.Parameters.AddWithValue("imageId", imageId);
            featureCommand.Parameters.AddWithValue("vector", features);
            await featureCommand.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return imageId;
    }

    public async Task<IReadOnlyList<(double[] Vector, int ClassId)>> GetTrainingVectorsAsync()
    {
        var result = new List<(double[] Vector, int ClassId)>();
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("""
            SELECT f.vector, i.class_id
            FROM features f
            INNER JOIN images i ON i.id = f.image_id;
            """, connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var vector = reader.GetFieldValue<double[]>(0);
            int classId = reader.GetInt32(1);
            result.Add((vector, classId));
        }

        return result;
    }

    public async Task<int> CreateModelAsync(ModelInfo modelInfo)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("""
            INSERT INTO models(name, dimension, training_sample_count, default_k, created_at, description)
            VALUES (@name, @dimension, @trainingCount, @defaultK, @createdAt, @description)
            RETURNING id;
            """, connection);

        command.Parameters.AddWithValue("name", modelInfo.Name);
        command.Parameters.AddWithValue("dimension", modelInfo.Dimension);
        command.Parameters.AddWithValue("trainingCount", modelInfo.TrainingSampleCount);
        command.Parameters.AddWithValue("defaultK", modelInfo.DefaultK);
        command.Parameters.AddWithValue("createdAt", modelInfo.CreatedAt);
        command.Parameters.AddWithValue("description", (object?)modelInfo.Description ?? DBNull.Value);

        object? idObj = await command.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task SaveExperimentAsync(Experiment experiment)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("""
            INSERT INTO experiments(
                model_id, train_sample_count, test_sample_count, accuracy,
                kd_tree_build_time_ms, kd_tree_search_time_ms, linear_search_time_ms,
                performed_at, notes)
            VALUES (
                @modelId, @trainCount, @testCount, @accuracy,
                @buildMs, @searchMs, @linearMs,
                @performedAt, @notes);
            """, connection);

        command.Parameters.AddWithValue("modelId", experiment.ModelId);
        command.Parameters.AddWithValue("trainCount", experiment.TrainSampleCount);
        command.Parameters.AddWithValue("testCount", experiment.TestSampleCount);
        command.Parameters.AddWithValue("accuracy", experiment.Accuracy);
        command.Parameters.AddWithValue("buildMs", experiment.KdTreeBuildTimeMs);
        command.Parameters.AddWithValue("searchMs", experiment.KdTreeSearchTimeMs);
        command.Parameters.AddWithValue("linearMs", experiment.LinearSearchTimeMs);
        command.Parameters.AddWithValue("performedAt", experiment.PerformedAt);
        command.Parameters.AddWithValue("notes", (object?)experiment.Notes ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task SavePredictionAsync(Prediction prediction)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("""
            INSERT INTO predictions(
                image_id, model_id, predicted_class_id, actual_class_id,
                distance, used_kd_tree, k, created_at)
            VALUES (
                @imageId, @modelId, @predictedClassId, @actualClassId,
                @distance, @usedKdTree, @k, @createdAt);
            """, connection);

        command.Parameters.AddWithValue("imageId", prediction.ImageId);
        command.Parameters.AddWithValue("modelId", prediction.ModelId);
        command.Parameters.AddWithValue("predictedClassId", prediction.PredictedClassId);
        command.Parameters.AddWithValue("actualClassId", (object?)prediction.ActualClassId ?? DBNull.Value);
        command.Parameters.AddWithValue("distance", prediction.Distance);
        command.Parameters.AddWithValue("usedKdTree", prediction.UsedKdTree);
        command.Parameters.AddWithValue("k", prediction.K);
        command.Parameters.AddWithValue("createdAt", prediction.CreatedAt);

        await command.ExecuteNonQueryAsync();
    }
}
