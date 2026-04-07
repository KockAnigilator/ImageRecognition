# Памятка по классам

## UI слой
- `MainWindow`  
  Точка входа UI. Управляет сценарием пользователя: подключение к БД, добавление данных, обучение, классификация, benchmark.

## Application слой
- `RecognitionService`  
  Оркестратор бизнес-сценариев. Не знает про UI и SQL-детали, работает через интерфейсы.
- `ImagePreprocessingService`  
  Извлекает признаки из изображения (`16x16`, grayscale, `double[256]`).
- `ApplicationFactory`  
  Собирает зависимости и возвращает готовый `IRecognitionService`.

## Domain слой (алгоритмы)
- `KDNode`  
  Узел KD-дерева: точка, метка класса, ось разделения, ссылки на потомков.
- `KDTree`  
  Построение и поиск ближайших соседей.
- `KNearestNeighbors`  
  Классификация на основе соседей (через KD-Tree или линейный поиск).
- `DistanceCalculator`  
  Евклидово расстояние между двумя векторами.

## Domain слой (сущности)
- `ImageClass` — класс изображения (`digit_5`, `shape_square` и т.д.).
- `ImageRecord` — запись об изображении (`image_name`, `image_data` в `BYTEA`).
- `ImageFeatures` — вектор признаков `FLOAT8[]`.
- `ModelInfo` — параметры обученной модели.
- `Experiment` — результаты benchmark.
- `Prediction` — история предсказаний.

## Infrastructure слой
- `PostgresOptions`  
  Параметры подключения к PostgreSQL.
- `PostgresConnectionFactory`  
  Создает открытое соединение.
- `RecognitionRepository`  
  SQL-реализация `IRecognitionRepository`: миграция схемы, CRUD, статистика.

## Интерфейсы (контракты)
- `IRecognitionService`  
  Контракт сценариев приложения для UI.
- `IImagePreprocessingService`  
  Контракт извлечения признаков.
- `IRecognitionRepository`  
  Контракт доступа к данным и хранилищу моделей/результатов.

## Что спрашивают на защите
- Почему `RecognitionService` не работает с SQL напрямую: соблюдение DIP/SRP.
- Почему `KDTree` отдельно от `kNN`: SRP и тестируемость.
- Почему храним `image_data` в `BYTEA`: требование предметной области и воспроизводимость.
- Почему UI не содержит бизнес-логики: легче поддерживать и тестировать.

