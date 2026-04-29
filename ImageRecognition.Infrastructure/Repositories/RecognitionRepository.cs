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
                image_name TEXT NOT NULL,
                image_data BYTEA NOT NULL,
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

            ALTER TABLE classes ADD COLUMN IF NOT EXISTS description TEXT NULL;
            ALTER TABLE classes ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT NOW();

            ALTER TABLE images ADD COLUMN IF NOT EXISTS image_name TEXT;
            ALTER TABLE images ADD COLUMN IF NOT EXISTS image_data BYTEA;
            ALTER TABLE images ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT NOW();

            -- Заполняем image_name безопасно:
            -- если в старой схеме был file_path — переносим имя, иначе используем 'unknown'.
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_name = 'images'
                      AND column_name = 'file_path'
                ) THEN
                    EXECUTE
                        'UPDATE images
                         SET image_name = COALESCE(image_name, file_path, ''unknown'')
                         WHERE image_name IS NULL';
                ELSE
                    UPDATE images SET image_name = 'unknown' WHERE image_name IS NULL;
                END IF;
            END $$;

            UPDATE images SET image_data = COALESCE(image_data, decode('', 'hex')) WHERE image_data IS NULL;
            ALTER TABLE images ALTER COLUMN image_name SET NOT NULL;
            ALTER TABLE images ALTER COLUMN image_data SET NOT NULL;

            ALTER TABLE models ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT NOW();
            ALTER TABLE models ADD COLUMN IF NOT EXISTS description TEXT NULL;

            ALTER TABLE predictions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT NOW();
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

    public async Task<(int Classes, int Images, int Features, int Models, int Experiments, int Predictions)> GetStatisticsAsync()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("""
            SELECT
                (SELECT COUNT(*) FROM classes),
                (SELECT COUNT(*) FROM images),
                (SELECT COUNT(*) FROM features),
                (SELECT COUNT(*) FROM models),
                (SELECT COUNT(*) FROM experiments),
                (SELECT COUNT(*) FROM predictions);
            """, connection);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        return (
            Classes: reader.GetInt32(0),
            Images: reader.GetInt32(1),
            Features: reader.GetInt32(2),
            Models: reader.GetInt32(3),
            Experiments: reader.GetInt32(4),
            Predictions: reader.GetInt32(5)
        );
    }

    public async Task<string?> GetClassNameByIdAsync(int classId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("SELECT name FROM classes WHERE id = @id;", connection);
        command.Parameters.AddWithValue("id", classId);
        object? value = await command.ExecuteScalarAsync();
        return value?.ToString();
    }

    public async Task<int> AddImageWithFeaturesAsync(string imageName, byte[] imageData, int classId, double[] features)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        await using var tx = await connection.BeginTransactionAsync();

        int imageId;
        await using (var imageCommand = new NpgsqlCommand("""
            INSERT INTO images(class_id, image_name, image_data) VALUES (@classId, @imageName, @imageData)
            RETURNING id;
            """, connection, tx))
        {
            imageCommand.Parameters.AddWithValue("classId", classId);
            imageCommand.Parameters.AddWithValue("imageName", imageName);
            imageCommand.Parameters.AddWithValue("imageData", imageData);
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
